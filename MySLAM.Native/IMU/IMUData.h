#ifndef IMUDATA_H
#define IMUDATA_H

#include <vector>
#include <opencv2/core/core.hpp>

namespace IMU
{
class IMUData
{
public:
	IMUData(cv::Mat R, long long timestamp, cv::Vec3f acc);
	~IMUData();
	static IMUData* Decode(float* raw);
public:
	cv::Mat mR;
	long long mTimestamp;
	cv::Vec3f mAcceleration;
};
}
#endif // IMUDATA_H


