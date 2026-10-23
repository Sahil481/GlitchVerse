using System.Collections;
using UnityEngine;

public class NeonPulse : MonoBehaviour
{
    public float scaleAmount = 0.06f;
    public float alphaAmount = 0.2f;
    public float speed = 2f;
    public float bob = 0f;
    public float phaseOffset = 0f;

    SpriteRenderer sr;
    Vector3 baseScale, basePos;
    float baseAlpha;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        basePos = transform.localPosition;
        if (sr != null) baseAlpha = sr.color.a;
    }

    void Update()
    {
        float s = Mathf.Sin(Time.time * speed + phaseOffset);
        transform.localScale = baseScale * (1f + s * scaleAmount);
        if (bob > 0f) transform.localPosition = basePos + new Vector3(0f, s * bob, 0f);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(baseAlpha + s * alphaAmount);
            sr.color = c;
        }
    }
}
