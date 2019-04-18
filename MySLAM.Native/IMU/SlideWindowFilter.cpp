#include "SlideWindowFilter.h"

namespace IMU
{
SlideWindowFilter::SlideWindowFilter(int windowSize)
{
	mWindowSize = windowSize;
	mCurWindoSize = 0;
	mWindowSum = IMUData();
}

SlideWindowFilter::~SlideWindowFilter()
{
	while (!mWindow.empty())
	{
		delete mWindow.front();
		mWindow.pop();
	}
}

IMUData * SlideWindowFilter::Filter(IMUData * input)
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
	mWindow.push(input);
	mWindowSum.mR += input->mR;
	mWindowSum.mAcceleration += input->mAcceleration;
	float a = input->mR.at<float>(0, 0);
	float b = mWindowSum.mR.at<float>(0,0);
	cv::Mat bb = mWindowSum.mR / mCurWindoSize;
	float c = bb.at<float>(0, 0);
	auto result = new IMUData(mWindowSum.mR / mCurWindoSize,
		input->mTimestamp,
		mWindowSum.mAcceleration / mCurWindoSize);
	return result;
}

}