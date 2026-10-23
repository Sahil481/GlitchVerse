using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional starter script for a controls settings panel.
/// Uses Unity's built-in UI (UGUI) Button and Text components.
/// Create one row per PlayerAction, assign a Rebind button and current-key Text,
/// then assign your PlayerController and optional status Text in the Inspector.
/// </summary>
public class ControlsSettingsMenu : MonoBehaviour
{
    [Serializable]
    public class BindingRow
    {
        public PlayerController.PlayerAction action;
        public Button rebindButton;
        public Text keyLabel;
    }

    [SerializeField] private PlayerController playerController;
    [SerializeField] private List<BindingRow> rows = new List<BindingRow>();
    [SerializeField] private Text statusText;

    private bool listeningForKey;
    private PlayerController.PlayerAction actionBeingRebound;

    void Awake()
    {
        foreach (BindingRow row in rows)
        {
            if (row == null || row.rebindButton == null) continue;
            BindingRow capturedRow = row;
            capturedRow.rebindButton.onClick.AddListener(() => BeginRebind(capturedRow.action));
        }

        RefreshLabels();
        SetStatus("Select Rebind next to an action to change its key.");
    }

    void OnEnable()
    {
        RefreshLabels();
    }

    void OnDisable()
    {
        StopListening(true);
    }

    void Update()
    {
        if (!listeningForKey || playerController == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopListening(true);
            SetStatus("Key change cancelled.");
            return;
        }

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (!IsBindableKeyboardKey(key) || !Input.GetKeyDown(key)) continue;

            if (playerController.TrySetBinding(actionBeingRebound, key))
            {
                StopListening(true);
                RefreshLabels();
                SetStatus("Controls saved.");
            }
            else
            {
                SetStatus("That key is already assigned. Press a different key, or Esc to cancel.");
            }
            return;
        }
    }

    public void BeginRebind(PlayerController.PlayerAction action)
    {
        if (playerController == null)
        {
            SetStatus("Assign the PlayerController in the Inspector first.");
            return;
        }

        actionBeingRebound = action;
        listeningForKey = true;
        playerController.SetGameplayInputEnabled(false);
        SetStatus("Press a key for " + action + ". Press Esc to cancel.");
    }

    /// <summary>Wire this public method to a Reset Controls button in the Inspector.</summary>
    public void ResetToDefaults()
    {
        if (playerController == null) return;
        StopListening(true);
        playerController.ResetControlsToDefaults();
        RefreshLabels();
        SetStatus("Default controls restored.");
    }

    public void CancelRebinding()
    {
        StopListening(true);
        SetStatus("Key change cancelled.");
    }

    void StopListening(bool restoreGameplayInput)
    {
        listeningForKey = false;
        if (restoreGameplayInput && playerController != null)
            playerController.SetGameplayInputEnabled(true);
    }

    void RefreshLabels()
    {
        if (playerController == null) return;
        foreach (BindingRow row in rows)
        {
            if (row == null || row.keyLabel == null) continue;
            row.keyLabel.text = playerController.GetBinding(row.action).ToString();
        }
    }

    void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    bool IsBindableKeyboardKey(KeyCode key)
    {
        int value = (int)key;
        bool letters = value >= (int)KeyCode.A && value <= (int)KeyCode.Z;
        bool topRowDigits = value >= (int)KeyCode.Alpha0 && value <= (int)KeyCode.Alpha9;
        bool numpadDigits = value >= (int)KeyCode.Keypad0 && value <= (int)KeyCode.Keypad9;
        bool functionKeys = value >= (int)KeyCode.F1 && value <= (int)KeyCode.F12;

        if (letters || topRowDigits || numpadDigits || functionKeys) return true;

        switch (key)
        {
            case KeyCode.Space:
            case KeyCode.LeftShift:
            case KeyCode.RightShift:
            case KeyCode.LeftControl:
            case KeyCode.RightControl:
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt:
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
            case KeyCode.Tab:
            case KeyCode.Backspace:
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Period:
            case KeyCode.Comma:
            case KeyCode.Slash:
            case KeyCode.Backslash:
            case KeyCode.Minus:
            case KeyCode.Equals:
            case KeyCode.LeftBracket:
            case KeyCode.RightBracket:
            case KeyCode.Semicolon:
            case KeyCode.Quote:
            case KeyCode.BackQuote:
                return true;
            default:
                return false;
        }
    }
}
