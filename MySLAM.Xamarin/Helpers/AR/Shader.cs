using Android.Opengl;

namespace MySLAM.Xamarin.Helpers.AR
{
    public class Shader
    {
        private static Shader instance;
        public static Shader Instance
        {
            get => instance = instance ?? new Shader();
        }

        public bool IsLoaded { get; protected set; }
        protected virtual string VertexShaderCode =>
            "uniform mat4 mvp;" +
            "attribute vec4 vertex;" +
            "void main() {" +
            "  gl_Position = mvp * vertex;" +
            "}";
        protected virtual string FragmentShaderCode =>
            "precision mediump float;" +
            "uniform vec4 color;" +
            "void main() {" +
            "  gl_FragColor = color;" +
            "}";
        protected int program;

        private Shader()
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