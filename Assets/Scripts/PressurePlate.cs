using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A pressure plate. Pressed while the player OR any Manipulable object (a dragged platform/block) rests on it.
/// Doors that list this plate open while it is pressed (or permanently, if Latch is on).
/// The detection area is checked by polling, so it also works with objects moved by the manipulation wheel.
/// </summary>
public class PressurePlate : MonoBehaviour
{
    public static event System.Action<PressurePlate, bool> StateChanged;

    [Header("Behaviour")]
    [Tooltip("On = stays pressed once triggered. Off = only pressed while something is standing on it.")]
    public bool latch = true;
    [Tooltip("Plate sits on a ceiling (for gravity-flip puzzles). Detection happens below it.")]
    public bool flipped = false;
    [Tooltip("Size of the area above the plate that counts as 'standing on it'.")]
    public Vector2 pressArea = new Vector2(1.5f, 0.45f);

    [Header("Look")]
    public SpriteRenderer baseRenderer;
    public SpriteRenderer buttonRenderer;
    public SpriteRenderer glowRenderer;
    public Color idleColor = new Color(1f, 0.70f, 0.28f, 1f);
    public Color pressedColor = new Color(0.36f, 1f, 0.69f, 1f);
    public float pressDepth = 0.14f;

    [Header("Wire to doors (filled by the level builder)")]
    public NeonDoor[] doors;

    public bool IsPressed { get; private set; }

    bool latched;
    bool lastPressed;
    float press01;                 // visual animation 0..1
    Vector3 buttonRest;
    LineRenderer[] wires;
    Transform pulse;
    float pulseT;

    void Awake()
    {
        if (buttonRenderer != null) buttonRest = buttonRenderer.transform.localPosition;
        BuildWires();
    }

    void OnEnable() { PlayerController.PlayerRespawned += ResetPlate; }
    void OnDisable() { PlayerController.PlayerRespawned -= ResetPlate; }

    public void ResetPlate()
    {
        latched = false;
        IsPressed = false;
        press01 = 0f;
        Apply();
    }

    void FixedUpdate()
    {
        bool occupied = latched || IsOccupied();
        if (occupied && latch) latched = true;
        SetPressed(occupied);
    }

    bool IsOccupied()
    {
        float dir = flipped ? -1f : 1f;
        Vector2 center = (Vector2)transform.position + new Vector2(0f, dir * (pressArea.y * 0.5f + 0.03f));
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, pressArea, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<PlayerController>() != null) return true;
            if (c.GetComponentInParent<Manipulable>() != null) return true;
        }
        return false;
    }

    void SetPressed(bool pressed)
    {
        IsPressed = pressed;
        if (pressed != lastPressed)
        {
            lastPressed = pressed;
            if (StateChanged != null) StateChanged(this, pressed);
        }
    }

    void Update()
    {
        press01 = Mathf.MoveTowards(press01, IsPressed ? 1f : 0f, Time.unscaledDeltaTime * 9f);
        Apply();
        UpdateWires();
    }

    void Apply()
    {
        Color c = Color.Lerp(idleColor, pressedColor, press01);
        if (baseRenderer != null) baseRenderer.color = c;
        if (buttonRenderer != null)
        {
            buttonRenderer.color = c;
            float dir = flipped ? 1f : -1f;
            buttonRenderer.transform.localPosition = buttonRest + new Vector3(0f, dir * pressDepth * press01, 0f);
        }
        if (glowRenderer != null)
        {
            Color g = c;
            g.a = Mathf.Lerp(0.35f, 0.6f, press01) * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f));
            glowRenderer.color = g;
        }
    }

    // ------------------------------------------------------------ wires to doors
    void BuildWires()
    {
        if (doors == null || doors.Length == 0) return;
        wires = new LineRenderer[doors.Length];
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] == null) continue;
            GameObject w = new GameObject("Wire_" + doors[i].name);
            w.transform.SetParent(transform, false);
            LineRenderer lr = w.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 4;
            lr.widthMultiplier = 0.07f;
            lr.numCornerVertices = 4;
            lr.sortingOrder = -3;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            wires[i] = lr;
        }

        GameObject p = new GameObject("WirePulse");
        p.transform.SetParent(transform, false);
        SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = glowRenderer != null ? glowRenderer.sprite : null;
        sr.sortingOrder = -2;
        p.transform.localScale = Vector3.one * 0.5f;
        pulse = p.transform;
        if (sr.sprite == null) pulse.gameObject.SetActive(false);
    }

    void UpdateWires()
    {
        if (wires == null) return;
        Color wc = IsPressed ? pressedColor : idleColor;
        Color dim = new Color(wc.r, wc.g, wc.b, IsPressed ? 0.75f : 0.28f);

        Vector3 a = transform.position + new Vector3(0f, flipped ? -0.15f : 0.15f, 0f);
        int shown = 0;
        Vector3 lastB = a;
        for (int i = 0; i < wires.Length; i++)
        {
            if (wires[i] == null || doors[i] == null) continue;
            Vector3 b = doors[i].WireAnchor;
            // elbow route: up/down from the plate, across, then into the door
            float midY = Mathf.Max(a.y, b.y) + 1.2f;
            if (flipped) midY = Mathf.Min(a.y, b.y) - 1.2f;
            wires[i].SetPosition(0, a);
            wires[i].SetPosition(1, new Vector3(a.x, midY, 0f));
            wires[i].SetPosition(2, new Vector3(b.x, midY, 0f));
            wires[i].SetPosition(3, b);
            wires[i].startColor = wires[i].endColor = dim;
            lastB = b;
            shown++;
        }

        if (pulse != null && shown > 0)
        {
            pulseT += Time.unscaledDeltaTime * (IsPressed ? 0.9f : 0.35f);
            if (pulseT > 1f) pulseT -= 1f;
            float midY2 = flipped ? Mathf.Min(a.y, lastB.y) - 1.2f : Mathf.Max(a.y, lastB.y) + 1.2f;
            Vector3 p1 = new Vector3(a.x, midY2, 0f);
            Vector3 p2 = new Vector3(lastB.x, midY2, 0f);
            float l0 = Mathf.Abs(p1.y - a.y);
            float l1 = Mathf.Abs(p2.x - p1.x);
            float l2 = Mathf.Abs(lastB.y - p2.y);
            float total = Mathf.Max(0.01f, l0 + l1 + l2);
            float d = pulseT * total;
            Vector3 pos;
            if (d < l0) pos = Vector3.Lerp(a, p1, d / Mathf.Max(0.01f, l0));
            else if (d < l0 + l1) pos = Vector3.Lerp(p1, p2, (d - l0) / Mathf.Max(0.01f, l1));
            else pos = Vector3.Lerp(p2, lastB, (d - l0 - l1) / Mathf.Max(0.01f, l2));
            pulse.position = pos;
            SpriteRenderer sr = pulse.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(wc.r, wc.g, wc.b, IsPressed ? 0.9f : 0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        float dir = flipped ? -1f : 1f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + new Vector3(0f, dir * (pressArea.y * 0.5f + 0.03f), 0f), pressArea);
    }
}
