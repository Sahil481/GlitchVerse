using UnityEngine;

/// <summary>
/// Code-built particle bursts, rings and after-images. No assets needed: textures and materials are generated at runtime.
/// Uses the built-in "Sprites/Default" shader so effects are unlit and glow through the bloom post effect.
/// </summary>
public static class FxLib
{
    static Texture2D dotTex, ringTex;
    static Sprite dotSprite, ringSprite;
    static Material particleMat, spriteMat;

    public static readonly Color Cyan = new Color(0.16f, 0.94f, 1f, 1f);
    public static readonly Color Magenta = new Color(1f, 0.24f, 0.95f, 1f);
    public static readonly Color Violet = new Color(0.61f, 0.42f, 1f, 1f);
    public static readonly Color Amber = new Color(1f, 0.70f, 0.28f, 1f);
    public static readonly Color Mint = new Color(0.36f, 1f, 0.69f, 1f);
    public static readonly Color Red = new Color(1f, 0.23f, 0.42f, 1f);

    public static Material SpriteMat
    {
        get
        {
            if (spriteMat == null) spriteMat = new Material(Shader.Find("Sprites/Default"));
            return spriteMat;
        }
    }

    public static Material ParticleMat
    {
        get
        {
            if (particleMat == null)
            {
                particleMat = new Material(Shader.Find("Sprites/Default"));
                particleMat.mainTexture = DotTexture;
            }
            return particleMat;
        }
    }

    public static Texture2D DotTexture
    {
        get
        {
            if (dotTex == null)
            {
                int n = 64;
                dotTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                dotTex.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - r);
                        a = a * a * (3f - 2f * a);
                        dotTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                dotTex.Apply();
            }
            return dotTex;
        }
    }

    public static Sprite RingSprite
    {
        get
        {
            if (ringSprite == null)
            {
                int n = 128;
                ringTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                ringTex.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.12f);
                        a = a * a * (3f - 2f * a);
                        ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                ringTex.Apply();
                ringSprite = Sprite.Create(ringTex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
            return ringSprite;
        }
    }

    public static Sprite DotSprite
    {
        get
        {
            if (dotSprite == null)
                dotSprite = Sprite.Create(DotTexture, new Rect(0, 0, DotTexture.width, DotTexture.height), new Vector2(0.5f, 0.5f), DotTexture.width);
            return dotSprite;
        }
    }

    /// <summary>One-shot burst of glowing dots. angleDeg is the centre direction, spreadDeg the cone width.</summary>
    public static void Burst(Vector3 pos, Color color, int count, float speed, float size, float life,
                             float gravity = 0f, float angleDeg = 90f, float spreadDeg = 360f, int order = 30)
    {
        if (count <= 0) return;
        pos.z = 0f;
        GameObject go = new GameObject("FxBurst");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = gravity;
        main.maxParticles = count + 8;

        ParticleSystem.EmissionModule em = ps.emission;
        em.enabled = false;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.enabled = false;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = ParticleMat;
        rend.sortingOrder = order;

        ps.Play();
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            float a = (angleDeg + Random.Range(-spreadDeg, spreadDeg) * 0.5f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            ep.position = pos;
            ep.velocity = dir * speed * Random.Range(0.35f, 1f);
            ep.startLifetime = life * Random.Range(0.6f, 1f);
            ep.startSize = size * Random.Range(0.5f, 1.25f);
            ep.startColor = color;
            ps.Emit(ep, 1);
        }
        Object.Destroy(go, life + 0.7f);
    }

    /// <summary>Expanding, fading ring.</summary>
    public static void Ring(Vector3 pos, Color color, float startSize, float endSize, float time, int order = 29)
    {
        pos.z = 0f;
        GameObject go = new GameObject("FxRing");
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RingSprite;
        sr.sharedMaterial = SpriteMat;
        sr.sortingOrder = order;
        sr.color = color;
        FxFade f = go.AddComponent<FxFade>();
        f.Begin(color, time, startSize, endSize, 1.4f);
    }

    /// <summary>Fading copy of a sprite (dash after-images).</summary>
    public static void Ghost(SpriteRenderer src, Color tint, float life)
    {
        if (src == null || src.sprite == null) return;
        GameObject go = new GameObject("FxGhost");
        go.transform.position = src.transform.position;
        go.transform.rotation = src.transform.rotation;
        go.transform.localScale = src.transform.lossyScale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = src.sprite;
        sr.flipX = src.flipX;
        sr.flipY = src.flipY;
        sr.sharedMaterial = SpriteMat;
        sr.sortingOrder = src.sortingOrder - 1;
        sr.color = tint;
        FxFade f = go.AddComponent<FxFade>();
        f.Begin(tint, life, 1f, 1f, 1.2f);
    }

    /// <summary>Make a ParticleSystem that keeps emitting (call from Start). Caller sets rate and shape afterwards.</summary>
    public static ParticleSystem Emitter(Transform parent, string name, Vector3 localPos, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = ParticleMat;
        rend.sortingOrder = order;
        return ps;
    }
}
