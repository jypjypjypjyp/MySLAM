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
                _SensorHelper = null;
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
        public static MySensorHelper SensorHelper
        {
            get
            {
                if(_SensorHelper == null)
                {
                    _SensorHelper = new MySensorHelper(_mainActivity);
                }
                return _SensorHelper;
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
        public static MySensorHelper _SensorHelper;
        public static MyLocationHelper _locationHelper;
    }
}