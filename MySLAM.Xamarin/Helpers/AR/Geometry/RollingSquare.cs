using Android.Opengl;
using System;

namespace MySLAM.Xamarin.Helpers.AR
{
    public class RollingSquare : GLSimpleEntity
    {
        private long startTime;
        private readonly int _T;
        private readonly float x0;
        private float s;
        private float x;

        public RollingSquare(int Ts, float x0, float s)
        {
            if (Ts <= 0) throw new ArgumentException("Time can not be negative!", "Ts");
            _T = (int)(Ts * 1e7);
            this.x0 = x0;
            this.s = s;
            x = x0;
            x0 = (x0 < 0) ? -x0 : x0;
        }

        private float[] _MMat = new float[16];
        private float[] _MVPMat = new float[16];
        protected override float[] UpdateMVPMat(float[] VPMat)
        {
            if (startTime == default(long))
                startTime = DateTime.Now.ToFileTimeUtc();
            var dt = DateTime.Now.ToFileTimeUtc() - startTime;
            Matrix.SetIdentityM(_MMat, 0);
            Matrix.TranslateM(_MMat, 0, x += (x < -x0 || x > x0) ? s *= -1 : s, 0, -5);
            Matrix.RotateM(_MMat, 0, 360 * dt / _T, 0, 1, 0);
            Matrix.MultiplyMM(_MVPMat, 0, VPMat, 0, _MMat, 0);
            return _MVPMat;
        }
    }
}

