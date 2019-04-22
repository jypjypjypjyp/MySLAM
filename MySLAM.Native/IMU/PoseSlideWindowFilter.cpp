#include "PoseSlideWindowFilter.h"

namespace IMU
{
PoseSlideWindowFilter::PoseSlideWindowFilter(int windowSize) :mWindowSize(windowSize)
{
	mCurWindoSize = 0;
}
PoseSlideWindowFilter::~PoseSlideWindowFilter()
{
	while (!mWindow.empty())
	{
		delete mWindow.front();
		mWindow.pop();
	}
}

cv::Vec3f PoseSlideWindowFilter::Filter(cv::Vec3f const & input)
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
		mWindowSum -= *temp;
		delete temp;
	}
	mWindow.push(new cv::Vec3f(input));
	mWindowSum += input;
	return mWindowSum / mCurWindoSize;
}
}

