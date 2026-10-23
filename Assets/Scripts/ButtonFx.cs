using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Hover / press animation for neon UI buttons and cards. Uses unscaled time so it works while paused.</summary>
public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public Image fill, border, glow;
    public Text label;
    public Color accent = Color.white;
    public Color fillNormal = new Color(0.07f, 0.05f, 0.18f, 0.88f);
    public Color fillHover = new Color(0.16f, 0.10f, 0.36f, 0.96f);
    public float hoverScale = 1.045f;
    public float glowBase = 0.35f;
    public float lift = 0f;

    bool hover, down;
    float k;
    Vector2 basePos;
    bool init;
    Selectable sel;

    void Init()
    {
        if (init) return;
        init = true;
        basePos = ((RectTransform)transform).anchoredPosition;
        sel = GetComponent<Selectable>();
    }

    void OnEnable() { init = false; hover = down = false; k = 0f; }

    void Update()
    {
        Init();
        bool on = hover && (sel == null || sel.interactable);
        k = Mathf.MoveTowards(k, on ? 1f : 0f, Time.unscaledDeltaTime * 9f);
        float e = Mathf.SmoothStep(0f, 1f, k);
        float s = Mathf.Lerp(1f, hoverScale, e) * (down ? 0.97f : 1f);
        transform.localScale = Vector3.one * s;
        if (lift != 0f) ((RectTransform)transform).anchoredPosition = basePos + new Vector2(0f, lift * e);
        if (fill != null) fill.color = Color.Lerp(fillNormal, fillHover, e);
        if (glow != null)
        {
            Color g = accent;
            g.a = Mathf.Lerp(glowBase, 0.95f, e) * (0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f));
            glow.color = g;
        }
        if (border != null)
        {
            Color b = Color.Lerp(accent, Color.white, e * 0.5f);
            b.a = Mathf.Lerp(0.85f, 1f, e);
            border.color = b;
        }
        if (label != null) label.color = Color.Lerp(Color.white, Color.Lerp(Color.white, accent, 0.25f), e);
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; Init(); }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnPointerDown(PointerEventData e) { down = true; }
    public void OnPointerUp(PointerEventData e) { down = false; }
    public void OnPointerClick(PointerEventData e)
    {
        if (sel == null || sel.interactable) SfxHub.Play("blip_hi");
    }
}
