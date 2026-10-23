using UnityEngine;

/// <summary>
/// Saved progress (PlayerPrefs): which levels are unlocked, best time / score / stars.
/// Also helpers for showing key names in the tutorial text and menus.
/// </summary>
public static class GameProgress
{
    public const int LevelCount = 5;
    const string P = "GlitchVerse.";

    /// <summary>Set before loading the main menu to open straight onto the level selector.</summary>
    public static bool OpenLevelSelectOnMenu;

    public static bool UnlockAll
    {
        get { return PlayerPrefs.GetInt(P + "UnlockAll", 0) == 1; }
        set { PlayerPrefs.SetInt(P + "UnlockAll", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static int HighestUnlocked
    {
        get
        {
            if (UnlockAll) return LevelCount;
            return Mathf.Clamp(PlayerPrefs.GetInt(P + "Unlocked", 1), 1, LevelCount);
        }
    }

    public static bool IsUnlocked(int level) { return level <= HighestUnlocked; }

    public static bool HasPlayed(int level) { return PlayerPrefs.GetInt(P + "Stars." + level, 0) > 0; }
    public static int Stars(int level) { return PlayerPrefs.GetInt(P + "Stars." + level, 0); }
    public static int BestScore(int level) { return PlayerPrefs.GetInt(P + "Score." + level, 0); }
    public static float BestTime(int level) { return PlayerPrefs.GetFloat(P + "Time." + level, 0f); }

    /// <summary>Stores a finished run. Returns true if it is a new best score.</summary>
    public static bool CompleteLevel(int level, float timeUsed, int score, int stars)
    {
        bool newBest = score > BestScore(level);
        if (newBest) PlayerPrefs.SetInt(P + "Score." + level, score);

        float oldTime = BestTime(level);
        if (oldTime <= 0f || timeUsed < oldTime) PlayerPrefs.SetFloat(P + "Time." + level, timeUsed);

        if (stars > Stars(level)) PlayerPrefs.SetInt(P + "Stars." + level, stars);

        int next = Mathf.Min(level + 1, LevelCount);
        if (next > PlayerPrefs.GetInt(P + "Unlocked", 1)) PlayerPrefs.SetInt(P + "Unlocked", next);

        PlayerPrefs.Save();
        return newBest;
    }

    public static void ResetAll()
    {
        for (int i = 1; i <= LevelCount; i++)
        {
            PlayerPrefs.DeleteKey(P + "Stars." + i);
            PlayerPrefs.DeleteKey(P + "Score." + i);
            PlayerPrefs.DeleteKey(P + "Time." + i);
        }
        PlayerPrefs.DeleteKey(P + "Unlocked");
        PlayerPrefs.DeleteKey(P + "UnlockAll");
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------ key names
    /// <summary>Reads the player's saved key for an action (works without a PlayerController in the scene).</summary>
    public static KeyCode GetBinding(PlayerController.PlayerAction action)
    {
        KeyCode fallback = DefaultKey(action);
        string key = "GlitchVerse.Control." + action;
        if (!PlayerPrefs.HasKey(key)) return fallback;
        int stored = PlayerPrefs.GetInt(key);
        return System.Enum.IsDefined(typeof(KeyCode), stored) ? (KeyCode)stored : fallback;
    }

    public static KeyCode DefaultKey(PlayerController.PlayerAction action)
    {
        switch (action)
        {
            case PlayerController.PlayerAction.MoveLeft: return KeyCode.A;
            case PlayerController.PlayerAction.MoveRight: return KeyCode.D;
            case PlayerController.PlayerAction.Sprint: return KeyCode.LeftShift;
            case PlayerController.PlayerAction.Jump: return KeyCode.Space;
            case PlayerController.PlayerAction.Dash: return KeyCode.Q;
            case PlayerController.PlayerAction.PhaseDash: return KeyCode.E;
            case PlayerController.PlayerAction.ToggleSize: return KeyCode.F;
            default: return KeyCode.None;
        }
    }

    public static string KeyName(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.LeftShift:
            case KeyCode.RightShift: return "SHIFT";
            case KeyCode.LeftControl:
            case KeyCode.RightControl: return "CTRL";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt: return "ALT";
            case KeyCode.Space: return "SPACE";
            case KeyCode.Return: return "ENTER";
            case KeyCode.Escape: return "ESC";
            case KeyCode.Mouse0: return "LEFT CLICK";
            case KeyCode.Mouse1: return "RIGHT CLICK";
            case KeyCode.UpArrow: return "UP";
            case KeyCode.DownArrow: return "DOWN";
            case KeyCode.LeftArrow: return "LEFT";
            case KeyCode.RightArrow: return "RIGHT";
        }
        string s = k.ToString();
        if (s.StartsWith("Alpha")) return s.Substring(5);
        return s.ToUpper();
    }

    /// <summary>Replaces {Jump}, {Dash}, {Shift} ... in tutorial text with the player's real keys.</summary>
    public static string FillKeys(string text, string shiftKeyName)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Replace("{MoveLeft}", Key(PlayerController.PlayerAction.MoveLeft));
        text = text.Replace("{MoveRight}", Key(PlayerController.PlayerAction.MoveRight));
        text = text.Replace("{Sprint}", Key(PlayerController.PlayerAction.Sprint));
        text = text.Replace("{Jump}", Key(PlayerController.PlayerAction.Jump));
        text = text.Replace("{Dash}", Key(PlayerController.PlayerAction.Dash));
        text = text.Replace("{PhaseDash}", Key(PlayerController.PlayerAction.PhaseDash));
        text = text.Replace("{ToggleSize}", Key(PlayerController.PlayerAction.ToggleSize));
        text = text.Replace("{Shift}", "<b><color=#FFD27A>[" + shiftKeyName + "]</color></b>");
        text = text.Replace("{Click}", "<b><color=#FFD27A>[LEFT CLICK]</color></b>");
        return text;
    }

    static string Key(PlayerController.PlayerAction a)
    {
        return "<b><color=#FFD27A>[" + KeyName(GetBinding(a)) + "]</color></b>";
    }

    public static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return m.ToString("00") + ":" + s.ToString("00");
    }
}
