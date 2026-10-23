using System.Collections;
using UnityEngine;

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

    NeonLight nl;

    void Start()
    {
        Paint(false);
        nl = gameObject.AddComponent<NeonLight>();
        nl.offset = new Vector2(0f, 1.2f);
        nl.color = offColor;
        nl.radius = 6.5f;
        nl.intensity = 1.0f;
        nl.pulseAmount = 0.1f;
    }

    void Update()
    {
        CheckOverlap();
        if (!active || glow == null) return;
        Color c = glow.color;
        c.a = 0.4f + 0.15f * Mathf.Sin(Time.time * 3f);
        glow.color = c;
    }

    PlayerController playerRef;
    Collider2D playerCol, myCol;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p == null || active) return;
        Activate(p);
    }

    void CheckOverlap()
    {
        if (active) return;
        if (playerRef == null)
        {
            playerRef = FindAnyObjectByType<PlayerController>();
            if (playerRef != null) playerCol = playerRef.GetComponent<Collider2D>();
            myCol = GetComponent<Collider2D>();
        }
        if (playerCol == null || myCol == null || playerRef.IsDead) return;
        if (myCol.bounds.Intersects(playerCol.bounds)) Activate(playerRef);
    }

    void Activate(PlayerController p)
    {
        if (active) return;
        if (LevelManager.Instance != null && !LevelManager.Instance.TryActivateCheckpoint(order)) return;
        active = true;
        p.SetSpawnPoint(spawnPosition);
        Paint(true);
        SfxHub.Play("checkpoint", transform.position);
        Vector3 cp = transform.position + Vector3.up * 1.2f;
        FxLib.Burst(cp, onColor, 28, 7f, 0.15f, 0.7f, -0.3f);
        FxLib.Ring(cp, onColor, 0.4f, 4f, 0.5f);
        if (nl != null) { nl.SetColor(onColor); nl.SetIntensity(2.0f); nl.Flash(3f); }
        if (CameraFx.Instance != null) CameraFx.Instance.Shake(0.08f, 0.2f);
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
