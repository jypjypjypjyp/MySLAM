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
        }
    }
}