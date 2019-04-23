using Android.Opengl;
using Java.Nio;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public abstract class GLEntity
    {
        protected abstract Shader Program { get; }
        public abstract void Draw(float[] viewMat);
    }
}