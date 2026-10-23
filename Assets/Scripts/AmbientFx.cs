using UnityEngine;

/// <summary>Floating neon dust that follows the camera: one layer behind the world, one sparse blurry layer in front.</summary>
[RequireComponent(typeof(Camera))]
public class AmbientFx : MonoBehaviour
{
    public Color tint = new Color(0.4f, 0.9f, 1f, 1f);

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float h = cam.orthographicSize * 2f * 1.5f;
        float w = h * Mathf.Max(1.78f, cam.aspect) * 1.1f;
        Make("MotesBack", w, h, 16f, 0.05f, 0.18f, -65, 0.55f);
        Make("MotesFront", w, h, 4f, 0.25f, 0.6f, 40, 0.18f);
    }

    void Make(string name, float w, float h, float rate, float sizeMin, float sizeMax, int order, float alpha)
    {
        ParticleSystem ps = FxLib.Emitter(transform, name, new Vector3(0f, 0f, 10f), order);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        Color a = tint; a.a = alpha;
        Color b = Color.Lerp(tint, new Color(1f, 0.4f, 0.95f, 1f), 0.6f); b.a = alpha;
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.maxParticles = 400;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = rate;

        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(w, h, 0.1f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ps.Play();
    }
}
