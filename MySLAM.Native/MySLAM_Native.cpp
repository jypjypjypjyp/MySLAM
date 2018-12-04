#include "MySLAM_Native.h"
#include "System.h"
//Gobal Varible
ORB_SLAM2::System *gSystem;
float* pose;

void MySLAM_Native_AR_InitSystem()
{
	if (gSystem != nullptr)
	{
		delete gSystem;
	}
	pose = new float[4 * 4];
	gSystem = new ORB_SLAM2::System("/storage/emulated/0/MySLAM/ORBvoc.bin", "/storage/emulated/0/MySLAM/orb_slam2.yaml", ORB_SLAM2::System::MONOCULAR);
	LOGI("Create a System Successfully!!!");
}

bool MySLAM_Native_AR_GetPose(long long mataddress, long long timestamp, float* out)
{
	if (gSystem == nullptr) return false;
	cv::Mat *pMat = (cv::Mat*)mataddress;

	clock_t start, end;
	start = clock();
	cv::Mat posemat = gSystem->TrackMonocular(*pMat, static_cast<double>(timestamp));
	end = clock();
	LOGI("Get Pose Use Time=%f\n", ((double)end - start) / CLOCKS_PER_SEC);
	/*
	static bool instialized = false;
	static bool markerDetected = false;
	if (gSystem->MapChanged())
	{
		instialized = false;
		markerDetected = false;
	}

	if (!posemat.empty())
	{
		cv::Mat rVec;
		cv::Rodrigues(posemat.colRange(0, 3).rowRange(0, 3), rVec);
		cv::Mat tVec = posemat.col(3).rowRange(0, 3);

		const vector<ORB_SLAM2::MapPoint*> vpMPs = gSystem->mpTracker->mpMap->GetAllMapPoints();//所有的地图点
		const vector<ORB_SLAM2::MapPoint*> vpTMPs = gSystem->GetTrackedMapPoints();
		vector<cv::KeyPoint> vKPs = gSystem->GetTrackedKeyPointsUn();
		if (vpMPs.size() > 0)
		{
			std::vector<cv::Point3f> allmappoints;
			for (size_t i = 0; i < vpMPs.size(); i++)
			{
				if (vpMPs[i])
				{
					cv::Point3f pos = cv::Point3f(vpMPs[i]->GetWorldPos());
					allmappoints.push_back(pos);
					//                  LOGE("Point's world pose is %f %f %f",pos.x,pos.y,pos.z );
				}
			}
			LOGI("all map points size %d", allmappoints.size());
			std::vector<cv::Point2f> projectedPoints;
			cv::projectPoints(allmappoints, rVec, tVec, gSystem->mpTracker->mK, gSystem->mpTracker->mDistCoef, projectedPoints);
			for (size_t j = 0; j < projectedPoints.size(); ++j)
			{
				cv::Point2f r1 = projectedPoints[j];
				if (r1.x < 640 && r1.x> 0 && r1.y > 0 && r1.y < 480)
					cv::circle(*pMat, cv::Point(r1.x, r1.y), 2, cv::Scalar(0, 255, 0), 1, 8);
			}

			if (instialized == false)
			{
				Plane mplane;
				cv::Mat tempTpw, rpw, rwp, tpw, twp;
				tempTpw = mplane.DetectPlane(posemat, vpTMPs, 50);
				if (!tempTpw.empty())
				{
					rpw = tempTpw.rowRange(0, 3).colRange(0, 3);
					tpw = tempTpw.col(3).rowRange(0, 3);
					rwp = rpw.t();
					twp = -rwp * tpw;
					rwp.copyTo(Plane2World.rowRange(0, 3).colRange(0, 3));
					twp.copyTo(Plane2World.col(3).rowRange(0, 3));
					centroid = mplane.o;
					instialized = true;
					Plane2World = tempTpw;
				}

			}
			else
			{
				cv::Mat Plane2Camera = posemat * Plane2World;
				vector<cv::Point3f> drawPoints(8);
				drawPoints[0] = cv::Point3f(0, 0, 0);
				drawPoints[1] = cv::Point3f(0.3, 0.0, 0.0);
				drawPoints[2] = cv::Point3f(0.0, 0, 0.3);
				drawPoints[3] = cv::Point3f(0.0, 0.3, 0);
				drawPoints[4] = cv::Point3f(0, 0.3, 0.3);
				drawPoints[5] = cv::Point3f(0.3, 0.3, 0.3);
				drawPoints[6] = cv::Point3f(0.3, 0, 0.3);
				drawPoints[7] = cv::Point3f(0.3, 0.3, 0);
				cv::Mat Rcp, Tcp;
				cv::Rodrigues(Plane2Camera.rowRange(0, 3).colRange(0, 3), Rcp);
				Tcp = Plane2Camera.col(3).rowRange(0, 3);
				cv::projectPoints(drawPoints, Rcp, Tcp, gSystem->mpTracker->mK, gSystem->mpTracker->mDistCoef, projectedPoints);

			}

		}
	}*/

	switch (gSystem->GetTrackingState())
	{
	case -1:
		cv::putText(*pMat, "SYSTEM NOT READY", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	case 0:
		cv::putText(*pMat, "NO IMAGES YET", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	case 1:
		cv::putText(*pMat, "SLAM NOT INITIALIZED", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	case 2:
		cv::putText(*pMat, "SLAM ON", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(0, 255, 0), 2);
		break;
	case 3:
		cv::putText(*pMat, "SLAM LOST", cv::Point(0, 400), cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(255, 0, 0), 2);
		break;
	default:
		break;
	}

	if (posemat.rows > 0)
	{
		for (int i = 0; i < posemat.rows; i++)
		{
			for (int j = 0; j < posemat.cols; j++)
			{
				out[i*posemat.cols + j] = posemat.at<float>(i, j);
			}
		}
		return true;
	}
	return false;
}

void MySLAM_Native_AR_ReleaseMap()
{
	if (pose != nullptr)
		delete[] pose;
	if (gSystem != nullptr)
	{
		gSystem->Shutdown();
		delete gSystem;
	}
}
