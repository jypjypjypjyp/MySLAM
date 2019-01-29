#include "IMUData.h"

namespace IMU
{
IMUData::IMUData(cv::Mat R, long long timestamp, cv::Vec3f acc)
	:mR(R), mTimestamp(timestamp), mAcceleration(acc)
{
}

IMUData::~IMUData()
{
}

IMUData* IMUData::Decode(float * raw)
{
	cv::Mat R(3,3,CV_32F);
	for (int r = 0; r < 3; r++)
	{
		for (int c = 0; c < 3; c++)
		{
			R.at<float>(r, c) = raw[r * 4 + c];
		}
	}

	cv::Vec3f acc;
	acc[0] = raw[3];
	acc[1] = raw[7];
	acc[2] = raw[11];

	long long timestamp;
	long long r1 = *(reinterpret_cast<unsigned int*>(raw+12));
	long long r2 = *(reinterpret_cast<int*>(raw+13));
	timestamp = (r2 << 32) + r1;

	return new IMUData(R,timestamp,acc);
}
}

