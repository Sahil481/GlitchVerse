using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives the post-processing volume at runtime: chromatic-aberration / bloom kicks on impacts and dashes,
/// and a red vignette pulse when the ramen is almost cold.
/// </summary>
[RequireComponent(typeof(Volume))]
public class PostFx : MonoBehaviour
{
    public static PostFx Instance { get; private set; }

    Volume vol;
    Bloom bloom;
    Vignette vignette;
    ChromaticAberration chroma;
    float baseBloom = 0.8f, baseVig = 0.28f;
    Color baseVigColor = Color.black;
    float chromaKick, bloomKick;

    void Awake()
    {
        Instance = this;
        vol = GetComponent<Volume>();
        VolumeProfile p = vol.profile;           // runtime copy
        if (p != null)
        {
            p.TryGet(out bloom);
            p.TryGet(out vignette);
            p.TryGet(out chroma);
        }
        if (bloom != null) baseBloom = bloom.intensity.value;
        if (vignette != null) { baseVig = vignette.intensity.value; baseVigColor = vignette.color.value; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Kick(float chromaAmount, float bloomAmount)
    {
        chromaKick = Mathf.Max(chromaKick, chromaAmount);
        bloomKick = Mathf.Max(bloomKick, bloomAmount);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        chromaKick = Mathf.Lerp(chromaKick, 0f, 1f - Mathf.Exp(-9f * dt));
        bloomKick = Mathf.Lerp(bloomKick, 0f, 1f - Mathf.Exp(-6f * dt));

        if (chroma != null) chroma.intensity.Override(Mathf.Clamp01(0.04f + chromaKick));
        if (bloom != null) bloom.intensity.Override(baseBloom + bloomKick);

        if (vignette != null)
        {
            float vi = baseVig;
            Color vc = baseVigColor;
            LevelManager lm = LevelManager.Instance;
            if (lm != null && lm.CurrentState == LevelManager.State.Playing && lm.TimeLeft < 20f)
            {
                float urgency = 1f - lm.TimeLeft / 20f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (4f + 6f * urgency));
                vi = baseVig + urgency * (0.12f + 0.1f * pulse);
                vc = Color.Lerp(baseVigColor, new Color(0.9f, 0.05f, 0.2f), urgency * (0.5f + 0.5f * pulse));
            }
            vignette.intensity.Override(vi);
            vignette.color.Override(vc);
        }
    }
}
