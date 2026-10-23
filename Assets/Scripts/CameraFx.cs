using UnityEngine;

/// <summary>
/// Camera juice on top of CameraFollow2D: screen shake, zoom punches, speed zoom and a smooth cinematic zoom.
/// Runs after the follow script, and removes its own offset before the next frame so it never drifts.
/// </summary>
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Camera))]
public class CameraFx : MonoBehaviour
{
    public static CameraFx Instance { get; private set; }

    Camera cam;
    float baseSize;
    float zoomOffset;        // cinematic zoom (negative = closer)
    float punch;             // short zoom kick
    float extraZoom;         // speed zoom
    float shakeAmp, shakeTime, shakeDur;
    Vector3 applied;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        baseSize = cam.orthographicSize;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Shake(float amplitude, float duration)
    {
        if (shakeTime <= 0f || amplitude >= shakeAmp * Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDur)))
            shakeAmp = amplitude;
        shakeDur = Mathf.Max(duration, 0.02f);
        shakeTime = shakeDur;
    }

    /// <summary>Positive widens the view for a moment, negative zooms in.</summary>
    public void Punch(float amount) { punch += amount; }

    public void SetExtraZoom(float z) { extraZoom = z; }

    /// <summary>Smoothly zoom to baseSize + offset (use 0 to return).</summary>
    public void SetCinematicZoom(float offset) { zoomOffset = offset; }

    void Update()
    {
        // remove last frame's shake before anything else moves the camera
        transform.position -= applied;
        applied = Vector3.zero;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        punch = Mathf.Lerp(punch, 0f, 1f - Mathf.Exp(-8f * dt));
        float target = baseSize + zoomOffset + extraZoom + punch;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, target, 1f - Mathf.Exp(-6f * dt));

        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            float k = Mathf.Clamp01(shakeTime / shakeDur);
            Vector2 o = Random.insideUnitCircle * shakeAmp * k * k;
            applied = new Vector3(o.x, o.y, 0f);
            transform.position += applied;
        }
    }
}
