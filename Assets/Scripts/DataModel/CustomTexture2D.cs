using System;
using UnityEngine;

namespace Download.Core.Editor
{
    [Serializable]
    public class CustomTexture2D
    {
        public int width;
        public int height;
        public byte[] bytes;

        public Texture2D Value()
        {
                var tex = new Texture2D(width, height);
                tex.LoadImage(bytes);
                return tex;
        }

        public CustomTexture2D(Texture2D origin)
        {
            width = origin.width;
            height = origin.height;
            
            var newTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            newTexture.SetPixels(0,0,width,height,origin.GetPixels());
            newTexture.Apply();
            bytes = newTexture.EncodeToPNG();
        }
        public CustomTexture2D(){}
    }
}