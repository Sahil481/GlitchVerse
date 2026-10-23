using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The courier. Plays the hand-cut sprite-sheet animations (idle, run, jump, fall, land, dash, phase, shrink,
/// death, respawn, cheer) on a separate "HeroRig" object that follows the physics body, so squash and size changes
/// never touch the collider. Also drives the hero's neon light, sparkles and effects.
/// Lives on the same object as PlayerController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerVisuals : MonoBehaviour
{
    public Sprite heroSprite;              // fallback single image if the sheet is missing
    public SpriteRenderer body;            // legacy renderer on the player object, switched off at runtime
    public float heroHeight = 1.85f;       // only used for the fallback image
    public Color glowColor = new Color(0.16f, 0.94f, 1f, 1f);
    public Color shiftColor = new Color(1f, 0.24f, 0.95f, 1f);

    /// <summary>Set by the delivery NPC: makes the hero cheer.</summary>
    public static bool Celebrate;

    const string SheetPath = "GlitchVerse/hero_sheet";

    Rigidbody2D rb;
    PlayerController pc;
    Collider2D col;
    Transform rig;
    SpriteRenderer vsr, halo;
    NeonLight light;
    Transform lightRoot;
    ParticleSystem sparkles;
    Vector3 baseScale = Vector3.one;

    readonly Dictionary<string, Sprite[]> anims = new Dictionary<string, Sprite[]>();
    bool haveSheet;

    // animation state
    string current = "idle";
    float animTime;
    bool facingRight = true;
    float flipT = 1f;
    float stepClock, ghostClock;
    float lastVy;
    float landTimer, jumpTimer, shrinkTimer, respawnTimer, deathTimer;
    bool wasGrounded = true, wasDashing, wasPhasing, wasSmall, wasDead;
    float airTime, holdTimer;
    bool runLatched;

    // testing aids: F2 shows the current animation, F3 cycles through every animation, F4 returns to normal
    static readonly string[] TestOrder = { "idle", "run", "jump", "fall", "land", "dash", "phase", "shrink", "death", "respawn", "cheer" };
    bool debugOverlay;
    int forcedIndex = -1;
    string forcedName;
    float forcedFrameTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pc = GetComponent<PlayerController>();
        col = GetComponent<Collider2D>();
        if (body == null) body = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        if (Mathf.Abs(baseScale.y) < 0.01f) baseScale = Vector3.one;
        Celebrate = false;
        LoadSheet();
        BuildRig();
    }

    // The sheet is cut into frames here at runtime (7 x N grid), so it works whatever import / slicing
    // settings Unity applied to the PNG, even if it was auto-sliced by hand.
    const int Cols = 7;
    const float DesignWidth = 3584f;      // width the sheet was authored at
    const float DesignPpu = 270f;
    const float FeetPivot = 28f / 576f;

    void LoadSheet()
    {
        Texture2D tex = Resources.Load<Texture2D>(SheetPath);
        TextAsset names = Resources.Load<TextAsset>(SheetPath + "_frames");
        if (tex == null || names == null)
        {
            Debug.LogWarning("PlayerVisuals: hero sheet not found in Resources/GlitchVerse (hero_sheet.png + hero_sheet_frames.txt). Using the single hero image.");
            return;
        }
        string[] list = names.text.Split(new[] { '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        int rows = (list.Length + Cols - 1) / Cols;
        float cw = tex.width / (float)Cols;
        float ch = tex.height / (float)rows;
        float ppu = DesignPpu * (tex.width / DesignWidth);

        Dictionary<string, SortedList<int, Sprite>> tmp = new Dictionary<string, SortedList<int, Sprite>>();
        for (int i = 0; i < list.Length; i++)
        {
            string n = list[i];
            int us = n.LastIndexOf('_');
            int idx;
            if (us < 0 || !int.TryParse(n.Substring(us + 1), out idx)) continue;
            string key = n.Substring(0, us).Replace("hero_", "");
            int r = i / Cols, c = i % Cols;
            Rect rect = new Rect(c * cw, tex.height - (r + 1) * ch, cw, ch);
            Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, FeetPivot), ppu, 0, SpriteMeshType.FullRect);
            sp.name = n;
            if (!tmp.ContainsKey(key)) tmp[key] = new SortedList<int, Sprite>();
            tmp[key][idx] = sp;
        }
        foreach (KeyValuePair<string, SortedList<int, Sprite>> kv in tmp)
        {
            Sprite[] arr = new Sprite[kv.Value.Count];
            kv.Value.Values.CopyTo(arr, 0);
            anims[kv.Key] = arr;
        }
        haveSheet = anims.ContainsKey("idle");
        if (haveSheet) Debug.Log("PlayerVisuals: hero sheet loaded (" + list.Length + " frames, " + anims.Count + " animations).");
    }

    void BuildRig()
    {
        if (body != null) body.enabled = false;

        GameObject v = new GameObject("HeroRig");
        rig = v.transform;
        vsr = v.AddComponent<SpriteRenderer>();
        vsr.sharedMaterial = FxLib.SpriteMat;
        vsr.sortingOrder = 10;
        vsr.sprite = haveSheet ? anims["idle"][0] : (heroSprite != null ? heroSprite : (body != null ? body.sprite : null));

        GameObject h = new GameObject("Halo");
        h.transform.SetParent(rig, false);
        halo = h.AddComponent<SpriteRenderer>();
        halo.sprite = FxLib.DotSprite;
        halo.sharedMaterial = FxLib.SpriteMat;
        halo.sortingOrder = 9;
        halo.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.35f);

        GameObject lr = new GameObject("HeroLight");
        lightRoot = lr.transform;
        light = lr.AddComponent<NeonLight>();
        light.color = glowColor;
        light.radius = 12f;
        light.innerRadius = 0.6f;
        light.intensity = 1.9f;
        light.pulseAmount = 0.08f;
        light.pulseSpeed = 2.6f;

        sparkles = FxLib.Emitter(lightRoot, "HeroSparkles", Vector3.zero, 12);
        ParticleSystem.MainModule m = sparkles.main;
        m.loop = true;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        m.startColor = new ParticleSystem.MinMaxGradient(glowColor, shiftColor);
        m.maxParticles = 60;
        ParticleSystem.EmissionModule em = sparkles.emission;
        em.rateOverTime = 12f;
        ParticleSystem.ShapeModule sh = sparkles.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(0.9f, 1.6f, 0.1f);
        ParticleSystem.VelocityOverLifetimeModule vel = sparkles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        ParticleSystem.ColorOverLifetimeModule cl = sparkles.colorOverLifetime;
        cl.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        cl.color = g;
        sparkles.Play();

        Follow(1f);
    }

    void OnDestroy()
    {
        if (lightRoot != null) Destroy(lightRoot.gameObject);
        if (rig != null) Destroy(rig.gameObject);
    }

    void OnEnable() { PlayerController.PlayerRespawned += OnRespawn; }
    void OnDisable() { PlayerController.PlayerRespawned -= OnRespawn; }

    void OnRespawn()
    {
        if (rig == null) return;
        respawnTimer = 0.34f;
        deathTimer = 0f;
        animTime = 0f;
        Celebrate = false;
        Vector3 p = transform.position;
        FxLib.Ring(p, glowColor, 0.4f, 3.5f, 0.5f);
        FxLib.Burst(p, glowColor, 24, 7f, 0.16f, 0.6f);
        if (light != null) light.Flash(2.2f);
        SfxHub.Play("checkpoint");
    }

    // ------------------------------------------------------------------ per frame
    void LateUpdate()
    {
        if (rig == null) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (dt <= 0f) return;

        Vector2 v = rb.linearVelocity;
        bool dead = pc != null && pc.IsDead;
        bool grounded = pc != null && pc.IsGrounded;
        bool dashing = pc != null && pc.IsDashing;
        bool phasing = pc != null && pc.IsPhasing;
        bool small = pc != null && pc.IsSmall;
        float up = rb.gravityScale < 0f ? -1f : 1f;
        Vector3 pos = transform.position;
        Vector3 feet = FeetPoint(up);

        // ---------------- events
        if (dead && !wasDead) OnDeath(pos);
        wasDead = dead;
        if (dead) deathTimer += dt;

        if (!dead)
        {
            if (wasGrounded && !grounded && v.y * up > 2f)
            {
                jumpTimer = 0.0001f;
                FxLib.Burst(feet, new Color(0.7f, 0.9f, 1f), 5, 3f, 0.12f, 0.35f, 0f, 90f - (up < 0 ? 180f : 0f), 80f);
                SfxHub.Play("jump");
            }
            if (!wasGrounded && grounded)
            {
                float impact = Mathf.Clamp01(Mathf.Abs(lastVy) / 22f);
                jumpTimer = 0f;
                if (impact > 0.15f)
                {
                    landTimer = 0.16f + 0.06f * impact;
                    FxLib.Burst(feet, new Color(0.7f, 0.9f, 1f), 4 + (int)(impact * 8f), 3.5f + impact * 3f, 0.14f, 0.4f, 0f, 90f, 150f);
                    FxLib.Ring(feet, new Color(0.4f, 0.9f, 1f, 0.7f), 0.3f, 1.2f + impact, 0.3f);
                    SfxHub.Play("land");
                    if (impact > 0.6f && CameraFx.Instance != null) CameraFx.Instance.Shake(0.09f * impact, 0.18f);
                }
            }
            if (dashing && !wasDashing)
            {
                FxLib.Ring(pos, glowColor, 0.5f, 3f, 0.3f);
                SfxHub.Play("dash");
                if (CameraFx.Instance != null) { CameraFx.Instance.Shake(0.12f, 0.2f); CameraFx.Instance.Punch(0.35f); }
                if (PostFx.Instance != null) PostFx.Instance.Kick(0.35f, 0.5f);
                if (light != null) light.Flash(2.0f);
            }
            if (phasing != wasPhasing)
            {
                FxLib.Ring(pos, phasing ? shiftColor : glowColor, 0.4f, 2.6f, 0.35f);
                FxLib.Burst(pos, phasing ? shiftColor : glowColor, 14, 5f, 0.14f, 0.5f);
                SfxHub.Play("phase");
            }
            if (small != wasSmall)
            {
                shrinkTimer = 0.28f;
                FxLib.Ring(pos, FxLib.Violet, 0.3f, 2.2f, 0.35f);
                FxLib.Burst(pos, FxLib.Violet, 16, 5f, 0.13f, 0.5f);
                SfxHub.Play(small ? "shrink" : "grow");
            }
        }
        wasGrounded = grounded; wasDashing = dashing; wasPhasing = phasing; wasSmall = small;
        lastVy = v.y;

        // ---------------- pick the animation
        float speed = Mathf.Abs(v.x);
        airTime = grounded ? 0f : airTime + dt;
        bool airborne = airTime > 0.07f;                       // ignore one-frame ground flicker
        if (!runLatched && speed > 1.3f) runLatched = true;
        else if (runLatched && speed < 0.5f) runLatched = false;
        HandleTestKeys();
        if (Mathf.Abs(v.x) > 0.4f) facingRight = v.x > 0f;
        string want; float fps = 8f; bool loop = true;
        if (dead) { want = "death"; fps = 14f; loop = false; }
        else if (respawnTimer > 0f) { want = "respawn"; fps = 12f; loop = false; respawnTimer -= dt; }
        else if (Celebrate) { want = "cheer"; fps = 7f; }
        else if (dashing) { want = phasing ? "phase" : "dash"; fps = 20f; }
        else if (shrinkTimer > 0f && Has("shrink")) { want = "shrink"; shrinkTimer -= dt; loop = false; }
        else if (airborne)
        {
            if (v.y * up > 0.5f) { want = "jump"; fps = 14f; loop = false; }
            else { want = "fall"; fps = 9f; }
        }
        else if (landTimer > 0f) { want = "land"; fps = 13f; loop = false; landTimer -= dt; }
        else if (runLatched) { want = "run"; fps = Mathf.Lerp(10f, 19f, Mathf.Clamp01(speed / 12f)); }
        else { want = "idle"; fps = 7f; }
        if (!Has(want)) want = Has("idle") ? "idle" : want;

        // forced preview (testing)
        if (forcedName != null) { want = forcedName; fps = forcedName == "run" ? 12f : (forcedName == "dash" || forcedName == "phase" ? 8f : 6f); loop = true; }

        holdTimer -= dt;
        bool urgent = want == "death" || want == "respawn" || want == "dash" || want == "phase" || want == "cheer" || current == "death" || forcedName != null;
        if (want != current && (urgent || holdTimer <= 0f)) { current = want; animTime = 0f; holdTimer = 0.09f; }
        else if (want != current) { want = current; fps = current == "run" ? 12f : 8f; loop = current == "idle" || current == "run" || current == "fall"; }
        animTime += dt * fps;

        if (haveSheet && anims.ContainsKey(current))
        {
            Sprite[] frames = anims[current];
            int n = frames.Length;
            int f = loop ? ((int)animTime) % n : Mathf.Min((int)animTime, n - 1);
            vsr.sprite = frames[f];
        }

        // ---------------- run dust
        if (current == "run" && grounded)
        {
            stepClock += dt;
            if (stepClock > 0.17f)
            {
                stepClock = 0f;
                FxLib.Burst(feet, new Color(0.6f, 0.85f, 1f, 0.8f), 2, 1.8f, 0.1f, 0.3f, 0f, facingRight ? 160f : 20f, 40f, 8);
            }
        }

        // ---------------- dash after-images
        if (dashing)
        {
            ghostClock -= dt;
            if (ghostClock <= 0f)
            {
                ghostClock = 0.035f;
                Color gc = phasing ? shiftColor : glowColor;
                gc.a = 0.5f;
                FxLib.Ghost(vsr, gc, 0.26f);
            }
        }

        // ---------------- colour
        bool shifted = DimensionShift.IsShifted;
        Color tint = Color.white;
        if (phasing) tint = new Color(1f, 1f, 1f, 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 40f));
        else if (shifted) tint = new Color(1f, 0.85f, 1f, 1f);
        if (dead && deathTimer > 0.35f) tint.a = 0f;
        vsr.color = tint;

        Color hc = shifted || phasing ? shiftColor : glowColor;
        hc.a = (0.22f + 0.07f * Mathf.Sin(Time.unscaledTime * 3.2f)) * ((dead && deathTimer > 0.3f) ? 0f : 1f);
        halo.color = hc;
        if (light != null)
            light.SetColor(Color.Lerp(light.color, shifted || phasing ? shiftColor : glowColor, 1f - Mathf.Exp(-8f * dt)));
        if (sparkles != null)
        {
            ParticleSystem.EmissionModule em = sparkles.emission;
            em.rateOverTime = dead ? 0f : 12f;
        }

        // gravity flip: smooth, passes through zero height
        flipT = Mathf.MoveTowards(flipT, up, dt * 7f);
        Follow(up);

        if (CameraFx.Instance != null)
            CameraFx.Instance.SetExtraZoom(Mathf.Clamp01((speed - 8f) / 14f) * 0.8f);
    }

    void HandleTestKeys()
    {
        if (Input.GetKeyDown(KeyCode.F2)) debugOverlay = !debugOverlay;
        if (Input.GetKeyDown(KeyCode.F3))
        {
            forcedIndex = (forcedIndex + 1) % TestOrder.Length;
            forcedName = TestOrder[forcedIndex];
            current = forcedName; animTime = 0f; debugOverlay = true;
        }
        if (Input.GetKeyDown(KeyCode.F4)) { forcedIndex = -1; forcedName = null; }
    }

    void OnGUI()
    {
        if (!debugOverlay || !haveSheet) return;
        int n = anims.ContainsKey(current) ? anims[current].Length : 0;
        int f = n > 0 ? ((int)animTime) % n : 0;
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.fontSize = 22; st.fontStyle = FontStyle.Bold;
        st.normal.textColor = new Color(0.3f, 1f, 1f);
        GUI.Label(new Rect(20, Screen.height - 120, 900, 34), "ANIM: " + current + "   frame " + (f + 1) + "/" + n + (forcedName != null ? "   (PREVIEW)" : ""), st);
        st.fontSize = 16; st.normal.textColor = Color.white;
        GUI.Label(new Rect(20, Screen.height - 82, 900, 28), "F3 next animation   |   F4 back to normal   |   F2 hide", st);
    }

    bool Has(string key) { return haveSheet && anims.ContainsKey(key); }

    Vector3 FeetPoint(float up)
    {
        Vector3 pos = transform.position;
        float half = 0.8f * transform.lossyScale.y / Mathf.Max(0.01f, baseScale.y);
        if (col != null)
        {
            Bounds b = col.bounds;
            if (b.size.y > 0.05f)
                return new Vector3(b.center.x, up > 0f ? b.min.y : b.max.y, 0f);
            half = Mathf.Abs(transform.lossyScale.y) * 0.5f;
        }
        return new Vector3(pos.x, pos.y - up * half, 0f);
    }

    void Follow(float up)
    {
        Vector3 feet = FeetPoint(up);
        float size = transform.lossyScale.y / Mathf.Max(0.01f, baseScale.y);
        if (float.IsNaN(size) || size <= 0f) size = 1f;
        float norm = 1f;
        if (!haveSheet && vsr != null && vsr.sprite != null)
        {
            // fallback single image: its pivot is the centre, so scale it to heroHeight
            float h = vsr.sprite.bounds.size.y;
            norm = h > 0.0001f ? heroHeight / h : 1f;
            feet += Vector3.up * (heroHeight * 0.5f * size * flipT);
        }
        rig.position = feet;
        rig.localScale = new Vector3(size * norm, size * norm * flipT, 1f);
        vsr.flipX = !facingRight;

        // halo sits on the body
        halo.transform.localPosition = new Vector3(0f, haveSheet ? 0.9f : 0f, 0f);
        halo.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
        if (lightRoot != null)
        {
            Vector3 p = transform.position;
            p.z = 0f;
            lightRoot.position = p;
        }
    }

    void OnDeath(Vector3 pos)
    {
        current = "death";
        animTime = 0f;
        FxLib.Burst(pos, glowColor, 36, 11f, 0.2f, 0.8f, 1.5f);
        FxLib.Burst(pos, shiftColor, 22, 8f, 0.15f, 0.7f, 1.5f);
        FxLib.Ring(pos, Color.white, 0.4f, 4.5f, 0.4f);
        SfxHub.Play("fail");
        if (CameraFx.Instance != null) CameraFx.Instance.Shake(0.4f, 0.45f);
        if (PostFx.Instance != null) PostFx.Instance.Kick(0.7f, 1.2f);
        if (light != null) light.Flash(3f);
    }
}
