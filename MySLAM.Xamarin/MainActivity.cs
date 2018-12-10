using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using MySLAM.Xamarin.Helpers;
using MySLAM.Xamarin.Views;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;
using Fragment = Android.App.Fragment;

namespace MySLAM.Xamarin
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        public Fragment CurFragment
        {
            get => curFragment;
            set
            {
                if (!(curFragment?.GetType() == value.GetType()))
                {
                    curFragment = value;
                    if (curFragment is MyARFragment) FindViewById(Resource.Id.toolbar_layout).Visibility = ViewStates.Gone;
                    else FindViewById(Resource.Id.toolbar_layout).Visibility = ViewStates.Visible;
                    FragmentManager.BeginTransaction().Replace(Resource.Id.container, curFragment).Commit();
                }
            }
        }
        private Fragment curFragment;
        public IMenu NavigationMenu { get; set; }
        public IMenu ToolBarMenu { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            var toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            var navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);
            NavigationMenu = navigationView.Menu;

            CurFragment = new MyInfoFragment();

            HelperManager.MainActivity = this;
            AppSetting.Init(this);
            if (!HelperManager.PermissionHelper.ConfirmPermissions(
                    new string[]{
                        Manifest.Permission.WriteExternalStorage,
                        Manifest.Permission.Camera
                    }, 1))
            {
                NavigationMenu.FindItem(Resource.Id.frag_recorder).SetEnabled(false);
                NavigationMenu.FindItem(Resource.Id.frag_ar).SetEnabled(false);
            }
            else
            {
                AppConst.Init();
            }
            ////Getting Bitmap form TextureView has some bug, need to GC manually
            //var timer = new Timer(
            //    (o) =>
            //    {
            //        GC.Collect();
            //    }, null, 0, 1000);
        }

        public override void OnBackPressed()
        {
            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                if (CurFragment is MyPreferenceFragment)
                {
                    CurFragment = new MyRecorderFragment();
                }
                else if (CurFragment is MyRecorderFragment)
                {
                    CurFragment = new MyInfoFragment();
                }
                else if (CurFragment is MyARFragment)
                {
                    CurFragment = new MyInfoFragment();
                }
                else if (CurFragment is MyInfoFragment)
                {
                    Finish();
                }
                else
                {
                    base.OnBackPressed();
                }
            }
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.frag_info:
                    CurFragment = new MyInfoFragment();
                    break;
                case Resource.Id.frag_settings:
                    CurFragment = new MyPreferenceFragment();
                    break;
                case Resource.Id.frag_recorder:
                    CurFragment = new MyRecorderFragment();
                    break;
                case Resource.Id.frag_ar:
                    CurFragment = new MyARFragment();
                    break;
                default: break;

            }
            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
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
                    new MyDialog(DialogType.Error, Resources.GetString(Resource.String.permission_failed))
                    {
                        PositiveHandler = (o, e) =>
                        {
                            Finish();
                        }
                    }.Show(FragmentManager, null);
                    return;
                }
            }
            AppConst.Init();
            NavigationMenu.FindItem(Resource.Id.frag_recorder).SetEnabled(true);
            NavigationMenu.FindItem(Resource.Id.frag_ar).SetEnabled(true);
        }
    }
}