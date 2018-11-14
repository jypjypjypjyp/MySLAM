using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Org.Opencv.Android;
using Org.Opencv.Core;

namespace OpenCV4Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity,
                                ILoaderCallbackInterface,
                                CameraBridgeViewBase.ICvCameraViewListener2
    {
        public JavaCamera2View Camera2View;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Camera2View = FindViewById<JavaCamera2View>(Resource.Id.camera_view);
            Camera2View.Visibility = ViewStates.Visible;
            Camera2View.SetCvCameraViewListener2(this);

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != 1)
            {
                return;
            }

            if (permissions[0] == Manifest.Permission.Camera && grantResults[0] == Permission.Granted)
            {
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
            else
            {
                Finish();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug("OpenCV4Android", "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, this);
            }
            else
            {
                Log.Debug("OpenCV4Android", "OpenCV library found inside package. Using it!");
                var permissionHelper = new MyHelper.MyPermissionHelper(this);
                if(permissionHelper.ConfirmPermissions(new string[] { Manifest.Permission.Camera }, 1))
                {
                    OnManagerConnected(LoaderCallbackInterface.Success);
                }
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (Camera2View != null)
            {
                Camera2View.DisableView();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Camera2View != null)
            {
                Camera2View.DisableView();
            }
        }

        public void OnManagerConnected(int p0)
        {
            switch (p0)
            {
                case LoaderCallbackInterface.Success:
                    Log.Info("OpenCV4Android", "OpenCV loaded successfully");
                    Camera2View.EnableView();
                    break;
                default:
                    break;
            }
        }

        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {
        }

        public Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame p0)
        {
            return p0.Rgba();
        }

        public void OnCameraViewStarted(int p0, int p1)
        {
        }

        public void OnCameraViewStopped()
        {
        }
    }
}