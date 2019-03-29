#include "SimpleEstimator.h"
#include <algorithm>

//NeedToBeRemoved
#include <sstream>


namespace IMU
{
SimpleEstimator::SimpleEstimator(ORB_SLAM2::System* system, const std::string& strSettingsFile)
	:mSystemPtr(system),
	mTrackState(NotReady),
	mTrackPose(cv::Mat::zeros(4, 4, CV_32F)),
	mTranformR(cv::Mat::zeros(4, 4, CV_32F)),
	mTrackT(0),
	mScale(0)
{
	cv::FileStorage fSettings(strSettingsFile.c_str(), cv::FileStorage::READ);
	if (!fSettings.isOpened())
	{
		exit(-1);
	}
	cv::Mat temp;
	fSettings["IMUcovar"] >> temp;
	temp.convertTo(mIMUCovar, CV_32F);
	fSettings["IMUmean"] >> temp;
	temp = temp.t();
	temp.convertTo(mIMUMean, CV_32F);
	fSettings.release();
}

SimpleEstimator::~SimpleEstimator()
{
	for (auto i : mIMUFrameV)
	{
		delete i;
	}
	mIMUFrameV.clear();
	while (mIMUDataQ.size() > 0)
	{
		delete mIMUDataQ.front();
		mIMUDataQ.pop();
	}
}

void SimpleEstimator::TrackMonocular(cv::Mat & im, long long timestamp)
{
	// Save pervious state
	long long pervT = mTrackT;
	auto prevPose = mTrackPose;
	auto prevState = mTrackState;
	// Track
	auto tempMat = mSystemPtr->TrackMonocular(im, static_cast<double>(timestamp));
	{
		std::unique_lock<std::mutex> lock(mTrackMutex);
		mTrackState = static_cast<eTrackingState>(mSystemPtr->GetTrackingState());
		if (mTrackState == On)
		{
			mTrackPose = tempMat;
			mTrackT = timestamp;
			// Update EstimateV0: make sure estimatev0 is always the last track frame's velocity.
			UpdateEstimateV0(pervT, mTrackT);
			if (prevState == NotInitialized)
			{
				K = P = X = cv::Mat();
				auto initF = FindFrame(mSystemPtr->GetTraker()->mInitialFrame.mTimeStamp);
				(*initF)->mR.copyTo(mTranformR(cv::Rect(0, 0, 3, 3)));
			}
			mTrackPose = mTranformR * mTrackPose;
			if (prevState == On)
			{
				mTrackState = Steady;
				// Update Δx2, Δt2
				cv::Mat temp = mTrackPose - prevPose;
				cv::Vec3f dx = cv::Vec3f(temp.col(3).rowRange(0, 3));
				dx2 = dx;
				dts2 = mTrackT - pervT;

			}
			else if (prevState >= Steady)
			{
				mTrackState = prevState;
				// Update Δx1, Δx2, Δt1, Δt2
				cv::Mat temp = mTrackPose - prevPose;
				cv::Vec3f dx = cv::Vec3f(temp.col(3).rowRange(0, 3));
				dx1 = dx2;
				dts1 = dts2;
				dx2 = dx;
				dts2 = mTrackT - pervT;
				TryUpdateParams(timestamp);
			}
		}
	}
}

void SimpleEstimator::TryUpdateParams(long long timestamp)
{
	long long ts1 = timestamp - dts2;
	long long ts0 = ts1 - dts1;
	cv::Vec3f dv1, dv2;
	cv::Vec3f d1, d2;
	long long prevT = ts0;
	int needToRemove = -1;
	SimpleIMUFrame* cFrame;
	for (int i = 0; i < mIMUFrameV.size(); i++)
	{
		cFrame = mIMUFrameV[i];
		if (timestamp > cFrame->mTimestamp && cFrame->mTimestamp > ts1)
		{
			d2 += cFrame->mDisplacement + dv2 * (float)(cFrame->mTimestamp - prevT) * 1e-9;
			dv2 += cFrame->mDVelocity;
		}
		else if (cFrame->mTimestamp > ts0)
		{
			d1 += cFrame->mDisplacement + dv1 * (float)(cFrame->mTimestamp - prevT) * 1e-9;
			dv1 += cFrame->mDVelocity;
		}
		else
		{
			needToRemove = i;
			delete cFrame;
		}
		prevT = cFrame->mTimestamp;
	}
	mIMUFrameV.erase(mIMUFrameV.begin(), mIMUFrameV.begin() + needToRemove + 1);
	double dt1 = dts1 * 1e-9, dt2 = dts2 * 1e-9;
	// Check if d1, d2 are suitable.
	if (cv::norm(d1, 2) < 20 * 0.5 * cv::norm(mIMUMean, 2) * dt1 * dt1
		|| cv::norm(d2, 2) < 20 * 0.5 * cv::norm(mIMUMean, 2) * dt2 * dt2)
		return;
	cv::Vec3f A, C;
	double B;
	A = dx1 * dt2 - dx2 * dt1;
	B = 0.5 * dt1 * dt2 * (dt1 - dt2);
	C = d1 * dt2 - d2 * dt1 - dv1 * dt1 * dt2;
	cv::Mat H = cv::Mat::zeros(3, 4, CV_32F)
		, Z = cv::Mat::zeros(3, 1, CV_32F)
		, R = B * B * mIMUCovar;
	H.at<float>(0, 0) = B;
	H.at<float>(1, 1) = B;
	H.at<float>(2, 2) = B;
	H.at<float>(0, 3) = A[0];
	H.at<float>(1, 3) = A[1];
	H.at<float>(2, 3) = A[2];
	Z.at<float>(0, 0) = C[0];
	Z.at<float>(1, 0) = C[1];
	Z.at<float>(2, 0) = C[2];
	if (X.empty())
	{
		auto s2 = C - B * mIMUMean;
		std::vector<float> temp;
		cv::solve(A, s2, temp, CV_SVD);
		mScale = temp[0];
		X = cv::Mat::zeros(4, 1, CV_32F);
		X.at<float>(0, 0) = mIMUMean[0];
		X.at<float>(1, 0) = mIMUMean[1];
		X.at<float>(2, 0) = mIMUMean[2];
		X.at<float>(3, 0) = mScale;
		// K store H temporarily
		K = H;
	}
	else if (P.empty())
	{
		// K store H2 temporarily
		K.push_back(H);
		cv::Mat R2(6, 6, CV_32F);
		R.copyTo(R2(cv::Rect(0, 0, 3, 3)));
		R.copyTo(R2(cv::Rect(3, 3, 3, 3)));
		P = (K.t() * R2.inv() * K).inv();
		K = P * H.t() * R.inv();
		X = X + K * (Z - H * X);
	}
	else
	{
		K = P * H.t() * (H * P * H.t() + R).inv();
		X = X + K * (Z - H * X);
		P = (cv::Mat::eye(4, 4, CV_32F) - K * H) * P;
	}
	mScale = X.at<float>(0, 3);
	mIMUMean = cv::Vec3f(X.col(0).rowRange(0, 3));
	cv::Vec3f v0 = (mScale * dx1 - d1 + 0.5 * mIMUMean * dt1 * dt1) / dt1;
	mEstimatedV0 = v0 + dv1 + dv2;
	mTrackState = OK;
}

void SimpleEstimator::UpdateEstimateV0(long long start, long long end)
{
	for (auto iter = FindFrame(start) + 1; (*iter)->mTimestamp <= end; iter++)
		mEstimatedV0 += (*iter)->mDVelocity;
}

std::vector<SimpleIMUFrame*>::iterator SimpleEstimator::FindFrame(long long timestamp)
{
	SimpleIMUFrame temp(timestamp);
	return std::lower_bound(std::begin(mIMUFrameV), std::end(mIMUFrameV), &temp, SimpleIMUFrame::Compare);
}

cv::Mat SimpleEstimator::Estimate(long long timestamp)
{
	// Make Sure mIMUDataQ.size() greater than 1
	if (mIMUDataQ.size() < 2)
		return cv::Mat();
	// Integrate
	cv::Vec3f dv;
	long long curTimestamp = mIMUDataQ.front()->mTimestamp;
	long long nextTimestamp;
	cv::Vec3f curAcc = mIMUDataQ.front()->mAcceleration;
	cv::Vec3f displacement;
	delete mIMUDataQ.front();
	mIMUDataQ.pop();
	double dt;
	while ((nextTimestamp = mIMUDataQ.front()->mTimestamp) < timestamp)
	{
		dt = (int)(nextTimestamp - curTimestamp) * 1e-9;
		dv += ((curAcc + mIMUDataQ.front()->mAcceleration) / 2) * dt;
		displacement += dv * dt;
		curAcc = mIMUDataQ.front()->mAcceleration;
		curTimestamp = nextTimestamp;
		delete mIMUDataQ.front();
		mIMUDataQ.pop();
	}
	{
		std::unique_lock<std::mutex> lock(mTrackMutex);
		mIMUFrameV.push_back(new SimpleIMUFrame(mIMUDataQ.front()->mR, timestamp, displacement, dv));
		if (mTrackState == OK && !mTrackPose.empty())
		{
			cv::Mat estimatedPose = cv::Mat::eye(4, 4, CV_32F);
			mIMUDataQ.back()->mR.copyTo(estimatedPose(cv::Rect(0, 0, 3, 3)));
			// Estimate
			cv::Vec3f velocity = mEstimatedV0;
			cv::Vec3f position(mTrackPose(cv::Rect(3, 0, 1, 3)));
			auto iter = FindFrame(mTrackT);
			long long pervT = mTrackT;
			while (++iter != mIMUFrameV.end())
			{
				position += velocity * (((*iter)->mTimestamp - pervT) * 1e-9) + (*iter)->mDisplacement;
				velocity += (*iter)->mDVelocity;
				pervT = (*iter)->mTimestamp;
			}
			// Scale
			position *= mScale;
			cv::Mat(position).copyTo(estimatedPose(cv::Rect(3, 0, 1, 3)));
			return estimatedPose;
		}
		else
		{
			return cv::Mat();
		}
	}
}
}

