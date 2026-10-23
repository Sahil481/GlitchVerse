using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>A pulsing 2D point light. Created at runtime so the scene file stays light.</summary>
public class NeonLight : MonoBehaviour
{
    public Color color = new Color(0.16f, 0.94f, 1f, 1f);
    public float radius = 6f;
    public float innerRadius = 0.4f;
    public float intensity = 1f;
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float flicker = 0f;
    public Vector2 offset;

    public Light2D Light { get; private set; }
    float boost = 1f;
    float phase;

    void Awake()
    {
        GameObject g = new GameObject("NeonLight");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        Light = g.AddComponent<Light2D>();
        Light.lightType = Light2D.LightType.Point;
        Light.color = color;
        Light.intensity = intensity;
        Light.pointLightOuterRadius = radius;
        Light.pointLightInnerRadius = Mathf.Min(innerRadius, radius * 0.9f);
        phase = Random.value * 6.28f;
    }

    void Update()
    {
        if (Light == null) return;
        boost = Mathf.Lerp(boost, 1f, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed + phase) * pulseAmount;
        if (flicker > 0f && Random.value < 0.04f) s *= 1f - flicker;
        Light.intensity = intensity * s * boost;
    }

    public void Flash(float multiplier) { boost = Mathf.Max(boost, multiplier); }

    public void SetColor(Color c)
    {
        color = c;
        if (Light != null) Light.color = c;
    }

    public void SetIntensity(float i) { intensity = i; }
}
