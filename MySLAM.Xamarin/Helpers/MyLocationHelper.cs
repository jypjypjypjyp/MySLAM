using System;

using Android;
using Android.App;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MySLAM.Xamarin.Helpers
{
    public class MyLocationHelper
    {
        public delegate void ProcessGNSSData(Location location);

        private bool isAvailable;
        private LocationManager locationManager;

        private GnssMeasurementsEventCallback _GnssMECallback;
        private GnssNavigationMessageCallback _GnssNMCallback;
        private GnssStatusCallback _GnssSCallback;

        public MyLocationHelper(Activity owner)
        {
            locationManager = owner.GetSystemService(Activity.LocationService) as LocationManager;
            isAvailable = locationManager.AllProviders.Contains(LocationManager.NetworkProvider)
                && locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
            _GnssMECallback = new GnssMeasurementsEventCallback();
            _GnssNMCallback = new GnssNavigationMessageCallback();
            _GnssSCallback = new GnssStatusCallback();
        }

        public bool Start(Action<GnssMeasurementsEvent> action1 = null,
            Action<GnssNavigationMessage> action2 = null,
            Action<GnssStatus> action3 = null)
        {
            if (!isAvailable) return false;
            _GnssMECallback.Action = action1;
            _GnssNMCallback.Action = action2;
            _GnssSCallback.Action = action3;
            locationManager.RegisterGnssMeasurementsCallback(_GnssMECallback);
            locationManager.RegisterGnssNavigationMessageCallback(_GnssNMCallback);
            locationManager.RegisterGnssStatusCallback(_GnssSCallback);
            return true;
        }

        public bool Stop()
        {
            if (!isAvailable) return false;
            locationManager.UnregisterGnssMeasurementsCallback(_GnssMECallback);
            locationManager.UnregisterGnssNavigationMessageCallback(_GnssNMCallback);
            locationManager.UnregisterGnssStatusCallback(_GnssSCallback);
            return true;
        }

        class GnssMeasurementsEventCallback : GnssMeasurementsEvent.Callback
        {
            public Action<GnssMeasurementsEvent> Action;
            public override void OnGnssMeasurementsReceived(GnssMeasurementsEvent eventArgs)
            {
                Action(eventArgs);
            }
        }
        class GnssNavigationMessageCallback : GnssNavigationMessage.Callback
        {
            public Action<GnssNavigationMessage> Action;
            public override void OnGnssNavigationMessageReceived(GnssNavigationMessage e)
            {
                Action(e);
            }
        }
        class GnssStatusCallback : GnssStatus.Callback
        {
            public Action<GnssStatus> Action;
            public override void OnSatelliteStatusChanged(GnssStatus status)
            {
                Action(status);
            }
        }

    }
}