using System.Collections;
using UnityEngine;

/// <summary>Small gameplay objects: shards, checkpoints, the delivery terminal, tutorial zones, ambient pulses.</summary>

// ====================================================================== Shard
[RequireComponent(typeof(Collider2D))]
public class Shard : MonoBehaviour
{
    public SpriteRenderer sprite;
    public SpriteRenderer glow;
    public float bobAmplitude = 0.14f;
    public float bobSpeed = 2.2f;

    Vector3 basePos;
    float phase;
    bool collected;

    void Awake()
    {
        basePos = transform.position;
        phase = Random.value * 6.28f;
    }

    void Update()
    {
        if (collected) return;
        float t = Time.time * bobSpeed + phase;
        transform.position = basePos + new Vector3(0f, Mathf.Sin(t) * bobAmplitude, 0f);
        if (glow != null)
        {
            Color c = glow.color;
            c.a = 0.35f + 0.15f * Mathf.Sin(t * 1.7f);
            glow.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        collected = true;
        GetComponent<Collider2D>().enabled = false;
        if (LevelManager.Instance != null) LevelManager.Instance.CollectShard();
        SfxHub.Play("shard", transform.position);
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        float t = 0f;
        Vector3 s0 = transform.localScale;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.28f;
            float k = Mathf.Clamp01(t);
            transform.localScale = s0 * Mathf.Lerp(1f, 2.2f, k);
            if (sprite != null) { Color c = sprite.color; c.a = 1f - k; sprite.color = c; }
            if (glow != null) { Color g = glow.color; g.a = (1f - k) * 0.6f; glow.color = g; }
            yield return null;
        }
        gameObject.SetActive(false);
    }
}

// ====================================================================== Checkpoint
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Higher numbers are further along the level. Earlier checkpoints never override later ones.")]
    public int order = 1;
    [Tooltip("Where the player's centre is placed after respawning.")]
    public Vector2 spawnPosition;
    public SpriteRenderer beacon;
    public SpriteRenderer glow;
    public Color offColor = new Color(0.55f, 0.58f, 0.85f, 1f);
    public Color onColor = new Color(0.36f, 1f, 0.69f, 1f);

    bool active;

    void Start() { Paint(false); }

    void Update()
    {
        if (!active || glow == null) return;
        Color c = glow.color;
        c.a = 0.4f + 0.15f * Mathf.Sin(Time.time * 3f);
        glow.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p == null || active) return;
        if (LevelManager.Instance != null && !LevelManager.Instance.TryActivateCheckpoint(order)) return;
        active = true;
        p.SetSpawnPoint(spawnPosition);
        Paint(true);
        SfxHub.Play("checkpoint", transform.position);
        if (LevelManager.Instance != null && LevelManager.Instance.Hud != null)
            LevelManager.Instance.Hud.Toast("CHECKPOINT SAVED");
    }

    void Paint(bool on)
    {
        Color c = on ? onColor : offColor;
        if (beacon != null) beacon.color = c;
        if (glow != null) { c.a = on ? 0.5f : 0f; glow.color = c; }
    }
}

// ====================================================================== Delivery terminal (goal)
[RequireComponent(typeof(Collider2D))]
public class DeliveryGoal : MonoBehaviour
{
    bool done;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelManager.Instance == null) return;
        if (LevelManager.Instance.CurrentState != LevelManager.State.Playing) return;
        done = true;
        LevelManager.Instance.Complete();
    }
}

// ====================================================================== Tutorial zone
[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    public string title;
    [TextArea(2, 5)] public string body;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelManager.Instance == null || LevelManager.Instance.Hud == null) return;
        LevelManager.Instance.Hud.ShowTip(this, title, body);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelManager.Instance == null || LevelManager.Instance.Hud == null) return;
        LevelManager.Instance.Hud.HideTip(this);
    }
}

// ====================================================================== Ambient pulse / bob / spin
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

// ====================================================================== Parallax background layer
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("1 = moves with the camera (sky). Lower = closer layers that slide past faster.")]
    [Range(0f, 1f)] public float followX = 0.9f;
    [Range(0f, 1f)] public float followY = 0.9f;
    [Tooltip("Width of one repeat of the artwork in world units. 0 = no tiling.")]
    public float period = 24f;
    public Vector2 offset;

    Camera cam;
    float startCamY;
    bool ready;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 c = cam.transform.position;
        if (!ready) { startCamY = c.y; ready = true; }

        float x = c.x;
        if (period > 0.01f) x = c.x - Mathf.Repeat(c.x * (1f - followX), period);
        else x = c.x * followX;

        float y = Mathf.Lerp(startCamY, c.y, followY) + offset.y;
        transform.position = new Vector3(x + offset.x, y, transform.position.z);
    }
}
