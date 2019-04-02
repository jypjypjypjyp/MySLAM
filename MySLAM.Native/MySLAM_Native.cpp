#include "MySLAM_Native.h"
#include "System.h"
#include "SimpleEstimator.h"
#include "IMUData.h"

//Gobal Varible
ORB_SLAM2::System* gSystem;
IMU::SimpleEstimator* gIMUEstimator;
ProgressChangedCallback gProgressChangedCallback = nullptr;
cv::Mat gCVToGl;
float gScale;

void RegisterProgressChangedCallback(ProgressChangedCallback callback)
{
	gProgressChangedCallback = callback;
}
void UnRegisterProgressChangedCallback()
{
	gProgressChangedCallback = nullptr;
}

void InitSystem1(const char* rootPath)
{
	if (gSystem != nullptr)
	{
		delete gSystem;
	}
	gSystem = new ORB_SLAM2::System("/storage/emulated/0/MySLAM/ORBvoc.bin", "/storage/emulated/0/MySLAM/orb_slam2.yaml", ORB_SLAM2::System::MONOCULAR);
	LOGI("Create a System Successfully!!!");
	// init
	gCVToGl = cv::Mat::zeros(4, 4, CV_32F);
	gCVToGl.at<float>(0, 0) = 1.0f;
	gCVToGl.at<float>(1, 1) = -1.0f; // Invert the y axis
	gCVToGl.at<float>(2, 2) = -1.0f; // invert the z axis
	gCVToGl.at<float>(3, 3) = 1.0f;
	gScale = 5;
}

void InitSystem2(const char* rootPath)
{
	if (gSystem != nullptr)
	{
		delete gSystem;
	}
	string rootPathStr(rootPath);
	gSystem = new ORB_SLAM2::System(rootPathStr + "ORBvoc.bin", rootPathStr + "orb_slam2.yaml", ORB_SLAM2::System::MONOCULAR);
	// LocalizationMode is inefficient
	//gSystem->ActivateLocalizationMode();
	if (gIMUEstimator != nullptr)
	{
		delete gIMUEstimator;
	}
	gIMUEstimator = new IMU::SimpleEstimator(gSystem, rootPathStr + "imu.yaml");
	LOGI("Create a System Successfully!!!");
}

int GetPose(long long mataddress, long long timestamp, float* out)
{
	if (gSystem == nullptr) return false;
	cv::Mat * pMat = (cv::Mat*)mataddress;
	cv::Mat posemat = gSystem->TrackMonocular(*pMat, static_cast<double>(timestamp));

	if (posemat.rows > 0)
	{
		posemat = gCVToGl * posemat;
		cv::transpose(posemat.clone(), posemat);
		for (int j = 0; j < 4; j++)
		{
			for (int i = 0; i < 3; i++)
			{
				out[i * 4 + j] = posemat.at<float>(i, j);
			}
			out[12 + j] = gScale * posemat.at<float>(3, j);
		}
		out[15] = 1;
	}
	return gSystem->GetTrackingState();
}

int UpdateTracking(long long mataddress, long long timestamp)
{
	if (gSystem == nullptr) return false;
	cv::Mat * imgMat = (cv::Mat*)mataddress;
	// Track Monocular
	gIMUEstimator->TrackMonocular(*imgMat, timestamp);
	return gIMUEstimator->mTrackState;
	return 0;
}

void EstimatePose(float* data, int n, long long timestamp, float* out)
{
	// Input imu data.
	for (int i = 0; i < n; i++)
	{
		gIMUEstimator->mIMUDataQ.push(IMU::IMUData::Decode(data + i * 16));
	}
	cv::Mat poseMat = gIMUEstimator->Estimate(timestamp);
	if (!poseMat.empty())
	{
		poseMat = gCVToGl * poseMat;
		cv::transpose(poseMat.clone(), poseMat);
		for (int j = 0; j < 4; j++)
		{
			for (int i = 0; i < 3; i++)
			{
				out[i * 4 + j] = poseMat.at<float>(i, j);
			}
			out[12 + j] = gScale * poseMat.at<float>(3, j);
		}
		out[15] = 1;
	}
}

void ReleaseMap()
{
	if (gSystem != nullptr)
	{
		gSystem->Shutdown();
		delete gSystem;
		gSystem = nullptr;
	}
	if (gIMUEstimator != nullptr)
	{
		delete gIMUEstimator;
		gIMUEstimator = nullptr;
	}
}

bool WriteIMUSettings(const char* file, long long mataddress1, long long mataddress2)
{
	cv::Mat* m1 = (cv::Mat*)mataddress1;
	cv::Mat* m2 = (cv::Mat*)mataddress2;
	cv::FileStorage fs(file, cv::FileStorage::WRITE);
	if (!fs.isOpened())
	{
		return false;
	}
	fs << "IMUcovar" << *m1;
	fs << "IMUmean" << *m2;
	fs.release();
	return true;
}

