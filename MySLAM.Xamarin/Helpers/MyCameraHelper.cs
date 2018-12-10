using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Util;
using System;
using System.Threading;

namespace MySLAM.Xamarin.Helpers
{
    public enum CameraState
    {
        Unavailable, Ready, Open, Off, Record, Close
    }

    public interface IMyCallback
    {
        MyCameraHelper Outer { get; set; }
        event MyCameraHelper.Callback Callback;
    }

    public static class AppSetting
    {
        private static ISharedPreferences preferences;
        public static string CameraId => preferences.GetString("prefCamera", "0");
        public static Size Size
        {
            get
            {
                string size = preferences.GetString("prefSizeRaw", "0x0");
                int height = int.Parse(size.Substring(0, size.LastIndexOf("x")));
                int width = int.Parse(size.Substring(size.LastIndexOf("x") + 1));
                return new Size(width, height);
            }
        }
        public static float Focus => float.Parse(preferences.GetString("prefFocusLength", "5.0"));
        public static int IMUFreq => int.Parse(preferences.GetString("perfImuFreq", "1"));
        public static void Init(Context context)
        {
            preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }
    }

    public class MyCameraHelper
    {
        public delegate void Callback(int i = 0);

        public Semaphore CameraLock = new Semaphore(1, 1);
        public CameraState State { get; set; }
        public CameraManager Manager { get; set; }
        public CameraDevice CameraDevice { get; set; }
        public CameraCaptureSession CaptureSession { get; set; }

        private readonly Activity owner;

        public MyCameraHelper(Activity owner)
        {
            this.owner = owner;
            Manager = (CameraManager)owner.GetSystemService(Context.CameraService);
            State = CameraState.Unavailable;
        }

        ~MyCameraHelper()
        {
            if (State != CameraState.Close)
            {
                CloseCamera();
            }
        }

        public T CreateCallBack<T>(Callback callback) where T : IMyCallback, new()
        {
            var instance = new T
            {
                Outer = this
            };
            instance.Callback += callback;
            return instance;
        }

        public void OpenCamera(string cameraId, Callback callback = null, Handler handler = null)
        {
            if (HelperManager.CameraHelper.State == CameraState.Close
                || HelperManager.CameraHelper.State == CameraState.Ready
                || HelperManager.CameraHelper.State == CameraState.Unavailable)
            {
                if (ContextCompat.CheckSelfPermission(owner, Manifest.Permission.Camera) != Permission.Granted)
                {
                    return;
                }
                CameraLock.WaitOne();
                HelperManager.CameraHelper.State = CameraState.Ready;
                HelperManager.CameraHelper.Manager.OpenCamera(
                    cameraId,
                    CreateCallBack<MyStateCallback>(callback),
                    handler);
            }
        }
        public void CloseCamera()
        {
            if (HelperManager.CameraHelper.State != CameraState.Unavailable
                || HelperManager.CameraHelper.State != CameraState.Close)
            {
                HelperManager.CameraHelper.State = CameraState.Close;
                CaptureSession.Close();
                CaptureSession = null;
                CameraDevice.Close();
                CameraDevice = null;
                CameraLock.Release();
            }
        }
        
        public void TryGetCalibration(string cameraId, out float[] intrinsic, out float[] distortion)
        {
            intrinsic = null; distortion = null;
            if ((int)Build.VERSION.SdkInt >= 23)
            {
                var characteristics = Manager.GetCameraCharacteristics(cameraId);
                intrinsic = 
                    (float[])characteristics.Get(CameraCharacteristics.LensIntrinsicCalibration);
                distortion = 
                    (float[])characteristics.Get(CameraCharacteristics.LensRadialDistortion);
            }
        }

        public class MyStateCallback : CameraDevice.StateCallback, IMyCallback
        {
            public MyCameraHelper Outer { get; set; }
            public event Callback Callback;

            public override void OnDisconnected(CameraDevice camera)
            {
                Outer.State = CameraState.Unavailable;
                Outer.CameraDevice.Close();
                Outer.CameraDevice = null;
            }
            public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            {
                Outer.State = CameraState.Unavailable;
                throw new Exception("Error!!!");
            }
            public override void OnOpened(CameraDevice camera)
            {
                Outer.State = CameraState.Open;
                Outer.CameraDevice = camera;
                Callback();
            }
        }
        public class MySessionCallback : CameraCaptureSession.StateCallback, IMyCallback
        {
            public MyCameraHelper Outer { get; set; }
            public event Callback Callback;

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                Outer.State = CameraState.Unavailable;
                throw new Exception("Config Fail");
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                if (Outer.State != CameraState.Open)
                {
                    return;
                }
                Outer.CaptureSession = session;
                Callback();
                Outer.State = CameraState.Off;
            }
        }
        public class MyCaptureCallback : CameraCaptureSession.CaptureCallback, IMyCallback
        {
            private int fps = 0;
            private int frames = 0;
            private long lastUpdate = 0;
            public MyCameraHelper Outer { get; set; }
            public event Callback Callback;

            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                base.OnCaptureCompleted(session, request, result);
                frames++;
                long timestamp = (long)result.Get(CaptureResult.SensorTimestamp);
                if (lastUpdate == 0)
                {
                    lastUpdate = timestamp;
                }
                else if (timestamp - lastUpdate >= 1e9)
                {
                    lastUpdate = timestamp;
                    fps = frames;
                    frames = 0;
                    Outer.owner.RunOnUiThread(() => Callback(fps));
                }
            }
        }
    }
}