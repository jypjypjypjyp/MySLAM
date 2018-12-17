#include "MySLAM_Native.h"
#include "System.h"
//Gobal Varible
ORB_SLAM2::System *gSystem;

void MySLAM_Native_AR_InitSystem()
{
	if (gSystem != nullptr)
	{
		delete gSystem;
	}
	gSystem = new ORB_SLAM2::System("/storage/emulated/0/MySLAM/ORBvoc.txt", "/storage/emulated/0/MySLAM/orb_slam2.yaml", ORB_SLAM2::System::MONOCULAR);
	LOGI("Create a System Successfully!!!");
}

bool MySLAM_Native_AR_GetPose(long long mataddress, long long timestamp, float* out)
{
	if (gSystem == nullptr) return false;
	cv::Mat *pMat = (cv::Mat*)mataddress;

	clock_t start, end;
	start = clock();
	cv::Mat posemat = gSystem->TrackMonocular(*pMat, static_cast<double>(timestamp));
	end = clock();
	LOGI("Get Pose Use Time=%f\n", ((double)end - start) / CLOCKS_PER_SEC);
	
	/*switch (gSystem->GetTrackingState())
	{
	case -1:
		cv::putText(*pMat, "SYSTEM NOT READY", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	case 0:
		cv::putText(*pMat, "NO IMAGES YET", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	case 1:
		cv::putText(*pMat, "SLAM NOT INITIALIZED", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	case 2:
		cv::putText(*pMat, "SLAM ON", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(0, 255, 0), 2);
		break;
	case 3:
		cv::putText(*pMat, "SLAM LOST", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	default:
		break;
	}*/

	if (posemat.rows > 0)
	{
		for (int i = 0; i < posemat.rows; i++)
		{
			for (int j = 0; j < posemat.cols; j++)
			{
				out[i*posemat.cols + j] = posemat.at<float>(i, j);
			}
		}
		return true;
	}
	return false;
}

void MySLAM_Native_AR_ReleaseMap()
{
	if (gSystem != nullptr)
	{
		gSystem->Shutdown();
		delete gSystem;
	}
}
