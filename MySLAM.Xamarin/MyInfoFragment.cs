using Android.App;
using Android.Opengl;
using Android.OS;
using Android.Views;
using Android.Widget;
using MySLAM.Xamarin.Helpers;
using MySLAM.Xamarin.Helpers.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace MySLAM.Xamarin
{
    public class MyInfoFragment : Fragment
    {
        public GLSurfaceView GLSurfaceView;
        private MyARRenderer renderer;

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
            //OpenGL 
            GLSurfaceView = view.FindViewById<GLSurfaceView>(Resource.Id.info_gl_view);
            GLSurfaceView.SetEGLContextClientVersion(3);
            GLSurfaceView.SetEGLConfigChooser(8, 8, 8, 8, 16, 0); //Set Transparent
            GLSurfaceView.Holder.SetFormat(Android.Graphics.Format.Translucent);
            renderer = new MyARRenderer();
            float[] a = renderer.VMat;
            var b =new float[16] { 1,0,0,0,
                            0,-1,0,0,
                            0,0,-1,0,
                            0,0,0,1};
            b.CopyTo(a,0);
            GLSurfaceView.SetRenderer(renderer);
            GLSurfaceView.RenderMode = Rendermode.Continuously;
            GLSurfaceView.SetZOrderOnTop(true);

            renderer.ManageEntitys((e) =>
            {
                e["Earth"] = new Earth(3f);
            });
        }
    }
}