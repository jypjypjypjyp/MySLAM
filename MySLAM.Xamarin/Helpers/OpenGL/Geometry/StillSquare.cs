using Android.Opengl;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class StillSquare : GLSimpleEntity
    {
        private readonly float z;
        public StillSquare(float z)
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