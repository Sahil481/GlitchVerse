using UnityEngine;

/// <summary>
/// Picks the courier sprite frame (idle / run / jump / dash), faces the movement direction,
/// flips upside-down in reversed gravity and tints the aura while the dimension shift is active.
/// Lives on the same object as PlayerController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerVisuals : MonoBehaviour
{
    public SpriteRenderer body;
    public SpriteRenderer aura;
    public Sprite idle, runA, runB, jump, dash;
    public float runFps = 11f;
    public Color auraNormal = new Color(0.16f, 0.94f, 1f, 0.28f);
    public Color auraShifted = new Color(1f, 0.24f, 0.95f, 0.45f);

    Rigidbody2D rb;
    PlayerController pc;
    bool facingRight = true;
    float runClock;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pc = GetComponent<PlayerController>();
        if (body == null) body = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (body == null) return;

        Vector2 v = rb.linearVelocity;
        if (Mathf.Abs(v.x) > 0.4f) facingRight = v.x > 0f;
        body.flipX = !facingRight;
        body.flipY = rb.gravityScale < 0f;

        bool grounded = pc != null && pc.IsGrounded;
        Sprite s = idle;
        if (Mathf.Abs(v.x) > 18f) s = dash != null ? dash : idle;
        else if (!grounded) s = jump != null ? jump : idle;
        else if (Mathf.Abs(v.x) > 0.8f)
        {
            runClock += Time.deltaTime * runFps * Mathf.Clamp(Mathf.Abs(v.x) / 9f, 0.7f, 1.6f);
            s = ((int)runClock % 2 == 0) ? runA : runB;
            if (s == null) s = idle;
        }
        if (s != null) body.sprite = s;

        if (aura != null)
        {
            Color c = DimensionShift.IsShifted ? auraShifted : auraNormal;
            c.a *= 0.85f + 0.15f * Mathf.Sin(Time.time * 3.2f);
            aura.color = c;
        }
    }
}
