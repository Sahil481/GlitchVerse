using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A radial "weapon wheel" that builds itself in code (no prefab or Canvas needed).
/// Hover a wedge and click it, or press, flick toward a wedge and release.
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

    [Header("Size (pixels)")]
    public float innerRadius = 60f;
    public float outerRadius = 160f;
    public float gapDegrees = 4f;

    [Header("Look")]
    public Color holeColor = new Color(0.06f, 0.07f, 0.10f, 0.9f);
    public int fontSize = 18;

    public bool IsOpen { get; private set; }
    /// <summary>Frame number on which the wheel last closed (lets other scripts ignore that frame's click).</summary>
    public int ClosedFrame { get; private set; } = -1;

    GameObject canvasGO;
    RectTransform root;
    Sprite circleSprite;
    Font font;

    readonly List<Image> wedges = new List<Image>();
    List<Option> options;
    int hovered = -1;
    int openFrame;
    float openTime;
    Vector2 center;

    void Awake()
    {
        circleSprite = MakeCircleSprite(256);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvasGO = new GameObject("RadialMenuCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        root = canvasGO.GetComponent<RectTransform>();
        canvasGO.SetActive(false);
    }

    // ------------------------------------------------------------ OPEN / CLOSE
    public void Open(Vector2 screenPos, string title, List<Option> newOptions)
    {
        Close();
        if (newOptions == null || newOptions.Count == 0) return;

        options = newOptions;
        float margin = outerRadius + 10f;
        center = new Vector2(
            Mathf.Clamp(screenPos.x, margin, Mathf.Max(margin, Screen.width - margin)),
            Mathf.Clamp(screenPos.y, margin, Mathf.Max(margin, Screen.height - margin)));

        Build(title);
        canvasGO.SetActive(true);
        IsOpen = true;
        openFrame = Time.frameCount;
        openTime = Time.unscaledTime;
        hovered = -1;
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
        wedges.Clear();
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

        // Which wedge is the mouse over? Angle is measured clockwise from "up".
        Vector2 d = (Vector2)Input.mousePosition - center;
        int newHover = -1;
        if (d.magnitude >= innerRadius)
        {
            float seg = 360f / options.Count;
            float ang = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            newHover = Mathf.Min((int)(ang / seg), options.Count - 1);
        }
        if (newHover != hovered) SetHover(newHover);

        if (Time.frameCount == openFrame) return; // ignore the click that opened us

        bool click = Input.GetMouseButtonDown(0);
        bool flickRelease = Input.GetMouseButtonUp(0) && Time.unscaledTime - openTime > 0.12f;
        if (!click && !flickRelease) return;

        if (hovered >= 0)
        {
            Action act = options[hovered].onSelect;
            Close();
            act?.Invoke();
        }
        else if (click)
        {
            Close(); // clicked empty space
        }
    }

    void SetHover(int index)
    {
        hovered = index;
        for (int i = 0; i < wedges.Count; i++)
        {
            Color baseColor = options[i].color;
            bool on = i == hovered;
            wedges[i].color = on ? Color.Lerp(baseColor, Color.white, 0.35f) : baseColor;
            wedges[i].rectTransform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }
    }

    // ------------------------------------------------------------ BUILD
    void Build(string title)
    {
        wedges.Clear();
        int n = options.Count;
        float seg = 360f / n;

        // Wedges: filled discs, each showing one slice, rotated into place.
        for (int i = 0; i < n; i++)
        {
            Image img = NewImage("Wedge" + i, outerRadius * 2f, center, options[i].color);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = Mathf.Max(0.01f, (seg - gapDegrees) / 360f);
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(i * seg + gapDegrees * 0.5f));
            wedges.Add(img);
        }

        // Hole in the middle.
        NewImage("Hole", innerRadius * 2f, center, holeColor);

        // Labels.
        float rMid = (innerRadius + outerRadius) * 0.5f;
        for (int i = 0; i < n; i++)
        {
            float a = (i + 0.5f) * seg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            NewText(options[i].label, center + dir * rMid, fontSize, FontStyle.Bold);
        }

        if (!string.IsNullOrEmpty(title))
            NewText(title, center, fontSize - 4, FontStyle.Normal);
    }

    Image NewImage(string name, float size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    void NewText(string text, Vector2 pos, int size, FontStyle style)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(130f, 40f);
        rt.anchoredPosition = pos;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = Color.white;
        t.text = text;
        t.raycastTarget = false;
    }

    static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)); // 1px soft edge
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
