using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySLAM.Xamarin.Helpers
{
    public class MySensorHelper : Java.Lang.Object, ISensorEventListener
    {
        public delegate void Callback(object data);
        public event Callback ProcessSensorData;
        public long Timestamp;

        public enum ModeType
        {
            Close, Record, AR, Calibrate
        }
        public ModeType Mode { get; private set; }

        private SensorManager sensorManager;
        private Sensor linearAccel;
        private Sensor accel;
        private Sensor gyro;
        private Sensor rotate;
        private Sensor grav;
        private Sensor magnet;
        private Sensor pressure;

        private IList<float> accelData;
        private IList<float> gyroData;
        private IList<float> rotateData;
        private IList<float> gravData;
        private IList<float> magnetData;
        private IList<float> pressureData;

        public MySensorHelper(Activity owner)
        {
            sensorManager = (SensorManager)owner.GetSystemService(Context.SensorService);
            linearAccel = sensorManager.GetDefaultSensor(SensorType.LinearAcceleration);
            accel = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            gyro = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
            rotate = sensorManager.GetDefaultSensor(SensorType.RotationVector);
            grav = sensorManager.GetDefaultSensor(SensorType.Gravity);
            magnet = sensorManager.GetDefaultSensor(SensorType.MagneticField);
            pressure = sensorManager.GetDefaultSensor(SensorType.Pressure);
            Mode = ModeType.Close;
        }

        private void Register(Handler handler = null)
        {
            Timestamp = 0;
            SensorDelay sensorRate = (SensorDelay)AppSetting.IMUFreq;
            switch (Mode)
            {
                case ModeType.Record:
                    sensorManager.RegisterListener(this, accel, sensorRate, handler);
                    sensorManager.RegisterListener(this, gyro, sensorRate, handler);
                    sensorManager.RegisterListener(this, pressure, sensorRate, handler);
                    break;
                case ModeType.AR:
                    sensorManager.RegisterListener(this, linearAccel, sensorRate, handler);
                    sensorManager.RegisterListener(this, rotate, sensorRate, handler);
                    sensorManager.RegisterListener(this, grav, sensorRate, handler);
                    sensorManager.RegisterListener(this, magnet, sensorRate, handler);
                    break;
                case ModeType.Calibrate:
                    sensorManager.RegisterListener(this, grav, sensorRate, handler);
                    sensorManager.RegisterListener(this, linearAccel, sensorRate, handler);
                    sensorManager.RegisterListener(this, magnet, sensorRate, handler);
                    break;
            }
        }
        public void Register(ModeType mode, Callback callback, Handler handler = null)
        {
            if (Mode != ModeType.Close) return; else Mode = mode;
            Register(handler);
            ProcessSensorData = callback;
        }
        public void UnRegister()
        {
            if (Mode == ModeType.Close) return;
            sensorManager.UnregisterListener(this);
            Mode = ModeType.Close;
            accelData = gyroData = gravData = magnetData = null;
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }
        public void OnSensorChanged(SensorEvent e)
        {
            long timestamp;
            switch (e.Sensor.Type)
            {
                case SensorType.LinearAcceleration:
                case SensorType.Accelerometer:
                    timestamp = e.Timestamp;
                    accelData = e.Values;
                    break;
                case SensorType.Gyroscope:
                    gyroData = e.Values;
                    return;
                case SensorType.RotationVector:
                    rotateData = e.Values;
                    return;
                case SensorType.Gravity:
                    gravData = e.Values;
                    return;
                case SensorType.MagneticField:
                    magnetData = e.Values;
                    return;
                case SensorType.Pressure:
                    pressureData = e.Values;
                    return;
                default:
                    return;
            }
            switch (Mode)
            {
                case ModeType.Close:
                    break;
                case ModeType.Record when gyroData != null && pressureData != null:
                    ProcessSensorData((timestamp, gyroData, accelData, pressureData));
                    break;
                case ModeType.AR when rotateData != null:
                {
                    
                    float[] RT = new float[16],
                                    T = new float[4],
                                    A = accelData.Append(0).ToArray();
                    SensorManager.GetRotationMatrixFromVector(RT, rotateData.ToArray());

                    float[] inv = new float[16];
                    //OpenGL Matrix based on Columns
                    Matrix.InvertM(inv, 0, RT, 0);
                    Matrix.MultiplyMV(T, 0, inv, 0, A, 0);
                    RT[3] = T[0];
                    RT[7] = T[1];
                    RT[11] = T[2];
                    byte[] raw = BitConverter.GetBytes(timestamp);
                    RT[12] = BitConverter.ToSingle(raw, 0);
                    RT[13] = BitConverter.ToSingle(raw, 4);
                    ProcessSensorData(RT);
                    break;
                }
                case ModeType.Calibrate when gravData != null && magnetData != null:
                {
                    float[] RT = new float[16],
                            T = new float[4],
                            A = accelData.Append(0).ToArray();
                    SensorManager.GetRotationMatrix(RT, null, gravData.ToArray(), magnetData.ToArray());
                    float[] inv = new float[16];
                    //OpenGL Matrix based on Columns
                    Matrix.InvertM(inv, 0, RT, 0);
                    Matrix.MultiplyMV(T, 0, inv, 0, A, 0);
                    ProcessSensorData(T.Take(3).ToArray());
                    break;
                }
                default:
                    break;
            }
            Timestamp = timestamp;
        }

        public int GetWindowSize()
        {
            switch ((SensorDelay)AppSetting.IMUFreq)
            {
                case SensorDelay.Fastest:
                    return 5;
                case SensorDelay.Game:
                    return 3;
                case SensorDelay.Normal:
                    return 2;
                case SensorDelay.Ui:
                    return 1;
                default:
                    return 1;
            }
        }
    }
}