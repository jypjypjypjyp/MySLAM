using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using ICTCollector.Xamarin.Helper;

namespace ICTCollector.Xamarin
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Fragment curFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, CameraFragment.Instance).Commit();
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
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, CameraFragment.Instance).Commit();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
    }
}