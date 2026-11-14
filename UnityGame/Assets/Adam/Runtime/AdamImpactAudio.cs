using System;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Original procedural PCM sketch, not sampled audio or final commercial sound design.
    // One prewarmed source per voice; no PlayOneShot allocation or gameplay random generator.
    public sealed class AdamImpactAudio : IDisposable
    {
        public const int VoiceCapacity=8;
        public const int SampleRate=22050;
        public static readonly string[] Weapons={"bible","jawbone","sling"};
        readonly AudioListener ownedListener;
        readonly AudioSource[] voices=new AudioSource[VoiceCapacity];
        readonly AudioClip[] clips=new AudioClip[3];
        readonly double[] ends=new double[VoiceCapacity];
        readonly double[] last={-100,-100,-100};
        int generation=-1;
        long cursor;
        public bool Muted;
        public float Volume=.5f;
        public long PlayedCount {get;private set;}
        public int ActiveVoices {get;private set;}
        public AdamImpactAudio(GameObject owner)
        {
            if(UnityEngine.Object.FindFirstObjectByType<AudioListener>()==null)ownedListener=owner.AddComponent<AudioListener>();
            for(var i=0;i<3;i++)
            {
                var samples=Samples(Weapons[i]);clips[i]=AudioClip.Create("original-pcm-"+Weapons[i],samples.Length,1,SampleRate,false);
                clips[i].SetData(samples,0);
            }
            for(var i=0;i<voices.Length;i++)
            {var child=new GameObject("impact voice "+i);child.transform.SetParent(owner.transform,false);voices[i]=child.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
        }
        public static float[] Samples(string weapon)
        {
            var index=Array.IndexOf(Weapons,weapon);if(index<0)throw new ArgumentException("Unknown impact weapon");
            var length=index==1?.14:.095;var samples=new float[(int)(SampleRate*length)];uint noise=0x514e3a7u;
            for(var n=0;n<samples.Length;n++)
            {
                var t=n/(double)SampleRate;var envelope=Math.Min(1,t/.002)*Math.Exp(-t*(index==1?35:55));
                noise^=noise<<13;noise^=noise>>17;noise^=noise<<5;var grit=(noise/(double)uint.MaxValue*2-1);
                var tone=index==0 ? Math.Sin(2*Math.PI*740*t)+.35*Math.Sin(2*Math.PI*1180*t) :
                    index==1 ? Math.Sin(2*Math.PI*(120*t-90*t*t))+.6*grit : .6*Math.Sin(2*Math.PI*1900*t)+.7*grit;
                samples[n]=(float)(.35*envelope*tone);
            }
            return samples;
        }
        public void Update(AdamHost host,AdamArtWorld view)
        {
            if(generation!=view.Generation){Stop();cursor=0;generation=view.Generation;for(var i=0;i<last.Length;i++)last[i]=-100;}
            if(host==null || !host.CanPlay || Muted || Volume<=0){Stop();foreach(var impact in view.FrameImpacts)cursor=Math.Max(cursor,impact.Sequence);return;}
            ActiveVoices=0;
            for(var i=0;i<ends.Length;i++) {if(ends[i]<=view.Clock){ends[i]=0;voices[i].Stop();}else ActiveVoices++;}
            foreach(var impact in view.FrameImpacts)
            {
                if(impact.Sequence<=cursor)continue;cursor=impact.Sequence;
                var kind=Array.IndexOf(Weapons,impact.Cause);if(kind<0 || view.Clock-last[kind]<.035 || ActiveVoices>=VoiceCapacity)continue;
                var slot=Array.FindIndex(ends,e=>e==0);if(slot<0)break;
                var source=voices[slot];source.clip=clips[kind];
                var distance=UnityEngine.Vector2.Distance(new UnityEngine.Vector2(impact.Position.X,impact.Position.Y),
                    new UnityEngine.Vector2(view.Snapshot.PlayerX,view.Snapshot.PlayerY));
                source.volume=Mathf.Clamp01(Volume)*Mathf.Clamp01(1-distance/20f);
                source.pitch=1;source.Play();ends[slot]=view.Clock+clips[kind].length;last[kind]=view.Clock;ActiveVoices++;PlayedCount++;
            }
        }
        public void Stop(){for(var i=0;i<voices.Length;i++){if(voices[i]!=null)voices[i].Stop();ends[i]=0;}ActiveVoices=0;}
        public void Dispose(){Stop();if(ownedListener!=null)UnityEngine.Object.Destroy(ownedListener);foreach(var source in voices)if(source!=null)UnityEngine.Object.Destroy(source.gameObject);foreach(var clip in clips)if(clip!=null)UnityEngine.Object.Destroy(clip);}
    }
}
