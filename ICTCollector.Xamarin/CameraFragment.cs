using Android.App;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Views;
using ICTCollector.Xamarin.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
        private ImageReader imageReader;
        private Handler backgroundHandler;
        private HandlerThread backgroundThread;
        private Handler saveImageHandler;
        private HandlerThread saveImageThread;
        private CaptureRequest.Builder requestBuilder;

        private string markedTimestampList;
        private string curTimestamp;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HelperManager.CameraHelper = new MyCameraHelper(Activity);
            backgroundThread = new HandlerThread("ReaptingCapture");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
            saveImageThread = new HandlerThread("SaveImage");
            saveImageThread.Start();
            saveImageHandler = new Handler(saveImageThread.Looper);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.camera_fragment, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            TextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            view.FindViewById(Resource.Id.button_record).Click += Record;
            view.FindViewById(Resource.Id.button_mark).Click += Mark;
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
                markedTimestampList = "";
                ImageSaver.Path = 
                    Android.OS.Environment.ExternalStorageDirectory.AbsolutePath
                    + "/ICTCollector/"
                    + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "/";
                Directory.CreateDirectory(ImageSaver.Path);
                HelperManager.CameraHelper.CaptureSession.StopRepeating();
                requestBuilder.AddTarget(imageReader.Surface);
                HelperManager.CameraHelper.CaptureSession
                    .SetRepeatingRequest(requestBuilder.Build(), null, backgroundHandler);
                HelperManager.CameraHelper.State = CameraState.Record;
            }
            else
            {
                HelperManager.CameraHelper.CaptureSession.StopRepeating();
                requestBuilder.RemoveTarget(imageReader.Surface);
                HelperManager.CameraHelper.CaptureSession
                    .SetRepeatingRequest(requestBuilder.Build(), null, backgroundHandler);
                HelperManager.CameraHelper.State = CameraState.Off;
                File.WriteAllText(ImageSaver.Path + "marked.txt", markedTimestampList);
                markedTimestampList = "";
                curTimestamp = null;
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            if (TextureView.IsAvailable)
            {
                OpenCamera();
            }
            else
            {
                TextureView.SurfaceTextureAvailable +=
                (object sender, TextureView.SurfaceTextureAvailableEventArgs e) =>
                {
                    OpenCamera();
                };
            }

        }

        public override void OnPause()
        {
            CloseCamera();
            base.OnPause();
        }

        public void OpenCamera()
        {
            CameraSetting cameraSetting = CameraSetting.GetCameraSetting(Activity);
            TextureView.SetAspectRatio(cameraSetting.Size.Width, cameraSetting.Size.Height);
            HelperManager.CameraHelper.CameraStateCallback.CreateSession
                += CreateSession;
            HelperManager.CameraHelper.OpenCamera(cameraSetting);

            imageReader = ImageReader.NewInstance(cameraSetting.Size.Width, cameraSetting.Size.Height, ImageFormatType.Jpeg, 3);
            imageReader.SetOnImageAvailableListener(this, saveImageHandler);
        }

        public void CreateSession()
        {
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
            requestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);

            List<Surface> surfaces = new List<Surface>
            {
                surface,
                imageReader.Surface
            };
            HelperManager.CameraHelper.CameraSessionCallback.StartCapture += StartCapture;
            HelperManager.CameraHelper.CameraDevice.CreateCaptureSession(
                    surfaces,
                    HelperManager.CameraHelper.CameraSessionCallback,
                    null);
        }

        private void StartCapture()
        {
            HelperManager.CameraHelper.CaptureSession
                .SetRepeatingRequest(requestBuilder.Build(), null, backgroundHandler);
        }

        public void CloseCamera()
        {
            HelperManager.CameraHelper.CloseCamera();
            imageReader.Close();
            imageReader = null;
        }

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = reader.AcquireNextImage();
            curTimestamp = image.Timestamp.ToString();
            ThreadPool.QueueUserWorkItem((o) =>
            {
                ImageSaver.SaveImage(image, image.Timestamp.ToString() + ".png");
            });
        }

    }
}