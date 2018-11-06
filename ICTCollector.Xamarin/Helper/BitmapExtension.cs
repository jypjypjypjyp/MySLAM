using Android.Graphics;

namespace ICTCollector.Xamarin.Helper
{
    public static class BitmapExtension
    {
        private static Paint paint = new Paint();

        static BitmapExtension()
        {
            ColorMatrix cm = new ColorMatrix();
            cm.SetSaturation(0);
            ColorMatrixColorFilter f = new ColorMatrixColorFilter(cm);
            paint.SetColorFilter(f);
        }

        public static Bitmap ToGraySacle(this Bitmap origin)
        {
            Bitmap grayscaleBitmap =
                Bitmap.CreateBitmap(origin.Width, origin.Height, Bitmap.Config.Argb8888);
            Canvas c = new Canvas(grayscaleBitmap);
            c.DrawBitmap(origin, 0, 0, paint);
            return grayscaleBitmap;
        }
    }
}