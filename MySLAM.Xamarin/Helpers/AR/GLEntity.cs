using Android.Opengl;
using Java.Nio;

namespace MySLAM.Xamarin.Helpers.AR
{
    public abstract class GLEntity
    {
        protected abstract Shader Program { get; }
        public abstract void Draw(float[] viewMat);
    }

    public class GLSimpleEntity : GLEntity
    {
        protected override Shader Program => Shader.Instance;

        protected virtual int CoordsPerVertex => 3;
        protected virtual float[] Vertexs => new float[] {
            -0.5f,  0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.5f,  0.5f, 0.0f
        };
        protected virtual short[] DrawOrder => new short[] {
            0,
            1,
            2,
            0,
            2,
            3
        };
        protected virtual float[] Colors => new float[] {
            0.2f,
            0.709803922f,
            0.898039216f,
            1.0f
        };

        protected FloatBuffer vertexBuffer;
        protected ShortBuffer drawListBuffer;

        protected int vertexHandle;
        protected int colorHandle;
        protected int _MVPHandle;

        public GLSimpleEntity()
        {
            if (!Program.IsLoaded)
                throw new System.Exception("Program is not used!");
            // Put model into buffer
            vertexBuffer = (FloatBuffer)ByteBuffer.AllocateDirect(Vertexs.Length * 4)
                .Order(ByteOrder.NativeOrder())
                .AsFloatBuffer()
                .Put(Vertexs)
                .Position(0);
            drawListBuffer = (ShortBuffer)ByteBuffer.AllocateDirect(DrawOrder.Length * 2)
                .Order(ByteOrder.NativeOrder())
                .AsShortBuffer()
                .Put(DrawOrder)
                .Position(0);
        }

        private void InitHandles()
        {
            vertexHandle = GLES30.GlGetAttribLocation(Program, "vertex");
            GLES30.GlEnableVertexAttribArray(vertexHandle);
            GLES30.GlVertexAttribPointer(vertexHandle, CoordsPerVertex,
                GLES30.GlFloat, false,
                0, vertexBuffer);

            colorHandle = GLES30.GlGetUniformLocation(Program, "color");
            GLES30.GlUniform4fv(colorHandle, 1, Colors, 0);

            _MVPHandle = GLES30.GlGetUniformLocation(Program, "mvp");
        }

        public sealed override void Draw(float[] VPMat)
        {
            if (vertexHandle == 0)
                InitHandles();
            GLES30.GlUniformMatrix4fv(_MVPHandle, 1, false, MVPMat(VPMat), 0);
            GLES30.GlDrawElements(GLES30.GlTriangles, DrawOrder.Length,
                GLES30.GlUnsignedShort, drawListBuffer);
        }

        protected virtual float[] MVPMat(float[] VPMat) => VPMat;
    }
}