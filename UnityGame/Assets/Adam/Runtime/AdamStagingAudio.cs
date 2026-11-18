using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal enum AdamStagingCue { Fall, Arrive, Seal }

    internal sealed class AdamStagingAudio
    {
        internal const string ResourceRoot = "BossStaging03/";
        static readonly string[] ClipNames = { "sfx_boss-fall", "sfx_boss-arrive", "sfx_boss-seal" };
        static readonly HashSet<(AdamStagingCue cue, string id)> Delivered = new HashSet<(AdamStagingCue, string)>();
        readonly AudioSource voice;
        readonly AudioClip[] clips;
        AdamStagingCue playing;

        public AdamStagingAudio(GameObject owner)
        {
            var child = new GameObject("boss staging voice");
            child.transform.SetParent(owner.transform, false);
            voice = child.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0;
            voice.dopplerLevel = 0;
            clips = System.Array.ConvertAll(ClipNames, name => Resources.Load<AudioClip>(ResourceRoot + name));
        }

        public void SetVolume(float volume)
        {
            if (volume <= 0) Stop();
            voice.volume = Mathf.Clamp01(volume);
        }

        // True means this cue key was consumed. A busy voice or a closed gate never queues it.
        public bool Due(AdamStagingCue cue, string id, bool allowed)
        {
            if (string.IsNullOrWhiteSpace(id) || !Delivered.Add((cue, id))) return false;
            var clip = clips[(int)cue];
            if (!allowed || clip == null || (voice.isPlaying && cue >= playing)) return true;
            voice.Stop();
            voice.clip = clip;
            playing = cue;
            voice.Play();
            return true;
        }

        public void Stop()
        {
            if (voice == null || voice.clip == null) return;
            voice.Stop();
            voice.clip = null;
        }

        public void Dispose()
        {
            Stop();
            if (voice != null) Object.Destroy(voice.gameObject);
        }
    }
}
