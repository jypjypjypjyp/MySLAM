using System;

using Android;
using Android.App;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MySLAM.Xamarin.Helpers
{
    public class MyLocationHelper : Java.Lang.Object, ILocationListener
    {
        public enum LocationState
        {
            Disabled, Available, On
        }
        public LocationState State { get; set; }

        private LocationManager locationManager;
        private Criteria criteria;
        private Action<Location> action;

        public MyLocationHelper(Activity owner)
        {
            State = ContextCompat.CheckSelfPermission(owner, Manifest.Permission.AccessFineLocation) == Permission.Granted
            && (locationManager = owner.GetSystemService(Activity.LocationService) as LocationManager) != null ? LocationState.Available : LocationState.Disabled;
            criteria = new Criteria()
            {
                Accuracy = Accuracy.Fine,
                SpeedRequired = true,
                CostAllowed = false,
                BearingRequired = true,
                AltitudeRequired = true,
                PowerRequirement = Power.Medium
            };
        }

        public bool Start(Action<Location> action)
        {
            switch (State)
            {
                case LocationState.Disabled:
                    return false;
                case LocationState.Available:
                    this.action = action;
                    locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 100, 1f, this);
                    State = LocationState.On;
                    return true;
                case LocationState.On:
                    return true;
                default:
                    return false;
            }
        }

        public void Stop()
        {
            if(State == LocationState.On)
            {
                locationManager.RemoveUpdates(this);
                State = LocationState.Available;
            }
        }

        public void OnLocationChanged(Location location)
        {
            action(location);
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
        }

    }
}