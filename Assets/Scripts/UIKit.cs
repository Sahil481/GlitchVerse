using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Small helpers that build neon-styled UGUI elements from code (same approach as DeathScreenUI).</summary>
public static class UIKit
{
    public static readonly Color Cyan = new Color32(40, 240, 255, 255);
    public static readonly Color Magenta = new Color32(255, 61, 242, 255);
    public static readonly Color Violet = new Color32(155, 107, 255, 255);
    public static readonly Color Amber = new Color32(255, 179, 71, 255);
    public static readonly Color Mint = new Color32(92, 255, 176, 255);
    public static readonly Color Red = new Color32(255, 59, 107, 255);
    public static readonly Color PanelDark = new Color(0.045f, 0.035f, 0.12f, 0.92f);
    public static readonly Color TextSoft = new Color(0.86f, 0.86f, 1f, 1f);

    static Font fontBold, fontMedium;
    static Sprite white, circle, roundedFill, roundedRing, softGlow;

    // ------------------------------------------------------------ generated rounded sprites
    static Sprite MakeRounded(int size, float radius, float inset, float thickness, float soft, bool fill, int border)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f - inset;
        float r = radius - inset;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - size * 0.5f) - (half - r);
                float dy = Mathf.Abs(y + 0.5f - size * 0.5f) - (half - r);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                float dist = outside + inside - r;          // signed distance to the edge (negative = inside)
                float a;
                if (fill) a = Mathf.Clamp01(0.5f - dist);
                else if (soft > 0f) a = Mathf.Exp(-(dist * dist) / (soft * soft));
                else a = Mathf.Clamp01(thickness * 0.5f + 0.5f - Mathf.Abs(dist + thickness * 0.5f));
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                             new Vector4(border, border, border, border));
    }

    public static Sprite RoundedFill { get { if (roundedFill == null) roundedFill = MakeRounded(128, 40f, 0f, 0f, 0f, true, 44); return roundedFill; } }
    public static Sprite RoundedRing { get { if (roundedRing == null) roundedRing = MakeRounded(128, 40f, 0f, 3f, 0f, false, 44); return roundedRing; } }
    public static Sprite SoftGlow { get { if (softGlow == null) softGlow = MakeRounded(128, 40f, 24f, 0f, 9f, false, 60); return softGlow; } }

    public static Font FontBold
    {
        get
        {
            if (fontBold == null) fontBold = LoadFont("GlitchVerse/UIFontBold");
            return fontBold;
        }
    }

    public static Font FontMedium
    {
        get
        {
            if (fontMedium == null) fontMedium = LoadFont("GlitchVerse/UIFontMedium");
            return fontMedium;
        }
    }

    static Font LoadFont(string path)
    {
        Font f = Resources.Load<Font>(path);
        if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f;
    }

    public static Sprite White
    {
        get
        {
            if (white == null)
            {
                Texture2D t = Texture2D.whiteTexture;
                white = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            }
            return white;
        }
    }

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        s.dynamicPixelsPerUnit = 2f;                 // renders dynamic fonts at 2x so text stays crisp
        return c;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // ------------------------------------------------------------ layout
    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------ elements
    public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                              Color color, bool outline, Color outlineColor)
    {
        RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        bool flat = size == Vector2.zero;          // full-screen dim panels stay square
        if (flat) { img.sprite = White; }
        else { img.sprite = RoundedFill; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
        img.color = color;
        if (outline && !flat)
        {
            Image glow = Rect("Glow", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            glow.sprite = SoftGlow; glow.type = Image.Type.Sliced;
            glow.rectTransform.anchorMin = Vector2.zero; glow.rectTransform.anchorMax = Vector2.one;
            glow.rectTransform.offsetMin = new Vector2(-24f, -24f); glow.rectTransform.offsetMax = new Vector2(24f, 24f);
            glow.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.38f);
            glow.raycastTarget = false;

            Image ring = Rect("Border", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ring.sprite = RoundedRing; ring.type = Image.Type.Sliced;
            Stretch(ring.rectTransform);
            ring.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, Mathf.Max(0.85f, outlineColor.a));
            ring.raycastTarget = false;
        }
        return img;
    }

    public static Image Icon(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color tint)
    {
        RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = tint;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    public static Text Label(string name, Transform parent, string text, int fontSize, Color color,
                             Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                             TextAnchor align, bool bold)
    {
        RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = bold ? FontBold : FontMedium;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.fontStyle = FontStyle.Normal;
        Shadow sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
        sh.effectDistance = new Vector2(0f, -2f);
        return t;
    }

    /// <summary>Glass pill button with a neon border and glow. Hover scales and brightens it.</summary>
    public static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pivot, Vector2 pos,
                                    Vector2 size, int fontSize, Color accent, UnityEngine.Events.UnityAction onClick)
    {
        Image img = Panel(name, parent, anchor, pivot, pos, size, new Color(0.07f, 0.05f, 0.18f, 0.88f), true, accent);
        Button btn = img.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        if (onClick != null) btn.onClick.AddListener(onClick);

        Text t = Label(name + "_Label", img.transform, label, fontSize, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                       Vector2.zero, size - new Vector2(24f, 6f), TextAnchor.MiddleCenter, true);

        ButtonFx fx = img.gameObject.AddComponent<ButtonFx>();
        fx.fill = img;
        fx.border = img.transform.Find("Border").GetComponent<Image>();
        fx.glow = img.transform.Find("Glow").GetComponent<Image>();
        fx.label = t;
        fx.accent = accent;
        return btn;
    }

    public static Text ButtonLabel(Button b)
    {
        return b.GetComponentInChildren<Text>();
    }
}
