using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Neon "YOU DIED" overlay with a glitching title, run stats, quick respawn (R / Enter / Space)
/// and a level-selector with proper level cards. Built at runtime; no references needed except the player.
/// Public API is unchanged (Show / Hide / RespawnPlayer / OpenLevelSelector / BackToDeathMenu / LoadLevel / QuitGame).
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("Optional explicit scene names (only used if the level catalog is unavailable).")]
    [SerializeField] private List<string> levelSceneNames = new List<string>();

    [Header("Behavior")]
    [SerializeField] private bool pauseGameOnDeath = true;

    static readonly Color Accent = new Color32(255, 61, 107, 255);
    static readonly Color[] CardAccents = { UIKit.Cyan, UIKit.Mint, UIKit.Violet, UIKit.Amber, UIKit.Magenta };
    static readonly string[] Lines =
    {
        "Reality has rejected your current timeline.",
        "The glitch got you this time.",
        "Ramen waits for no one. Neither does physics.",
        "That one is going on the highlight reel.",
        "Vex is still hungry. Try again.",
        "Error 404: courier not found.",
        "Your ramen is getting cold...",
        "Almost. Keep moving, keep delivering.",
    };

    GameObject canvasRoot, overlay, mainPanel, levelPanel;
    CanvasGroup mainGroup, levelGroup;
    Image flash, vignette, scan;
    Text titleMain, titleRed, titleCyan, subtitle, message;
    Text statDeaths, statShards, statTime;
    Sprite vignetteSprite;
    float shownAt, nextGlitch, glitchUntil;
    bool visible;
    float openedLevels;

    // ------------------------------------------------------------------ lifecycle
    void Awake()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        EnsureEventSystem();
        Build();
        overlay.SetActive(false);
    }

    void Update()
    {
        if (!visible) return;
        float t = Time.unscaledTime - shownAt;

        // entrance
        float e = Mathf.Clamp01(t / 0.45f);
        float ease = 1f - Mathf.Pow(1f - e, 3f);
        if (mainPanel.activeSelf)
        {
            mainGroup.alpha = ease;
            ((RectTransform)mainPanel.transform).anchoredPosition = new Vector2(0f, Mathf.Lerp(-60f, 0f, ease));
        }
        if (levelPanel.activeSelf)
        {
            float le = Mathf.Clamp01((Time.unscaledTime - openedLevels) / 0.35f);
            levelGroup.alpha = 1f - Mathf.Pow(1f - le, 3f);
        }
        flash.color = new Color(1f, 0.15f, 0.25f, Mathf.Clamp01(0.55f * (1f - t / 0.5f)));

        // vignette pulse + slow scan line
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f);
        vignette.color = new Color(Accent.r, Accent.g, Accent.b, 0.32f + 0.18f * pulse);
        RectTransform sr = scan.rectTransform;
        float h = ((RectTransform)canvasRoot.transform).rect.height;
        sr.anchoredPosition = new Vector2(0f, h * 0.5f - Mathf.Repeat(Time.unscaledTime * 160f, h));

        // glitchy title
        if (Time.unscaledTime >= nextGlitch)
        {
            glitchUntil = Time.unscaledTime + Random.Range(0.06f, 0.16f);
            nextGlitch = Time.unscaledTime + Random.Range(0.7f, 2.0f);
        }
        bool g = Time.unscaledTime < glitchUntil || t < 0.5f;
        float amt = g ? Random.Range(6f, 18f) : 2.5f;
        titleRed.rectTransform.anchoredPosition = new Vector2(-amt + (g ? Random.Range(-4f, 4f) : 0f), g ? Random.Range(-4f, 4f) : 0f);
        titleCyan.rectTransform.anchoredPosition = new Vector2(amt + (g ? Random.Range(-4f, 4f) : 0f), g ? Random.Range(-4f, 4f) : 0f);
        titleMain.rectTransform.anchoredPosition = g ? new Vector2(Random.Range(-5f, 5f), 0f) : Vector2.zero;
        float ta = g ? Random.Range(0.45f, 0.8f) : 0.6f;
        titleRed.color = new Color(1f, 0.1f, 0.3f, ta);
        titleCyan.color = new Color(0.1f, 0.9f, 1f, ta);

        // quick keys
        if (mainPanel.activeSelf && t > 0.35f &&
            (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            RespawnPlayer();
        else if (levelPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            BackToDeathMenu();
    }

    // ------------------------------------------------------------------ public API
    public void Show()
    {
        if (overlay == null) Build();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        LevelManager lm = LevelManager.Instance;
        subtitle.text = Lines[Random.Range(0, Lines.Length)];
        statDeaths.text = ((lm != null ? lm.Deaths : 0) + 1).ToString();
        statShards.text = lm != null ? lm.Shards + " / " + lm.totalShards : "-";
        statTime.text = lm != null ? GameProgress.FormatTime(lm.TimeLeft) : "-";

        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
        overlay.SetActive(true);
        mainGroup.alpha = 0f;
        shownAt = Time.unscaledTime;
        nextGlitch = shownAt + 1.2f;
        visible = true;

        SfxHub.Play("death_sting");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (pauseGameOnDeath) Time.timeScale = 0f;
    }

    public void Hide()
    {
        visible = false;
        if (pauseGameOnDeath) Time.timeScale = 1f;
        if (overlay != null) overlay.SetActive(false);
    }

    public void RespawnPlayer()
    {
        Time.timeScale = 1f;
        visible = false;
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null) playerController.Respawn();
        else Debug.LogError("DeathScreenUI: No PlayerController is assigned or present in this scene.");
        if (overlay != null) overlay.SetActive(false);
    }

    public void OpenLevelSelector()
    {
        SfxHub.Play("ui_open");
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
        levelGroup.alpha = 0f;
        openedLevels = Time.unscaledTime;
        BuildCards();
    }

    public void BackToDeathMenu()
    {
        SfxHub.Play("ui_back");
        levelPanel.SetActive(false);
        mainPanel.SetActive(true);
        shownAt = Time.unscaledTime - 0.45f;
    }

    public void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            if (message != null) message.text = "Scene isn't in Build Settings: " + sceneName;
            Debug.LogError("DeathScreenUI: Scene '" + sceneName + "' cannot be loaded. Add it to Build Settings, or check its name.");
            return;
        }
        Time.timeScale = 1f;
        visible = false;
        SfxHub.Play("ui_click");
        SceneFader.Load(sceneName);
    }

    public void GoToMainMenu() { LoadLevel(LevelCatalog.MenuScene); }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ building
    void Build()
    {
        Canvas canvas = UIKit.CreateCanvas("DeathScreenCanvas", 1000);
        canvasRoot = canvas.gameObject;
        canvasRoot.transform.SetParent(transform, false);

        Vector2 c = new Vector2(0.5f, 0.5f);
        overlay = UIKit.Panel("DeathOverlay", canvasRoot.transform, Vector2.zero, c, Vector2.zero, Vector2.zero,
                              new Color(0.02f, 0.008f, 0.04f, 0.9f), false, Color.clear).gameObject;
        UIKit.Stretch(overlay.GetComponent<RectTransform>());

        vignetteSprite = MakeVignette();
        vignette = NewFull("Vignette", overlay.transform, vignetteSprite);

        // faint horizontal scan-line
        scan = UIKit.Panel("Scan", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 3f),
                           new Color(1f, 0.3f, 0.45f, 0.1f), false, Color.clear);
        scan.sprite = UIKit.White; scan.type = Image.Type.Simple; scan.raycastTarget = false;

        // ---- main panel
        mainPanel = UIKit.Rect("MainPanel", overlay.transform, c, c, Vector2.zero, new Vector2(1000f, 860f)).gameObject;
        mainGroup = mainPanel.AddComponent<CanvasGroup>();
        Transform m = mainPanel.transform;

        UIKit.Label("System", m, "GLITCHVERSE   //   SIMULATION ERROR", 24, new Color(0.6f, 0.9f, 1f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -10f), new Vector2(900f, 36f), TextAnchor.MiddleCenter, true);

        RectTransform titleRoot = UIKit.Rect("TitleRoot", m, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 200f));
        titleRed = UIKit.Label("TitleR", titleRoot, "YOU DIED", 170, new Color(1f, 0.1f, 0.3f, 0.6f), c, c, Vector2.zero, new Vector2(900f, 200f), TextAnchor.MiddleCenter, true);
        titleCyan = UIKit.Label("TitleC", titleRoot, "YOU DIED", 170, new Color(0.1f, 0.9f, 1f, 0.6f), c, c, Vector2.zero, new Vector2(900f, 200f), TextAnchor.MiddleCenter, true);
        titleMain = UIKit.Label("TitleW", titleRoot, "YOU DIED", 170, Color.white, c, c, Vector2.zero, new Vector2(900f, 200f), TextAnchor.MiddleCenter, true);
        subtitle = UIKit.Label("Sub", m, Lines[0], 32, UIKit.TextSoft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                               new Vector2(0f, -280f), new Vector2(900f, 50f), TextAnchor.MiddleCenter, false);

        // stat chips
        statDeaths = StatChip(m, "DEATHS", -320f, Accent);
        statShards = StatChip(m, "SHARDS", 0f, UIKit.Amber);
        statTime = StatChip(m, "TIME LEFT", 320f, UIKit.Cyan);

        // buttons
        UIKit.MakeButton("Respawn", m, "RESPAWN", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 236f), new Vector2(620f, 96f), 40, Accent, RespawnPlayer);
        UIKit.MakeButton("Levels", m, "LEVEL SELECT", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 118f), new Vector2(300f, 76f), 28, UIKit.Cyan, OpenLevelSelector);
        UIKit.MakeButton("Menu", m, "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 118f), new Vector2(300f, 76f), 28, UIKit.Violet, GoToMainMenu);
        UIKit.Label("Hint", m, "PRESS  R  OR  ENTER  TO RESPAWN", 22, new Color(1f, 1f, 1f, 0.55f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                           new Vector2(0f, 70f), new Vector2(700f, 32f), TextAnchor.MiddleCenter, true);
        UIKit.MakeButton("Quit", m, "QUIT GAME", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(240f, 50f), 20, new Color(1f, 1f, 1f, 0.5f), QuitGame);

        // ---- level panel
        levelPanel = UIKit.Rect("LevelPanel", overlay.transform, Vector2.zero, c, Vector2.zero, Vector2.zero).gameObject;
        UIKit.Stretch(levelPanel.GetComponent<RectTransform>());
        levelGroup = levelPanel.AddComponent<CanvasGroup>();
        Transform lp = levelPanel.transform;
        UIKit.Label("Header", lp, "SELECT LEVEL", 100, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -70f), new Vector2(1400f, 120f), TextAnchor.MiddleCenter, true);
        UIKit.Label("Sub", lp, "Jump back into the simulation", 30, UIKit.TextSoft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -190f), new Vector2(1000f, 44f), TextAnchor.MiddleCenter, false);
        message = UIKit.Label("Message", lp, "", 22, new Color(1f, 0.67f, 0.73f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                              new Vector2(0f, 130f), new Vector2(1200f, 34f), TextAnchor.MiddleCenter, false);
        UIKit.MakeButton("Back", lp, "BACK", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 60f), new Vector2(260f, 70f), 28, UIKit.Cyan, BackToDeathMenu);

        // flash sits on top of everything
        flash = NewFull("Flash", overlay.transform, null);
        flash.color = new Color(1f, 0.15f, 0.25f, 0f);
        levelPanel.SetActive(false);
    }

    Text StatChip(Transform parent, string label, float x, Color accent)
    {
        Image chip = UIKit.Panel("Chip_" + label, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -350f), new Vector2(290f, 124f),
                                 new Color(0.05f, 0.035f, 0.13f, 0.9f), true, new Color(accent.r, accent.g, accent.b, 0.8f));
        chip.raycastTarget = false;
        UIKit.Label("L", chip.transform, label, 20, new Color(accent.r, accent.g, accent.b, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -12f), new Vector2(270f, 28f), TextAnchor.MiddleCenter, true);
        return UIKit.Label("V", chip.transform, "0", 56, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                           new Vector2(0f, 12f), new Vector2(270f, 72f), TextAnchor.MiddleCenter, true);
    }

    void BuildCards()
    {
        Transform lp = levelPanel.transform;
        Transform old = lp.Find("Cards");
        if (old != null) Destroy(old.gameObject);
        message.text = "";

        RectTransform row = UIKit.Rect("Cards", lp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1800f, 560f));
        int count = LevelCatalog.SceneNames.Length;
        if (count == 0) { message.text = "No levels found."; return; }
        string current = SceneManager.GetActiveScene().name;

        float cw = 330f, ch = 520f, gap = 26f;
        float x0 = -((count - 1) * (cw + gap)) * 0.5f;
        for (int i = 0; i < count; i++)
            BuildCard(row, i + 1, LevelCatalog.SceneNames[i], current, new Vector2(x0 + i * (cw + gap), 0f), new Vector2(cw, ch));
    }

    void BuildCard(Transform parent, int level, string sceneName, string current, Vector2 pos, Vector2 size)
    {
        Color accent = CardAccents[(level - 1) % CardAccents.Length];
        bool unlocked = GameProgress.IsUnlocked(level);
        bool isCurrent = sceneName == current;
        int stars = GameProgress.Stars(level);

        Image card = UIKit.Panel("Card" + level, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size,
                                 isCurrent ? new Color(0.14f, 0.07f, 0.16f, 0.95f) : new Color(0.06f, 0.04f, 0.16f, 0.92f), true,
                                 new Color(accent.r, accent.g, accent.b, unlocked ? 0.95f : 0.3f));
        Button btn = card.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = card;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.interactable = unlocked;
        ButtonFx fx = card.gameObject.AddComponent<ButtonFx>();
        fx.fill = card;
        fx.fillNormal = card.color;
        fx.border = card.transform.Find("Border").GetComponent<Image>();
        fx.glow = card.transform.Find("Glow").GetComponent<Image>();
        fx.accent = accent;
        fx.hoverScale = 1.045f;
        fx.lift = 12f;
        fx.glowBase = unlocked ? 0.35f : 0.1f;
        SlideIn si = card.gameObject.AddComponent<SlideIn>();
        si.from = new Vector2(0f, -60f);
        si.delay = 0.04f * level;
        string captured = sceneName;
        btn.onClick.AddListener(delegate { LoadLevel(captured); });

        Transform c = card.transform;
        UIKit.Label("Num", c, level.ToString("00"), 120, accent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -10f), new Vector2(300f, 140f), TextAnchor.MiddleCenter, true);
        UIKit.Label("Name", c, LevelCatalog.Titles[level - 1], 32, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -150f), new Vector2(310f, 48f), TextAnchor.MiddleCenter, true);
        UIKit.Label("Sub", c, LevelCatalog.Subtitles[level - 1], 20, UIKit.TextSoft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -204f), new Vector2(290f, 110f), TextAnchor.UpperCenter, false);

        // star pips (diamonds)
        for (int s = 0; s < 3; s++)
        {
            Image pip = UIKit.Panel("Pip" + s, c, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 58f, 150f), new Vector2(34f, 34f),
                                    s < stars ? UIKit.Amber : new Color(1f, 1f, 1f, 0.14f), false, Color.clear);
            pip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            pip.raycastTarget = false;
        }

        string info = !unlocked ? "LOCKED"
                    : (GameProgress.HasPlayed(level) ? "BEST  " + GameProgress.FormatTime(GameProgress.BestTime(level)) : "PAR  " + GameProgress.FormatTime(LevelCatalog.ParTimes[level - 1]));
        UIKit.Label("Info", c, info, 22, unlocked ? UIKit.TextSoft : new Color(1f, 1f, 1f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 80f), new Vector2(310f, 34f), TextAnchor.MiddleCenter, false);
        UIKit.Label("Go", c, !unlocked ? "FINISH LEVEL " + Mathf.Max(1, level - 1) : (isCurrent ? "RESTART LEVEL" : "PLAY"), 26,
                    !unlocked ? new Color(1f, 1f, 1f, 0.4f) : (isCurrent ? Accent : accent),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(310f, 38f), TextAnchor.MiddleCenter, true);

        if (isCurrent)
        {
            Image tag = UIKit.Panel("Cur", c, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(150f, 34f),
                                    Accent, false, Color.clear);
            tag.raycastTarget = false;
            UIKit.Label("T", tag.transform, "YOU ARE HERE", 17, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(150f, 34f), TextAnchor.MiddleCenter, true);
        }
        if (!unlocked)
        {
            Image veil = UIKit.Panel("Veil", c, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                     new Color(0.02f, 0.01f, 0.07f, 0.6f), false, Color.clear);
            UIKit.Stretch(veil.rectTransform);
            veil.sprite = UIKit.RoundedFill; veil.type = Image.Type.Sliced; veil.raycastTarget = false;
        }
    }

    Image NewFull(string name, Transform parent, Sprite sprite)
    {
        RectTransform rt = UIKit.Rect(name, parent, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        UIKit.Stretch(rt);
        Image im = rt.gameObject.AddComponent<Image>();
        im.sprite = sprite;
        im.raycastTarget = false;
        return im;
    }

    static Sprite MakeVignette()
    {
        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.5f + Mathf.Sqrt(dx * dx + dy * dy) * 0.5f;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0.35f, 1.2f, r)));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
