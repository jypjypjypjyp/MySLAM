using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MySLAM.Xamarin.MyHelper;
using MySLAM.Xamarin.MyView;
using Fragment = Android.App.Fragment;

namespace MySLAM.Xamarin
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        private Fragment curFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            AppSetting.Init(this);
            HelperManager.PermissionHelper = new MyPermissionHelper(this);
            if (HelperManager.PermissionHelper.ConfirmPermissions(
                    new string[]{
                        Manifest.Permission.WriteExternalStorage,
                        Manifest.Permission.Camera
                    }, 1))
            {
                curFragment = MyCameraFragment.Instance;
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, MyCameraFragment.Instance).Commit();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_settings:
                    {
                        if (curFragment == null
                            || HelperManager.CameraHelper?.State == CameraState.Record)
                        {
                            Toast.MakeText(this, "Setting is unavailble right now!", ToastLength.Short).Show();
                        }
                        HelperManager.CameraHelper.State = CameraState.Close;
                        curFragment = new MyPreferenceFragment();
                        FragmentManager.BeginTransaction().Replace(Resource.Id.container, curFragment).Commit();
                        break;
                    }
                default: break;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back && curFragment is MyPreferenceFragment)
            {
                curFragment = MyCameraFragment.Instance;
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, MyCameraFragment.Instance).Commit();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != 1)
            {
                return;
            }
            foreach (int i in grantResults)
            {
                if (i != (int)Permission.Granted)
                {
                    new MyDialog(DialogType.Error, "Get permission Failed!!")
                    {
                        PositiveHandler = (o, e) =>
                        {
                            Finish();
                        }
                    }.Show(FragmentManager, "Error!!!");
                    return;
                }
            }
            curFragment = MyCameraFragment.Instance;
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, MyCameraFragment.Instance).Commit();
        }
    }
}