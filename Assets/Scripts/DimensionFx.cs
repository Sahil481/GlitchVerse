using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes the dimension shift feel great: implosion + shockwave rings, particle bursts, glitch scan-lines,
/// a pulsing screen-edge tint while shifted, camera punch / shake, bloom + chroma kick, sparkles on ghost
/// platforms, a looping hum and a low-energy warning. Listens to DimensionShift.ShiftChanged.
/// Added to the camera by the level builder (no references to set).
/// </summary>
public class DimensionFx : MonoBehaviour
{
    PlayerController player;
    DimensionShift shift;
    Canvas canvas;
    Image edge;
    Image flash;
    Image[] bars;
    AudioSource hum;
    float edgeAlpha, warnTimer, sparkleTimer, humTarget;
    bool shifted;
    Sprite edgeSprite;

    static readonly Color On = new Color(0.16f, 0.94f, 1f, 1f);
    static readonly Color Off = new Color(1f, 0.24f, 0.95f, 1f);

    // Safety net: any scene that has a DimensionShift gets this component even if the scene was
    // built before it existed (no need to rebuild scenes for it to appear).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, mode) => EnsureExists();
        EnsureExists();
    }

    static void EnsureExists()
    {
        if (FindFirstObjectByType<DimensionShift>() == null) return;
        if (FindFirstObjectByType<DimensionFx>() != null) return;
        GameObject go = new GameObject("DimensionFx");
        go.AddComponent<DimensionFx>();
    }

    void OnEnable() { DimensionShift.ShiftChanged += OnShift; }
    void OnDisable() { DimensionShift.ShiftChanged -= OnShift; }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        shift = FindFirstObjectByType<DimensionShift>();
        if (shift != null) shift.showEnergyBar = false;   // replaced by the big neon bar below
        BuildOverlay();

        hum = gameObject.AddComponent<AudioSource>();
        hum.clip = Resources.Load<AudioClip>("GlitchVerse/Sfx/shift_hum");
        hum.loop = true;
        hum.volume = 0f;
        hum.playOnAwake = false;
        hum.spatialBlend = 0f;
    }

    void BuildOverlay()
    {
        GameObject c = new GameObject("DimensionFxCanvas");
        c.transform.SetParent(transform, false);
        canvas = c.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        BuildRing();

        edgeSprite = MakeEdgeSprite();
        edge = MakeImage(c.transform, "Edge", edgeSprite);
        edge.color = new Color(On.r, On.g, On.b, 0f);
        flash = MakeImage(c.transform, "Flash", null);
        flash.color = new Color(1, 1, 1, 0f);

        bars = new Image[7];
        for (int i = 0; i < bars.Length; i++)
        {
            Image b = MakeImage(c.transform, "Scan" + i, null);
            RectTransform rt = b.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(0, 6);
            b.color = new Color(1, 1, 1, 0);
            bars[i] = b;
        }
    }

    // ------------------------------------------------------------ shift energy ring (around the hero)
    const int ARC_PTS = 64;
    Transform ringRoot;
    LineRenderer ringTrack, ringGlow, ringArc;
    float shownEnergy = 1f, ringAlpha, ringPulse;

    LineRenderer MakeLine(string name, float width, int order)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(ringRoot, false);
        LineRenderer lr = g.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.View;
        lr.numCapVertices = 6;
        lr.numCornerVertices = 2;
        lr.widthMultiplier = width;
        lr.sharedMaterial = FxLib.SpriteMat;
        lr.sortingOrder = order;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.positionCount = 2;
        return lr;
    }

    void BuildRing()
    {
        GameObject r = new GameObject("ShiftRing");
        ringRoot = r.transform;
        ringTrack = MakeLine("Track", 0.07f, 58);
        ringGlow = MakeLine("Glow", 0.30f, 57);
        ringArc = MakeLine("Arc", 0.11f, 59);
        SetArc(ringTrack, 1f, 1.35f);
        ringRoot.gameObject.SetActive(false);
    }

    static void SetArc(LineRenderer lr, float frac, float radius)
    {
        frac = Mathf.Clamp01(frac);
        int n = Mathf.Max(2, Mathf.CeilToInt(ARC_PTS * Mathf.Max(frac, 0.03f)));
        lr.positionCount = n;
        float total = Mathf.Max(frac, 0.03f) * Mathf.PI * 2f;
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : i / (float)(n - 1);
            float ang = Mathf.PI * 0.5f - t * total;      // start at the top, drain clockwise
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
        }
    }

    static void SetColor(LineRenderer lr, Color c) { lr.startColor = c; lr.endColor = c; }

    void UpdateRing(float dt)
    {
        if (ringRoot == null || shift == null) return;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        float e = Mathf.Clamp01(shift.Energy01);
        shownEnergy = Mathf.Lerp(shownEnergy, e, 1f - Mathf.Exp(-14f * dt));
        bool full = e > 0.995f;
        bool low = shifted && e < 0.28f;

        // fade in only while shifting or recharging
        float want = (shifted || !full) ? 1f : 0f;
        ringAlpha = Mathf.MoveTowards(ringAlpha, want, dt * (want > ringAlpha ? 6f : 1.6f));
        bool show = ringAlpha > 0.01f && player != null;
        if (ringRoot.gameObject.activeSelf != show) ringRoot.gameObject.SetActive(show);
        if (!show) return;

        ringRoot.position = player.transform.position + Vector3.up * 0.85f;
        ringPulse += dt * (low ? 20f : 4f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(ringPulse);
        Color baseC = e > 0.5f ? On : (e > 0.28f ? Color.Lerp(UIKit.Amber, On, (e - 0.28f) / 0.22f) : UIKit.Red);

        float scale = shifted ? 1f + 0.03f * pulse : 0.92f;
        ringRoot.localScale = Vector3.one * scale;

        SetArc(ringArc, shownEnergy, 1.35f);
        SetArc(ringGlow, shownEnergy, 1.35f);
        float flicker = low ? (0.55f + 0.45f * pulse) : 1f;
        Color arcC = Color.Lerp(baseC, Color.white, shifted ? 0.18f * pulse : 0f);
        arcC.a = ringAlpha * flicker;
        SetColor(ringArc, arcC);
        SetColor(ringGlow, new Color(baseC.r, baseC.g, baseC.b, ringAlpha * (shifted ? 0.28f : 0.14f) * flicker));
        SetColor(ringTrack, new Color(baseC.r, baseC.g, baseC.b, ringAlpha * 0.16f));
    }

    static Image MakeImage(Transform parent, string name, Sprite s)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image im = go.AddComponent<Image>();
        im.sprite = s;
        im.raycastTarget = false;
        RectTransform rt = im.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return im;
    }

    static Sprite MakeEdgeSprite()
    {
        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.55f + Mathf.Sqrt(dx * dx + dy * dy) * 0.45f;
                float a = Mathf.SmoothStep(0.45f, 1.15f, r);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    Vector3 PlayerPos()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        return player != null ? player.transform.position + Vector3.up * 0.8f : transform.position;
    }

    void OnShift(bool on)
    {
        shifted = on;
        Vector3 p = PlayerPos();
        Color c = on ? On : Off;
        SfxHub.Play(on ? "shift_on" : "shift_off");
        humTarget = on ? 0.32f : 0f;

        if (CameraFx.Instance != null)
        {
            CameraFx.Instance.Shake(on ? 0.22f : 0.16f, 0.28f);
            CameraFx.Instance.Punch(on ? -0.35f : 0.25f);
        }
        if (PostFx.Instance != null) PostFx.Instance.Kick(on ? 0.75f : 0.5f, on ? 2.2f : 1.4f);

        StopAllCoroutinesSafe();
        StartCoroutine(on ? EnterRoutine(p, c) : ExitRoutine(p, c));
        StartCoroutine(FlashRoutine(c, on ? 0.3f : 0.2f));
        StartCoroutine(ScanRoutine(c));
        SparklePlatforms(on, c, 3);
    }

    void StopAllCoroutinesSafe() { /* routines are short; let them finish */ }

    IEnumerator EnterRoutine(Vector3 p, Color c)
    {
        // implosion: rings and dots rush IN, then a shockwave rings OUT
        FxLib.Ring(p, new Color(c.r, c.g, c.b, 0.9f), 11f, 0.6f, 0.22f);
        FxLib.Ring(p, new Color(1f, 1f, 1f, 0.6f), 7f, 0.4f, 0.2f);
        for (int i = 0; i < 14; i++)
        {
            float a = i / 14f * Mathf.PI * 2f;
            Vector3 from = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 4.5f;
            FxLib.Burst(from, c, 3, 3f, 0.18f, 0.25f, 0f, (a * Mathf.Rad2Deg) + 180f, 10f);
        }
        yield return new WaitForSeconds(0.2f);
        p = PlayerPos();
        FxLib.Ring(p, new Color(c.r, c.g, c.b, 1f), 0.5f, 16f, 0.55f);
        FxLib.Ring(p, new Color(1f, 1f, 1f, 0.7f), 0.3f, 10f, 0.35f);
        FxLib.Ring(p, new Color(Off.r, Off.g, Off.b, 0.5f), 0.2f, 22f, 0.7f);
        FxLib.Burst(p, c, 60, 12f, 0.22f, 0.7f);
        FxLib.Burst(p, Color.white, 20, 7f, 0.16f, 0.5f);
        SparklePlatforms(true, c, 6);
    }

    IEnumerator ExitRoutine(Vector3 p, Color c)
    {
        FxLib.Ring(p, new Color(c.r, c.g, c.b, 0.9f), 0.4f, 12f, 0.4f);
        FxLib.Ring(p, new Color(On.r, On.g, On.b, 0.5f), 0.3f, 18f, 0.5f);
        FxLib.Burst(p, c, 45, 10f, 0.2f, 0.6f, -2f);
        FxLib.Burst(p, Color.white, 12, 6f, 0.14f, 0.4f);
        yield return null;
    }

    IEnumerator FlashRoutine(Color c, float peak)
    {
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - t / 0.22f;
            flash.color = new Color(c.r, c.g, c.b, peak * k * k);
            yield return null;
        }
        flash.color = new Color(c.r, c.g, c.b, 0f);
    }

    IEnumerator ScanRoutine(Color c)
    {
        float h = Screen.height;
        for (int i = 0; i < bars.Length; i++)
        {
            bars[i].rectTransform.anchoredPosition = new Vector2(0, Random.Range(-0.45f, 0.45f) * h);
            bars[i].rectTransform.sizeDelta = new Vector2(0, Random.Range(3f, 16f));
            bars[i].color = new Color(c.r, c.g, c.b, Random.Range(0.25f, 0.6f));
        }
        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - t / 0.28f;
            for (int i = 0; i < bars.Length; i++)
            {
                Color bc = bars[i].color; bc.a = Mathf.Min(bc.a, 0.6f) * k;
                bars[i].color = bc;
                if (Random.value < 0.35f)
                    bars[i].rectTransform.anchoredPosition = new Vector2(0, Random.Range(-0.45f, 0.45f) * h);
            }
            yield return null;
        }
        for (int i = 0; i < bars.Length; i++) bars[i].color = new Color(1, 1, 1, 0);
    }

    void SparklePlatforms(bool on, Color c, int perPlatform)
    {
        ShiftPlatform[] ps = FindObjectsByType<ShiftPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera cam = Camera.main;
        for (int i = 0; i < ps.Length; i++)
        {
            SpriteRenderer sr = ps[i].GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) continue;
            Bounds b = sr.bounds;
            if (cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(b.center);
                if (vp.x < -0.3f || vp.x > 1.3f || vp.y < -0.3f || vp.y > 1.3f) continue;
            }
            for (int k = 0; k < perPlatform; k++)
            {
                Vector3 pt = new Vector3(Random.Range(b.min.x, b.max.x), b.max.y, 0f);
                FxLib.Burst(pt, c, 2, 2.2f, 0.14f, 0.55f, -0.4f, 90f, 80f);
            }
            if (on && perPlatform >= 3) FxLib.Ring(b.center, new Color(c.r, c.g, c.b, 0.35f), 0.3f, Mathf.Max(b.size.x, 1f) * 0.8f, 0.4f, 8);
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (hum != null)
        {
            hum.volume = Mathf.MoveTowards(hum.volume, humTarget * SfxHub.Volume, dt * 2.5f);
            if (hum.volume > 0.01f && !hum.isPlaying) hum.Play();
            else if (hum.volume <= 0.01f && humTarget <= 0f && hum.isPlaying) hum.Stop();
        }

        UpdateRing(dt);
        float energy = shift != null ? shift.Energy01 : 1f;
        bool low = shifted && energy < 0.28f;

        // screen-edge tint
        float target = shifted ? 0.34f : 0f;
        if (shifted)
        {
            target += 0.06f * Mathf.Sin(Time.unscaledTime * 3f * Mathf.PI * 2f * 0.5f);
            if (low) target += 0.2f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f));
        }
        edgeAlpha = Mathf.Lerp(edgeAlpha, target, 1f - Mathf.Exp(-9f * dt));
        Color ec = low ? FxLib.Red : On;
        edge.color = new Color(ec.r, ec.g, ec.b, Mathf.Clamp01(edgeAlpha));

        // low-energy warning
        if (low)
        {
            warnTimer -= dt;
            if (warnTimer <= 0f)
            {
                warnTimer = Mathf.Lerp(0.14f, 0.4f, energy / 0.28f);
                SfxHub.Play("shift_warn");
            }
        }
        else warnTimer = 0f;

        // ambient sparkle on ghost platforms while shifted
        if (shifted)
        {
            sparkleTimer -= dt;
            if (sparkleTimer <= 0f) { sparkleTimer = 0.35f; SparklePlatforms(true, On, 1); }
        }
    }
}
