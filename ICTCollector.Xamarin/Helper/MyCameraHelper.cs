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

        public CameraState State { get; set; }
        public CameraManager Manager { get; set; }
        public CameraDevice CameraDevice { get; set; }
        public CameraCaptureSession CaptureSession { get; set; }
        public MyStateCallback CameraStateCallback { get; private set; }
        public MySessionCallback CameraSessionCallback { get; private set; }

        private readonly Activity owner;

        public MyCameraHelper(Activity owner)
        {
            this.owner = owner;
            Manager = (CameraManager)owner.GetSystemService(Context.CameraService);
            State = CameraState.Unavailable;
            CameraStateCallback = new MyStateCallback(this);
            CameraSessionCallback = new MySessionCallback(this);
        }
        ~MyCameraHelper()
        {
            if (State != CameraState.Close)
            {
                CloseCamera();
            }

            HelperManager.CameraHelper = null;
        }

        public void OpenCamera(CameraSetting cameraSetting)
        {
            if (ContextCompat.CheckSelfPermission(owner, Manifest.Permission.Camera) != Permission.Granted)
            {
                return;
            }
            HelperManager.CameraHelper.State = CameraState.Ready;
            HelperManager.CameraHelper.Manager.OpenCamera(
                cameraSetting.CameraId,
                CameraStateCallback,
                null);
        }
        public void CloseCamera()
        {
            CaptureSession.Close();
            CaptureSession = null;
            CameraDevice.Close();
            CameraDevice = null;
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
    }
}