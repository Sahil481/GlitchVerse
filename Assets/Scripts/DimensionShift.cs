using System;
using UnityEngine;

/// <summary>
/// The "dimension shift" ability. Put on the same object as PlayerController.
/// While shifted, ShiftPlatforms become visible and solid. It drains an energy
/// meter and recharges when you're back in the normal dimension.
/// </summary>
public class DimensionShift : MonoBehaviour
{
    [Header("Input")]
    public KeyCode toggleKey = KeyCode.R;
    [Tooltip("Off = press to toggle. On = shifted only while the key is held.")]
    public bool holdToShift = false;

    [Header("Energy (seconds of shift)")]
    public float maxDuration = 5f;
    public float rechargeRate = 1f;
    [Tooltip("Need at least this much energy to start a shift.")]
    public float minEnergyToStart = 1f;

    [Header("HUD")]
    public bool showEnergyBar = true;

    public static bool IsShifted { get; private set; }
    public static event Action<bool> ShiftChanged;

    public float Energy01 => maxDuration <= 0f ? 0f : energy / maxDuration;

    float energy;
    PlayerController pc;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsShifted = false;
        ShiftChanged = null;
    }

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        energy = maxDuration;
    }

    void OnEnable() { PlayerController.PlayerRespawned += OnRespawn; }

    void OnDisable()
    {
        PlayerController.PlayerRespawned -= OnRespawn;
        SetShift(false);
    }

    void OnRespawn()
    {
        energy = maxDuration;
        SetShift(false);
    }

    void Update()
    {
        bool inputAllowed = pc == null || pc.GameplayInputEnabled;

        if (inputAllowed)
        {
            if (holdToShift)
            {
                if (Input.GetKeyDown(toggleKey) && energy >= minEnergyToStart) SetShift(true);
                if (Input.GetKeyUp(toggleKey)) SetShift(false);
            }
            else if (Input.GetKeyDown(toggleKey))
            {
                if (IsShifted) SetShift(false);
                else if (energy >= minEnergyToStart) SetShift(true);
            }
        }

        if (IsShifted)
        {
            energy -= Time.deltaTime;
            if (energy <= 0f)
            {
                energy = 0f;
                SetShift(false);
            }
        }
        else
        {
            energy = Mathf.Min(maxDuration, energy + rechargeRate * Time.deltaTime);
        }
    }

    static void SetShift(bool on)
    {
        if (IsShifted == on) return;
        IsShifted = on;
        ShiftChanged?.Invoke(on);
    }

    void OnGUI()
    {
        if (!showEnergyBar) return;

        const float w = 220f, h = 10f;
        var r = new Rect(20f, 20f, w, h);
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = IsShifted ? new Color(0.4f, 0.9f, 1f) : new Color(0.8f, 0.8f, 0.9f);
        GUI.DrawTexture(new Rect(r.x, r.y, w * Energy01, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
