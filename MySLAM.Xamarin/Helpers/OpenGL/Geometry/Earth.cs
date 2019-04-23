using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class Earth : GLEarth
    {
        private readonly float z;
        public Earth(float z)
        {
            this.z = z;
        }

        private float[] _MMat = new float[16];
        private float[] _MVPMat = new float[16];
        protected override float[] UpdateMVPMat(float[] VPMat)
        {
            Matrix.SetIdentityM(_MMat, 0);
            Matrix.TranslateM(_MMat, 0, 0, 0, z);
            Matrix.MultiplyMM(_MVPMat, 0, VPMat, 0, _MMat, 0);
            return _MVPMat;
        }
    }
}