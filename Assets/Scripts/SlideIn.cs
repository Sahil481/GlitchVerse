using UnityEngine;

/// <summary>Slides a UI element in from an offset with a fade, once, when it appears. Unscaled time.</summary>
public class SlideIn : MonoBehaviour
{
    public Vector2 from = new Vector2(-120f, 0f);
    public float delay = 0f;
    public float time = 0.5f;

    RectTransform rt;
    CanvasGroup cg;
    Vector2 target;
    float t;

    void Awake()
    {
        rt = (RectTransform)transform;
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        t = -delay;
        target = rt.anchoredPosition;
        cg.alpha = 0f;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(t / time);
        float e = 1f - Mathf.Pow(1f - k, 3f);
        // the element may be moved by ButtonFx lift later; only drive position while animating
        if (k < 1f) rt.anchoredPosition = target + from * (1f - e);
        cg.alpha = e;
        if (k >= 1f) { rt.anchoredPosition = target; enabled = false; }
    }
}
