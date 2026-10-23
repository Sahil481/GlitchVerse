using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiny sound-effect player. Clips are loaded from Resources/GlitchVerse/Sfx/&lt;name&gt;.
/// If a clip is missing nothing happens, so gameplay never depends on audio files.
/// </summary>
public static class SfxHub
{
    public static float Volume = 0.8f;

    static AudioSource source;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static void Play(string name, Vector3 position)
    {
        AudioClip clip = Get(name);
        if (clip == null) return;
        Ensure();
        source.PlayOneShot(clip, Volume);
    }

    public static void Play(string name)
    {
        Play(name, Vector3.zero);
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
        if (source != null) return;
        GameObject go = new GameObject("SfxHub");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.playOnAwake = false;
    }
}
