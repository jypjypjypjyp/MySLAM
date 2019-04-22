#ifndef POSESLIDEWINDOWFILTER_H
#define POSESLIDEWINDOWFILTER_H

#include <opencv2/core/core.hpp>
#include <queue>

namespace IMU
{
class PoseSlideWindowFilter
{
public:
	PoseSlideWindowFilter(int windowSize);
	~PoseSlideWindowFilter();
	cv::Vec3f Filter(cv::Vec3f const &input);
private:
	cv::Vec3f mWindowSum;
	std::queue<cv::Vec3f* > mWindow;
	int mCurWindoSize;
	int mWindowSize;
};
}

#endif // POSESLIDEWINDOWFILTER_H