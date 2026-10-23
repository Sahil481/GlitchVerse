using UnityEngine;

/// <summary>
/// An energy door. Opens while ALL plates listed in "plates" are pressed.
/// The slab retracts upward; its collider is only switched on while the door is mostly closed.
/// The door never closes on top of the player (it waits until the doorway is clear).
/// </summary>
public class NeonDoor : MonoBehaviour
{
    [Tooltip("All of these plates must be pressed to open the door (filled by the level builder).")]
    public PressurePlate[] plates;
    public SpriteRenderer slab;
    public BoxCollider2D doorCollider;
    public SpriteRenderer[] leds;          // one status light per plate
    public SpriteRenderer topEmitter, bottomEmitter;

    public Color closedColor = new Color(1f, 0.24f, 0.78f, 0.9f);
    public Color openColor = new Color(0.36f, 1f, 0.69f, 0.9f);
    public float openSpeed = 3.2f;
    public float closeSpeed = 2.2f;

    public float Open01 { get; private set; }
    public bool IsOpen { get { return Open01 > 0.98f; } }

    Vector2 fullSize;
    int playerMask;
    bool wasOpening;

    /// <summary>Where the wire from a plate should end.</summary>
    public Vector3 WireAnchor
    {
        get { return transform.position + new Vector3(0f, fullSize.y * 0.5f + 0.6f, 0f); }
    }

    void Awake()
    {
        if (slab != null) fullSize = slab.size;
        else if (doorCollider != null) fullSize = doorCollider.size;
        playerMask = LayerMask.GetMask("Player");
    }

    void OnEnable() { PlayerController.PlayerRespawned += ResetDoor; }
    void OnDisable() { PlayerController.PlayerRespawned -= ResetDoor; }

    void ResetDoor()
    {
        Open01 = 0f;
        Apply();
    }

    void Update()
    {
        bool want = WantsOpen();
        if (want != wasOpening)
        {
            wasOpening = want;
            SfxHub.Play(want ? "door_open" : "door_close", transform.position);
        }

        if (want)
        {
            Open01 = Mathf.MoveTowards(Open01, 1f, openSpeed * Time.deltaTime);
        }
        else if (Open01 > 0f && !PlayerInDoorway())
        {
            Open01 = Mathf.MoveTowards(Open01, 0f, closeSpeed * Time.deltaTime);
        }
        Apply();
    }

    bool WantsOpen()
    {
        if (plates == null || plates.Length == 0) return false;
        for (int i = 0; i < plates.Length; i++)
            if (plates[i] == null || !plates[i].IsPressed) return false;
        return true;
    }

    bool PlayerInDoorway()
    {
        if (doorCollider == null) return false;
        Vector2 c = (Vector2)transform.position + doorCollider.offset;
        return Physics2D.OverlapBox(c, fullSize + new Vector2(0.4f, 0.2f), 0f, playerMask) != null;
    }

    void Apply()
    {
        if (slab != null)
        {
            float h = Mathf.Max(0.05f, fullSize.y * (1f - Open01));
            slab.size = new Vector2(fullSize.x, h);
            slab.transform.localPosition = new Vector3(0f, fullSize.y * 0.5f - h * 0.5f, 0f);
            Color c = Color.Lerp(closedColor, openColor, Open01);
            c.a = Mathf.Lerp(closedColor.a, 0.45f, Open01);
            slab.color = c;
            slab.enabled = Open01 < 0.995f;
        }
        if (doorCollider != null) doorCollider.enabled = Open01 < 0.6f;

        Color em = Color.Lerp(closedColor, openColor, Open01);
        em.a = 1f;
        if (topEmitter != null) topEmitter.color = em;
        if (bottomEmitter != null) bottomEmitter.color = em;

        if (leds != null && plates != null)
        {
            for (int i = 0; i < leds.Length && i < plates.Length; i++)
            {
                if (leds[i] == null || plates[i] == null) continue;
                bool on = plates[i].IsPressed;
                Color lc = on ? openColor : closedColor;
                lc.a = on ? 1f : 0.55f;
                leds[i].color = lc;
            }
        }
    }
}
