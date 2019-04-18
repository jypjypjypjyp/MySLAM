using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using MySLAM.Xamarin.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace MySLAM.Xamarin
{
    public class MyInfoFragment : Fragment
    {
        Button onlyIMUBtn;
        bool isRecording;
        string path;
        string sensorDataString;
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.info_frag, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            onlyIMUBtn = view.FindViewById<Button>(Resource.Id.only_imu);
            onlyIMUBtn.Click += OnlyIMUBtn_Click;
        }

        private void OnlyIMUBtn_Click(object sender, System.EventArgs e)
        {
            if (isRecording)
            {
                onlyIMUBtn.Text = "Start Only IMU";
                HelperManager.SensorHelper.UnRegister();
                File.WriteAllText(path + "sensor0.csv", sensorDataString);
                isRecording = false;
            }
            else
            {
                onlyIMUBtn.Text = "Stop Only IMU";
                path =
                    AppConst.RootPath
                    + DateTime.Now.ToString("yyMMddHHmmss") + "/";
                Directory.CreateDirectory(path);
                File.AppendAllText(path + "sensor0.csv",
                    "#timestamp,omega_x,omega_y,omega_z,alpha_x,alpha_y,alpha_z,pressure");
                sensorDataString = "";
                HelperManager.SensorHelper.Register(MySensorHelper.ModeType.Record, ProcessSensorData);
                isRecording = true;
            }
        }

        private void ProcessSensorData(object data)
        {
            var record = ((long, IList<float>, IList<float>, IList<float>))data;
            sensorDataString += record.Item1 + ",";
            sensorDataString += string.Join(',', record.Item2) + ",";
            sensorDataString += string.Join(',', record.Item3) + "\n";
            //sensorDataString += string.Join(',', record.Item4) + "\n";
            if (sensorDataString.Length > 1e5)
            {
                File.AppendAllText(path + "sensor0.csv", sensorDataString);
                sensorDataString = "";
            }
        }
    }
}