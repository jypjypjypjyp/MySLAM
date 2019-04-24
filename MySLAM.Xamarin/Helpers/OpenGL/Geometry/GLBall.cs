using Android.Opengl;
using Java.Nio;
using System;
using System.Collections.Generic;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class GLBall : GLEntity
    {
        protected override Shader Program => BallShader.Instance;

        public float R { get; set; }
        public int AngleSpan { get; set; }
        public string Name { get; set; }

        private int vertexN = 0;
        private FloatBuffer vertexBuffer;
        private FloatBuffer textureBuffer;

        private int textureHandle = -1;
        private int uTextureUnitHandle;
        private int aTextureCoordHandle;
        private int uMatrixHandle;
        private int aPositionHandle;

        public GLBall(float r, int angleSpan,string name)
        {
            R = r;
            AngleSpan = angleSpan;
            Name = name;
        }

        private void InitHandles()
        {
            InitBuffer();
            aPositionHandle = GLES30.GlGetAttribLocation(Program, "a_Position");
            uMatrixHandle = GLES30.GlGetUniformLocation(Program, "u_Matrix");
            // ***********************************************************************
            InitTexture();
            // **********************************************************************
            //---------传入顶点数据数据
            GLES30.GlVertexAttribPointer(aPositionHandle, 3,
                    GLES30.GlFloat, false, 0, vertexBuffer);
            GLES30.GlEnableVertexAttribArray(aPositionHandle);

            // ***********************************************************************
            GLES30.GlVertexAttribPointer(aTextureCoordHandle, 2,
                    GLES30.GlFloat, false, 0, textureBuffer);
            GLES30.GlEnableVertexAttribArray(aTextureCoordHandle);
            // **********************************************************************
        }

        private float ToRadians(int degree)
        {
            return (float)(degree * Math.PI / 180);
        }

        public void InitBuffer()
        {
            var vertixList = new List<float>();
            var textureList = new List<float>();
            for (int v = 0; v < 180; v = v + AngleSpan)// vertical
            {
                for (int h = 0; h <= 360; h = h + AngleSpan)// horizonal
                {
                    double sv = Math.Sin(ToRadians(v)), cv = Math.Cos(ToRadians(v)),
                        sh = Math.Sin(ToRadians(h)), ch = Math.Cos(ToRadians(h)),
                        svi = Math.Sin(ToRadians(v + AngleSpan)), cvi = Math.Cos(ToRadians(v + AngleSpan)),
                        shi = Math.Sin(ToRadians(h + AngleSpan)), chi = Math.Cos(ToRadians(h + AngleSpan));

                    float x0 = (float)(R * sv * ch);
                    float y0 = (float)(R * cv);
                    float z0 = (float)(R * sv * sh);

                    float x1 = (float)(R * sv * chi);
                    float y1 = (float)(R * cv);
                    float z1 = (float)(R * sv * shi);

                    float x2 = (float)(R * svi * chi);
                    float y2 = (float)(R * cvi);
                    float z2 = (float)(R * svi * shi);

                    float x3 = (float)(R * svi * ch);
                    float y3 = (float)(R * cvi);
                    float z3 = (float)(R * svi * sh);

                    float s0 = h / 360.0f;
                    float s1 = (h + AngleSpan) / 360.0f;
                    float t0 = 1 - v / 180.0f;
                    float t1 = 1 - (v + AngleSpan) / 180.0f;
                    // two triangle panel
                    vertixList.Add(x1); textureList.Add(s1);
                    vertixList.Add(y1); textureList.Add(t0);
                    vertixList.Add(z1);
                    vertixList.Add(x3); textureList.Add(s0);
                    vertixList.Add(y3); textureList.Add(t1);
                    vertixList.Add(z3);
                    vertixList.Add(x0); textureList.Add(s0);
                    vertixList.Add(y0); textureList.Add(t0);
                    vertixList.Add(z0);

                    vertixList.Add(x1); textureList.Add(s1);
                    vertixList.Add(y1); textureList.Add(t0);
                    vertixList.Add(z1);
                    vertixList.Add(x2); textureList.Add(s1);
                    vertixList.Add(y2); textureList.Add(t1);
                    vertixList.Add(z2);
                    vertixList.Add(x3); textureList.Add(s0);
                    vertixList.Add(y3); textureList.Add(t1);
                    vertixList.Add(z3);
                }
            }
            vertexN = vertixList.Count / 3;
            // put list into buffer
            float[] vertices = new float[vertexN * 3];
            for (int i = 0; i < vertixList.Count; i++)
            {
                vertices[i] = vertixList[i];
            }
            vertexBuffer = ByteBuffer
                    .AllocateDirect(vertices.Length * sizeof(float))
                    .Order(ByteOrder.NativeOrder())
                    .AsFloatBuffer();
            vertexBuffer.Put(vertices);
            vertexBuffer.Position(0);
            
            float[] textures = new float[textureList.Count];
            for (int i = 0; i < textureList.Count; i++)
            {
                textures[i] = textureList[i];
            }
            textureBuffer = ByteBuffer
                    .AllocateDirect(textures.Length * sizeof(float))
                    .Order(ByteOrder.NativeOrder())
                    .AsFloatBuffer();
            textureBuffer.Put(textures);
            // 设置buffer，从第一个坐标开始读
            textureBuffer.Position(0);
       }

        private void InitTexture()
        {
            aTextureCoordHandle = GLES30.GlGetAttribLocation(Program, "a_TextureCoordinates");
            uTextureUnitHandle = GLES30.GlGetAttribLocation(Program, "u_TextureUnit");
            textureHandle = TextureLoader.LoadTexture(name, false);
            // Set the active texture unit to texture unit 0.
            GLES30.GlActiveTexture(GLES30.GlTexture0);
            // Bind the texture to this unit.
            GLES30.GlBindTexture(GLES30.GlTexture2d, textureHandle);
            // Tell the texture uniform sampler to use this texture in the shader by
            // telling it to read from texture unit 0.
            GLES30.GlUniform1i(uTextureUnitHandle, 0);
        }
        
        public sealed override void Draw(float[] VPMat)
        {
            if (textureHandle == -1)
                InitHandles();
            GLES30.GlUniformMatrix4fv(uMatrixHandle, 1, false, UpdateMVPMat(VPMat), 0);
            GLES30.GlDrawArrays(GLES30.GlTriangles, 0, vertexN);
        }

        protected virtual float[] UpdateMVPMat(float[] VPMat)
        {
            return VPMat;
        }
    }
}