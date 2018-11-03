using System.IO;
using Android.Media;

namespace ICTCollector.Xamarin.Helper
{
    public static class ImageSaver
    {
        public static string Path { get; set; }

        public static void SaveImage(Image image, string name)
        {
            Java.Nio.ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            File.WriteAllBytes(Path + name, bytes);
            image.Close();
        }
    }
}