using UnityEngine;

/// <summary>Plays a looping music track from Resources/GlitchVerse/Music/&lt;trackName&gt;.</summary>
public class MusicPlayer : MonoBehaviour
{
    public string trackName = "theme_calm";
    [Range(0f, 1f)] public float volume = 0.35f;

    void Start()
    {
        AudioClip clip = Resources.Load<AudioClip>("GlitchVerse/Music/" + trackName);
        if (clip == null) return;
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.volume = volume;
        src.spatialBlend = 0f;
        src.ignoreListenerPause = true;
        src.Play();
    }
}
