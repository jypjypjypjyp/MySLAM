using Android.Graphics;
using System.IO;

namespace MySLAM.Xamarin.MyHelper
{
    public static class AppConst
    {
        public static void Init()
        {
            RootPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/MySLAM/";
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
        }

        public static string RootPath;
    }


    public static class YamlExtension
    {
        public static bool CopyWholeFile(this Stream src, Stream dist)
        {
            try
            {
                if (src.CanRead && dist.CanWrite)
                {
                    using (var reader = new StreamReader(src))
                    using (var writer = new StreamWriter(dist))
                    {

                        writer.Write(reader.ReadToEnd());
                        writer.Flush();
                    }
                    return true;
                }
            }
            finally
            {
                src.Close();
                dist.Close();
            }
            return false;
        }

        public static bool Edit(this FileStream file, string key, string value)
        {
            if (file.CanRead && file.CanWrite)
            {
                using (var reader = new StreamReader(file))
                using (var writer = new StreamWriter(file))
                {
                    string allText = reader.ReadToEnd();
                    int index = allText.IndexOf(':', allText.IndexOf(key)) + 1;
                    allText = allText.Remove(index, allText.IndexOf('\n', index) - index);
                    allText = allText.Insert(index, " " + value);
                    file.Seek(0, SeekOrigin.Begin);
                    writer.Write(allText);
                    writer.Flush();
                }
                return true;
            }
            return false;
        }
    }

    public static class BitmapExtension
    {
        private static Paint paint = new Paint();

        static BitmapExtension()
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