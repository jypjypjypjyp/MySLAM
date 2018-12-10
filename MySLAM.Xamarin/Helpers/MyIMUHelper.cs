using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using System.Collections.Generic;

namespace MySLAM.Xamarin.Helpers
{
    public class MyIMUHelper : Java.Lang.Object, ISensorEventListener
    {
        public delegate void Callback(string data);
        public event Callback ProcessSensorData;

        private readonly Activity owner;

        private SensorManager sensorManager;
        private readonly Sensor accel;
        private readonly Sensor gyro;

        private long accelTimestamp;
        private IList<float> accelData;
        private long gyroTimestamp;
        private IList<float> gyroData;

        public MyIMUHelper(Activity owner)
        {
            this.owner = owner;
            sensorManager = (SensorManager)owner.GetSystemService(Context.SensorService);
            accel = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            gyro = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
        }

        public void Register(Callback callback = null, Handler handler = null)
        {
            sensorManager.RegisterListener(this, accel, (SensorDelay)AppSetting.IMUFreq, handler);
            sensorManager.RegisterListener(this, gyro, (SensorDelay)AppSetting.IMUFreq, handler);
            if (callback != null)
            {
                ProcessSensorData += callback;
            }
        }

        public void UnRegister()
        {
            sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                accelTimestamp = e.Timestamp;
                accelData = e.Values;
            }
            else if (e.Sensor.Type == SensorType.Gyroscope)
            {
                gyroTimestamp = e.Timestamp;
                gyroData = e.Values;
            }
            else
            {
                return;
            }

            if (accelTimestamp != 0 && gyroTimestamp != 0)
            {
                string data = accelTimestamp + "," + accelData[0] + "," + accelData[1] + "," + accelData[2] + ","
                            + gyroData[0] + "," + gyroData[1] + "," + gyroData[2];
                ProcessSensorData(data);
            }
        }
    }
}