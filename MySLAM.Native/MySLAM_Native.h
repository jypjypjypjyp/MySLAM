#include <jni.h>
#include <unistd.h>
#include <android/log.h>
#include <errno.h>

#include <string.h>
#include <sys/resource.h>

#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "MySLAM_Native", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "MySLAM_Native", __VA_ARGS__))

#include "System.h"
//Gobal Varible
ORB_SLAM2::System *gSystem;

extern "C" jboolean MySLAM_Native_AR_InitSystem(JNIEnv *env)
{
	if (gSystem != nullptr)
	{
		delete gSystem;
	}
	gSystem = new ORB_SLAM2::System("/storage/emulated/0/MySLAM/ORBvoc.bin", "/storage/emulated/0/MySLAM/orb_slam2.yaml", ORB_SLAM2::System::MONOCULAR);
}

extern "C" jfloatArray MySLAM_Native_AR_GetPose(JNIEnv *env, jlong mataddress, jlong timestamp)
{
	if (gSystem == nullptr) return;
	cv::Mat *pMat = (cv::Mat*)mataddress;

	clock_t start, end;
	start = clock();
	cv::Mat pose = gSystem->TrackMonocular(*pMat, static_cast<double>(timestamp));
	end = clock();
	LOGI("Get Pose Use Time=%f\n", ((double)end - start) / CLOCKS_PER_SEC);

	switch (gSystem->GetTrackingState())
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
	}

	jfloatArray result_arr = env->NewFloatArray(pose.rows * pose.cols);
	jfloat *result_ptr;

	result_ptr = env->GetFloatArrayElements(result_arr, nullptr);
	for (int i = 0; i < pose.rows; i++)
		for (int j = 0; j < pose.cols; j++)
		{
			float tempdata = pose.at<float>(i, j);
			result_ptr[i * pose.rows + j] = tempdata;
		}
	env->ReleaseFloatArrayElements(result_arr, result_ptr, 0);
	return result_arr;
}

extern "C" jboolean MySLAM_Native_AR_ReleaseMap(JNIEnv *env)
{
	if (gSystem != nullptr)
	{
		gSystem->Shutdown();
		delete gSystem;
	}
}

extern "C"  jint  MySLAM_Native_AR_Test(JNIEnv *env, jint src)
{
	src++;
	return src;
}

