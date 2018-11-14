using Android.Graphics;

namespace MySLAM.Xamarin.MyHelper
{
    public static class Extension
    {
        private static Paint paint = new Paint();

        static Extension()
        {
            var cm = new ColorMatrix();
            cm.SetSaturation(0);
            var f = new ColorMatrixColorFilter(cm);
            paint.SetColorFilter(f);
        }

        public static Bitmap ToGraySacle(this Bitmap origin)
        {
            var grayscaleBitmap =
                Bitmap.CreateBitmap(origin.Width, origin.Height, Bitmap.Config.Argb8888);
            var c = new Canvas(grayscaleBitmap);
            c.DrawBitmap(origin, 0, 0, paint);
            return grayscaleBitmap;
        }

        public static int Area(this Android.Util.Size size)
        {
            return size.Width * size.Height;
        }
    }
}