using System.IO;

namespace ICTCollector.Xamarin.Helper
{
    public static class FileSaver
    {
        public static string Path { get; set; }

        public static void SaveBytes(byte[] bytes, string name)
        {
            File.WriteAllBytes(Path + name, bytes);
        }
    }
}