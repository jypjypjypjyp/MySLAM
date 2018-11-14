using Android.App;

namespace MySLAM.Xamarin.MyHelper
{
    public static class HelperManager
    {
        public static void Init(Activity context)
        {
            if (CameraHelper == null) CameraHelper = new MyCameraHelper(context);
            if (PermissionHelper == null) PermissionHelper = new MyPermissionHelper(context);
            if (IMUHelper == null) IMUHelper = new MyIMUHelper(context);
        }
        public static MyCameraHelper CameraHelper;
        public static MyPermissionHelper PermissionHelper;
        public static MyIMUHelper IMUHelper;
    }
}