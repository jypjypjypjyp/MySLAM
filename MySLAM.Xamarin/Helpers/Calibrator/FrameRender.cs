using Android.Content.Res;
using Org.Opencv.Android;
using Org.Opencv.Core;
using Org.Opencv.Imgproc;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MySLAM.Xamarin.MyHelper
{
    public abstract class FrameRender
    {
        protected CameraCalibrator calibrator;

        public abstract Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame);
    }

    internal class PreviewFrameRender : FrameRender
    {
        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            return inputFrame.Rgba();
        }
    }

    internal class ARFrameRender : FrameRender , IDisposable
    {
        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "MySLAM_Native_AR_InitSystem")]
        private static extern void InitSystem();
        [DllImport("MySLAM_Native", EntryPoint = "MySLAM_Native_AR_GetPose")]
        private static extern bool GetPose(long mataddress, long timestamp, [In,Out] float[] pose);
        [DllImport("MySLAM_Native", EntryPoint = "MySLAM_Native_AR_ReleaseMap")]
        private static extern void ReleaseMap();
        #endregion

        public delegate void CallBack(float[] pose);
        public event CallBack UpdatePose;
        
        private readonly int width;
        private readonly int height;
        private readonly DateTime fromSystemInit;
        private readonly float[] pose = new float[4 * 4];

        public ARFrameRender(int width, int height)
        {
            InitSystem();
            fromSystemInit = DateTime.Now;
            this.width = width;
            this.height = height;
        }

        public void Dispose()
        {
            ReleaseMap();
        }

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbaFrame = inputFrame.Rgba();
            if (GetPose(rgbaFrame.NativeObjAddr,
                        (long)(DateTime.Now - fromSystemInit).TotalMilliseconds * 1000,
                        pose))
            {
                UpdatePose(pose);
            }
            return rgbaFrame;
        }
    }

    internal class CalibrationFrameRender : FrameRender
    {
        public CalibrationFrameRender(CameraCalibrator calibrator)
        {
            base.calibrator = calibrator;
        }

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbaFrame = inputFrame.Rgba();
            var grayFrame = inputFrame.Gray();
            calibrator.ProcessFrame(grayFrame, rgbaFrame);
            return rgbaFrame;
        }
    }

    internal class UndistortionFrameRender : FrameRender
    {
        public UndistortionFrameRender(CameraCalibrator calibrator)
        {
            base.calibrator = calibrator;
        }

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbMat = inputFrame.Rgba();
            var renderedFrame = new Mat(rgbMat.Size(), rgbMat.Type());
            Imgproc.Undistort(rgbMat, renderedFrame,
                    calibrator.CameraMatrix, calibrator.DistortionCoefficients);
            return renderedFrame;
        }
    }

    internal class ComparisonFrameRender : FrameRender
    {
        private readonly int width;
        private readonly int height;
        private Resources resources;
        public ComparisonFrameRender(CameraCalibrator calibrator, int width, int height, Resources resources)
        {
            base.calibrator = calibrator;
            this.width = width;
            this.height = height;
            this.resources = resources;
        }

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbMat = inputFrame.Rgba();
            var undistortedFrame = new Mat(rgbMat.Size(), rgbMat.Type());
            Imgproc.Undistort(rgbMat, undistortedFrame,
                    calibrator.CameraMatrix, calibrator.DistortionCoefficients);

            var comparisonFrame = rgbMat;
            undistortedFrame.ColRange(new Range(0, width / 2)).CopyTo(comparisonFrame.ColRange(new Range(width / 2, width)));
            var border = new List<MatOfPoint>();
            int shift = (int)(width * 0.005);
            border.Add(new MatOfPoint(new Point(width / 2 - shift, 0), new Point(width / 2 + shift, 0),
                    new Point(width / 2 + shift, height), new Point(width / 2 - shift, height)));
            Imgproc.FillPoly(comparisonFrame, border, new Scalar(255, 255, 255));

            Imgproc.PutText(comparisonFrame, resources.GetString(Resource.String.original), new Point(width * 0.1, height * 0.1),
                    Core.FontHersheySimplex, 1.0, new Scalar(255, 255, 0));
            Imgproc.PutText(comparisonFrame, resources.GetString(Resource.String.undistorted), new Point(width * 0.6, height * 0.1),
                    Core.FontHersheySimplex, 1.0, new Scalar(255, 255, 0));

            return comparisonFrame;
        }
    }
}