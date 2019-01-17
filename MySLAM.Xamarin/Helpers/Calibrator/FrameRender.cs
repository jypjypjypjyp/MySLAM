using Android.OS;
using Org.Opencv.Android;
using Org.Opencv.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

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

    public class ARFrameRender : FrameRender, IDisposable
    {
        public delegate void Callback(float[] a);
        public static event Callback Update = delegate{};

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

        public ARFrameRender()
        {
            State = TrackingState.NotReady;
        }
        public void Perpare(float[] vmat)
        {
            InitSystem();
            imuThread = new HandlerThread("IMU Handler Thread");
            imuThread.Start();
            imuHandler = new Handler(imuThread.Looper);
            HelperManager.IMUHelper.Register(
                (float[] data) =>
                {
                    lock (_IMUData)
                    {
                        _IMUData.Add(data);
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
            if (timestamp == default(long))
                goto Finish;
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
        ORB_SLAM2:
            long matAddr = rgbMat.NativeObjAddr;
            Task.Run(
                () =>
                {
                    State = UpdateTracking(matAddr, timestamp);
                });
            State = TrackingState.Running;
        Estimate:
            float[] data;
            int n;
            lock (_IMUData)
            {
                data = _IMUData.Aggregate((cat, next) => cat.Concat(next).ToArray());
                n = _IMUData.Count;
                _IMUData.Clear();
            }
            EstimatePose(data, n, timestamp , pose);
            Update(pose);
        Finish:
            MatExtension.ConvertToGL(pose, _VMat);
            return rgbMat;
        }

        #region Native
        [DllImport("MySLAM_Native", EntryPoint = "InitSystem")]
        private static extern void InitSystem();
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