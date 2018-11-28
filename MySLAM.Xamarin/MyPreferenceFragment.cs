using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.OS;
using Android.Preferences;
using Android.Widget;
using MySLAM.Xamarin.MyHelper;
using System;

namespace MySLAM.Xamarin
{
    public class MyPreferenceFragment : PreferenceFragment, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private CameraManager manager;
        private ISharedPreferences sharedPreferences;

        public MyPreferenceFragment() { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Menu.settings);
            sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Activity);
            sharedPreferences.RegisterOnSharedPreferenceChangeListener(this);
            // Get our exit button
            PreferenceManager.FindPreference("exitlink")
                .PreferenceClick += (a, arg) =>
                {
                    ((MainActivity)Activity).CurFragment = new MyRecorderFragment();
                };
            // Init cameraList
            manager = HelperManager.CameraHelper.Manager;
            InitCameras();
            // Init others
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            try
            {
                UpdataSize();
                UpdateFoucs();
            }
            catch (Exception)
            {
                Toast.MakeText(Activity, "Error!", ToastLength.Short);
            }
        }

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            if (key == "prefCamera")
            {
                UpdateCamera();
            }
        }

        private void InitCameras()
        {
            string[] cameraIdList = manager.GetCameraIdList();
            string[] entries = new string[cameraIdList.Length];
            string[] entriesValues = new string[cameraIdList.Length];
            for (int i = 0; i < cameraIdList.Length; i++)
            {
                string cameraId = cameraIdList[i];
                var characteristics = manager.GetCameraCharacteristics(cameraId);
                switch ((LensFacing)((int)characteristics.Get(CameraCharacteristics.LensFacing)))
                {
                    case LensFacing.Back:
                        {
                            entries[i] = cameraId + " - Lens Facing Back";
                            break;
                        }
                    case LensFacing.Front:
                        {
                            entries[i] = cameraId + " - Lens Front";
                            break;
                        }
                    case LensFacing.External:
                        {
                            entries[i] = cameraId + " - Lens Facing External";
                            break;
                        }
                }
                entriesValues[i] = cameraId;
            }
            // Update our settings entry
            var cameraList = (ListPreference)PreferenceManager.FindPreference("prefCamera");
            cameraList.SetEntries(entries);
            cameraList.SetEntryValues(entriesValues);
            cameraList.SetDefaultValue(entriesValues[0]);
        }

        private void UpdataSize()
        {
            var characteristics = manager
                .GetCameraCharacteristics(sharedPreferences.GetString("prefCamera", "0"));
            var streamConfigurationMap =
                (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            var sizes = streamConfigurationMap
                .GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture)));
            int rezSize = sizes.Length;
            string[] rez = new string[rezSize];
            string[] rezValues = new string[rezSize];
            for (int i = 0; i < sizes.Length; i++)
            {
                rez[i] = sizes[i].Width + "x" + sizes[i].Height;
                rezValues[i] = sizes[i].Width + "x" + sizes[i].Height;
            }
            var cameraRez = (ListPreference)PreferenceManager.FindPreference("prefSizeRaw");
            cameraRez.SetEntries(rez);
            cameraRez.SetEntryValues(rezValues);
            cameraRez.SetDefaultValue(rezValues[0]);
        }

        private void UpdateFoucs()
        {
            var characteristics = manager
                .GetCameraCharacteristics(sharedPreferences.GetString("prefCamera", "0"));
            float[] focus_lengths = (float[])characteristics.Get(CameraCharacteristics.LensInfoAvailableFocalLengths);
            string[] focuses = new string[focus_lengths.Length];
            for (int i = 0; i < focus_lengths.Length; i++)
            {
                focuses[i] = focus_lengths[i] + "";
            }
            var cameraFocus = (ListPreference)PreferenceManager.FindPreference("prefFocusLength");
            cameraFocus.SetEntries(focuses);
            cameraFocus.SetEntryValues(focuses);
            cameraFocus.SetDefaultValue(focuses[0]);
            cameraFocus.SetValueIndex(0);
        }
    }
}