using System.IO;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class BallShader : Shader
    {
        private static Shader instance;
        public static Shader Instance
        {
            get
            {
                return instance == null ? instance = new BallShader() : instance;
            }
        }
        protected override string VertexShaderCode =>
            new StreamReader(
                HelperManager.MainActivity.Assets.Open("ball_shader_vertex.glsl")
                ).ReadToEnd();
        protected override string FragmentShaderCode =>
            new StreamReader(
                HelperManager.MainActivity.Assets.Open("ball_shader_fragment.glsl")
                ).ReadToEnd();
    }
}