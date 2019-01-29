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
	Lost = 3
};
class SimpleEstimator
{
public:
	SimpleEstimator(ORB_SLAM2::System *system);
	~SimpleEstimator();
	cv::Mat Estimate(long long timestamp);
	void TrackMonocular(cv::Mat& im, long long timestamp);
public:
	eTrackingState mTrackState;
	std::vector<SimpleIMUFrame*> mIMUFrameV;
	std::queue<IMUData*> mIMUDataQ;
	float mScale;
private:
	void ComputeScaleAndV(long long timestamp);
	std::vector<SimpleIMUFrame*>::iterator FindFrame(long long timestamp);
private:
	ORB_SLAM2::System *mSystemPtr;
	cv::Mat mTrackPose;
	cv::Vec3f mEstimatedV0;
	long long mTrackT;
	cv::Vec3f dx1, dx2;
	long long dt1, dt2;
	cv::Mat mTranformR;
	std::mutex sTrackMutex;
};
}

#endif // ESTIMATOR_H


