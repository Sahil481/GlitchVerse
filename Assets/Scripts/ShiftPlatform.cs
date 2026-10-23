using UnityEngine;

/// <summary>
/// Put on platforms that live in the other dimension. Hidden and non-solid normally;
/// visible and solid while DimensionShift is active. Tick "Inverted" for platforms
/// that exist normally but vanish during the shift.
/// Remember: the platform's layer must be in PlayerController's Ground Layers.
/// </summary>
public class ShiftPlatform : MonoBehaviour
{
    [Tooltip("Off = solid only while shifted. On = solid only while NOT shifted.")]
    public bool inverted = false;
    [Range(0f, 1f)] public float hiddenAlpha = 0f;
    public float fadeSpeed = 8f;

    Collider2D[] colliders;
    SpriteRenderer[] sprites;
    float[] baseAlpha;
    Manipulable manipulable;
    float alpha;
    float targetAlpha;

    void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        manipulable = GetComponent<Manipulable>();

        baseAlpha = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) baseAlpha[i] = sprites[i].color.a;
    }

    void OnEnable()
    {
        DimensionShift.ShiftChanged += OnShiftChanged;
        Refresh();
        alpha = targetAlpha; // no fade on scene start
        ApplyAlpha();
    }

    void OnDisable() { DimensionShift.ShiftChanged -= OnShiftChanged; }

    void OnShiftChanged(bool shifted) { Refresh(); }

    /// <summary>Re-applies solid/visible state. Also called by Manipulable after a reset.</summary>
    public void Refresh()
    {
        bool solid = inverted ? !DimensionShift.IsShifted : DimensionShift.IsShifted;
        if (manipulable != null && manipulable.IsDeleted) solid = false;

        foreach (var c in colliders) if (c != null) c.enabled = solid;
        targetAlpha = solid ? 1f : hiddenAlpha;
    }

    void Update()
    {
        if (Mathf.Approximately(alpha, targetAlpha)) return;
        alpha = Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        ApplyAlpha();
    }

    void ApplyAlpha()
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Color c = sprites[i].color;
            c.a = baseAlpha[i] * alpha;
            sprites[i].color = c;
        }
    }
}
