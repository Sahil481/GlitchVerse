using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Comic-book intro for level 1: Vex yells at the courier to get the ramen over here before it goes cold.
/// Four panels slam in one by one with speech bubbles that type out. Click / Space / Enter advances, Esc or SKIP ends it.
/// Everything is built at runtime. Art is loaded from Resources/GlitchVerse/Comic.
/// </summary>
public class ComicCutscene : MonoBehaviour
{
    /// <summary>Set once the cutscene has been shown (or skipped) in this play session.</summary>
    public static bool Seen;

    static readonly Color Ink = new Color(0.04f, 0.02f, 0.1f, 1f);
    static readonly Color Paper = new Color(1f, 0.98f, 0.92f, 1f);

    Action onDone;
    Canvas canvas;
    RectTransform shakeRoot;
    Image fade, flash;
    bool advance, skipAll;
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    class Bubble
    {
        public RectTransform rt;
        public Text text;
        public string full;
        public bool npc;
    }

    public static void Play(Action done)
    {
        GameObject go = new GameObject("ComicCutscene");
        ComicCutscene c = go.AddComponent<ComicCutscene>();
        c.onDone = done;
    }

    // ------------------------------------------------------------------ flow
    void Start()
    {
        Build();
        StartCoroutine(Run());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) advance = true;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) skipAll = true;
    }

    IEnumerator Run()
    {
        fade.color = Color.black;
        yield return Fade(1f, 0f, 0.5f);

        // ---------------- beat 1: Vex storms in
        RectTransform pA = Panels[0];
        yield return Reveal(pA, true);
        yield return TypeBubbles(0);
        yield return WaitAdvance();

        // ---------------- beat 2: the courier
        if (!skipAll) { yield return Reveal(Panels[1], false); yield return TypeBubbles(1); yield return WaitAdvance(); }

        // ---------------- beat 3: the shout
        if (!skipAll) { yield return Reveal(Panels[2], true); SfxHub.Play("death_spike", 0.6f, 1.2f); yield return TypeBubbles(2); yield return WaitAdvance(); }

        // ---------------- beat 4: the threat
        if (!skipAll) { yield return Reveal(Panels[3], false); yield return TypeBubbles(3); yield return WaitAdvance(); }

        // ---------------- beat 5: GO!
        if (!skipAll)
        {
            goText.gameObject.SetActive(true);
            SfxHub.Play("dash", 1f, 0.9f);
            Shake(0.35f, 22f);
            yield return Pop(goText.rectTransform, 0.25f, 2.4f, 1f);
            SfxHub.Play("win", 0.5f, 1.2f);
            yield return WaitAdvance(1.6f);
        }

        yield return Fade(0f, 1f, 0.45f);
        Seen = true;
        if (onDone != null) onDone();
        Destroy(gameObject);
    }

    IEnumerator WaitAdvance(float auto = 3.2f)
    {
        advance = false;
        float t = 0f;
        while (!advance && !skipAll && t < auto)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        advance = false;
    }

    IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.Clamp01(t / time)));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, to);
    }

    // ------------------------------------------------------------------ panel reveal / bubbles
    IEnumerator Reveal(RectTransform panel, bool hard)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        panel.gameObject.SetActive(true);
        Vector3 baseScale = Vector3.one;
        float baseRot = panel.localEulerAngles.z;
        if (baseRot > 180f) baseRot -= 360f;
        SfxHub.Play(hard ? "land" : "ui_open", 1f, hard ? 0.7f : 1f);
        flash.color = new Color(1f, 1f, 1f, hard ? 0.7f : 0.35f);
        if (hard) Shake(0.25f, 14f);

        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            cg.alpha = e;
            panel.localScale = baseScale * Mathf.Lerp(1.25f, 1f, e);
            panel.localEulerAngles = new Vector3(0f, 0f, baseRot + (1f - e) * (hard ? 5f : -4f));
            flash.color = new Color(1f, 1f, 1f, flash.color.a * 0.85f);
            yield return null;
        }
        cg.alpha = 1f;
        panel.localScale = baseScale;
        panel.localEulerAngles = new Vector3(0f, 0f, baseRot);
        flash.color = new Color(1f, 1f, 1f, 0f);
        yield return new WaitForSecondsRealtime(0.15f);
    }

    IEnumerator TypeBubbles(int panelIndex)
    {
        List<Bubble> list = Bubbles[panelIndex];
        foreach (Bubble b in list)
        {
            b.rt.gameObject.SetActive(true);
            b.text.text = "";
            StartCoroutine(Pop(b.rt, 0.18f, 0.6f, 1f));
            yield return new WaitForSecondsRealtime(0.12f);
            int shown = 0;
            float acc = 0f;
            advance = false;
            while (shown < b.full.Length && !skipAll)
            {
                if (advance) { shown = b.full.Length; advance = false; break; }   // click = finish the line
                acc += Time.unscaledDeltaTime * 42f;
                int target = Mathf.Min(b.full.Length, Mathf.FloorToInt(acc));
                while (shown < target)
                {
                    shown++;
                    char ch = b.full[shown - 1];
                    if (ch != ' ' && shown % 2 == 0) SfxHub.Play(b.npc ? "blip" : "blip_hi", 0.5f, 1f);
                }
                b.text.text = b.full.Substring(0, shown);
                yield return null;
            }
            b.text.text = b.full;
            yield return new WaitForSecondsRealtime(skipAll ? 0f : 0.35f);
        }
    }

    IEnumerator Pop(RectTransform rt, float time, float from, float to)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            float e = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);   // overshoot
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, e);
            yield return null;
        }
        rt.localScale = Vector3.one * to;
    }

    float shakeUntil, shakeTotal, shakeAmp;
    void Shake(float time, float amp) { shakeUntil = Time.unscaledTime + time; shakeTotal = time; shakeAmp = amp; }

    void LateUpdate()
    {
        if (shakeRoot == null) return;
        float left = shakeUntil - Time.unscaledTime;
        if (left > 0f)
        {
            float k = left / Mathf.Max(0.01f, shakeTotal);
            shakeRoot.anchoredPosition = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * shakeAmp * k;
        }
        else shakeRoot.anchoredPosition = Vector2.zero;
    }

    // ------------------------------------------------------------------ building
    readonly List<RectTransform> Panels = new List<RectTransform>();
    readonly List<List<Bubble>> Bubbles = new List<List<Bubble>>();
    Text goText;

    void Build()
    {
        UIKit.EnsureEventSystem();
        canvas = UIKit.CreateCanvas("ComicCanvas", 900);
        canvas.transform.SetParent(transform, false);
        Transform root = canvas.transform;
        Vector2 c = new Vector2(0.5f, 0.5f);

        // page background: deep purple with a halftone dot pattern
        Image bg = UIKit.Panel("Page", root, Vector2.zero, c, Vector2.zero, Vector2.zero, new Color(0.07f, 0.03f, 0.16f, 1f), false, Color.clear);
        UIKit.Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        Image dots = UIKit.Panel("Halftone", bg.transform, Vector2.zero, c, Vector2.zero, Vector2.zero, new Color(1f, 0.25f, 0.8f, 0.22f), false, Color.clear);
        UIKit.Stretch(dots.rectTransform);
        dots.sprite = MakeHalftone();
        dots.type = Image.Type.Tiled;
        dots.raycastTarget = false;

        shakeRoot = UIKit.Rect("Shake", bg.transform, Vector2.zero, c, Vector2.zero, Vector2.zero);
        UIKit.Stretch(shakeRoot);

        // ---- panel layout (1920 x 1080 reference)
        float h = 430f, gutter = 24f;
        float[] cx = { -332f, 562f, -562f, 332f };
        float[] cy = { 227f, 227f, -227f, -227f };
        float[] w = { 1100f, 640f, 640f, 1100f };
        float[] rot = { 0.8f, -1.1f, -0.9f, 1.0f };
        Color[] bgA = { new Color(0.16f, 0.07f, 0.30f), new Color(0.05f, 0.20f, 0.30f), new Color(0.35f, 0.06f, 0.12f), new Color(0.12f, 0.08f, 0.32f) };
        Color[] bgB = { new Color(0.34f, 0.10f, 0.50f), new Color(0.10f, 0.40f, 0.50f), new Color(0.65f, 0.14f, 0.18f), new Color(0.30f, 0.14f, 0.52f) };
        Transform[] content = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            RectTransform p = UIKit.Rect("Panel" + i, shakeRoot, c, c, new Vector2(cx[i], cy[i]), new Vector2(w[i], h));
            p.localEulerAngles = new Vector3(0f, 0f, rot[i]);
            p.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            Image shadow = FlatImage("Shadow", p, new Color(0f, 0f, 0f, 0.55f), new Vector2(w[i], h), new Vector2(12f, -12f));
            FlatImage("Border", p, Paper, new Vector2(w[i], h), Vector2.zero);
            RectTransform inner = FlatImage("Inner", p, bgA[i], new Vector2(w[i] - 16f, h - 16f), Vector2.zero).rectTransform;
            inner.gameObject.AddComponent<RectMask2D>();
            // vertical gradient feel: lighter lower half
            Image glow = FlatImage("Glow", inner, new Color(bgB[i].r, bgB[i].g, bgB[i].b, 0.55f), new Vector2(w[i] - 16f, (h - 16f) * 0.55f), new Vector2(0f, -(h - 16f) * 0.225f));
            glow.sprite = MakeGradient();
            // halftone corner
            Image hd = FlatImage("Dots", inner, new Color(1f, 1f, 1f, 0.10f), new Vector2(w[i] - 16f, h - 16f), Vector2.zero);
            hd.sprite = MakeHalftone(); hd.type = Image.Type.Tiled;
            p.gameObject.SetActive(false);
            Panels.Add(p);
            Bubbles.Add(new List<Bubble>());
            content[i] = inner;
            shadow.raycastTarget = false;
        }

        // ================= panel A: Vex storms in
        Transform a = content[0];
        FocusLines(a, new Color(1f, 0.85f, 0.3f, 0.12f), new Vector2(-250f, -40f), 1300f);
        Portrait(a, "npc_vex", new Vector2(-300f, -120f), new Vector2(520f, 780f));
        Caption(a, "THE NOODLE DISTRICT.  11:58 PM.", new Vector2(-10f, 175f), new Vector2(520f, 52f), true);
        AddBurstBubble(0, a, "WHERE'S MY\nRAMEN?!", new Vector2(250f, -10f), new Vector2(560f, 330f), true, 54);
        Sfx(a, "SLAM!", new Vector2(-160f, 150f), 10f, 64, new Color(1f, 0.85f, 0.2f));

        // ================= panel B: the courier
        Transform b = content[1];
        Portrait(b, "portrait_hero", new Vector2(-150f, -60f), new Vector2(340f, 340f));
        AddBubble(1, b, "Uh... it's on its\nway, Vex?", new Vector2(110f, 90f), new Vector2(330f, 170f), false, 36, new Vector2(-1f, -1f));
        Sweat(b, new Vector2(-30f, 70f));
        Sfx(b, "gulp...", new Vector2(150f, -150f), -6f, 38, new Color(0.5f, 0.95f, 1f));

        // ================= panel C: THE SHOUT
        Transform cc = content[2];
        FocusLines(cc, new Color(1f, 1f, 1f, 0.28f), new Vector2(-130f, -20f), 1000f);
        Portrait(cc, "portrait_npc", new Vector2(-150f, -40f), new Vector2(430f, 430f));
        AddBurstBubble(2, cc, "I SAID\nHOT!!", new Vector2(160f, 70f), new Vector2(360f, 280f), true, 50);
        Sfx(cc, "BAM!", new Vector2(190f, -140f), -8f, 56, new Color(1f, 0.3f, 0.35f));

        // ================= panel D: the threat
        Transform d = content[3];
        Portrait(d, "ramen", new Vector2(-405f, -10f), new Vector2(300f, 300f));
        Text clock = UIKit.Label("Clock", d, "03:00", 96, new Color(1f, 0.35f, 0.4f), c, c, new Vector2(-410f, -150f), new Vector2(360f, 110f), TextAnchor.MiddleCenter, true);
        clock.gameObject.AddComponent<Outline>().effectColor = Ink;
        UIKit.Label("ClockSub", d, "UNTIL IT GOES COLD", 22, Color.white, c, c, new Vector2(-410f, -205f), new Vector2(360f, 30f), TextAnchor.MiddleCenter, true);
        AddBubble(3, d, "THREE MINUTES!  Late, and you're\nwashing dishes at my joint...\nFOR A YEAR!", new Vector2(60f, 70f), new Vector2(520f, 230f), true, 33, new Vector2(1f, 1f));
        Portrait(d, "portrait_hero", new Vector2(420f, -105f), new Vector2(190f, 190f));
        AddBubble(3, d, "On it!!", new Vector2(412f, 78f), new Vector2(180f, 76f), false, 34, new Vector2(0f, -1f));

        // big GO on top of everything
        goText = UIKit.Label("Go", shakeRoot, "GO!!!", 300, new Color(1f, 0.9f, 0.1f), c, c, new Vector2(0f, 10f), new Vector2(1400f, 360f), TextAnchor.MiddleCenter, true);
        goText.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
        Outline o1 = goText.gameObject.AddComponent<Outline>();
        o1.effectColor = Ink; o1.effectDistance = new Vector2(10f, -10f);
        Outline o2 = goText.gameObject.AddComponent<Outline>();
        o2.effectColor = new Color(1f, 0.2f, 0.5f, 1f); o2.effectDistance = new Vector2(-5f, 5f);
        goText.gameObject.SetActive(false);

        // skip hint
        UIKit.Label("Skip", bg.transform, "CLICK / SPACE  next      ESC  skip", 22, new Color(1f, 1f, 1f, 0.6f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                                new Vector2(-46f, 26f), new Vector2(720f, 34f), TextAnchor.MiddleRight, true);

        flash = FlatImage("Flash", root, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.zero);
        UIKit.Stretch(flash.rectTransform);
        flash.raycastTarget = false;
        fade = FlatImage("Fade", root, Color.black, Vector2.zero, Vector2.zero);
        UIKit.Stretch(fade.rectTransform);
        fade.raycastTarget = false;
    }

    // ------------------------------------------------------------------ element helpers
    Image FlatImage(string name, Transform parent, Color color, Vector2 size, Vector2 pos)
    {
        RectTransform rt = UIKit.Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image im = rt.gameObject.AddComponent<Image>();
        im.sprite = UIKit.White;
        im.color = color;
        im.raycastTarget = false;
        return im;
    }

    Sprite Load(string name)
    {
        Sprite s;
        if (sprites.TryGetValue(name, out s)) return s;
        Texture2D tex = Resources.Load<Texture2D>("GlitchVerse/Comic/" + name);
        s = tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sprites[name] = s;
        return s;
    }

    void Portrait(Transform parent, string spriteName, Vector2 pos, Vector2 size)
    {
        Sprite s = Load(spriteName);
        if (s == null) return;
        // dark halo behind the art so it pops from the panel
        Image halo = UIKit.Icon("Halo", parent, UIKit.SoftGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size * 1.15f, new Color(0f, 0f, 0f, 0.55f));
        halo.type = Image.Type.Sliced; halo.preserveAspect = false;
        UIKit.Icon("Art", parent, s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, Color.white);
    }

    void Caption(Transform parent, string text, Vector2 pos, Vector2 size, bool yellow)
    {
        Image box = FlatImage("Caption", parent, yellow ? new Color(1f, 0.9f, 0.3f) : Paper, size, pos);
        Image edge = FlatImage("CapEdge", box.transform, Ink, size + new Vector2(6f, 6f), Vector2.zero);
        edge.transform.SetAsFirstSibling();
        UIKit.Label("T", box.transform, text, 22, Ink, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(16f, 6f), TextAnchor.MiddleCenter, true);
    }

    void Sfx(Transform parent, string text, Vector2 pos, float rotation, int size, Color color)
    {
        Text t = UIKit.Label("Sfx", parent, text, size, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(520f, size * 1.4f), TextAnchor.MiddleCenter, true);
        t.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = Ink; o.effectDistance = new Vector2(5f, -5f);
    }

    void Sweat(Transform parent, Vector2 pos)
    {
        for (int i = 0; i < 3; i++)
        {
            Image d = UIKit.Panel("Sweat" + i, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos + new Vector2(i * 38f, -i * 20f), new Vector2(18f, 28f),
                                  new Color(0.55f, 0.92f, 1f, 0.95f), false, Color.clear);
            d.raycastTarget = false;
            d.rectTransform.localEulerAngles = new Vector3(0f, 0f, 15f - i * 12f);
        }
    }

    void FocusLines(Transform parent, Color color, Vector2 pos, float size)
    {
        Image im = UIKit.Icon("Focus", parent, MakeFocus(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), color);
        im.preserveAspect = false;
    }

    // a speech bubble; tail points in tailDir (e.g. (-1,-1) = bottom-left)
    Text AddBubble(int panelIndex, Transform parent, string text, Vector2 pos, Vector2 size, bool npc, int fontSize, Vector2 tailDir)
    {
        RectTransform root = UIKit.Rect("Bubble", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image outline = UIKit.Panel("O", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size + new Vector2(12f, 12f), Ink, false, Color.clear);
        Image body = UIKit.Panel("B", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, Paper, false, Color.clear);
        outline.raycastTarget = false; body.raycastTarget = false;
        // tail
        Vector2 tp = new Vector2(Mathf.Sign(tailDir.x) * size.x * 0.28f * Mathf.Abs(tailDir.x), Mathf.Sign(tailDir.y) * (size.y * 0.5f + 2f) * Mathf.Abs(tailDir.y));
        Image tailO = FlatImage("TailO", root, Ink, new Vector2(46f, 46f), tp);
        tailO.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        tailO.transform.SetAsFirstSibling();
        Image tail = FlatImage("Tail", root, Paper, new Vector2(34f, 34f), tp);
        tail.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        body.transform.SetAsLastSibling();
        // re-add a cover so the tail merges into the bubble body
        Image cover = UIKit.Panel("Cover", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(4f, 4f), Paper, false, Color.clear);
        cover.raycastTarget = false;

        Text t = UIKit.Label("T", root, "", fontSize, Ink, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(30f, 20f), TextAnchor.MiddleCenter, true);
        t.GetComponent<Shadow>().enabled = false;
        root.gameObject.SetActive(false);
        Bubbles[panelIndex].Add(new Bubble { rt = root, text = t, full = text, npc = npc });
        return t;
    }

    // jagged shout bubble
    Text AddBurstBubble(int panelIndex, Transform parent, string text, Vector2 pos, Vector2 size, bool npc, int fontSize)
    {
        RectTransform root = UIKit.Rect("Burst", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Sprite burst = MakeBurst();
        Image o = UIKit.Icon("O", root, burst, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size * 1.1f, Ink);
        o.preserveAspect = false;
        Image b = UIKit.Icon("B", root, burst, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(1f, 0.96f, 0.55f, 1f));
        b.preserveAspect = false;
        Text t = UIKit.Label("T", root, "", fontSize, new Color(0.8f, 0.05f, 0.15f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size * 0.7f, TextAnchor.MiddleCenter, true);
        t.GetComponent<Shadow>().enabled = false;
        root.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-4f, 4f));
        root.gameObject.SetActive(false);
        Bubbles[panelIndex].Add(new Bubble { rt = root, text = t, full = text, npc = npc });
        return t;
    }

    // ------------------------------------------------------------------ procedural sprites
    static Sprite halftone, gradient, burst, focus;

    static Sprite MakeHalftone()
    {
        if (halftone != null) return halftone;
        const int n = 24;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - n * 0.5f + 0.5f, dy = y - n * 0.5f + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (d - 3.2f))));
            }
        t.Apply();
        halftone = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return halftone;
    }

    static Sprite MakeGradient()
    {
        if (gradient != null) return gradient;
        Texture2D t = new Texture2D(2, 64, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        {
            float a = Mathf.SmoothStep(0f, 1f, y / 63f);
            for (int x = 0; x < 2; x++) t.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - a));
        }
        t.Apply();
        gradient = Sprite.Create(t, new Rect(0, 0, 2, 64), new Vector2(0.5f, 0.5f), 100f);
        return gradient;
    }

    static Sprite MakeBurst()
    {
        if (burst != null) return burst;
        const int n = 256, spikes = 14;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float zig = Mathf.Abs(Mathf.Repeat(ang / (Mathf.PI * 2f) * spikes, 1f) - 0.5f) * 2f;   // 0..1 triangle wave
                float edge = Mathf.Lerp(0.98f, 0.62f, zig);
                float a = Mathf.Clamp01((edge - r) * n * 0.25f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        burst = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return burst;
    }

    static Sprite MakeFocus()
    {
        if (focus != null) return focus;
        const int n = 256, rays = 46;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float w = Mathf.Repeat(ang / (Mathf.PI * 2f) * rays, 1f);
                float width = 0.12f + 0.25f * Mathf.PerlinNoise(Mathf.Floor(ang / (Mathf.PI * 2f) * rays) * 3.7f, 0.5f);
                float ray = Mathf.Abs(w - 0.5f) < width * 0.5f ? 1f : 0f;
                float fade = Mathf.SmoothStep(0.15f, 0.5f, r) * (1f - Mathf.SmoothStep(0.9f, 1f, r));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, ray * fade));
            }
        t.Apply();
        focus = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return focus;
    }
}
