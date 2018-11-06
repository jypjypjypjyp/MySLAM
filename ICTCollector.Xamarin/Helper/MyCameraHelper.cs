using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Camera2;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Util;
using System;

namespace ICTCollector.Xamarin.Helper
{
    public enum CameraState
    {
        Unavailable, Ready, Open, Off, Record, Close
    }

    public interface ICameraView
    {
        void RequestCameraPermission();
    }

    public class CameraSetting
    {
        public string CameraId { get; private set; }
        public Size Size { get; private set; }
        public float Focus { get; private set; }

        public static CameraSetting GetCameraSetting(Context context)
        {
            ISharedPreferences preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            string cameraId = preferences.GetString("prefCamera", "0");
            string size = preferences.GetString("prefSizeRaw", "640x480");
            int height = int.Parse(size.Substring(0, size.LastIndexOf("x")));
            int width = int.Parse(size.Substring(size.LastIndexOf("x") + 1));
            float focus = float.Parse(preferences.GetString("prefFocusLength", "5.0"));
            return new CameraSetting()
            {
                CameraId = cameraId,
                Size = new Size(width, height),
                Focus = focus
            };
        }
    }

    public class MyCameraHelper
    {
        public delegate void Callback();
        public delegate void FpsCallback(int fps);

        public CameraState State { get; set; }
        public CameraManager Manager { get; set; }
        public CameraDevice CameraDevice { get; set; }
        public CameraCaptureSession CaptureSession { get; set; }
        public MyStateCallback CameraStateCallback { get; private set; }
        public MySessionCallback CameraSessionCallback { get; private set; }
        public MyCaptureCallback CaptureCallback { get; private set; }

        private readonly Activity owner;

        public MyCameraHelper(Activity owner)
        {
            this.owner = owner;
            Manager = (CameraManager)owner.GetSystemService(Context.CameraService);
            State = CameraState.Unavailable;
            CameraStateCallback = new MyStateCallback(this);
            CameraSessionCallback = new MySessionCallback(this);
            CaptureCallback = new MyCaptureCallback(this);
        }
        ~MyCameraHelper()
        {
            if (State != CameraState.Close)
            {
                CloseCamera();
            }

            HelperManager.CameraHelper = null;
        }

        public void OpenCamera(CameraSetting setting)
        {
            if (HelperManager.CameraHelper.State == CameraState.Close
                || HelperManager.CameraHelper.State == CameraState.Ready
                || HelperManager.CameraHelper.State == CameraState.Unavailable)
            {
                if (ContextCompat.CheckSelfPermission(owner, Manifest.Permission.Camera) != Permission.Granted)
                {
                    return;
                }
                HelperManager.CameraHelper.State = CameraState.Ready;
                HelperManager.CameraHelper.Manager.OpenCamera(
                    setting.CameraId,
                    CameraStateCallback,
                    null);
            }
        }
        public void CloseCamera()
        {
            if (HelperManager.CameraHelper.State != CameraState.Unavailable
                || HelperManager.CameraHelper.State != CameraState.Close)
            {
                CaptureSession.Close();
                CaptureSession = null;
                CameraDevice.Close();
                CameraDevice = null;
            }
        }

        public class MyStateCallback : CameraDevice.StateCallback
        {
            private readonly MyCameraHelper outer;
            public event Callback CreateSession;

            public MyStateCallback(MyCameraHelper outer)
            {
                this.outer = outer;
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                outer.State = CameraState.Unavailable;
                outer.CameraDevice.Close();
                outer.CameraDevice = null;
            }

            public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            {
                outer.State = CameraState.Unavailable;
                throw new Exception("Error!!!");
            }

            public override void OnOpened(CameraDevice camera)
            {
                outer.State = CameraState.Open;
                outer.CameraDevice = camera;
                CreateSession();
            }
        }

        public class MySessionCallback : CameraCaptureSession.StateCallback
        {
            private readonly MyCameraHelper outer;
            public event Callback StartCapture;

            public MySessionCallback(MyCameraHelper outer)
            {
                this.outer = outer;
            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                outer.State = CameraState.Unavailable;
                throw new Exception("Config Fail");
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                if (outer.State != CameraState.Open)
                {
                    return;
                }
                outer.CaptureSession = session;
                StartCapture();
                outer.State = CameraState.Off;
            }
        }

        public class MyCaptureCallback : CameraCaptureSession.CaptureCallback
        {
            private readonly MyCameraHelper outer;
            private int fps = 0;
            private int frames = 0;
            private long lastUpdate = 0;
            public event FpsCallback UpdateFps;

            public MyCaptureCallback(MyCameraHelper outer)
            {
                this.outer = outer;
            }

            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                base.OnCaptureCompleted(session, request, result);
                frames++;
                long timestamp = (long)result.Get(CaptureResult.SensorTimestamp);
                if (lastUpdate==0)
                {
                    lastUpdate = timestamp;
                }
                else if (timestamp - lastUpdate >= 1e9)
                {
                    lastUpdate = timestamp;
                    fps = frames;
                    frames = 0;
                    outer.owner.RunOnUiThread(()=>UpdateFps(fps));
                }
            }
        }
    }
}