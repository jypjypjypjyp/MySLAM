using Android.OS;
using MySLAM.Xamarin.Helpers.Calibrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        public float[] VMat;

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
                HelperManager.SensorHelper.UnRegister();
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
                        VMat);
            switch (State)
            {
                case TrackingState.NotReady:
                    break;
                case TrackingState.NoImagesYet:
                    break;
                case TrackingState.NotInitialized:
                    break;
                case TrackingState.On:
                    Update(VMat);
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
            VisualOK = 5,
            IMUOK = 6,
            Running = 7
        }
        public volatile TrackingState State;

        private Handler imuHandler;
        private HandlerThread imuThread;
        // Reference of VMat
        private float[] _VMat;
        private List<float[]> _IMUData = new List<float[]>();

        public AR2FrameRender()
        {
            State = TrackingState.NotReady;
        }
        public void Perpare(float[] vmat)
        {
            InitSystem(AppConst.RootPath, HelperManager.SensorHelper.GetWindowSize());
            imuThread = new HandlerThread("IMU Handler Thread");
            imuThread.Start();
            imuHandler = new Handler(imuThread.Looper);
            HelperManager.SensorHelper.Register(MySensorHelper.ModeType.AR,
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
                HelperManager.SensorHelper.UnRegister();
                ReleaseMap();
                isDisposed = true;
            }
        }
        #endregion

        public override Mat Render(CameraBridgeViewBase.ICvCameraViewFrame inputFrame)
        {
            var rgbMat = inputFrame.Rgba();
            // Uniform timestamp by IMU
            long timestamp = HelperManager.SensorHelper.Timestamp;
            if (timestamp == 0) goto Finish;
            // State Machine Control
            switch (State)
            {
                case TrackingState.NotReady:
                    goto Finish;
                case TrackingState.Ready:
                case TrackingState.NoImagesYet:
                case TrackingState.NotInitialized:
                case TrackingState.On:
                case TrackingState.Lost:
                case TrackingState.Steady:
                case TrackingState.VisualOK:
                case TrackingState.IMUOK:
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
            EstimatePose(data, n, timestamp, _VMat);
            Update(_VMat);
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
            return rgbMat;
        }

        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "InitSystem2")]
        private static extern void InitSystem(string rootPath, int windowSize);
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