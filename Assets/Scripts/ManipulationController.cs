using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hover a Manipulable and left-click: a wheel opens offering Move / Material / Delete
/// (only what that object allows). Time slows while the wheel is open.
///
/// Move: the platform follows your mouse (limited to its range). Click or Enter to drop,
/// right-click or Esc to cancel.
///
/// Add to one empty GameObject in the scene. RadialMenu is added automatically.
/// </summary>
[RequireComponent(typeof(RadialMenu))]
public class ManipulationController : MonoBehaviour
{
    enum Mode { Idle, Moving }

    [Header("References (auto-found if empty)")]
    public PlayerController player;

    [Header("Targeting")]
    [Tooltip("Layers the mouse can pick objects from.")]
    public LayerMask clickableLayers = ~0;
    [Tooltip("How close the player must be (world units) to manipulate something.")]
    public float reach = 10f;

    [Header("Move")]
    [Tooltip("Dragged platforms stop at colliders on these layers. Tick Player and your Ground layer.")]
    public LayerMask moveBlockers;
    public float dragSpeed = 30f;

    [Header("Wheel")]
    [Range(0.05f, 1f)] public float wheelTimeScale = 0.2f;

    [Header("Outline colors")]
    public Color hoverColor = new Color(1f, 1f, 1f, 0.9f);
    public Color farColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    public Color activeColor = new Color(0.4f, 0.9f, 1f, 1f);
    public Color movingColor = new Color(1f, 0.85f, 0.2f, 1f);

    RadialMenu menu;
    LineRenderer outline;
    Mode mode = Mode.Idle;
    Manipulable target;
    Vector2 menuScreenPos;
    Vector2 grabOffset;
    Vector2 moveStartPos;
    int moveStartFrame;
    bool slowActive;
    float baseFixedDelta;

    void Awake()
    {
        menu = GetComponent<RadialMenu>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        baseFixedDelta = Time.fixedDeltaTime;
        CreateOutline();
    }

    void OnDisable()
    {
        if (slowActive) { slowActive = false; SetTime(1f); }
    }

    void Update()
    {
        bool wantSlow = menu.IsOpen;
        if (wantSlow != slowActive)
        {
            slowActive = wantSlow;
            SetTime(wantSlow ? wheelTimeScale : 1f);
        }

        if (player != null && !player.GameplayInputEnabled) { HideOutline(); return; }

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        if (mode == Mode.Moving) { UpdateMoving(mouseWorld); return; }

        if (menu.IsOpen)
        {
            if (target != null) ShowOutline(target, activeColor);
            return;
        }
        if (menu.ClosedFrame == Time.frameCount) return; // that click just closed the wheel

        Manipulable hover = FindUnderMouse(mouseWorld);
        if (hover == null) { HideOutline(); return; }

        bool inReach = InReach(hover);
        ShowOutline(hover, inReach ? hoverColor : farColor);

        if (inReach && Input.GetMouseButtonDown(0)) OpenMainWheel(hover);
    }

    // ------------------------------------------------------------ PICKING
    Manipulable FindUnderMouse(Vector2 worldPoint)
    {
        foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPoint, clickableLayers))
        {
            Manipulable m = hit.GetComponentInParent<Manipulable>();
            if (m != null && !m.IsDeleted) return m;
        }
        return null;
    }

    bool InReach(Manipulable m)
    {
        if (player == null) return true;
        Vector2 p = player.transform.position;
        return Vector2.Distance(p, m.ClosestPoint(p)) <= reach;
    }

    // ------------------------------------------------------------ WHEELS
    void OpenMainWheel(Manipulable m)
    {
        target = m;
        menuScreenPos = Input.mousePosition;

        var opts = new List<RadialMenu.Option>();
        if (m.canMove)
            opts.Add(new RadialMenu.Option { label = "Move", color = new Color(0.20f, 0.45f, 0.75f, 0.92f), onSelect = BeginMove });
        if (m.CanChangeMaterial)
            opts.Add(new RadialMenu.Option { label = "Material", color = new Color(0.25f, 0.60f, 0.40f, 0.92f), onSelect = OpenMaterialWheel });
        if (m.canDelete)
            opts.Add(new RadialMenu.Option { label = "Delete", color = new Color(0.75f, 0.25f, 0.25f, 0.92f), onSelect = DoDelete });

        if (opts.Count == 0) { target = null; return; }
        menu.Open(menuScreenPos, m.displayName, opts);
    }

    void OpenMaterialWheel()
    {
        if (target == null) return;

        var opts = new List<RadialMenu.Option>();
        foreach (SurfaceType t in target.allowedMaterials)
        {
            if (t == target.CurrentMaterial) continue;
            SurfaceType type = t;
            SurfaceProfile p = SurfaceDatabase.Get(type);
            Color c = type == SurfaceType.Normal ? new Color(0.45f, 0.48f, 0.55f) : p.color * 0.7f;
            c.a = 0.92f;
            opts.Add(new RadialMenu.Option { label = p.label, color = c, onSelect = () => ApplyMaterial(type) });
        }

        if (opts.Count == 0) { target = null; return; }
        menu.Open(menuScreenPos, "Material", opts);
    }

    void ApplyMaterial(SurfaceType type)
    {
        if (target != null) target.SetMaterial(type);
        target = null;
        HideOutline();
    }

    void DoDelete()
    {
        if (target != null) target.Delete();
        target = null;
        HideOutline();
    }

    // ------------------------------------------------------------ MOVE MODE
    void BeginMove()
    {
        if (target == null) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        grabOffset = (Vector2)target.transform.position - mouseWorld;
        moveStartPos = target.transform.position;
        moveStartFrame = Time.frameCount;
        mode = Mode.Moving;
    }

    void UpdateMoving(Vector2 mouseWorld)
    {
        if (target == null || target.IsDeleted) { EndMove(); return; }

        target.StepToward(mouseWorld + grabOffset, dragSpeed, Time.deltaTime, moveBlockers);
        ShowOutline(target, movingColor);

        if (Time.frameCount == moveStartFrame) return; // ignore the click that picked "Move"

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
        {
            EndMove();
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            target.SnapTo(moveStartPos);
            EndMove();
        }
    }

    void EndMove()
    {
        mode = Mode.Idle;
        target = null;
        HideOutline();
    }

    // ------------------------------------------------------------ OUTLINE
    void CreateOutline()
    {
        var go = new GameObject("ManipulationOutline");
        go.transform.SetParent(transform, false);
        outline = go.AddComponent<LineRenderer>();
        outline.useWorldSpace = true;
        outline.loop = true;
        outline.positionCount = 4;
        outline.widthMultiplier = 0.07f;
        outline.sortingOrder = 1000;
        outline.material = new Material(Shader.Find("Sprites/Default"));
        outline.enabled = false;
    }

    void ShowOutline(Manipulable m, Color color)
    {
        Bounds b = m.Bounds;
        Vector3 min = b.min - new Vector3(0.05f, 0.05f, 0f);
        Vector3 max = b.max + new Vector3(0.05f, 0.05f, 0f);
        outline.SetPosition(0, new Vector3(min.x, min.y, 0f));
        outline.SetPosition(1, new Vector3(max.x, min.y, 0f));
        outline.SetPosition(2, new Vector3(max.x, max.y, 0f));
        outline.SetPosition(3, new Vector3(min.x, max.y, 0f));
        outline.startColor = outline.endColor = color;
        outline.enabled = true;
    }

    void HideOutline()
    {
        if (outline != null) outline.enabled = false;
    }

    void SetTime(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixedDelta * scale;
    }
}
