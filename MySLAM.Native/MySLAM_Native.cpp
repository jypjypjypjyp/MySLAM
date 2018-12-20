#include "MySLAM_Native.h"
#include "System.h"
//Gobal Varible
ORB_SLAM2::System *gSystem;
ProgressChangedCallback gProgressChangedCallback = nullptr;
cv::Mat gCVToGl;
float gScale;

void MySLAM_Native_RegisterProgressChangedCallback(ProgressChangedCallback callback)
{
	gProgressChangedCallback = callback;
}

void MySLAM_Native_UnRegisterProgressChangedCallback()
{
	gProgressChangedCallback = nullptr;
}

void MySLAM_Native_InitSystem()
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

int MySLAM_Native_GetPose(long long mataddress, long long timestamp, float* out)
{
	if (gSystem == nullptr) return false;
	cv::Mat *pMat = (cv::Mat*)mataddress;
	cv::Mat posemat = gSystem->TrackMonocular(*pMat, static_cast<double>(timestamp));

	if (posemat.rows > 0)
	{
		posemat = gCVToGl * posemat;
		cv::transpose(posemat.clone(), posemat);
		for (int j = 0; j < 4; j++)
		{
			for (int i = 0; i < 3; i++)
			{
				out[i*4 + j] = posemat.at<float>(i, j);
			}
			out[12 + j] = gScale * posemat.at<float>(3, j);
		}
		out[15] = 1;
	}
	return gSystem->GetTrackingState();
}

void MySLAM_Native_ReleaseMap()
{
	if (gSystem != nullptr)
	{
		gSystem->Shutdown();
		delete gSystem;
	}
}
