namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class SimpleShader : Shader
    {
        private static Shader instance;
        public static Shader Instance
        {
            get
            {
                return instance == null ? instance = new SimpleShader() : instance;
            }
        }
    }
}