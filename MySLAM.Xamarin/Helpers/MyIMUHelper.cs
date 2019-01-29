using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using System.Linq;
using System;
using System.Collections.Generic;

namespace MySLAM.Xamarin.Helpers
{
    public class MyIMUHelper : Java.Lang.Object, ISensorEventListener
    {
        public delegate void Callback1(string data);
        public delegate void Callback2(float[] data);
        public event Callback1 ProcessSensorData1;
        public event Callback2 ProcessSensorData2;
        public long Timestamp;
        public int Mode { get; private set; }

        private SensorManager sensorManager;
        private Sensor accel;
        private Sensor gyro;
        private Sensor grav;
        private Sensor magnet;

        private IList<float> accelData;
        private IList<float> gyroData;
        private IList<float> gravData;
        private IList<float> magnetData;

        public MyIMUHelper(Activity owner)
        {
            sensorManager = (SensorManager)owner.GetSystemService(Context.SensorService);
            accel = sensorManager.GetDefaultSensor(SensorType.LinearAcceleration);
            gyro = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
            grav = sensorManager.GetDefaultSensor(SensorType.Gravity);
            magnet = sensorManager.GetDefaultSensor(SensorType.MagneticField);
            Mode = 0;
        }

        private void Register(Handler handler = null)
        {
            Timestamp = 0;
            var sensorRate = (SensorDelay)AppSetting.IMUFreq;
            switch (Mode)
            {
                case 1:
                    sensorManager.RegisterListener(this, gyro, sensorRate, handler);
                    goto default;
                case 2:
                    sensorManager.RegisterListener(this, grav, sensorRate, handler);
                    sensorManager.RegisterListener(this, magnet, sensorRate, handler);
                    goto default;
                default:
                    sensorManager.RegisterListener(this, accel, sensorRate, handler);
                    break;
            }
        }
        public void Register(Callback1 callback, Handler handler = null)
        {
            if (Mode != 0) return; else Mode = 1;
            Register(handler);
            ProcessSensorData1 = callback;
        }
        public void Register(Callback2 callback, Handler handler = null)
        {
            if (Mode != 0) return; else Mode = 2;
            Register(handler);
            ProcessSensorData2 = callback;
        }
        public void UnRegister()
        {
            if (Mode == 0) return;
            sensorManager.UnregisterListener(this);
            Mode = 0;
            accelData = gyroData = gravData = magnetData = null;
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }
        public void OnSensorChanged(SensorEvent e)
        {
            long timestamp;
            lock (this)
            {
                switch (e.Sensor.Type)
                {
                    case SensorType.LinearAcceleration:
                        timestamp = e.Timestamp;
                        accelData = e.Values;
                        break;
                    case SensorType.Gyroscope:
                        gyroData = e.Values;
                        return;
                    case SensorType.Gravity:
                        gravData = e.Values;
                        return;
                    case SensorType.MagneticField:
                        magnetData = e.Values;
                        return;
                    default:
                        return;
                }
            }
            if (Mode == 1 && gyroData != null)
            {
                string data = timestamp + "," + accelData[0] + "," + accelData[1] + "," + accelData[2] + ","
                            + gyroData[0] + "," + gyroData[1] + "," + gyroData[2];
                ProcessSensorData1(data);
            }
            if (Mode == 2 && gravData != null && magnetData != null)
            {
                // Change the device relative acceleration values to earth relative values
                // X axis -> East
                // Y axis -> North Pole
                // Z axis -> Sky
                float[] RT = new float[16],
                        T = new float[4];
                SensorManager.GetRotationMatrix(RT, null, gravData.ToArray(), magnetData.ToArray());

                float[] inv = new float[16];
                //OpenGL Matrix based on Columns
                Matrix.InvertM(inv, 0, RT, 0);
                Matrix.MultiplyMV(T, 0, inv, 0, accelData.Append(0).ToArray(), 0);
                RT[3] = T[0];
                RT[7] = T[1];
                RT[11] = T[2];
                byte[] raw = BitConverter.GetBytes(timestamp);
                RT[12] = BitConverter.ToSingle(raw, 0);
                RT[13] = BitConverter.ToSingle(raw, 4);
                ProcessSensorData2(RT);
                Timestamp = timestamp;
            }
        }
    }
}