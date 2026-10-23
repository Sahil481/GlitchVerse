using UnityEngine;

/// <summary>Particles inside a glitch zone: rising/falling motes for gravity zones, jittery sparks for inversion zones.</summary>
public class ZoneFx : MonoBehaviour
{
    public Vector2 size = new Vector2(8f, 6f);
    public bool invert;
    public float gravityValue = -1f;

    void Start()
    {
        ParticleSystem ps = FxLib.Emitter(transform, "ZoneParticles", Vector3.zero, -3);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        Color c = invert ? new Color(1f, 0.35f, 0.9f, 0.7f) : new Color(0.55f, 0.7f, 1f, 0.7f);
        main.startColor = c;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = Mathf.Clamp(size.x * size.y * 0.08f, 5f, 45f);

        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(size.x, size.y, 0.1f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        if (invert)
        {
            vel.x = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.8f;
        }
        else
        {
            float dir = gravityValue < 0f ? 1f : -1f;
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(dir * 2.2f, dir * 4.5f);
        }

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ps.Play();
    }
}
