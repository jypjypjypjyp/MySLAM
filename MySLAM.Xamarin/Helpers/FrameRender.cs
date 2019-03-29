using Android.OS;
using Org.Opencv.Android;
using Org.Opencv.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using MySLAM.Xamarin.Helpers.Calibrator;

namespace MySLAM.Xamarin.Helpers
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

    public class AR1FrameRender : FrameRender, IDisposable
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
        public static event CallBack Update = delegate { };

        private readonly DateTime fromSystemInit;
        public float[] Pose;

        public AR1FrameRender()
        {
            InitSystem(AppConst.RootPath);
            fromSystemInit = DateTime.Now;
        }

        #region IDispose
        private bool isDisposed = false;
        public void Dispose()
        {
            if (!isDisposed)
            {
                State = TrackingState.NotReady;
                HelperManager.IMUHelper.UnRegister();
                ReleaseMap();
                isDisposed = true;
            }
        }
        #endregion

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
                    Update(Pose);
                    break;
                case TrackingState.Lost:
                    break;
                default:
                    break;
            }
            return rgbaFrame;
        }

        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "InitSystem1")]
        private static extern void InitSystem(string rootPath);
        [DllImport("MySLAM_Native", EntryPoint = "GetPose")]
        private static extern TrackingState GetPose(long mataddress, long timestamp, [In, Out] float[] pose);
        [DllImport("MySLAM_Native", EntryPoint = "ReleaseMap")]
        private static extern void ReleaseMap();
        #endregion
    }


    public class AR2FrameRender : FrameRender, IDisposable
    {
        public delegate void Callback(float[] a);
        public static event Callback Update = delegate { };

        public enum TrackingState
        {
            Ready = -2,
            NotReady = -1,
            NoImagesYet = 0,
            NotInitialized = 1,
            On = 2,
            Lost = 3,
            Steady = 4,
            Running = 5
        }
        public volatile TrackingState State;

        private Handler imuHandler;
        private HandlerThread imuThread;
        // Reference of VMat
        private float[] _VMat;
        private float[] pose = new float[16];
        private List<float[]> _IMUData = new List<float[]>();

        public AR2FrameRender()
        {
            State = TrackingState.NotReady;
        }
        public void Perpare(float[] vmat)
        {
            InitSystem(AppConst.RootPath);
            imuThread = new HandlerThread("IMU Handler Thread");
            imuThread.Start();
            imuHandler = new Handler(imuThread.Looper);
            HelperManager.IMUHelper.Register(MyIMUHelper.ModeType.AR,
                (object data) =>
                {
                    lock (_IMUData)
                    {
                        _IMUData.Add((float[])data);
                    }
                }, imuHandler);
            _VMat = vmat;
            State = TrackingState.Ready;
        }
        #region IDispose
        private bool isDisposed = false;
        public void Dispose()
        {
            if (!isDisposed)
            {
                State = TrackingState.NotReady;
                imuThread.QuitSafely();
                imuThread.Join();
                imuThread = null;
                imuHandler = null;
                HelperManager.IMUHelper.UnRegister();
                ReleaseMap();
                isDisposed = true;
            }
        }
        #endregion

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbMat = inputFrame.Rgba();
            // Uniform timestamp by IMU
            long timestamp = HelperManager.IMUHelper.Timestamp;
            if (timestamp == 0) goto Finish;
            // State Machine Control
            switch (State)
            {
                case TrackingState.Ready:
                    goto ORB_SLAM2;
                case TrackingState.NotReady:
                    goto Finish;
                case TrackingState.NoImagesYet:
                    goto ORB_SLAM2;
                case TrackingState.NotInitialized:
                    goto ORB_SLAM2;
                case TrackingState.On:
                    goto ORB_SLAM2;
                case TrackingState.Lost:
                    goto ORB_SLAM2;
                case TrackingState.Steady:
                    goto ORB_SLAM2;
                case TrackingState.Running:
                    goto Estimate;
            }
            Estimate:
            float[] data;
            int n;
            lock (_IMUData)
            {
                if ((n = _IMUData.Count) <= 1) goto Finish;
                data = _IMUData.Aggregate((cat, next) => cat.Concat(next).ToArray());
                _IMUData.Clear();
            }
            EstimatePose(data, n, timestamp, pose);
            Update(pose);
            goto Finish;
            ORB_SLAM2:
            long matAddr = rgbMat.NativeObjAddr;
            Task.Run(
                () =>
                {
                    State = UpdateTracking(matAddr, timestamp);
                });
            State = TrackingState.Running;
            goto Estimate;
            Finish:
            MatExtension.ConvertToGL(pose, _VMat);
            return rgbMat;
        }

        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "InitSystem2")]
        private static extern void InitSystem(string rootPath);
        [DllImport("MySLAM_Native", EntryPoint = "UpdateTracking")]
        private static extern TrackingState UpdateTracking(long mataddress, long timestamp);
        [DllImport("MySLAM_Native", EntryPoint = "EstimatePose")]
        private static extern void EstimatePose([In] float[] data, int n, long timestamp, [Out] float[] pose);
        [DllImport("MySLAM_Native", EntryPoint = "ReleaseMap")]
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
            var rgbMat = inputFrame.Rgba();
            var grayMat = inputFrame.Gray();
            calibrator.ProcessFrame(grayMat, rgbMat);
            return rgbMat;
        }
    }
}