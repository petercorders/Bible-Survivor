using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    [Serializable] public sealed class AdamArtIndex
    {
        public int schema, ppu;
        public string manifestSha256;
        public string[] layers;
        public AdamArtClip[] assets;
    }
    [Serializable] public sealed class AdamArtClip
    {
        public string id, source, sha256, sorting, quality, alpha, layer, animation;
        public int width, height, frames, pivotX, pivotY;
        public float fps;
        public bool loop;
        public int[] borders, alphaValues, attachX, attachY;
        public string[] states;
        public int Frame(double seconds)
        {
            var frame = (int)Math.Floor(Math.Max(0, seconds) * fps + 1e-8);
            return loop ? frame % frames : Math.Min(frame, frames - 1);
        }
    }
    // The original PNGs are not repainted or packed. Imported full-rect sprites keep transparent registration pixels.
    public sealed class AdamArt
    {
        public const string IndexSha = "c6be027b8da2f2c543a25d6317493c0359452526b513fcc0cdb9d0293b486fc6";
        public static bool ReviewedIndex(string json)
        { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant()==IndexSha; }
        public const string ManifestSha = "0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string, AdamArtClip> clips = new Dictionary<string, AdamArtClip>();
        readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();
        public bool OptionalEffects = true;
        public AdamArt()
        {
            var json = Resources.Load<TextAsset>("Art03/index");
            if (json == null) throw new InvalidOperationException("Art03 contract is missing");
            if(!ReviewedIndex(json.text)) throw new InvalidOperationException("Art03 index differs from reviewed import contract");
            Index = JsonUtility.FromJson<AdamArtIndex>(json.text);
            if (Index.schema != 1 || Index.ppu != 32 || Index.manifestSha256 != ManifestSha || Index.assets.Length != 79)
                throw new InvalidOperationException("Unreviewed Art03 contract");
            foreach (var clip in Index.assets)
            {
                if (clip.frames < 1 || clip.width < 1 || clip.height < 1 || clip.fps < 0 || clip.borders.Length != 4)
                    throw new InvalidOperationException("Invalid art clip: " + clip.id);
                clips.Add(clip.id, clip);
                var frames = Resources.LoadAll<Sprite>("Art03/" + clip.id);
                Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
                if (frames.Length != clip.frames) throw new InvalidOperationException("Missing imported frames: " + clip.id);
                for(var i=0;i<frames.Length;i++)
                    if(frames[i].rect!=new Rect(i*clip.width,0,clip.width,clip.height) || frames[i].pivot!=new Vector2(clip.pivotX,clip.pivotY) ||
                        frames[i].pixelsPerUnit!=32 || frames[i].texture.filterMode!=FilterMode.Point ||
                        frames[i].border!=new Vector4(clip.borders[0],clip.borders[1],clip.borders[2],clip.borders[3]))
                        throw new InvalidOperationException("Invalid imported registration: "+clip.id);
                sprites.Add(clip.id, frames);
            }
        }
        public AdamArtClip Clip(string id) => clips[id];
        public Sprite Sprite(string id, int frame) => sprites[id][Mathf.Clamp(frame, 0, clips[id].frames - 1)];
        public static Rect PlacedRect(AdamArtClip clip, Vector2 feet, int scale) => new Rect(
            Mathf.Round(feet.x) - clip.pivotX * scale, Mathf.Round(feet.y) - (clip.height - clip.pivotY) * scale,
            clip.width * scale, clip.height * scale);
        public void Draw(string id, Vector2 feet, int scale, double time = 0, bool left = false, int frame = -1)
        {
            var clip = Clip(id);
            if (!OptionalEffects && clip.quality == "optional-fx") return;
            var rect = PlacedRect(clip, feet, scale);
            if (left) rect.x = Mathf.Round(feet.x) - (clip.width - clip.pivotX) * scale;
            Blit(Sprite(id, frame < 0 ? clip.Frame(time) : frame), rect, left);
        }
        public static void Blit(Sprite sprite, Rect destination, bool flip = false)
        {
            var uv = sprite.rect; var texture = sprite.texture;
            uv = new Rect(uv.x / texture.width, uv.y / texture.height, uv.width / texture.width, uv.height / texture.height);
            if (flip) { uv.x += uv.width; uv.width = -uv.width; }
            GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
        }
        // Nine-slice corners remain fixed; every edge/centre repeats at the original integer game-pixel scale.
        public void Panel(Rect rect, string id = "ui_panel_9slice_16", int state = 0, int scale = 2)
        {
            var clip = Clip(id); var sprite = Sprite(id, state); var b = clip.borders;
            var xs = new[] { 0, b[0], clip.width - b[2], clip.width };
            var ys = new[] { 0, b[3], clip.height - b[1], clip.height };
            var dx = new[] { rect.x, rect.x + b[0]*scale, rect.xMax-b[2]*scale, rect.xMax };
            var dy = new[] { rect.y, rect.y+b[3]*scale, rect.yMax-b[1]*scale, rect.yMax };
            for (var row=0; row<3; row++) for (var col=0; col<3; col++)
            {
                var sw=xs[col+1]-xs[col]; var sh=ys[row+1]-ys[row];
                if (sw<=0 || sh<=0) continue;
                for (var y=dy[row]; y<dy[row+1]-.01f; y+=sh*scale)
                for (var x=dx[col]; x<dx[col+1]-.01f; x+=sw*scale)
                {
                    var w=Mathf.Min(sw*scale, dx[col+1]-x); var h=Mathf.Min(sh*scale, dy[row+1]-y);
                    var uv=new Rect((sprite.rect.x+xs[col])/sprite.texture.width,
                        (sprite.rect.y+clip.height-ys[row]-h/scale)/sprite.texture.height,
                        w/scale/sprite.texture.width, h/scale/sprite.texture.height);
                    GUI.DrawTextureWithTexCoords(new Rect(x,y,w,h), sprite.texture,uv,true);
                }
            }
        }
    }
}
