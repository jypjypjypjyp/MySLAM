using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MySLAM.Xamarin.Helpers.AR
{
    public class MyRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        private Semaphore dictLock = new Semaphore(0, 1);
        private Dictionary<string, GLEntity> entityDict = new Dictionary<string, GLEntity>();

        private float[] _VPMat = new float[16];
        private float[] _PMat = new float[16];
        protected volatile float[] _VMat = new float[16];

        #region IRenderer
        public void OnDrawFrame(IGL10 gl)
        {
            if (entityDict.Count == 0) return;
            GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
            Matrix.MultiplyMM(_VPMat, 0, _PMat, 0, _VMat, 0);
            dictLock.WaitOne();
            foreach (var e in entityDict)
                e.Value.Draw((float[])_VPMat.Clone());
            dictLock.Release();
        }
        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            GLES30.GlViewport(0, 0, width, height);
            float ratio = (float)width / height;
            Matrix.FrustumM(_PMat, 0, -ratio, ratio, -1, 1, 1, 10);
        }
        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            GLES30.GlClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GLES30.GlEnable(GLES20.GlDepthTest);
            GLES30.GlEnable(GLES20.GlCullFaceMode);

            Shader.Instance.UseProgram();
            dictLock.Release();
        }
        #endregion

        public void ManageEntitys(Action<Dictionary<string, GLEntity>> action)
        {
            Task.Run(() =>
            {
                dictLock.WaitOne();
                action(entityDict);
                dictLock.Release();
            });
        }
    }

    public class MyARRenderer : MyRenderer
    {
        public float[] VMat { get => _VMat; }
    }
}