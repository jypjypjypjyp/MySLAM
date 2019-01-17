using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using System.Linq;
using System;

namespace MySLAM.Xamarin.Helpers
{
    public class MyIMUHelper : Java.Lang.Object, ISensorEventListener
    {
        public delegate void Callback1(string data);
        public delegate void Callback2(float[] data);
        public event Callback1 ProcessSensorData1 = delegate { };
        public event Callback2 ProcessSensorData2 = delegate { };
        public long Timestamp;

        private readonly Activity owner;

        private SensorManager sensorManager;
        private readonly Sensor accel;
        private readonly Sensor gyro;
        private readonly Sensor grav;
        private readonly Sensor magnet;

        private float[] accelData;
        private float[] gyroData;
        private float[] gravData;
        private float[] magnetData;
        private bool isRegistered = false;

        public MyIMUHelper(Activity owner)
        {
            this.owner = owner;
            sensorManager = (SensorManager)owner.GetSystemService(Context.SensorService);
            accel = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            gyro = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
            grav = sensorManager.GetDefaultSensor(SensorType.Gravity);
            magnet = sensorManager.GetDefaultSensor(SensorType.MagneticField);
        }

        public void Register(Handler handler = null)
        {
            if (!isRegistered)
            {
                sensorManager.RegisterListener(this, accel, SensorDelay.Fastest, handler);
                sensorManager.RegisterListener(this, grav, SensorDelay.Fastest, handler);
                sensorManager.RegisterListener(this, magnet, SensorDelay.Fastest, handler);
                sensorManager.RegisterListener(this, gyro, SensorDelay.Fastest, handler);

                accelData = null;
                gyroData = null;
                gravData = null;
                magnetData = null;

                isRegistered = true;
            }
        }
        public void Register(Callback1 callback, Handler handler = null)
        {
            Register(handler);
            ProcessSensorData1 += callback;
        }
        public void Register(Callback2 callback, Handler handler = null)
        {
            Register(handler);
            ProcessSensorData2 += callback;
        }

        public void UnRegister()
        {
            sensorManager.UnregisterListener(this);
            isRegistered = false;
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent e)
        {
            switch (e.Sensor.Type)
            {
                case SensorType.Accelerometer:
                    Timestamp = e.Timestamp;
                    accelData = e.Values.ToArray();
                    break;
                case SensorType.Gyroscope:
                    gyroData = e.Values.ToArray();
                    return;
                case SensorType.Gravity:
                    gravData = e.Values.ToArray();
                    return;
                case SensorType.MagneticField:
                    magnetData = e.Values.ToArray();
                    return;
                default:
                    return;
            }
            if (gyroData != null)
            {
                string data = Timestamp + "," + accelData[0] + "," + accelData[1] + "," + accelData[2] + ","
                            + gyroData[0] + "," + gyroData[1] + "," + gyroData[2];
                ProcessSensorData1(data);
            }
            if ((gravData != null) && (magnetData != null))
            {
                // Change the device relative acceleration values to earth relative values
                // X axis -> East
                // Y axis -> North Pole
                // Z axis -> Sky

                float[] RT = new float[16],
                    T = new float[4],
                    accelVec = accelData.Append(0).ToArray();

                SensorManager.GetRotationMatrix(RT, null, gravData, magnetData);

                float[] inv = new float[16];
                //OpenGL Matrix based on Columns
                Matrix.InvertM(inv, 0, RT, 0);
                Matrix.MultiplyMV(T, 0, inv, 0, accelVec, 0);
                RT[3] = T[0];
                RT[7] = T[1];
                RT[11] = T[2];
                byte[] raw = BitConverter.GetBytes(Timestamp);
                RT[12] = BitConverter.ToSingle(raw, 0);
                RT[13] = BitConverter.ToSingle(raw, 4);
                ProcessSensorData2(RT);
            }
        }
    }
}