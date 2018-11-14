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
using MySLAM.Xamarin.MyHelper;
using MySLAM.Xamarin.MyView;
using System;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;
using Fragment = Android.App.Fragment;

namespace MySLAM.Xamarin
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait)]
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
                    FragmentManager.BeginTransaction().Replace(Resource.Id.container, curFragment).Commit();
                }
            }
        }
        private Fragment curFragment;
        public IMenu NavigationMenu { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            var toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            var navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);
            NavigationMenu = navigationView.Menu;

            CurFragment = new MyInfoFragment();

            HelperManager.Init(this);
            AppSetting.Init(this);
            HelperManager.PermissionHelper = new MyPermissionHelper(this);
            if (!HelperManager.PermissionHelper.ConfirmPermissions(
                    new string[]{
                        Manifest.Permission.WriteExternalStorage,
                        Manifest.Permission.Camera
                    }, 1))
            {
                NavigationMenu.FindItem(Resource.Id.action_recorder).SetEnabled(false);
                NavigationMenu.FindItem(Resource.Id.action_ar).SetEnabled(false);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return base.OnOptionsItemSelected(item);
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
                if (!(CurFragment is MyInfoFragment))
                {
                    CurFragment = new MyRecorderFragment();
                }
                else
                {
                    base.OnBackPressed();
                }
            }
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            var view = (View)sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_info:
                    CurFragment = new MyInfoFragment();
                    break;
                case Resource.Id.action_settings:
                    CurFragment = new MyPreferenceFragment();
                    break;
                case Resource.Id.action_recorder:
                    CurFragment = new MyRecorderFragment();
                    break;
                case Resource.Id.action_ar:
                    CurFragment = new MyInfoFragment();
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
            NavigationMenu.FindItem(Resource.Id.action_recorder).SetEnabled(true);
            NavigationMenu.FindItem(Resource.Id.action_ar).SetEnabled(true);
        }
    }
}