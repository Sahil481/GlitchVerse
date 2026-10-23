using UnityEngine;

/// <summary>Scales and fades a SpriteRenderer over its lifetime, then destroys itself. Runs on unscaled time.</summary>
public class FxFade : MonoBehaviour
{
    float life = 0.4f, t;
    float startScale = 1f, endScale = 1f, power = 1.5f;
    Color color = Color.white;
    Vector3 baseScale = Vector3.one;
    SpriteRenderer sr;

    public void Begin(Color c, float lifeTime, float from, float to, float fadePower)
    {
        color = c;
        life = Mathf.Max(0.02f, lifeTime);
        startScale = from;
        endScale = to;
        power = fadePower;
        baseScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        Apply(0f);
    }

    void Update()
    {
        t += Time.unscaledDeltaTime / life;
        if (t >= 1f) { Destroy(gameObject); return; }
        Apply(t);
    }

    void Apply(float k)
    {
        float s = Mathf.Lerp(startScale, endScale, 1f - (1f - k) * (1f - k));
        transform.localScale = baseScale * s;
        if (sr != null)
        {
            Color c = color;
            c.a = color.a * Mathf.Pow(1f - k, power);
            sr.color = c;
        }
    }
}
