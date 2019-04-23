using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Nio;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    public class GLEarth : GLEntity
    {
        protected override Shader Program => BallShader.Instance;

        private static float UNIT_SIZE = 1.0f;// 单位尺寸
        private float r = 0.7f; // 球的半径
        int angleSpan = 5;// 将球进行单位切分的角度
        private FloatBuffer vertexBuffer;// 顶点坐标
        int vCount = 0;// 顶点个数，先初始化为0
                       // float类型的字节数
        private static  int BYTES_PER_FLOAT = 4;
        // 数组中每个顶点的坐标数
        private static  int COORDS_PER_VERTEX = 3;

        // **********************************************************************
        private static  int TEXTURE_COORDIANTES_COMPONENT_COUNT = 2; // 每个纹理坐标为 S T两个
        private static  string A_TEXTURE_COORDINATES = "a_TextureCoordinates";//纹理
	    private static  string U_TEXTURE_UNIT = "u_TextureUnit";//纹理
	    private FloatBuffer textureBuffer;// 纹理坐标
        private int uTextureUnitLocation;
        private int aTextureCoordinates;
        private int texture = -1;
        // ***********************************************************************

        private static  string A_POSITION = "a_Position";
	    private static  string U_MATRIX = "u_Matrix";
	    private int uMatrixLocation;
        private int aPositionLocation;

        public GLEarth()
        {
        }

        private void InitHandles()
        {
            initVertexData();
            aPositionLocation = GLES30.GlGetAttribLocation(Program, A_POSITION);
            uMatrixLocation = GLES30.GlGetUniformLocation(Program, U_MATRIX);
            // ***********************************************************************
            initTexture();
            // **********************************************************************
            //---------传入顶点数据数据
            GLES30.GlVertexAttribPointer(aPositionLocation, COORDS_PER_VERTEX,
                    GLES30.GlFloat, false, 0, vertexBuffer);
            GLES30.GlEnableVertexAttribArray(aPositionLocation);

            // ***********************************************************************
            GLES30.GlVertexAttribPointer(aTextureCoordinates, TEXTURE_COORDIANTES_COMPONENT_COUNT,
                    GLES30.GlFloat, false, 0, textureBuffer);
            GLES30.GlEnableVertexAttribArray(aTextureCoordinates);
            // **********************************************************************
        }

        private float ToRadians(int degree)
        {
            return (float)(degree * Math.PI / 180);
        }

        public void initVertexData()
        {
            List<float> alVertix = new List<float>();// 存放顶点坐标的ArrayList
                                                               // ***************************************
            List<float> textureVertix = new List<float>();// 存放纹理坐标的ArrayList
                                                                    // ***************************************
            for (int vAngle = 0; vAngle < 180; vAngle = vAngle + angleSpan)// 垂直方向angleSpan度一份
            {
                for (int hAngle = 0; hAngle <= 360; hAngle = hAngle + angleSpan)// 水平方向angleSpan度一份
                {
                    // 纵向横向各到一个角度后计算对应的此点在球面上的坐标
                    float x0 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle)) * Math.Cos(ToRadians(hAngle)));
                    float y0 = (float)(r * UNIT_SIZE * Math.Cos(ToRadians(vAngle)));
                    float z0 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle)) * Math.Sin(ToRadians(hAngle)));


                    float x1 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle)) * Math.Cos(ToRadians(hAngle + angleSpan)));
                    float y1 = (float)(r * UNIT_SIZE * Math.Cos(ToRadians(vAngle)));
                    float z1 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle)) * Math.Sin(ToRadians(hAngle + angleSpan)));

                    float x2 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle + angleSpan)) * Math.Cos(ToRadians(hAngle + angleSpan)));
                    float y2 = (float)(r * UNIT_SIZE * Math.Cos(ToRadians(vAngle + angleSpan)));
                    float z2 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle + angleSpan)) * Math.Sin(ToRadians(hAngle + angleSpan)));

                    float x3 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle + angleSpan)) * Math.Cos(ToRadians(hAngle)));
                    float y3 = (float)(r * UNIT_SIZE * Math.Cos(ToRadians(vAngle + angleSpan)));
                    float z3 = (float)(r * UNIT_SIZE * Math.Sin(ToRadians(vAngle + angleSpan)) * Math.Sin(ToRadians(hAngle)));

                    // 将计算出来的XYZ坐标加入存放顶点坐标的ArrayList
                    alVertix.Add(x1);
                    alVertix.Add(y1);
                    alVertix.Add(z1);
                    alVertix.Add(x3);
                    alVertix.Add(y3);
                    alVertix.Add(z3);
                    alVertix.Add(x0);
                    alVertix.Add(y0);
                    alVertix.Add(z0);

                    // *****************************************************************
                    float s0 = hAngle / 360.0f;
                    float s1 = (hAngle + angleSpan) / 360.0f;
                    float t0 = 1 - vAngle / 180.0f;
                    float t1 = 1 - (vAngle + angleSpan) / 180.0f;

                    textureVertix.Add(s1);// x1 y1对应纹理坐标
                    textureVertix.Add(t0);
                    textureVertix.Add(s0);// x3 y3对应纹理坐标
                    textureVertix.Add(t1);
                    textureVertix.Add(s0);// x0 y0对应纹理坐标
                    textureVertix.Add(t0);

                    // *****************************************************************
                    alVertix.Add(x1);
                    alVertix.Add(y1);
                    alVertix.Add(z1);
                    alVertix.Add(x2);
                    alVertix.Add(y2);
                    alVertix.Add(z2);
                    alVertix.Add(x3);
                    alVertix.Add(y3);
                    alVertix.Add(z3);

                    // *****************************************************************
                    textureVertix.Add(s1);// x1 y1对应纹理坐标
                    textureVertix.Add(t0);
                    textureVertix.Add(s1);// x2 y3对应纹理坐标
                    textureVertix.Add(t1);
                    textureVertix.Add(s0);// x3 y3对应纹理坐标
                    textureVertix.Add(t1);
                    // *****************************************************************
                }
            }
            vCount = alVertix.Count / COORDS_PER_VERTEX;// 顶点的数量
                                                         // 将alVertix中的坐标值转存到一个float数组中
            float[] vertices = new float[vCount * COORDS_PER_VERTEX];
            for (int i = 0; i < alVertix.Count; i++)
            {
                vertices[i] = alVertix[i];
            }
            vertexBuffer = ByteBuffer
                    .AllocateDirect(vertices.Length * BYTES_PER_FLOAT)
                    .Order(ByteOrder.NativeOrder())
                    .AsFloatBuffer();
            // 把坐标们加入FloatBuffer中
            vertexBuffer.Put(vertices);
            // 设置buffer，从第一个坐标开始读
            vertexBuffer.Position(0);
            // *****************************************************************
            float[] textures = new float[textureVertix.Count];
            for (int i = 0; i < textureVertix.Count; i++)
            {
                textures[i] = textureVertix[i];
            }
            textureBuffer = ByteBuffer
                    .AllocateDirect(textures.Length * BYTES_PER_FLOAT)
                    .Order(ByteOrder.NativeOrder())
                    .AsFloatBuffer();
            // 把坐标们加入FloatBuffer中
            textureBuffer.Put(textures);
            // 设置buffer，从第一个坐标开始读
            textureBuffer.Position(0);
            // *****************************************************************
        }

        // *******************************************************
        //初始化加载纹理
        private void initTexture()
        {
            aTextureCoordinates = GLES30.GlGetAttribLocation(Program, A_TEXTURE_COORDINATES);
            uTextureUnitLocation = GLES30.GlGetAttribLocation(Program, U_TEXTURE_UNIT);
            texture = TextureHelper.loadTexture(HelperManager.MainActivity, Resource.Mipmap.logo, false);
            // Set the active texture unit to texture unit 0.
            GLES30.GlActiveTexture(GLES30.GlTexture0);
            // Bind the texture to this unit.
            GLES30.GlBindTexture(GLES30.GlTexture2d, texture);
            // Tell the texture uniform sampler to use this texture in the shader by
            // telling it to read from texture unit 0.
            GLES30.GlUniform1i(uTextureUnitLocation, 0);
        }

        // *******************************************************
        public sealed override void Draw(float[] VPMat)
        {
            if (texture == -1)
                InitHandles();
            //将最终变换矩阵写入
            GLES30.GlUniformMatrix4fv(uMatrixLocation, 1, false, UpdateMVPMat(VPMat), 0);
            GLES30.GlDrawArrays(GLES30.GlTriangles, 0, vCount);
        }

        protected virtual float[] UpdateMVPMat(float[] VPMat) => VPMat;
    }

    public class TextureHelper
    {
    public static int loadTexture(Context context, int resourceId, bool isRepeat)
        {
            /*
             * 第一步 : 创建纹理对象
             */
             int[] textureObjectId = new int[1];//用于存储返回的纹理对象ID
            GLES30.GlGenTextures(1, textureObjectId, 0);
            if (textureObjectId[0] == 0)
            {//若返回为0,,则创建失败
                
                return 0;
            }
            /*
             * 第二步: 加载位图数据并与纹理绑定
             */
             BitmapFactory.Options options = new BitmapFactory.Options();
            options.InScaled = false;//OpenGl需要非压缩形式的原始数据
             Bitmap bitmap = BitmapFactory.DecodeResource(context.Resources, resourceId, options);
            if (bitmap == null)
            {
                GLES30.GlDeleteTextures(1, textureObjectId, 0);
                return 0;
            }
            GLES30.GlBindTexture(GLES30.GlTexture2d, textureObjectId[0]);//通过纹理ID进行绑定

            if (isRepeat)
            {
                GLES30.GlTexParameterf(GLES30.GlTexture2d, GLES30.GlTextureWrapS, GLES30.GlRepeat);
                GLES30.GlTexParameterf(GLES30.GlTexture2d, GLES30.GlTextureWrapT, GLES30.GlRepeat);
            }

            /*
             * 第三步: 设置纹理过滤
             */
            //设置缩小时为三线性过滤
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMinFilter, GLES30.GlLinearMipmapLinear);
            //设置放大时为双线性过滤
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMagFilter, GLES30.GlLinear);
            /*
             * 第四步: 加载纹理到OpenGl并返回ID
             */
            GLUtils.TexImage2D(GLES30.GlTexture2d, 0, bitmap, 0);
            bitmap.Recycle();
            GLES30.GlGenerateMipmap(GLES30.GlTexture2d);
            return textureObjectId[0];
        }
    }
}