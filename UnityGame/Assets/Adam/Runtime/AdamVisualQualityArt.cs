using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    [Serializable] public sealed class AdamVisualManifest
    {
        public string schema,bundle,pivotSpace,borderOrder;
        public int schemaVersion,ppu;
        public AdamVisualAsset[] assets;
    }
    [Serializable] public sealed class AdamVisualAsset
    {
        public string id,file,alpha,sha256,sorting,quality;
        public int[] cell,pivot,borders,attachX,attachY,alphaValues;
        public int frames;
        public float fps;
        public bool loop;
        public string[] states;
    }
    // Final reviewed overlay. Resources owns imported textures; no Core state or original index is changed.
    public static class AdamVisualQualityArt
    {
        public const string Root="VisualQuality01/",PolishRoot="VisualQuality02/",PolishManifestSha="23b25655a3e80920a5086df978a144ceb3e367ea2be0f1d9d5971bfed9f539c8";
        public const string UiManifestSha="cdf356aeef006982aed99c2c7adcee2cb0cff1817a4681d30a6632965642448b",WorldManifestSha="9dc1c57aa5b5dc72ab0d1b47c500318b553a4631eb932a375cebc03889c20d9c",EffectsManifestSha="e6aec4f76b3b679af00dc6f2c61cd705ccbc1f78240049396abd663b56d7c47b";
        public static readonly string[] Bundles={"UI","World","Effects"};
        static readonly List<UnityEngine.Object> ownedPresentation=new List<UnityEngine.Object>();
        static AdamVisualQualityArt(){Application.quitting+=Release;}
        static Dictionary<string,AdamArtClip> clips;
        static Dictionary<string,Sprite[]> sprites;
        public static string ManifestSha(string bundle)=>bundle=="UI"?UiManifestSha:bundle=="World"?WorldManifestSha:bundle=="Effects"?EffectsManifestSha:bundle=="Polish02"?PolishManifestSha:throw new InvalidOperationException("Unknown visual bundle");
        public static string[] Ids(string bundle)
        {
            switch(bundle)
            {
                case "UI":return new[]{"ui_panel_9slice_16","ui_card_9slice_24","ui_button_9slice_16","ui_arrow_6x9","ui_slot_18","ui_pip_2","ui_bar-frame_9slice_8x6","ui_bar-hp_tile_1x2","ui_bar-xp_tile_1x4","ui_swatch_12","ui_badge-evolve_8","ui_keycap_9slice_12","ui_chip_9slice_8","ui_bar-boss_tile_1x4","ui_rule_tile_2x1"};
                case "World":return new[]{"til_eden_grass-a_static_32","til_eden_grass-b_static_32","til_eden_grass-c_static_32","til_eden_grass-d_static_32","til_eden_meadow-a_static_32","til_eden_meadow-b_static_32","til_eden_path-16mask_static_32","fx_shadow_static_16x6","fx_shadow-snake_static_20x4","fx_shadow-wide_static_24x6","fx_shadow-small_static_12x4","dec_eden_canopy-shade_static_64"};
                case "Effects":return new[]{"fx_hit-spark_hit_16","fx_enemy-death_burst_32","fx_jawbone-swing-trail_hit_64","fx_jawbone-swing-edge_hit_64"};
                case "Polish02":return new[]{"ico_wpn_bible_emblem_32","ico_wpn_jawbone_emblem_32","ico_wpn_sling_emblem_32","ico_atk-shape_static_16","ui_focus-bracket_9slice_12","fx_jawbone-thud_hit_24","fx_sling-windup_loop_24","fx_sling-crack_hit_16","fx_bible-page-flare_hit_16"};
                default:throw new InvalidOperationException("Unknown visual bundle");
            }
        }
        public static AdamVisualManifest Parse(byte[] bytes,string bundle)
        {
            if(AdamBibleArtSidecar.Hash(bytes)!=ManifestSha(bundle))throw new InvalidOperationException("Unreviewed visual manifest "+bundle);
            var manifest=JsonUtility.FromJson<AdamVisualManifest>(System.Text.Encoding.UTF8.GetString(bytes));
            if(manifest==null || manifest.schema!="bible-survivors.visual-quality-art" || manifest.schemaVersion!=1 || manifest.ppu!=32 ||
                manifest.bundle!=bundle.ToLowerInvariant() || manifest.pivotSpace!="bottom-left-pixel-edge" || manifest.borderOrder!="left,bottom,right,top" || manifest.assets==null)
                throw new InvalidOperationException("Invalid visual handshake "+bundle);
            var remaining=new HashSet<string>(Ids(bundle));
            foreach(var a in manifest.assets)
                if(a==null || !remaining.Remove(a.id) || a.file!=a.id+".png" || a.cell==null || a.cell.Length!=2 || a.cell[0]<1 || a.cell[1]<1 || a.pivot==null || a.pivot.Length!=2 ||
                    a.borders==null || a.borders.Length!=4 || a.frames<1 || a.fps<0 || string.IsNullOrEmpty(a.sha256) || (a.alpha!="binary"&&a.alpha!="blend"))throw new InvalidOperationException("Invalid visual asset "+bundle);
            if(remaining.Count!=0)throw new InvalidOperationException("Incomplete visual whitelist "+bundle);return manifest;
        }
        public static AdamArtClip Contract(AdamVisualAsset a,AdamArtIndex original)
        {
            AdamArtClip old=null;foreach(var c in original.assets)if(c.id==a.id){old=c;break;}
            if(old!=null)
            {
                if(old.width!=a.cell[0] || old.height!=a.cell[1] || old.frames!=a.frames || old.fps!=a.fps || old.loop!=a.loop || old.pivotX!=a.pivot[0] || old.pivotY!=a.pivot[1] || old.alpha!=a.alpha)
                    throw new InvalidOperationException("Visual replacement changes registration "+a.id);
                for(var i=0;i<4;i++)if(old.borders[i]!=a.borders[i])throw new InvalidOperationException("Visual replacement changes border "+a.id);
                // The authoritative attach arrays remain the original index contract.
                if(a.attachX!=null && a.attachX.Length>0 && !Same(a.attachX,old.attachX) || a.attachY!=null && a.attachY.Length>0 && !Same(a.attachY,old.attachY))throw new InvalidOperationException("Visual attachment drift "+a.id);
            }
            return new AdamArtClip{id=a.id,source=Root+a.id,sha256=a.sha256,width=a.cell[0],height=a.cell[1],frames=a.frames,fps=a.fps,loop=a.loop,pivotX=a.pivot[0],pivotY=a.pivot[1],alpha=a.alpha,alphaValues=a.alphaValues??new[]{0,255},borders=(int[])a.borders.Clone(),attachX=old?.attachX,attachY=old?.attachY,states=a.states,quality=a.quality??(a.id=="dec_eden_canopy-shade_static_64"||a.id=="fx_jawbone-swing-trail_hit_64"?"optional-fx":old?.quality),sorting=a.sorting??old?.sorting};
        }
        static bool Same(int[] a,int[] b){if(b==null||a.Length!=b.Length)return false;for(var i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
        static void Load()
        {
            if(clips!=null)return;
            var nextClips=new Dictionary<string,AdamArtClip>();var nextSprites=new Dictionary<string,Sprite[]>();
            var original=JsonUtility.FromJson<AdamArtIndex>(Resources.Load<TextAsset>("Art03/index").text);
            var bundles=new List<string>(Bundles);
            if(Resources.Load<TextAsset>(PolishRoot+"manifest")!=null)bundles.Add("Polish02");
            foreach(var bundle in bundles)
            {
                var path=bundle=="Polish02"?PolishRoot:Root+bundle+"/";
                var text=Resources.Load<TextAsset>(path+"manifest");if(text==null)throw new InvalidOperationException("Missing visual manifest "+bundle);
                foreach(var a in Parse(text.bytes,bundle).assets)
                {
                    var c=Contract(a,original);c.source=path+a.id;var frames=Resources.LoadAll<Sprite>(path+a.id);Array.Sort(frames,(x,y)=>string.CompareOrdinal(x.name,y.name));
                    if(frames.Length!=c.frames)throw new InvalidOperationException("Missing visual sprites "+c.id);
                    for(var i=0;i<frames.Length;i++)
                    {
                        var s=frames[i];Texture2D t=s.texture;
                        if(s.name!=c.id+"_"+i.ToString("D2") || s.rect!=new Rect(i*c.width,0,c.width,c.height) || s.pivot!=new Vector2(c.pivotX,c.pivotY) || s.border!=new Vector4(c.borders[0],c.borders[1],c.borders[2],c.borders[3]) ||
                            s.pixelsPerUnit!=32 || t.filterMode!=FilterMode.Point || t.wrapMode!=TextureWrapMode.Clamp || t.mipmapCount!=1 || t.width!=c.width*c.frames || t.height!=c.height)throw new InvalidOperationException("Visual import drift "+c.id);
                    }
                    // IMGUI's texture pass and the Linear framebuffer each encode gamma. Keep source PNGs intact;
                    // a bounded owned float copy cancels those two encodes without changing alpha or registration.
                    if(QualitySettings.activeColorSpace==ColorSpace.Linear)frames=PresentationFrames(frames);
                    nextClips.Add(c.id,c);nextSprites.Add(c.id,frames);
                }
            }
            sprites=nextSprites;clips=nextClips;
        }
        public static Color PresentationPixel(Color source,bool linear)=>linear?source.linear.linear:source;
        static Sprite[] PresentationFrames(Sprite[] source)
        {
            var original=source[0].texture;var raw=original.GetPixels32();var pixels=new Color[raw.Length];for(var i=0;i<pixels.Length;i++)pixels[i]=PresentationPixel((Color)raw[i],true);
            var texture=new Texture2D(original.width,original.height,TextureFormat.RGBAFloat,false,true){name=original.name+" owned IMGUI pixels",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels(pixels);texture.Apply(false,false);ownedPresentation.Add(texture);var result=new Sprite[source.Length];
            for(var i=0;i<source.Length;i++){var s=source[i];var item=UnityEngine.Sprite.Create(texture,s.rect,new Vector2(s.pivot.x/s.rect.width,s.pivot.y/s.rect.height),s.pixelsPerUnit,0,SpriteMeshType.FullRect,s.border);item.name=s.name;item.hideFlags=HideFlags.HideAndDontSave;ownedPresentation.Add(item);result[i]=item;}return result;
        }
        public static void Release()
        {
            for(var i=ownedPresentation.Count-1;i>=0;i--)if(ownedPresentation[i]!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(ownedPresentation[i]);else UnityEngine.Object.DestroyImmediate(ownedPresentation[i]);}
            ownedPresentation.Clear();clips=null;sprites=null;
        }
        public static void Reload(){Release();Load();}
        public static AdamArtClip GetClip(string id){Load();return clips[id];}
        public static bool TryGetClip(string id,out AdamArtClip clip){Load();return clips.TryGetValue(id,out clip);}
        public static Sprite Sprite(string id,int frame){Load();return sprites[id][Mathf.Clamp(frame,0,clips[id].frames-1)];}
        internal static void Apply(Dictionary<string,AdamArtClip> targetClips,Dictionary<string,Sprite[]> targetSprites)
        {Load();foreach(var pair in clips){targetClips[pair.Key]=pair.Value;targetSprites[pair.Key]=sprites[pair.Key];}}
    }
}
