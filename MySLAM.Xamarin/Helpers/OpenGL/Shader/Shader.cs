using Android.Opengl;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class Shader
    {
        public bool IsLoaded { get; protected set; }
        protected virtual string VertexShaderCode =>
            "";
        protected virtual string FragmentShaderCode =>
            "";
        protected int program;

        public Shader()
        {
            IsLoaded = false;
        }

        public void UseProgram()
        {
            int vertexShader = LoadShader(GLES30.GlVertexShader,
                                   VertexShaderCode);
            int fragmentShader = LoadShader(GLES30.GlFragmentShader,
                                     FragmentShaderCode);
            program = GLES30.GlCreateProgram();
            GLES30.GlAttachShader(program, vertexShader);
            GLES30.GlAttachShader(program, fragmentShader);
            GLES30.GlLinkProgram(program);
            GLES30.GlUseProgram(program);
            if (program != 0)
                IsLoaded = true;
        }

        protected static int LoadShader(int type, string shaderCode)
        {
            int shader = GLES30.GlCreateShader(type);
            GLES30.GlShaderSource(shader, shaderCode);
            GLES30.GlCompileShader(shader);
            return shader;
        }
        public static implicit operator int(Shader p)
        {
            return p.program;
        }
    }
}