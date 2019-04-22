#ifndef IMUSLIDEWINDOWFILTER_H
#define IMUSLIDEWINDOWFILTER_H

#include "IMUData.h"
#include <queue>

namespace IMU
{
class IMUSlideWindowFilter
{
public:
	IMUSlideWindowFilter(int windowSize);
	~IMUSlideWindowFilter();
	IMUData* Filter(IMUData * input);
private:
	IMUData mWindowSum;
	std::queue<IMUData* > mWindow;
	int mCurWindoSize;
	int mWindowSize;
};
}

#endif // IMUSLIDEWINDOWFILTER_H