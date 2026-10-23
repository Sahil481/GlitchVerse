using System.Collections;
using UnityEngine;

/// <summary>
/// Put on any platform/object the player is allowed to mess with.
/// Tick which actions it offers; ManipulationController builds the wheel from that.
/// Needs to be its own GameObject (not a Tilemap tile) with a Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Manipulable : MonoBehaviour
{
    public enum MoveAxis { Both, HorizontalOnly, VerticalOnly }

    [Tooltip("Shown in the middle of the wheel.")]
    public string displayName = "Platform";

    [Header("What the player can do")]
    public bool canMove = true;
    public bool canDelete = false;
    public bool canChangeMaterial = false;

    [Header("Move")]
    public MoveAxis axis = MoveAxis.Both;
    [Tooltip("Max distance from where the object started.")]
    [Min(0f)] public float moveRange = 3f;

    [Header("Material")]
    public SurfaceType[] allowedMaterials =
    {
        SurfaceType.Normal, SurfaceType.Ice, SurfaceType.Mud, SurfaceType.Boost, SurfaceType.Bouncy
    };

    [Header("Reset")]
    [Tooltip("Undo move / delete / material change when the player respawns (prevents soft-locks).")]
    public bool resetOnRespawn = true;

    public bool IsDeleted { get; private set; }
    public bool CanChangeMaterial => canChangeMaterial && surface != null && allowedMaterials != null && allowedMaterials.Length > 0;
    public SurfaceType CurrentMaterial => surface != null ? surface.type : SurfaceType.Normal;
    public Vector2 ClosestPoint(Vector2 p) => col.ClosestPoint(p);
    public Bounds Bounds => col.bounds;

    Collider2D col;
    Collider2D[] colliders;
    Renderer[] renderers;
    SurfaceMaterial surface;
    ShiftPlatform shift;

    Vector3 originPos, originScale;
    SurfaceType originMaterial;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<Renderer>();
        shift = GetComponent<ShiftPlatform>();

        surface = GetComponent<SurfaceMaterial>();
        if (surface == null && canChangeMaterial) surface = gameObject.AddComponent<SurfaceMaterial>();

        originPos = transform.position;
        originScale = transform.localScale;
        originMaterial = CurrentMaterial;
    }

    void OnEnable() { PlayerController.PlayerRespawned += OnPlayerRespawned; }
    void OnDisable() { PlayerController.PlayerRespawned -= OnPlayerRespawned; }

    void OnPlayerRespawned()
    {
        if (resetOnRespawn) ResetState();
    }

    // ------------------------------------------------------------ MATERIAL
    public void SetMaterial(SurfaceType type)
    {
        if (surface != null) surface.SetType(type);
    }

    // ------------------------------------------------------------ DELETE
    public void Delete()
    {
        if (IsDeleted || !canDelete) return;
        IsDeleted = true;
        SetCollidersEnabled(false); // stops being solid immediately
        StopAllCoroutines();
        StartCoroutine(Vanish());
    }

    IEnumerator Vanish()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.15f;
            transform.localScale = Vector3.Lerp(originScale, originScale * 0.01f, t);
            yield return null;
        }
        foreach (var r in renderers) if (r != null) r.enabled = false;
        transform.localScale = originScale;
    }

    // ------------------------------------------------------------ MOVE
    /// <summary>Moves toward 'desired' (clamped to range), sliding along obstacles instead of passing through.</summary>
    public void StepToward(Vector2 desired, float speed, float dt, LayerMask blockers)
    {
        Vector2 pos = transform.position;
        Vector2 goal = ClampToRange(desired);
        Vector2 next = Vector2.MoveTowards(pos, goal, speed * dt);

        if (Fits(next, blockers)) { Place(next); return; }

        Vector2 xOnly = new Vector2(next.x, pos.y);
        if (Fits(xOnly, blockers)) { Place(xOnly); return; }

        Vector2 yOnly = new Vector2(pos.x, next.y);
        if (Fits(yOnly, blockers)) Place(yOnly);
    }

    public void SnapTo(Vector2 pos) { Place(pos); }

    Vector2 ClampToRange(Vector2 desired)
    {
        Vector2 d = desired - (Vector2)originPos;
        if (axis == MoveAxis.HorizontalOnly) d.y = 0f;
        else if (axis == MoveAxis.VerticalOnly) d.x = 0f;
        return (Vector2)originPos + Vector2.ClampMagnitude(d, moveRange);
    }

    bool Fits(Vector2 pos, LayerMask blockers)
    {
        Bounds b = col.bounds;
        Vector2 offset = pos - (Vector2)transform.position;
        Vector2 center = (Vector2)b.center + offset;
        Vector2 size = (Vector2)b.size * 0.96f;

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f, blockers))
        {
            if (hit.isTrigger) continue;
            if (hit.transform.IsChildOf(transform)) continue; // ourselves
            return false;
        }
        return true;
    }

    void Place(Vector2 p)
    {
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    // ------------------------------------------------------------ RESET
    public void ResetState()
    {
        StopAllCoroutines();
        IsDeleted = false;
        transform.localScale = originScale;
        transform.position = originPos;
        foreach (var r in renderers) if (r != null) r.enabled = true;
        SetCollidersEnabled(true);
        if (surface != null) surface.SetType(originMaterial);
        if (shift != null) shift.Refresh();
    }

    void SetCollidersEnabled(bool on)
    {
        foreach (var c in colliders) if (c != null) c.enabled = on;
    }
}
