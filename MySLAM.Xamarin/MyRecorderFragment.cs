using Android.App;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Views;
using Android.Widget;
using MySLAM.Xamarin.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MySLAM.Xamarin
{

    public class MyRecorderFragment : Fragment
    {
        private FrameLayout PreviewLayout;
        private TextureView PreviewView;
        private Button markButton;
        private Button recordButton;
        private TextView fpsTextView;
        private Handler captureHandler;
        private HandlerThread captureThread;
        private Handler imuHandler;
        private HandlerThread imuThread;
        private CaptureRequest.Builder requestBuilder;
        private Matrix transformMat;

        private string path;
        private string sensorDataString;
        private string markedTimestampList;
        private long? curTimestamp;
        private volatile int availableProcessers = AppConst.CoreNumber;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.recorder_frag, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            PreviewLayout = view.FindViewById<FrameLayout>(Resource.Id.preview_layout);
            (recordButton = view.FindViewById<Button>(Resource.Id.button_record)).Click += Record;
            (markButton = view.FindViewById<Button>(Resource.Id.button_mark)).Click += Mark;
            fpsTextView = Activity.FindViewById<TextView>(Resource.Id.fpsTextView);
        }

        private void Mark(object sender, EventArgs e)
        {
            if (HelperManager.CameraHelper.State == CameraState.Record && curTimestamp != null)
            {
                markedTimestampList += curTimestamp + '\n';
            }
        }
        private void Record(object sender, EventArgs e)
        {
            if (HelperManager.CameraHelper.State == CameraState.Off)
            {
                ((MainActivity)Activity).NavigationMenu.FindItem(Resource.Id.frag_settings).SetEnabled(false);
                recordButton.Text = Context.Resources.GetString(Resource.String.button_stop_recording);
                HelperManager.CameraHelper.State = CameraState.Record;
                markedTimestampList = "";
                path =
                    AppConst.RootPath
                    + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "/";
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(path + "cam0/");
                File.WriteAllText(path + "sensor0.csv",
                    "#timestamp,omega_x,omega_y,omega_z,alpha_x,alpha_y,alpha_z,pressure");
                PreviewView.SurfaceTextureUpdated += ProcessFrame;
                HelperManager.CameraHelper.State = CameraState.Record;
                HelperManager.IMUHelper.Register(MySensorHelper.ModeType.Record, ProcessSensorData, imuHandler);
            }
            else
            {
                ((MainActivity)Activity).NavigationMenu.FindItem(Resource.Id.frag_settings).SetEnabled(true);
                recordButton.Text = Context.Resources.GetString(Resource.String.button_start_recording);
                HelperManager.CameraHelper.State = CameraState.Off;
                PreviewView.SurfaceTextureUpdated -= ProcessFrame;
                HelperManager.IMUHelper.UnRegister();
                File.WriteAllText(path + "marked.txt", markedTimestampList);
                markedTimestampList = "";
            }
        }

        private void StartThread()
        {
            captureThread = new HandlerThread("IMUThread");
            captureThread.Start();
            captureHandler = new Handler(captureThread.Looper);
            imuThread = new HandlerThread("IMUThread");
            imuThread.Start();
            imuHandler = new Handler(imuThread.Looper);
        }
        private void StopThread()
        {
            captureThread.QuitSafely();
            captureThread.Join();
            captureThread = null;
            captureHandler = null;
            imuThread.QuitSafely();
            imuThread.Join();
            imuThread = null;
            imuHandler = null;
        }

        public override void OnResume()
        {
            base.OnResume();
            OpenCamera();
            StartThread();
        }

        public override void OnPause()
        {
            if (HelperManager.CameraHelper.State == CameraState.Record)
            {
                Record(null, null);
            }
            CloseCamera();
            StopThread();
            base.OnPause();
        }

        private void OpenCamera()
        {
            HelperManager.CameraHelper.OpenCamera(AppSetting.CameraId, PrepareSession);
        }

        public void PrepareSession(int i = 0)
        {
            //Get image size
            var imageSize = AppSetting.Size;
            if (imageSize.Width == 0 || imageSize.Height == 0)
            {
                var map = ((Android.Hardware.Camera2.Params.StreamConfigurationMap)
                    HelperManager.CameraHelper.Manager
                    .GetCameraCharacteristics(AppSetting.CameraId)
                    .Get(CameraCharacteristics.ScalerStreamConfigurationMap));
                var sizeCollection = map.GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture)));
                imageSize = sizeCollection.Aggregate(
                    (a1, a2) => a1.Area() < a2.Area() ? a1 : a2);
            }

            if (PreviewView != null)
            {
                PreviewLayout.RemoveView(PreviewView);
            }
            PreviewView = new TextureView(Activity);
            PreviewLayout.AddView(PreviewView,
                new FrameLayout.LayoutParams(imageSize.Width, imageSize.Height, GravityFlags.Center));
            PreviewView.SurfaceTextureAvailable +=
                (object sender, TextureView.SurfaceTextureAvailableEventArgs e) =>
                {
                    CreateSession();
                };
            // Set PreviewView Transform
            transformMat = new Matrix();
            var viewRect = new RectF(0, 0, imageSize.Width, imageSize.Height); // TextureView size
            var bufferRect = new RectF(0, 0, imageSize.Height, imageSize.Width); // Camera output size
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
            transformMat.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
            transformMat.PostRotate(270, centerX, centerY);
            PreviewView.SetTransform(transformMat);
        }

        public void CreateSession()
        {
            var previewTexture = PreviewView.SurfaceTexture;
            if (previewTexture == null)
            {
                throw new Exception("texture is null");
            }
            var previewSurface = new Surface(previewTexture);
            requestBuilder =
                HelperManager.CameraHelper.CameraDevice.CreateCaptureRequest(CameraTemplate.Record);
            requestBuilder.AddTarget(previewSurface);
            requestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Off);
            requestBuilder.Set(CaptureRequest.LensFocalLength, AppSetting.Focus);
            requestBuilder.Set(CaptureRequest.JpegOrientation, (int)SurfaceOrientation.Rotation0);

            var surfaces = new List<Surface>
            {
                previewSurface
            };

            HelperManager.CameraHelper.CameraDevice.CreateCaptureSession(
                    surfaces,
                    HelperManager.CameraHelper.CreateCallBack<MyCameraHelper.MySessionCallback>(StartCapture),
                    null);
        }

        private void StartCapture(int i = 0)
        {
            HelperManager.CameraHelper.CaptureSession
                .SetRepeatingRequest(
                    requestBuilder.Build(),
                    HelperManager.CameraHelper.CreateCallBack<MyCameraHelper.MyCaptureCallback>(UpdateFps),
                    captureHandler);
        }

        public void CloseCamera()
        {
            HelperManager.CameraHelper.CloseCamera();
        }


        public void ProcessFrame(object sender, TextureView.SurfaceTextureUpdatedEventArgs e)
        {
            long timestamp = e.Surface.Timestamp;
            // 32-bits data type's operates are atomic
            if (availableProcessers == 0 || curTimestamp >= timestamp) return;
            curTimestamp = timestamp;
            availableProcessers--;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                (long timestamp, Bitmap bitmap) state = ((long, Bitmap))o;
                state.bitmap =
                Bitmap.CreateBitmap(state.bitmap, 0, 0, state.bitmap.Width, state.bitmap.Height, transformMat, true);
                FileStream file = null;
                try
                {   //Sometimes it will get a exception "win32 IO returned 997". I have no idea
                    file = File.Create(path + "cam0/" + state.timestamp + ".jpg");
                    state.bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, file);
                    if (HelperManager.CameraHelper.State != CameraState.Record
                        && state.timestamp == curTimestamp)
                    {
                        Activity.RunOnUiThread(() =>
                        {
                            Toast.MakeText(Activity, "Complete", ToastLength.Short).Show();
                        });
                    }
                }
                catch (Exception)
                {
                    File.Delete(path + "cam0/" + state.timestamp + ".jpg");
                    Activity.RunOnUiThread(() =>
                    {
                        Toast.MakeText(Activity, "Error, Skip", ToastLength.Short).Show();
                    });
                }
                finally
                {
                    file?.Close();
                    availableProcessers++;
                }
            }, (timestamp, PreviewView.Bitmap));
        }

        public void ProcessSensorData(object data)
        {
            var record = ((long, IList<float>, IList<float>, IList<float>))data;
            sensorDataString += record.Item1 + ",";
            sensorDataString += string.Join(',', record.Item2) + ",";
            sensorDataString += string.Join(',', record.Item3) + ",";
            sensorDataString += string.Join(',', record.Item4) + "\n";
            if (sensorDataString.Length > 1e4)
            {
                File.AppendAllText(path + "sensor0.csv", sensorDataString);
                sensorDataString = "";
            }
        }

        public void UpdateFps(int fps)
        {
            fpsTextView.Text = fps + " fps";
        }
    }
}