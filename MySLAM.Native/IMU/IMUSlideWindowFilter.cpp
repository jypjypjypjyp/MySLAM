#include "IMUSlideWindowFilter.h"
#include <cmath>

namespace IMU
{
IMUSlideWindowFilter::IMUSlideWindowFilter(int windowSize)
{
	mWindowSize = windowSize;
	mCurWindoSize = 0;
	mWindowSum = IMUData();
}

IMUSlideWindowFilter::~IMUSlideWindowFilter()
{
	while (!mWindow.empty())
	{
		delete mWindow.front();
		mWindow.pop();
	}
}

IMUData * IMUSlideWindowFilter::Filter(IMUData * input)
{
	// Slide Window 
	if (mCurWindoSize < mWindowSize)
	{
		mCurWindoSize++;
	}
	else
	{
		auto temp = mWindow.front();
		mWindow.pop();
		mWindowSum.mR -= temp->mR;
		mWindowSum.mAcceleration -= temp->mAcceleration;
		delete temp;
	}
	if (std::abs(input->mAcceleration[0]) < 0.3 &&
		std::abs(input->mAcceleration[1]) < 0.3 &&
		std::abs(input->mAcceleration[2]) < 0.3)
		input->mAcceleration = cv::Vec3f(0, 0, 0);
	mWindow.push(input);
	mWindowSum.mR += input->mR;
	mWindowSum.mAcceleration += input->mAcceleration;
	return new IMUData(mWindowSum.mR / mCurWindoSize,
		input->mTimestamp,
		mWindowSum.mAcceleration / mCurWindoSize);
}

}