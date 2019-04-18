#ifndef SLIDEWINDOWFILTER_H
#define SLIDEWINDOWFILTER_H

#include "IMUData.h"
#include <queue>

namespace IMU
{
class SlideWindowFilter
{
public:
	SlideWindowFilter(int windowSize);
	~SlideWindowFilter();
	IMUData* Filter(IMUData * input);
private:
	IMUData mWindowSum;
	std::queue<IMUData* > mWindow;
	int mCurWindoSize;
	int mWindowSize;
};
}

#endif // SLIDEWINDOWFILTER_H