using UnityEngine;

/// <summary>The materials a platform can have. Add new ones here, then add a row in SurfaceDatabase.</summary>
public enum SurfaceType { Normal, Ice, Mud, Boost, Bouncy }

public readonly struct SurfaceProfile
{
    public readonly string label;
    public readonly Color color;
    /// <summary>Multiplies ground acceleration/deceleration. Low = slippery.</summary>
    public readonly float grip;
    /// <summary>Multiplies run speed while standing on it (kept in the air until you land).</summary>
    public readonly float speed;
    /// <summary>Multiplies the first (ground) jump.</summary>
    public readonly float jump;

    public SurfaceProfile(string label, Color color, float grip, float speed, float jump)
    {
        this.label = label;
        this.color = color;
        this.grip = grip;
        this.speed = speed;
        this.jump = jump;
    }
}

/// <summary>All the tuning numbers for every material live here.</summary>
public static class SurfaceDatabase
{
    public static SurfaceProfile Get(SurfaceType type)
    {
        switch (type)
        {
            case SurfaceType.Ice:
                // Slides like Minecraft ice: very low grip, but faster top speed.
                return new SurfaceProfile("Ice", new Color(0.55f, 0.85f, 1f), 0.08f, 1.35f, 1f);
            case SurfaceType.Mud:
                return new SurfaceProfile("Mud", new Color(0.45f, 0.30f, 0.15f), 1.6f, 0.55f, 0.85f);
            case SurfaceType.Boost:
                return new SurfaceProfile("Boost", new Color(1f, 0.85f, 0.2f), 1.3f, 1.7f, 1f);
            case SurfaceType.Bouncy:
                return new SurfaceProfile("Bouncy", new Color(1f, 0.4f, 0.8f), 1f, 1f, 1.5f);
            default:
                return new SurfaceProfile("Normal", Color.white, 1f, 1f, 1f);
        }
    }
}

/// <summary>
/// Put on any platform to give it a surface type. PlayerController reads this from
/// whatever the player is standing on. Manipulable adds it automatically if
/// "Can Change Material" is ticked.
/// </summary>
[DisallowMultipleComponent]
public class SurfaceMaterial : MonoBehaviour
{
    public SurfaceType type = SurfaceType.Normal;
    public bool tintSprites = true;

    SpriteRenderer[] sprites;
    Color[] originalColors;

    public float Grip => SurfaceDatabase.Get(type).grip;
    public float SpeedMultiplier => SurfaceDatabase.Get(type).speed;
    public float JumpMultiplier => SurfaceDatabase.Get(type).jump;

    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) originalColors[i] = sprites[i].color;
        Apply();
    }

    public void SetType(SurfaceType newType)
    {
        type = newType;
        Apply();
    }

    void Apply()
    {
        if (!tintSprites || sprites == null) return;

        Color tint = SurfaceDatabase.Get(type).color;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Color c = type == SurfaceType.Normal ? originalColors[i] : tint;
            c.a = sprites[i].color.a; // keep alpha so ShiftPlatform fading still works
            sprites[i].color = c;
        }
    }
}
