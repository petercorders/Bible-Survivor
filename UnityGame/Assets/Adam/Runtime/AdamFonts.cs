using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Imported font data is explicit; there is no OS-font creation or named fallback in this rendering path.
    public sealed class AdamFonts : IDisposable
    {
        public readonly Font Title,Label,Body,Small;
        public static string HashFor(string filename)
        {
            switch(filename)
            {
                case "Galmuri11-Bold":return "45d901e379138dd91873af157640f60aa4cf6beaa885c3efd2a8c279e039a237";
                case "Galmuri11":return "e24256f42e43713d2ea086a1e1669d78b968f5b3cc547e5c157f0606ffa5def1";
                case "Galmuri9":return "e84e821b18be15b9e3a907ceb83cfba25fabf51c80b7edf0d2921cf8f8e1a11d";
                case "Galmuri7":return "c845c6bf6f7236ed370aed4a8f8d9b4f25e6a18f63515777fe4153d287c32917";
                default:throw new InvalidOperationException("Unreviewed font: "+filename);
            }
        }
        public AdamFonts()
        {
            Title=Load("Galmuri11-Bold");Label=Load("Galmuri11");Body=Load("Galmuri9");Small=Load("Galmuri7");
            Font.textureRebuilt+=OnRebuilt;
        }
        static Font Load(string name)
        { var font=Resources.Load<Font>("ArtFonts/"+name);if(font==null)throw new InvalidOperationException("Missing outline font: "+name);SetPoint(font);return font; }
        static void SetPoint(Font font)
        { if(font.material?.mainTexture!=null) {font.material.mainTexture.filterMode=FilterMode.Point;font.material.mainTexture.wrapMode=TextureWrapMode.Clamp;} }
        void OnRebuilt(Font font) { if(font==Title||font==Label||font==Body||font==Small)SetPoint(font); }
        public void Dispose() {Font.textureRebuilt-=OnRebuilt;}
    }
}
