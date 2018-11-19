using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Org.Opencv.Android;
using Org.Opencv.Core;
using System;
namespace MySLAM.Xamarin
{
    public class MyARFragment : Fragment,
                                ILoaderCallbackInterface,
                                CameraBridgeViewBase.ICvCameraViewListener2
    {
        public JavaCamera2View Camera2View;
      
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.ar_fragment, container, false);
        }
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            Camera2View = view.FindViewById<JavaCamera2View>(Resource.Id.ar_view);
            Camera2View.SetCvCameraViewListener2(this);
        }

        public override void OnResume()
        {
            base.OnResume();
            if (!OpenCVLoader.InitDebug())
            {
                Log.Debug("OpenCV4Android", "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, Activity, this);
            }
            else
            {
                Log.Debug("OpenCV4Android", "OpenCV library found inside package. Using it!");
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }
        public override void OnPause()
        {
            base.OnPause();
            if (Camera2View != null)
            {
                Camera2View.DisableView();
            }
        }

        #region ICvCameraViewListener2
        public Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame p0)
        {
            return p0.Gray();
        }
        public void OnCameraViewStarted(int p0, int p1)
        {
        }
        public void OnCameraViewStopped()
        {
        }
        #endregion

        #region ILoaderCallbackInterface
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
        #endregion
    }
}