#include "MySLAM_Native.h"
#include "System.h"
#include "SimpleEstimator.h"
#include "IMUData.h"

//Gobal Varible
ORB_SLAM2::System* gSystem;
IMU::SimpleEstimator* gIMUEstimator;
ProgressChangedCallback gProgressChangedCallback = nullptr;

void RegisterProgressChangedCallback(ProgressChangedCallback callback)
{
	gProgressChangedCallback = callback;
}
void UnRegisterProgressChangedCallback()
{
	gProgressChangedCallback = nullptr;
}

void InitSystem()
{
	if (gSystem != nullptr)
	{
		delete gSystem;
	}
	gSystem = new ORB_SLAM2::System("/storage/emulated/0/MySLAM/ORBvoc.bin", "/storage/emulated/0/MySLAM/orb_slam2.yaml", ORB_SLAM2::System::MONOCULAR);
	// LocalizationMode is inefficient
	//gSystem->ActivateLocalizationMode();
	if (gIMUEstimator != nullptr)
	{
		delete gIMUEstimator;
	}
	gIMUEstimator = new IMU::SimpleEstimator(gSystem);
	LOGI("Create a System Successfully!!!");
}

int UpdateTracking(long long mataddress, long long timestamp)
{
	if (gSystem == nullptr) return false;
	cv::Mat * imgMat = (cv::Mat*)mataddress;
	// Track Monocular
	gIMUEstimator->TrackMonocular(*imgMat, timestamp);
	return gIMUEstimator->mTrackState;
}

void EstimatePose(float* data, int n, long long timestamp, float* pose)
{
	// Input imu data.
	for (int i = 0; i < n; i++)
	{
		gIMUEstimator->mIMUDataQ.push(IMU::IMUData::Decode(data + i * 16));
	}
	cv::Mat poseMat = gIMUEstimator->Estimate(timestamp);
	if (poseMat.rows == 4 && poseMat.cols == 4)
	{
		std::copy(poseMat.begin<float>(), poseMat.end<float>(), pose);
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
