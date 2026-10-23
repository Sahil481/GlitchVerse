using UnityEngine;

/// <summary>
/// Put on an empty object with a Collider2D set to "Is Trigger".
/// Applies a glitch to the player while they are inside, and resets it on exit.
///
/// GravityMultiplier : value 0.3 = low gravity, -1 = gravity flip, 2 = heavy
/// ControlInversion  : left/right swapped (value ignored)
/// SurfaceGrip       : value 0.05 = ice, 1 = normal
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GlitchZone : MonoBehaviour
{
    public enum ZoneType { GravityMultiplier, ControlInversion, SurfaceGrip }

    public ZoneType type = ZoneType.GravityMultiplier;
    public float value = 0.3f;

    void Reset()
    {
        // Auto-tick "Is Trigger" when the script is first added
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        switch (type)
        {
            case ZoneType.GravityMultiplier: player.SetGravityMultiplier(value); break;
            case ZoneType.ControlInversion:  player.controlsInverted = true; break;
            case ZoneType.SurfaceGrip:       player.surfaceGrip = value; break;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        switch (type)
        {
            case ZoneType.GravityMultiplier: player.ResetGravity(); break;
            case ZoneType.ControlInversion:  player.controlsInverted = false; break;
            case ZoneType.SurfaceGrip:       player.surfaceGrip = 1f; break;
        }
    }
}
