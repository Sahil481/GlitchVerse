using System.Collections;
using UnityEngine;

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
        NeonLight nl = gameObject.AddComponent<NeonLight>();
        nl.color = new Color(1f, 0.75f, 0.35f);
        nl.radius = 5f;
        nl.intensity = 1.5f;
        nl.pulseAmount = 0.2f;
        nl.pulseSpeed = 3f;
    }

    void Update()
    {
        if (collected) return;
        CheckOverlap();
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

    PlayerController playerRef;
    Collider2D playerCol;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        Collect();
    }

    // backup for the physics callback: collect when the player's body overlaps the shard
    void CheckOverlap()
    {
        if (playerRef == null)
        {
            playerRef = FindAnyObjectByType<PlayerController>();
            if (playerRef != null) playerCol = playerRef.GetComponent<Collider2D>();
        }
        if (playerCol == null || !playerCol.enabled || playerRef.IsDead) return;
        Bounds b = playerCol.bounds;
        b.Expand(0.5f);
        if (b.Contains(new Vector3(transform.position.x, transform.position.y, b.center.z))) Collect();
    }

    void Collect()
    {
        if (collected) return;
        collected = true;
        GetComponent<Collider2D>().enabled = false;
        if (LevelManager.Instance != null) LevelManager.Instance.CollectShard();
        SfxHub.Play("shard", transform.position);
        Vector3 sp = transform.position;
        FxLib.Burst(sp, FxLib.Amber, 18, 6f, 0.14f, 0.55f);
        FxLib.Burst(sp, Color.white, 6, 3f, 0.1f, 0.4f);
        FxLib.Ring(sp, FxLib.Amber, 0.3f, 2.2f, 0.35f);
        NeonLight own = GetComponent<NeonLight>();
        if (own != null) own.Flash(3.5f);
        if (CameraFx.Instance != null) CameraFx.Instance.Punch(-0.12f);
        if (PostFx.Instance != null) PostFx.Instance.Kick(0.05f, 0.25f);
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
