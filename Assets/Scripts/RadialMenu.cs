using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A glass neon "weapon wheel" that builds itself in code (no prefab or Canvas needed).
/// Hover a segment and click it, or press, flick toward a segment and release.
/// Right-click, Esc, or clicking the empty space closes it.
/// </summary>
[DefaultExecutionOrder(-50)]
public class RadialMenu : MonoBehaviour
{
    public class Option
    {
        public string label;
        public Color color = new Color(0.22f, 0.27f, 0.38f, 0.92f);
        public Action onSelect;
    }

    // kept so existing prefab / scene values still deserialize; the wheel now sizes itself
    [HideInInspector] public float innerRadius = 60f;
    [HideInInspector] public float outerRadius = 160f;
    [HideInInspector] public float gapDegrees = 4f;
    [HideInInspector] public Color holeColor = new Color(0.06f, 0.07f, 0.10f, 0.9f);
    [HideInInspector] public int fontSize = 18;

    const float InnerR = 92f;       // canvas units (1920x1080 reference)
    const float OuterR = 236f;
    const float GapDeg = 3.5f;

    public bool IsOpen { get; private set; }
    /// <summary>Frame number on which the wheel last closed (lets other scripts ignore that frame's click).</summary>
    public int ClosedFrame { get; private set; } = -1;

    GameObject canvasGO;
    Canvas canvas;
    RectTransform root;
    CanvasGroup rootGroup;
    RectTransform wheel;
    Sprite sectorSprite, outerEdgeSprite, innerEdgeSprite, glowSprite, discSprite;

    class Seg
    {
        public RectTransform rt;
        public Image fill, edge, inner, glow;
        public Text label;
        public RectTransform labelRt;
        public Color accent;
        public float h;
        public Vector2 dir;
    }
    readonly List<Seg> segs = new List<Seg>();
    Image centerGlow, centerRing;
    Text centerText;
    List<Option> options;
    int hovered = -1;
    int openFrame;
    float openTime;
    Vector2 centerScreen;
    float anim;

    void Awake()
    {
        float ratio = InnerR / OuterR;
        sectorSprite = MakeRing(512, ratio, 1f, 0.004f);
        outerEdgeSprite = MakeRing(512, 0.975f, 1f, 0.004f);
        innerEdgeSprite = MakeRing(512, ratio, ratio + 0.022f, 0.004f);
        glowSprite = MakeSoftRing(512, ratio, 1f);
        discSprite = MakeRing(256, 0f, 1f, 0.006f);

        canvasGO = new GameObject("RadialMenuCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        cs.dynamicPixelsPerUnit = 2f;
        root = canvasGO.GetComponent<RectTransform>();
        rootGroup = canvasGO.AddComponent<CanvasGroup>();
        rootGroup.blocksRaycasts = false;
        canvasGO.SetActive(false);
    }

    // ------------------------------------------------------------ OPEN / CLOSE
    public void Open(Vector2 screenPos, string title, List<Option> newOptions)
    {
        Close();
        if (newOptions == null || newOptions.Count == 0) return;

        options = newOptions;
        canvasGO.SetActive(true);
        Canvas.ForceUpdateCanvases();
        float sf = Mathf.Max(0.1f, canvas.scaleFactor);
        float margin = OuterR * sf * 1.06f + 10f;
        centerScreen = new Vector2(
            Mathf.Clamp(screenPos.x, margin, Mathf.Max(margin, Screen.width - margin)),
            Mathf.Clamp(screenPos.y, margin, Mathf.Max(margin, Screen.height - margin)));

        Build(title);
        IsOpen = true;
        openFrame = Time.frameCount;
        openTime = Time.unscaledTime;
        hovered = -1;
        anim = 0f;
        rootGroup.alpha = 0f;
        wheel.localScale = Vector3.one * 0.55f;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        ClosedFrame = Time.frameCount;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject c = root.GetChild(i).gameObject;
            c.SetActive(false);
            Destroy(c);
        }
        segs.Clear();
        hovered = -1;
        canvasGO.SetActive(false);
    }

