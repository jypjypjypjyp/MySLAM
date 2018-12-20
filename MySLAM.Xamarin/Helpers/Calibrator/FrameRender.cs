using Org.Opencv.Android;
using Org.Opencv.Core;
using System;
using System.Runtime.InteropServices;

namespace MySLAM.Xamarin.Helpers.Calibrator
{
    public abstract class FrameRender
    {
        protected CameraCalibrator calibrator;

        public abstract Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame);
    }

    public class PreviewFrameRender : FrameRender
    {
        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            return inputFrame.Rgba();
        }
    }

    public class ARFrameRender : FrameRender
    {
        public enum TrackingState
        {
            NotReady = -1,
            NoImagesYet = 0,
            NotInitialized = 1,
            On = 2,
            Lost = 3
        }
        public TrackingState State { get; set; }

        public delegate void CallBack(float[] pose);
        public CallBack UpdatePose;

        private readonly DateTime fromSystemInit;
        public float[] Pose;

        public ARFrameRender()
        {
            InitSystem();
            fromSystemInit = DateTime.Now;
        }

        public void Init()
        {
            InitSystem();
        }
        public void Release()
        {
            ReleaseMap();
        }

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbaFrame = inputFrame.Rgba();

            State = GetPose(rgbaFrame.NativeObjAddr,
                        (long)(DateTime.Now - fromSystemInit).TotalMilliseconds * 1000,
                        Pose);
            switch (State)
            {
                case TrackingState.NotReady:
                    break;
                case TrackingState.NoImagesYet:
                    break;
                case TrackingState.NotInitialized:
                    break;
                case TrackingState.On:
                    UpdatePose(Pose);
                    break;
                case TrackingState.Lost:
                    break;
                default:
                    break;
            }
            return rgbaFrame;
        }

        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "MySLAM_Native_InitSystem")]
        private static extern void InitSystem();
        [DllImport("MySLAM_Native", EntryPoint = "MySLAM_Native_GetPose")]
        private static extern TrackingState GetPose(long mataddress, long timestamp, [In, Out] float[] pose);
        [DllImport("MySLAM_Native", EntryPoint = "MySLAM_Native_ReleaseMap")]
        private static extern void ReleaseMap();
        #endregion
    }

    public class CalibrationFrameRender : FrameRender
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
}