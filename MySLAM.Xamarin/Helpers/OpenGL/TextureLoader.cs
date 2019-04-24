using Android.Graphics;
using Android.Opengl;

namespace MySLAM.Xamarin.Helpers.OpenGL
{
    class TextureLoader
    {
        public static int LoadTexture(string fileName, bool isRepeat)
        {
            
            var bitmap = 
                BitmapFactory.DecodeStream(HelperManager.MainActivity.Assets.Open("Texture/" + fileName));
            if (bitmap == null)
                return 0;
            // Create texture object
            int[] textureObjectId = new int[1];
            GLES30.GlGenTextures(1, textureObjectId, 0);
            if (textureObjectId[0] == 0)
                return 0;
            // Bind texture
            var options = new BitmapFactory.Options
            {
                InScaled = false
            };
            GLES30.GlBindTexture(GLES30.GlTexture2d, textureObjectId[0]);//通过纹理ID进行绑定

            if (isRepeat)
            {
                GLES30.GlTexParameterf(GLES30.GlTexture2d, GLES30.GlTextureWrapS, GLES30.GlRepeat);
                GLES30.GlTexParameterf(GLES30.GlTexture2d, GLES30.GlTextureWrapT, GLES30.GlRepeat);
            }
            // Set texture linear filter
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMinFilter, GLES30.GlLinearMipmapLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMagFilter, GLES30.GlLinear);

            GLUtils.TexImage2D(GLES30.GlTexture2d, 0, bitmap, 0);
            bitmap.Recycle();
            GLES30.GlGenerateMipmap(GLES30.GlTexture2d);
            return textureObjectId[0];
        }
    }
}