    // ------------------------------------------------------------ UPDATE
    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            Close();
            return;
        }

        float dt = Time.unscaledDeltaTime;
        anim = Mathf.MoveTowards(anim, 1f, dt * 7f);
        float e = 1f - Mathf.Pow(1f - anim, 3f);
        rootGroup.alpha = e;
        float pop = Mathf.Lerp(0.55f, 1f, e) + 0.04f * Mathf.Sin(anim * Mathf.PI);
        wheel.localScale = Vector3.one * pop;

        // Which segment is the mouse over? Angle is measured clockwise from "up".
        float sf = Mathf.Max(0.1f, canvas.scaleFactor);
        Vector2 d = (Vector2)Input.mousePosition - centerScreen;
        int newHover = -1;
        if (d.magnitude >= InnerR * sf * 0.9f)
        {
            float seg = 360f / options.Count;
            float ang = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            newHover = Mathf.Min((int)(ang / seg), options.Count - 1);
        }
        if (newHover != hovered)
        {
            hovered = newHover;
            if (hovered >= 0) SfxHub.Play("blip");
        }
        AnimateSegments(dt);

        if (Time.frameCount == openFrame) return; // ignore the click that opened us

        bool click = Input.GetMouseButtonDown(0);
        bool flickRelease = Input.GetMouseButtonUp(0) && Time.unscaledTime - openTime > 0.12f;
        if (!click && !flickRelease) return;

        if (hovered >= 0)
        {
            Action act = options[hovered].onSelect;
            SfxHub.Play("blip_hi");
            Close();
            act?.Invoke();
        }
        else if (click)
        {
            Close(); // clicked empty space
        }
    }

    void AnimateSegments(float dt)
    {
        for (int i = 0; i < segs.Count; i++)
        {
            Seg s = segs[i];
            float target = i == hovered ? 1f : 0f;
            s.h = Mathf.MoveTowards(s.h, target, dt * 9f);
            float k = Mathf.SmoothStep(0f, 1f, s.h);
            Color a = s.accent;

            Color f = new Color(a.r * 0.20f, a.g * 0.20f, a.b * 0.24f, 0.86f);
            Color fh = new Color(a.r * 0.55f, a.g * 0.55f, a.b * 0.6f, 0.95f);
            s.fill.color = Color.Lerp(f, fh, k);
            Color ec = Color.Lerp(new Color(a.r, a.g, a.b, 0.75f), Color.white, k * 0.55f);
            s.edge.color = ec;
            s.inner.color = Color.Lerp(new Color(a.r, a.g, a.b, 0.35f), new Color(a.r, a.g, a.b, 0.95f), k);
            s.glow.color = new Color(a.r, a.g, a.b, Mathf.Lerp(0.16f, 0.85f, k));
            float sc = 1f + 0.065f * k;
            s.rt.localScale = Vector3.one * sc;
            s.labelRt.anchoredPosition = s.dir * Mathf.Lerp(LabelR, LabelR + 12f, k);
            s.label.color = Color.Lerp(new Color(1f, 1f, 1f, 0.9f), Color.white, k);
            s.labelRt.localScale = Vector3.one * Mathf.Lerp(1f, 1.14f, k);
        }
        if (centerGlow != null)
        {
            Color c = hovered >= 0 ? segs[hovered].accent : new Color(0.4f, 0.9f, 1f);
            c.a = 0.35f + 0.08f * Mathf.Sin(Time.unscaledTime * 3f);
            centerGlow.color = Color.Lerp(centerGlow.color, c, 1f - Mathf.Exp(-12f * dt));
            Color rc = c; rc.a = 0.9f;
            centerRing.color = Color.Lerp(centerRing.color, rc, 1f - Mathf.Exp(-12f * dt));
        }
    }

    const float LabelR = (InnerR + OuterR) * 0.5f + 4f;

    // ------------------------------------------------------------ BUILD
    void Build(string title)
    {
        segs.Clear();
        int n = options.Count;
        float seg = 360f / n;

        // soft dark backdrop so the wheel pops against bright levels
        Image dim = NewImage("Dim", root, discSprite, Vector2.zero, 1f, new Color(0.01f, 0f, 0.05f, 0.45f));
        dim.type = Image.Type.Simple;
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero;
        dim.rectTransform.offsetMax = Vector2.zero;
        dim.sprite = UIKit.White;

        // wheel container, positioned at the click point
        GameObject wg = new GameObject("Wheel", typeof(RectTransform));
        wheel = (RectTransform)wg.transform;
        wheel.SetParent(root, false);
        wheel.anchorMin = wheel.anchorMax = new Vector2(0.5f, 0.5f);
        wheel.pivot = new Vector2(0.5f, 0.5f);
        wheel.sizeDelta = Vector2.zero;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, centerScreen, null, out local);
        wheel.anchoredPosition = local;

        // faint outer halo ring for depth
        Image halo = NewImage("Halo", wheel, discSprite, Vector2.zero, (OuterR + 26f) * 2f, new Color(0.45f, 0.85f, 1f, 0.07f));
        halo.rectTransform.SetAsFirstSibling();

        for (int i = 0; i < n; i++)
        {
            Color acc = options[i].color;
            acc.a = 1f;
            acc = Color.Lerp(acc, Color.white, 0.18f);

            GameObject sg = new GameObject("Seg" + i, typeof(RectTransform));
            RectTransform srt = (RectTransform)sg.transform;
            srt.SetParent(wheel, false);
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = Vector2.zero;
            srt.anchoredPosition = Vector2.zero;

            Seg s = new Seg();
            s.rt = srt;
            s.accent = acc;
            float fillAmt = Mathf.Max(0.01f, (seg - GapDeg) / 360f);
            Quaternion rot = Quaternion.Euler(0f, 0f, -(i * seg + GapDeg * 0.5f));

            s.glow = NewSector("Glow", srt, glowSprite, (OuterR + 34f) * 2f, fillAmt, rot, Color.clear);
            s.fill = NewSector("Fill", srt, sectorSprite, OuterR * 2f, fillAmt, rot, Color.clear);
            s.edge = NewSector("Edge", srt, outerEdgeSprite, OuterR * 2f, fillAmt, rot, Color.clear);
            s.inner = NewSector("Inner", srt, innerEdgeSprite, OuterR * 2f, fillAmt, rot, Color.clear);

            float a = (i + 0.5f) * seg * Mathf.Deg2Rad;
            s.dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            Text t = UIKit.Label("Label", srt, options[i].label.ToUpper(), 30, Color.white, new Vector2(0.5f, 0.5f),
                                 new Vector2(0.5f, 0.5f), s.dir * LabelR, new Vector2(190f, 44f), TextAnchor.MiddleCenter, true);
            s.label = t;
            s.labelRt = t.rectTransform;
            segs.Add(s);
        }

        // centre hub
        centerGlow = NewImage("HubGlow", wheel, glowSprite, Vector2.zero, (InnerR + 30f) * 2f, new Color(0.4f, 0.9f, 1f, 0.35f));
        NewImage("Hub", wheel, discSprite, Vector2.zero, InnerR * 1.78f, new Color(0.03f, 0.02f, 0.1f, 0.94f));
        centerRing = NewImage("HubRing", wheel, outerEdgeSprite, Vector2.zero, InnerR * 1.78f, new Color(0.4f, 0.9f, 1f, 0.9f));
        string hub = string.IsNullOrEmpty(title) ? "GLITCH" : title.ToUpper();
        centerText = UIKit.Label("HubText", wheel, hub, 22, new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 0.5f),
                                 new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(InnerR * 1.5f, 70f), TextAnchor.MiddleCenter, true);
        centerText.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    Image NewImage(string name, Transform parent, Sprite sprite, Vector2 pos, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Image NewSector(string name, Transform parent, Sprite sprite, float size, float fillAmount, Quaternion rot, Color color)
    {
        Image img = NewImage(name, parent, sprite, Vector2.zero, size, color);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.fillAmount = fillAmount;
        img.rectTransform.localRotation = rot;
        return img;
    }

    // ------------------------------------------------------------ generated sprites
    // ring from rIn to rOut (fractions of the half-size), anti-aliased
    static Sprite MakeRing(int size, float rIn, float rOut, float aa)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((rOut - r) / aa + 0.5f) * Mathf.Clamp01((r - rIn) / aa + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // soft glow that peaks along the ring and fades outward and slightly inward
    static Sprite MakeSoftRing(int size, float rIn, float rOut)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[size * size];
        float half = size * 0.5f;
        float reach = 1f;   // the glow sprite is drawn bigger than the ring, so ring edge sits at (OuterR/(OuterR+34))
        float ringOut = OuterR / (OuterR + 34f);
        float ringIn = rIn * ringOut;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * reach;
                float d = r > ringOut ? (r - ringOut) / (1f - ringOut) : (r < ringIn ? (ringIn - r) / ringIn * 3f : 0f);
                float a = r > 1f ? 0f : Mathf.Exp(-d * d * 5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
