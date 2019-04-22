#include "SimpleEstimator.h"
#include <algorithm>

//NeedToBeRemoved
#include <sstream>
#include "MySLAM_Native.h"


namespace IMU
{
SimpleEstimator::SimpleEstimator(ORB_SLAM2::System* system,PoseSlideWindowFilter* psfw, const std::string& strSettingsFile)
	:mSystemPtr(system),
	mPoseSlideWindowFilterPtr(psfw),
	mTrackState(NotReady),
	mTrackPose(cv::Mat::eye(4, 4, CV_32F)),
	mTranformR(cv::Mat::eye(4, 4, CV_32F)),
	mTrackT(0),
	mScale(5)
{
	cv::FileStorage fSettings(strSettingsFile.c_str(), cv::FileStorage::READ);
	if (!fSettings.isOpened())
	{
		exit(-1);
	}
	// We dont need IMU calibration
	/*cv::Mat temp;
	fSettings["IMUcovar"] >> temp;
	temp.convertTo(mIMUCovar, CV_32F);
	fSettings["IMUmean"] >> temp;
	temp = temp.t();
	temp.convertTo(mIMUBias, CV_32F);*/
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
	auto Tcw = mSystemPtr->TrackMonocular(im, static_cast<double>(timestamp));
	{
		std::unique_lock<std::mutex> lock(mTrackMutex);
		mTrackState = static_cast<eTrackingState>(mSystemPtr->GetTrackingState());
		if (mTrackState == On)
		{
			mTrackPose = TcwToTwc(Tcw);
			mTrackT = timestamp;
			// Update EstimateV0: make sure estimatev0 is always the last track frame's velocity.
			UpdateEstimateV0(pervT, mTrackT);
			if (prevState == NotInitialized)
			{
				H = cv::Mat(); Z = cv::Mat(); X = cv::Mat();
				auto initF = FindFrame(mSystemPtr->GetTraker()->mInitialFrame.mTimeStamp);
				(*initF)->mR.copyTo(mTranformR(cv::Rect(0, 0, 3, 3)));
				mTranformRInv = mTranformR.inv();
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
				if (mTrackState < VisualOK)
					mTrackState = VisualOK;
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
	//// Check if d1, d2 are suitable.
	//if (cv::norm(d1, cv::NORM_L2) < 20 * 0.5 * cv::norm(mIMUBias, cv::NORM_L2) * dt1 * dt1
	//	|| cv::norm(d2, cv::NORM_L2) < 20 * 0.5 * cv::norm(mIMUBias, cv::NORM_L2) * dt2 * dt2)
	//	return;
	cv::Vec3f A, C;
	double B;
	A = dx1 * dt2 - dx2 * dt1;
	B = -0.5 * dt1 * dt2 * (dt1 + dt2);
	C = d1 * dt2 - d2 * dt1 - dv1 * dt1 * dt2;
	if (mTrackState != IMUOK)
	{
		cv::Mat h = cv::Mat::zeros(3, 1, CV_32F);
		cv::Mat z = cv::Mat::zeros(3, 1, CV_32F);
		h.at<float>(0, 0) = A[0];
		h.at<float>(1, 0) = A[1];
		h.at<float>(2, 0) = A[2];
		z.at<float>(0, 0) = C[0];
		z.at<float>(1, 0) = C[1];
		z.at<float>(2, 0) = C[2];
		cv::Mat x;
		cv::solve(h, z, x, CV_SVD);
		float s = x.at<float>(0, 0);
		// Filter out abnormal value
		if (s<0 || s>0.1)
			return;
		H.push_back(h);
		Z.push_back(z);
		if (H.rows / 3 >= 100)
		{
			cv::solve(H, Z, X, CV_SVD);
			mScale = X.at<float>(0, 0);
			mTrackState = IMUOK;
		}
	}
	else
	{
		//mIMUBias = (C - mScale * A) / B;
		cv::Vec3f v0 = (mScale * dx1 - d1) / dt1; //0.5 * mIMUBias * dt1 * dt1) / dt1;
		mEstimatedV0 = v0 + dv1 + dv2;
	}
}

void SimpleEstimator::UpdateEstimateV0(long long start, long long end)
{
	for (auto iter = FindFrame(start) + 1;
		iter != mIMUFrameV.end() && (*iter)->mTimestamp <= end;
		iter++)
		mEstimatedV0 += (*iter)->mDVelocity;
}

std::vector<SimpleIMUFrame*>::iterator SimpleEstimator::FindFrame(long long timestamp)
{
	SimpleIMUFrame temp(timestamp);
	return std::lower_bound(std::begin(mIMUFrameV), std::end(mIMUFrameV), &temp, SimpleIMUFrame::Compare);
}
// Twc imu -> world system. Need to be rotate, because of landscape
cv::Mat SimpleEstimator::r1 = []
{
	cv::Mat r1 = cv::Mat::zeros(3, 3, CV_32F);
	r1.at<float>(0, 0) = 1;
	r1.at<float>(1, 1) = -1;
	r1.at<float>(2, 2) = -1;
	return r1;
}();
cv::Mat SimpleEstimator::r2 = []
{
	cv::Mat r21 = cv::Mat::zeros(3, 3, CV_32F);
	r21.at<float>(0, 1) = -1;
	r21.at<float>(1, 0) = -1;
	r21.at<float>(2, 2) = -1;
	return r21;
}();
cv::Mat SimpleEstimator::r3 = []
{
	cv::Mat r3 = cv::Mat::zeros(3, 3, CV_32F);
	r3.at<float>(0, 0) = 1;
	r3.at<float>(1, 1) = -1;
	r3.at<float>(2, 2) = 1;
	return r3;
}();
cv::Mat SimpleEstimator::r4 = []
{
	cv::Mat r4 = cv::Mat::zeros(3, 3, CV_32F);
	r4.at<float>(0, 1) = -1;
	r4.at<float>(1, 0) = 1;
	r4.at<float>(2, 2) = 1;
	return r4;
}();
cv::Mat SimpleEstimator::TcwToTwc(cv::Mat & Tcw)
{
	cv::Mat rcw = Tcw.rowRange(0, 3).colRange(0, 3);
	cv::Mat tcw = Tcw.rowRange(0, 3).col(3);
	//rwc = r21.inv()*(r1.inv() * rcw).t();=> r.21.inv() = r21, r1.inv() = r1;
	cv::Mat rwc = r1 * (r2 * rcw).t();
	cv::Mat twc = r4 * (r3 * rcw).t() * tcw;
	cv::Mat Twc = cv::Mat::eye(4, 4, Tcw.type());
	rwc.copyTo(Twc.rowRange(0, 3).colRange(0, 3));
	twc.copyTo(Twc.rowRange(0, 3).col(3));
	return Twc;
}
cv::Mat SimpleEstimator::TwcToTcw(cv::Mat & Twc)
{
	cv::Mat rwc = Twc.rowRange(0, 3).colRange(0, 3);
	cv::Mat twc = Twc.rowRange(0, 3).col(3);
	cv::Mat rcw = (rwc * r2).t() * r1;
	cv::Mat tcw = (r3 * rcw) * twc;
	cv::Mat Tcw = cv::Mat::eye(4, 4, Twc.type());
	rcw.copyTo(Tcw.rowRange(0, 3).colRange(0, 3));
	tcw.copyTo(Tcw.rowRange(0, 3).col(3));
	return Tcw;
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
		if (mTrackState == VisualOK)
		{
			cv::Mat estimatedPose = cv::Mat::eye(4, 4, CV_32F);
			mIMUDataQ.back()->mR.copyTo(estimatedPose(cv::Rect(0, 0, 3, 3)));
			//((cv::Mat)(0.1*mTrackPose(cv::Rect(3, 0, 1, 3)))).copyTo(estimatedPose(cv::Rect(3, 0, 1, 3)));
			estimatedPose = mTranformRInv * estimatedPose;
			return TwcToTcw(estimatedPose);
		}
		else if (mTrackState == IMUOK)
		{
			cv::Mat estimatedPose = cv::Mat::eye(4, 4, CV_32F);
			mIMUDataQ.back()->mR.copyTo(estimatedPose(cv::Rect(0, 0, 3, 3)));
			// Estimate
			cv::Vec3f velocity = mEstimatedV0;
			cv::Vec3f position(mTrackPose(cv::Rect(3, 0, 1, 3)));
			auto iter = FindFrame(mTrackT);
			long long pervT = mTrackT;
			// Scale, now position unit is m 
			position *= mScale;
			cv::Vec3f displacement;
			while (++iter != mIMUFrameV.end())
			{
				displacement += velocity * (((*iter)->mTimestamp - pervT) * 1e-9) + (*iter)->mDisplacement;
				velocity += (*iter)->mDVelocity;
				pervT = (*iter)->mTimestamp;
			}
			float k = cv::norm(dx2, cv::NORM_L2)*(timestamp - mTrackT) / (float)dts2;
			if (cv::norm(displacement, cv::NORM_L2) > k * mScale)
			{
				displacement /= cv::norm(displacement, cv::NORM_L2);
				displacement *= k * mScale;
			}
			position += displacement;
			position = mPoseSlideWindowFilterPtr->Filter(position);
			cv::Mat(position).copyTo(estimatedPose(cv::Rect(3, 0, 1, 3)));
			estimatedPose = mTranformRInv * estimatedPose;
			estimatedPose.at<float>(0, 3) *= 5 / mScale;
			estimatedPose.at<float>(1, 3) *= 5 / mScale;
			estimatedPose.at<float>(2, 3) *= 5 / mScale;
			return TwcToTcw(estimatedPose);
		}
		else
		{
			return cv::Mat();
		}
	}
}
}