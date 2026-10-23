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
    static Sprite white, circle;

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
        return c;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
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
        img.sprite = White;
        img.color = color;
        if (outline)
        {
            Outline o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = outlineColor;
            o.effectDistance = new Vector2(2f, -2f);
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
        Shadow sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    /// <summary>Neon button. Hover brightens it; the border uses the accent colour.</summary>
    public static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pivot, Vector2 pos,
                                    Vector2 size, int fontSize, Color accent, UnityEngine.Events.UnityAction onClick)
    {
        Image img = Panel(name, parent, anchor, pivot, pos, size, new Color(0.30f, 0.24f, 0.62f, 0.96f), true,
                          new Color(accent.r, accent.g, accent.b, 0.85f));
        Button btn = img.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.36f, 0.36f, 0.50f, 1f);
        cb.highlightedColor = Color.white;
        cb.selectedColor = new Color(0.55f, 0.55f, 0.70f, 1f);
        cb.pressedColor = new Color(accent.r, accent.g, accent.b, 1f);
        cb.disabledColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        if (onClick != null) btn.onClick.AddListener(onClick);

        Label(name + "_Label", img.transform, label, fontSize, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
              Vector2.zero, size - new Vector2(16f, 6f), TextAnchor.MiddleCenter, true);
        return btn;
    }

    public static Text ButtonLabel(Button b)
    {
        return b.GetComponentInChildren<Text>();
    }
}
