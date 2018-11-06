using Android.App;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Widget;
using ICTCollector.Xamarin.Helper;
using System;
using System.Collections.Generic;
using System.IO;

namespace ICTCollector.Xamarin
{
    public class CameraFragment : Fragment, ImageReader.IOnImageAvailableListener
    {
        //Singelton
        private static CameraFragment _instance;
        public static CameraFragment Instance
        {
            get
            {
                _instance = _instance ?? new CameraFragment();
                return _instance;
            }
        }

        public AutoFitTextureView TextureView { get; set; }
        private Button markButton;
        private Button recordButton;
        private TextView fpsTextView;
        private ImageReader imageReader;
        private Handler backgroundHandler;
        private HandlerThread backgroundThread;
        private CaptureRequest.Builder requestBuilder;

        private string markedTimestampList;
        private string curTimestamp;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HelperManager.CameraHelper = new MyCameraHelper(Activity);
        }

        private void StartThread()
        {
            backgroundThread = new HandlerThread("ReaptingCapture");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }
        private void StopThread()
        {
            backgroundThread.QuitSafely();
            backgroundThread.Join();
            backgroundThread = null;
            backgroundHandler = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.camera_fragment, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            TextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            (recordButton = view.FindViewById<Button>(Resource.Id.button_record)).Click += Record;
            (markButton = view.FindViewById<Button>(Resource.Id.button_mark)).Click += Mark;
            fpsTextView = view.FindViewById<TextView>(Resource.Id.fpsTextView);
            //Init Event
            TextureView.SurfaceTextureAvailable +=
                (object sender, TextureView.SurfaceTextureAvailableEventArgs e) =>
                {
                    OpenCamera();
                };
            HelperManager.CameraHelper.CameraSessionCallback.StartCapture += StartCapture;
            HelperManager.CameraHelper.CameraStateCallback.CreateSession += CreateSession;
            HelperManager.CameraHelper.CaptureCallback.UpdateFps += UpdateFps;
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
                recordButton.Text = Context.Resources.GetString(Resource.String.button_stop_recording);
                markedTimestampList = "";
                FileSaver.Path =
                    Android.OS.Environment.ExternalStorageDirectory.AbsolutePath
                    + "/ICTCollector/"
                    + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "/";
                Directory.CreateDirectory(FileSaver.Path);
                HelperManager.CameraHelper.CaptureSession.StopRepeating();

                requestBuilder.AddTarget(imageReader.Surface);
                HelperManager.CameraHelper.CaptureSession
                    .SetRepeatingRequest(requestBuilder.Build(), HelperManager.CameraHelper.CaptureCallback, backgroundHandler);
                HelperManager.CameraHelper.State = CameraState.Record;
            }
            else
            {
                recordButton.Text = Context.Resources.GetString(Resource.String.button_start_recording);
                HelperManager.CameraHelper.CaptureSession.StopRepeating();
                requestBuilder.RemoveTarget(imageReader.Surface);
                HelperManager.CameraHelper.State = CameraState.Off;
                HelperManager.CameraHelper.CaptureSession
                    .SetRepeatingRequest(requestBuilder.Build(), HelperManager.CameraHelper.CaptureCallback, backgroundHandler);
                File.WriteAllText(FileSaver.Path + "marked.txt", markedTimestampList);
                markedTimestampList = "";
                curTimestamp = null;
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            StartThread();
        }

        public override void OnPause()
        {
            CloseCamera();
            StopThread();
            base.OnPause();
        }

        public void OpenCamera()
        {
            CameraSetting cameraSetting = CameraSetting.GetCameraSetting(Activity);
            TextureView.SetAspectRatio(cameraSetting.Size.Width, cameraSetting.Size.Height);
            HelperManager.CameraHelper.OpenCamera(cameraSetting);
            imageReader = ImageReader.NewInstance(cameraSetting.Size.Width, cameraSetting.Size.Height, ImageFormatType.Jpeg, 20);
            imageReader.SetOnImageAvailableListener(this, backgroundHandler);
        }

        public void CreateSession()
        {
            CameraSetting cameraSetting = CameraSetting.GetCameraSetting(Activity);
            SurfaceTexture texture = TextureView.SurfaceTexture;
            if (texture == null)
            {
                throw new Exception("texture is null");
            }
            texture.SetDefaultBufferSize(TextureView.Width, TextureView.Height);
            Surface surface = new Surface(texture);
            requestBuilder =
                HelperManager.CameraHelper.CameraDevice.CreateCaptureRequest(CameraTemplate.Record);
            requestBuilder.AddTarget(surface);
            requestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Off);
            requestBuilder.Set(CaptureRequest.LensFocalLength, cameraSetting.Focus);

            List<Surface> surfaces = new List<Surface>
            {
                surface,
                imageReader.Surface
            };

            HelperManager.CameraHelper.CameraDevice.CreateCaptureSession(
                    surfaces,
                    HelperManager.CameraHelper.CameraSessionCallback,
                    null);
        }

        private void StartCapture()
        {
            HelperManager.CameraHelper.CaptureSession
                .SetRepeatingRequest(requestBuilder.Build(), HelperManager.CameraHelper.CaptureCallback, backgroundHandler);
        }

        public void CloseCamera()
        {
            HelperManager.CameraHelper.CloseCamera();
            imageReader.Close();
            imageReader = null;
        }

        public void OnImageAvailable(ImageReader reader)
        {
            //Image image = reader.AcquireNextImage();
            //string timestamp = image.Timestamp.ToString();
            //curTimestamp = timestamp;
            //ThreadPool.QueueUserWorkItem((o) =>
            //{
            //    Java.Nio.ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            //    byte[] bytes = new byte[buffer.Remaining()];
            //    buffer.Get(bytes);
            //    Bitmap graySclaeBitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length).ToGraySacle();
            //    FileStream file = File.Create(FileSaver.Path + timestamp + ".png");
            //    graySclaeBitmap.Compress(Bitmap.CompressFormat.Png, 80, file);
            //    file.Close();
            //    if (HelperManager.CameraHelper.State != CameraState.Record
            //    && timestamp == curTimestamp)
            //    {
            //        Activity.RunOnUiThread(() => Toast.MakeText(Activity, "Complete", ToastLength.Short).Show());
            //    }
            //    image.Close();
            //});
        }

        public void UpdateFps(int fps)
        {
            fpsTextView.Text = fps + " fps";
        }

    }
}