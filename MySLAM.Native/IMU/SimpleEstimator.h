#ifndef ESTIMATOR_H
#define ESTIMATOR_H

#include <opencv2/core/core.hpp>
#include <vector>
#include <queue>
#include <utility>
#include "System.h"
#include "SimpleIMUFrame.h"
#include "IMUData.h"

namespace IMU
{
enum eTrackingState
{
	NotReady = -1,
	NoImagesYet = 0,
	NotInitialized = 1,
	On = 2,
	Steady = 4,
	VisualOK = 5,
	IMUOK = 6,
	Lost = 3
};
class SimpleEstimator
{
public:
	SimpleEstimator(ORB_SLAM2::System* system, const std::string& strSettingsFile);
	~SimpleEstimator();
	cv::Mat Estimate(long long timestamp);
	void TrackMonocular(cv::Mat& im, long long timestamp);
public:
	eTrackingState mTrackState;
	std::vector<SimpleIMUFrame*> mIMUFrameV;
	std::queue<IMUData*> mIMUDataQ;
private:
	void TryUpdateParams(long long timestamp);
	void UpdateEstimateV0(long long start, long long end);
	std::vector<SimpleIMUFrame*>::iterator FindFrame(long long timestamp);
	static cv::Mat TcwToTwc(cv::Mat& Tcw);
	static cv::Mat TwcToTcw(cv::Mat& Twc);
private:
	ORB_SLAM2::System* mSystemPtr;
	cv::Mat mIMUCovar;
	cv::Vec3f mIMUBias;
	cv::Mat X, K, P;
	cv::Mat mTrackPose;
	float mScale;
	cv::Vec3f mEstimatedV0;
	long long mTrackT;
	cv::Vec3f dx1, dx2;
	long long dts1, dts2;
	cv::Mat mTranformR, mTranformRInv;
	std::mutex mTrackMutex;
	static cv::Mat r1, r21;
};
}

#endif // ESTIMATOR_H