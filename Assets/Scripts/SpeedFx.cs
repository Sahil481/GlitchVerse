using UnityEngine;

/// <summary>
/// "Feeling of speed" effects: wind streaks, after-image trail, foot dust, camera pull-back and a touch of
/// chromatic aberration. Starts when you sprint, goes wild when you move faster than a sprint (ice, boost, dashes).
/// Attaches itself to the player automatically.
/// </summary>
public class SpeedFx : MonoBehaviour
{
    const float StartSpeed = 8.5f;    // just above walking
    const float SprintSpeed = 13f;    // full sprint = tier 1 maxed
    const float FastSpeed = 14f;      // beyond sprint
    const float MaxSpeed = 23f;

    Rigidbody2D rb;
    PlayerController pc;
    SpriteRenderer sr;
    ParticleSystem streaks, sparks;
    ParticleSystem.EmissionModule streakEm, sparkEm;
    AudioSource wind;
    float ghostTimer, dustTimer, zoom, tier1, tier2, srSearch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, mode) => Ensure();
        Ensure();
    }

    static void Ensure()
    {
        PlayerController p = FindFirstObjectByType<PlayerController>();
        if (p == null || p.GetComponent<SpeedFx>() != null) return;
        p.gameObject.AddComponent<SpeedFx>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pc = GetComponent<PlayerController>();

        streaks = FxLib.Emitter(transform, "SpeedStreaks", Vector3.zero, 45);
        ParticleSystem.MainModule m = streaks.main;
        m.loop = true;
        m.playOnAwake = false;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        m.maxParticles = 160;
        streakEm = streaks.emission;
        streakEm.rateOverTime = 0f;
        ParticleSystem.ShapeModule sh = streaks.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(1f, 7f, 0.1f);
        ParticleSystem.ColorOverLifetimeModule col = streaks.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystemRenderer r = streaks.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 5f;
        r.velocityScale = 0.12f;
        r.alignment = ParticleSystemRenderSpace.View;

        sparks = FxLib.Emitter(transform, "SpeedSparks", Vector3.zero, 44);
        ParticleSystem.MainModule sm = sparks.main;
        sm.loop = true;
        sm.playOnAwake = false;
        sm.simulationSpace = ParticleSystemSimulationSpace.World;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        sm.maxParticles = 120;
        sparkEm = sparks.emission;
        sparkEm.rateOverTime = 0f;
        ParticleSystem.ShapeModule ssh = sparks.shape;
        ssh.shapeType = ParticleSystemShapeType.Box;
        ssh.scale = new Vector3(0.4f, 1.4f, 0.1f);
        ParticleSystem.ColorOverLifetimeModule scol = sparks.colorOverLifetime;
        scol.enabled = true;
        scol.color = g;
        ParticleSystem.SizeOverLifetimeModule ssz = sparks.sizeOverLifetime;
        ssz.enabled = true;
        ssz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        wind = gameObject.AddComponent<AudioSource>();
        wind.clip = Resources.Load<AudioClip>("GlitchVerse/Sfx/wind_loop");
        wind.loop = true;
        wind.playOnAwake = false;
        wind.spatialBlend = 0f;
        wind.volume = 0f;

        streaks.Play();
        sparks.Play();
    }

    SpriteRenderer FindHeroSprite()
    {
        SpriteRenderer best = null;
        float bestArea = 0f;
        foreach (SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!s.enabled || s.sprite == null || s.color.a < 0.1f) continue;
            float a = s.bounds.size.x * s.bounds.size.y;
            if (a > bestArea) { bestArea = a; best = s; }
        }
        return best;
    }

    void Update()
    {
        if (rb == null || streaks == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        bool dead = pc != null && pc.IsDead;
        Vector2 v = rb.linearVelocity;
        float speed = dead ? 0f : Mathf.Abs(v.x);
        // dashes count as "faster than sprint" too
        if (pc != null && pc.IsDashing) speed = Mathf.Max(speed, 20f);

        float t1 = Mathf.InverseLerp(StartSpeed, SprintSpeed, speed);
        float t2 = Mathf.InverseLerp(FastSpeed, MaxSpeed, speed);
        tier1 = Mathf.Lerp(tier1, t1, 1f - Mathf.Exp(-8f * dt));
        tier2 = Mathf.Lerp(tier2, t2, 1f - Mathf.Exp(-8f * dt));
        float dir = v.x >= 0f ? 1f : -1f;

        Color baseC = Color.Lerp(new Color(0.55f, 0.95f, 1f), new Color(1f, 0.85f, 0.45f), tier2);
        baseC = Color.Lerp(baseC, new Color(1f, 0.45f, 0.95f), tier2 * tier2 * 0.5f);

        // ---- wind streaks: spawn ahead of the hero, fly backwards past the screen
        var main = streaks.main;
        main.startColor = new Color(baseC.r, baseC.g, baseC.b, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed * 0.95f + 1f);
        streaks.transform.position = transform.position + new Vector3(dir * 5.5f, 0f, 0f);
        streaks.transform.rotation = Quaternion.Euler(0f, 0f, dir > 0f ? 180f : 0f);
        streakEm.rateOverTime = Mathf.Lerp(0f, 22f, tier1) + tier2 * 55f;
        // streak velocity is along local +X of the emitter; rotate the shape so +X points backwards
        var shape = streaks.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.2f, 7f + tier2 * 3f, 0.1f);
        // Box emits along +Z by default; use the transform rotation trick: face particles along the emitter's X axis
        shape.rotation = new Vector3(0f, 90f, 0f);

        // ---- sparks flying off the hero at tier 2
        var sm = sparks.main;
        sm.startColor = new Color(baseC.r, baseC.g, baseC.b, 0.9f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3.5f);
        sparks.transform.position = transform.position + new Vector3(-dir * 0.3f, -0.1f, 0f);
        sparkEm.rateOverTime = tier2 * 40f;

        // ---- after-image trail
        ghostTimer -= dt;
        if (tier2 > 0.08f && ghostTimer <= 0f)
        {
            ghostTimer = Mathf.Lerp(0.07f, 0.035f, tier2);
            srSearch -= 1f;
            if (sr == null || !sr.enabled || srSearch <= 0f) { sr = FindHeroSprite(); srSearch = 30f; }
            if (sr != null) FxLib.Ghost(sr, new Color(baseC.r, baseC.g, baseC.b, 0.32f + 0.2f * tier2), 0.28f);
        }

        // ---- foot dust while sprinting on the ground
        dustTimer -= dt;
        if (pc != null && pc.IsGrounded && tier1 > 0.3f && dustTimer <= 0f)
        {
            dustTimer = Mathf.Lerp(0.16f, 0.07f, tier1);
            Vector3 feet = transform.position + Vector3.down * 0.8f;
            FxLib.Burst(feet, new Color(0.75f, 0.9f, 1f, 0.8f), 2 + (int)(tier2 * 3f), 2.5f + tier2 * 2f, 0.12f, 0.35f, 0f,
                        dir > 0f ? 160f : 20f, 40f);
        }

        // ---- wind noise: a soft whoosh when sprinting, a roar when faster than that
        if (wind != null && wind.clip != null)
        {
            float wv = (0.07f * tier1 + 0.5f * tier2) * SfxHub.Volume;
            wind.volume = Mathf.MoveTowards(wind.volume, wv, dt * 1.2f);
            wind.pitch = 0.85f + 0.35f * tier1 + 0.35f * tier2;
            if (wind.volume > 0.005f && !wind.isPlaying) wind.Play();
            else if (wind.volume <= 0.005f && wind.isPlaying) wind.Stop();
        }

        // ---- camera pull-back + faint chroma
        float targetZoom = tier1 * 0.25f + tier2 * 0.9f;
        zoom = Mathf.Lerp(zoom, targetZoom, 1f - Mathf.Exp(-3f * dt));
        if (CameraFx.Instance != null) CameraFx.Instance.SetExtraZoom(zoom);
        if (tier2 > 0.05f && PostFx.Instance != null) PostFx.Instance.Kick(0.06f + 0.14f * tier2, 0f);
    }

    void OnDisable()
    {
        if (CameraFx.Instance != null) CameraFx.Instance.SetExtraZoom(0f);
    }
}
