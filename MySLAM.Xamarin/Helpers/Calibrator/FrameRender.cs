using Android.Content.Res;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MySLAM.Xamarin.Helpers.Calibrator
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

    internal class ARFrameRender : FrameRender, IDisposable
    {
        #region NativeFunction
        [DllImport("MySLAM_AR", EntryPoint = "MySLAM_Native_AR_InitSystem")]
        public static extern bool InitSystem(IntPtr jnienv);
        [DllImport("MySLAM_AR", EntryPoint = "MySLAM_Native_AR_GetPose")]
        public static extern float[] GetPose(IntPtr jnienv, long mataddress, long timestamp);
        [DllImport("MySLAM_AR", EntryPoint = "MySLAM_Native_AR_ReleaseMap")]
        public static extern bool ReleaseMap(IntPtr jnienv);
        #endregion

        private readonly int width;
        private readonly int height;

        public ARFrameRender(int width, int height)
        {
            if (!InitSystem(IntPtr.Zero))
                throw new Exception("Init failed!!!");
            this.width = width;
            this.height = height;
        }

        public void Dispose()
        {
            ReleaseMap(IntPtr.Zero);
        }

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbaFrame = inputFrame.Rgba();
            float[] poseArr = GetPose(JNIEnv.Handle, rgbaFrame.NativeObjAddr, Java.Lang.JavaSystem.CurrentTimeMillis() * 1000);
            if (poseArr.Count() != 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    Imgproc.PutText(
                            rgbaFrame,
                            string.Join(' ', poseArr.Skip(i * 4).Take(4).Select(a => a.ToString("F"))),
                            new Point(width * 0.1, height * 0.1),
                            Core.FontHersheySimplex,
                            0.5,
                            new Scalar(255, 255, 0));
                }
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