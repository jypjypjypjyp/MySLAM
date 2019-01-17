#include "SimpleIMUFrame.h"

namespace IMU
{

SimpleIMUFrame::SimpleIMUFrame(long long timestamp)
	:mTimestamp(timestamp)
{
}

SimpleIMUFrame::SimpleIMUFrame(cv::Mat R, long long timestamp, cv::Vec3f displacement, cv::Vec3f dv)
	: mR(R), mTimestamp(timestamp), mDisplacement(displacement), mDVelocity(dv)
{
}

SimpleIMUFrame::~SimpleIMUFrame()
{
}

bool SimpleIMUFrame::Compare(const SimpleIMUFrame *lhs, const SimpleIMUFrame *rhs)
{
	return lhs->mTimestamp < rhs->mTimestamp;
}

}

