using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiny sound-effect player with a small voice pool so each sound can have its own volume and pitch.
/// Clips are loaded from Resources/GlitchVerse/Sfx/&lt;name&gt;. If a clip is missing nothing happens,
/// so gameplay never depends on audio files. Works while the game is paused (Time.timeScale = 0).
/// </summary>
public static class SfxHub
{
    public static float Volume = 0.8f;

    const int Voices = 10;
    static AudioSource[] pool;
    static int next;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { pool = null; cache.Clear(); lastPlayed.Clear(); }

    public static void Play(string name, Vector3 position) { Play(name, 1f, 1f); }
    public static void Play(string name) { Play(name, 1f, 1f); }

    /// <summary>Plays a sound with a volume multiplier and pitch (1 = normal).</summary>
    public static void Play(string name, float volume, float pitch)
    {
        AudioClip clip = Get(name);
        if (clip == null) return;
        // stop machine-gunning the same sound within one frame or two
        float now = Time.unscaledTime, last;
        if (lastPlayed.TryGetValue(name, out last) && now - last < 0.03f) return;
        lastPlayed[name] = now;

        Ensure();
        AudioSource src = null;
        for (int i = 0; i < Voices; i++)
        {
            AudioSource s = pool[(next + i) % Voices];
            if (s != null && !s.isPlaying) { src = s; next = (next + i + 1) % Voices; break; }
        }
        if (src == null) { src = pool[next]; next = (next + 1) % Voices; }
        if (src == null) return;
        src.clip = clip;
        src.volume = Mathf.Clamp01(Volume * volume);
        src.pitch = Mathf.Clamp(pitch, 0.3f, 3f);
        src.Play();
    }

    /// <summary>Plays with a small random pitch variation so repeated sounds (steps, shards) don't feel robotic.</summary>
    public static void PlayVaried(string name, float volume = 1f, float pitchSpread = 0.08f)
    {
        Play(name, volume, 1f + Random.Range(-pitchSpread, pitchSpread));
    }

    static AudioClip Get(string name)
    {
        AudioClip clip;
        if (cache.TryGetValue(name, out clip) && clip != null) return clip;
        clip = Resources.Load<AudioClip>("GlitchVerse/Sfx/" + name);
        cache[name] = clip;
        return clip;
    }

    static void Ensure()
    {
        if (pool != null && pool[0] != null) return;
        GameObject go = new GameObject("SfxHub");
        Object.DontDestroyOnLoad(go);
        pool = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            AudioSource s = go.AddComponent<AudioSource>();
            s.spatialBlend = 0f;
            s.playOnAwake = false;
            s.ignoreListenerPause = true;
            pool[i] = s;
        }
    }
}
