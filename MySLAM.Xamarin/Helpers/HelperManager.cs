using Android.App;

namespace MySLAM.Xamarin.Helpers
{
    public static class HelperManager
    {
        public static Activity MainActivity {
            set
            {
                if (value == _mainActivity) return;
                _mainActivity = value;
                _cameraHelper = null;
                _permissionHelper = null;
                _IMUHelper = null;
                _locationHelper = null;
            }
        }
        public static MyCameraHelper CameraHelper
        {
            get
            {
                if (_cameraHelper == null)
                {
                    _cameraHelper = new MyCameraHelper(_mainActivity);
                }
                return _cameraHelper;
            }
        }
        public static MyPermissionHelper PermissionHelper
        {
            get
            {
                if (_permissionHelper == null)
                {
                    _permissionHelper = new MyPermissionHelper(_mainActivity);
                }
                return _permissionHelper;
            }
        }
        public static MySensorHelper IMUHelper
        {
            get
            {
                if(_IMUHelper == null)
                {
                    _IMUHelper = new MySensorHelper(_mainActivity);
                }
                return _IMUHelper;
            }
        }
        public static MyLocationHelper LocationHelper
        {
            get
            {
                if (_locationHelper == null)
                {
                    _locationHelper = new MyLocationHelper(_mainActivity);
                }
                return _locationHelper;
            }
        }

        public static Activity _mainActivity;
        public static MyCameraHelper _cameraHelper;
        public static MyPermissionHelper _permissionHelper;
        public static MySensorHelper _IMUHelper;
        public static MyLocationHelper _locationHelper;
    }
}