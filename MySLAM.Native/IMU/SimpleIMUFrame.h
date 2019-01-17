#ifndef SIMPLEIMUFRAME_H
#define SIMPLEIMUFRAME_H
#include <opencv2/core/core.hpp>

namespace IMU
{
// using the simplest IMU estimate method
class SimpleIMUFrame
{
public:
	SimpleIMUFrame(long long timestamp);
	SimpleIMUFrame(cv::Mat R, long long timestamp, cv::Vec3f displacement, cv::Vec3f dv);
	~SimpleIMUFrame();
	static bool Compare(const SimpleIMUFrame *lhs, const SimpleIMUFrame *rhs);
public:
	// Frame Rotation
	cv::Mat mR;

	// Frame timestamp
	long long mTimestamp;

	// Frame displacement
	cv::Vec3f mDisplacement;

	// Frame delta Velocity
	cv::Vec3f mDVelocity;
};
}
#endif // SIMPLEIMUFRAME_H

