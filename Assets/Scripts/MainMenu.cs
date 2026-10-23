using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Main menu with level selector, controls page and progress info. Built entirely from code.</summary>
public class MainMenu : MonoBehaviour
{
    public Sprite background, titleSprite, starOn, starOff, lockIcon, shardIcon;

    static readonly Color[] Accents = { UIKit.Cyan, UIKit.Mint, UIKit.Violet, UIKit.Amber, UIKit.Magenta };

    Canvas canvas;
    GameObject mainPanel, levelsPanel, controlsPanel;
    Text unlockLabel;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        UIKit.EnsureEventSystem();
        Build();

        if (GameProgress.OpenLevelSelectOnMenu) ShowPanel(levelsPanel);
        else ShowPanel(mainPanel);
        GameProgress.OpenLevelSelectOnMenu = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && mainPanel != null && !mainPanel.activeSelf) ShowPanel(mainPanel);
    }

    void ShowPanel(GameObject p)
    {
        mainPanel.SetActive(p == mainPanel);
        levelsPanel.SetActive(p == levelsPanel);
        controlsPanel.SetActive(p == controlsPanel);
    }

    // ================================================================ build
    void Build()
    {
        canvas = UIKit.CreateCanvas("MenuCanvas", 10);
        canvas.transform.SetParent(transform, false);
        Transform root = canvas.transform;

        Image bg = UIKit.Panel("Background", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                               new Color(0.05f, 0.03f, 0.14f, 1f), false, Color.clear);
        UIKit.Stretch(bg.rectTransform);
        bg.raycastTarget = false;
        if (background != null) { bg.sprite = background; bg.color = Color.white; bg.preserveAspect = false; }

        BuildMain(root);
        BuildLevels(root);
        BuildControls(root);
    }

    GameObject NewPanel(string name, Transform root)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);
        UIKit.Stretch((RectTransform)go.transform);
        return go;
    }

    void BuildMain(Transform root)
    {
        mainPanel = NewPanel("MainPanel", root);
        Transform p = mainPanel.transform;

        if (titleSprite != null)
            UIKit.Icon("Title", p, titleSprite, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1200f, 420f), Color.white);
        else
            UIKit.Label("TitleText", p, "GLITCHVERSE", 130, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -120f), new Vector2(1500f, 170f), TextAnchor.MiddleCenter, true);

        Vector2 sz = new Vector2(480f, 78f);
        int next = Mathf.Max(1, Mathf.Min(GameProgress.HighestUnlocked, GameProgress.LevelCount));
        string playLabel = GameProgress.HasPlayed(1) ? "CONTINUE  -  LEVEL " + next : "START DELIVERY";
        UIKit.MakeButton("Play", p, playLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), sz, 34, UIKit.Cyan,
                         delegate { SceneManager.LoadScene(LevelCatalog.Scene(next)); });
        UIKit.MakeButton("Levels", p, "LEVEL SELECT", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), sz, 34, UIKit.Violet,
                         delegate { ShowPanel(levelsPanel); });
        UIKit.MakeButton("Controls", p, "HOW TO PLAY", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -310f), sz, 34, UIKit.Amber,
                         delegate { ShowPanel(controlsPanel); });
        UIKit.MakeButton("Quit", p, "QUIT", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -405f), sz, 34, UIKit.Magenta,
                         delegate { Quit(); });

        UIKit.Label("Credit", p, "Deliver the package. Beat the clock. Break the rules of the world.", 22,
                    new Color(1f, 1f, 1f, 0.55f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f),
                    new Vector2(1400f, 32f), TextAnchor.MiddleCenter, false);
    }

    void BuildLevels(Transform root)
    {
        levelsPanel = NewPanel("LevelsPanel", root);
        Transform p = levelsPanel.transform;

        UIKit.Label("Header", p, "SELECT DELIVERY", 76, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -50f), new Vector2(1400f, 100f), TextAnchor.MiddleCenter, true);

        int total = 0;
        for (int i = 1; i <= GameProgress.LevelCount; i++) total += GameProgress.Stars(i);
        UIKit.Label("StarTotal", p, "STARS  " + total + " / " + (GameProgress.LevelCount * 3), 28, UIKit.Amber,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(600f, 40f),
                    TextAnchor.MiddleCenter, true);

        float cardW = 330f, cardH = 560f, gap = 28f;
        float startX = -((GameProgress.LevelCount - 1) * (cardW + gap)) * 0.5f;
        for (int i = 1; i <= GameProgress.LevelCount; i++)
            BuildCard(p, i, new Vector2(startX + (i - 1) * (cardW + gap), -10f), new Vector2(cardW, cardH));

        UIKit.MakeButton("Back", p, "BACK", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 36f),
                         new Vector2(240f, 64f), 28, UIKit.Cyan, delegate { ShowPanel(mainPanel); });

        Button unlock = UIKit.MakeButton("Unlock", p, GameProgress.UnlockAll ? "ALL LEVELS: UNLOCKED" : "UNLOCK ALL LEVELS",
                         new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(380f, 64f), 24, UIKit.Mint,
                         delegate { GameProgress.UnlockAll = !GameProgress.UnlockAll; Rebuild(); });
        unlockLabel = UIKit.ButtonLabel(unlock);

        UIKit.MakeButton("Reset", p, "RESET PROGRESS", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-440f, 36f),
                         new Vector2(300f, 64f), 22, UIKit.Red, delegate { GameProgress.ResetAll(); Rebuild(); });
    }

    void BuildCard(Transform parent, int level, Vector2 pos, Vector2 size)
    {
        Color accent = Accents[(level - 1) % Accents.Length];
        bool unlocked = GameProgress.IsUnlocked(level);
        int stars = GameProgress.Stars(level);

        Image card = UIKit.Panel("Level" + level, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size,
                                 new Color(0.30f, 0.26f, 0.62f, 0.97f), true, new Color(accent.r, accent.g, accent.b, unlocked ? 0.95f : 0.3f));
        Button btn = card.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.40f, 0.38f, 0.58f, 1f);
        cb.highlightedColor = Color.white;
        cb.pressedColor = accent;
        cb.selectedColor = new Color(0.55f, 0.55f, 0.70f, 1f);
        cb.disabledColor = new Color(0.22f, 0.20f, 0.34f, 1f);
        btn.colors = cb;
        btn.targetGraphic = card;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.interactable = unlocked;
        int captured = level;
        btn.onClick.AddListener(delegate { SceneManager.LoadScene(LevelCatalog.Scene(captured)); });

        Transform c = card.transform;
        UIKit.Label("Num", c, level.ToString("00"), 110, accent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -14f), new Vector2(300f, 130f), TextAnchor.MiddleCenter, true);
        UIKit.Label("Name", c, LevelCatalog.Titles[level - 1], 32, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -150f), new Vector2(310f, 48f), TextAnchor.MiddleCenter, true);
        UIKit.Label("Sub", c, LevelCatalog.Subtitles[level - 1], 20, UIKit.TextSoft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -205f), new Vector2(290f, 90f), TextAnchor.UpperCenter, false);
        UIKit.Label("Tags", c, LevelCatalog.Tags[level - 1].Replace("  |  ", "\n"), 17, new Color(accent.r, accent.g, accent.b, 0.95f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(290f, 90f), TextAnchor.MiddleCenter, true);

        for (int s = 0; s < 3; s++)
            UIKit.Icon("Star" + s, c, s < stars ? starOn : starOff, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                       new Vector2((s - 1) * 68f, 112f), new Vector2(60f, 60f), unlocked ? Color.white : new Color(1f, 1f, 1f, 0.3f));

        string info;
        if (!unlocked) info = "LOCKED";
        else if (GameProgress.HasPlayed(level))
            info = GameProgress.FormatTime(GameProgress.BestTime(level)) + "   |   " + GameProgress.BestScore(level);
        else info = "NOT PLAYED  -  PAR " + GameProgress.FormatTime(LevelCatalog.ParTimes[level - 1]);
        UIKit.Label("Info", c, info, 20, unlocked ? UIKit.TextSoft : new Color(1f, 1f, 1f, 0.4f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(310f, 34f), TextAnchor.MiddleCenter, false);
        UIKit.Label("Go", c, unlocked ? "PLAY" : "FINISH LEVEL " + Mathf.Max(1, level - 1), 24, unlocked ? accent : new Color(1f, 1f, 1f, 0.4f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(310f, 34f), TextAnchor.MiddleCenter, true);

        if (!unlocked)
        {
            Image veil = UIKit.Panel("Veil", c, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                     new Color(0.02f, 0.01f, 0.07f, 0.55f), false, Color.clear);
            UIKit.Stretch(veil.rectTransform);
            veil.raycastTarget = false;
            if (lockIcon != null)
                UIKit.Icon("Lock", c, lockIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f),
                           new Vector2(130f, 130f), Color.white);
        }
    }

    void BuildControls(Transform root)
    {
        controlsPanel = NewPanel("ControlsPanel", root);
        Transform p = controlsPanel.transform;

        UIKit.Label("Header", p, "HOW TO PLAY", 76, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -50f), new Vector2(1400f, 100f), TextAnchor.MiddleCenter, true);

        Image card = UIKit.Panel("Card", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f),
                                 new Vector2(1500f, 700f), UIKit.PanelDark, true, UIKit.Violet);

        string shift = GameProgress.KeyName(KeyCode.R);
        string left =
            "<b><color=#28F0FF>MOVEMENT</color></b>\n" +
            "Move  " + GameProgress.FillKeys("{MoveLeft} {MoveRight}", shift) + "\n" +
            "Jump  " + GameProgress.FillKeys("{Jump}", shift) + "  (press again in the air to double jump)\n" +
            "Sprint  " + GameProgress.FillKeys("hold {Sprint}", shift) + "\n" +
            "Dash  " + GameProgress.FillKeys("{Dash}", shift) + "  (burst of speed, short cooldown)\n" +
            "Phase Dash  " + GameProgress.FillKeys("{PhaseDash}", shift) + "  (pass through PHASE walls)\n" +
            "Shrink / Grow  " + GameProgress.FillKeys("{ToggleSize}", shift) + "  (fit through tiny gaps)\n\n" +
            "<b><color=#28F0FF>DELIVERY</color></b>\n" +
            "Reach the glowing terminal before the 3:00 countdown ends.\n" +
            "Collect shards for bonus points. Checkpoints save your spot.";

        string right =
            "<b><color=#FF3DF2>GLITCH POWERS</color></b>\n" +
            "Dimension Shift  " + GameProgress.FillKeys("{Shift}", shift) + "  (ghost platforms appear, energy drains)\n" +
            "Manipulate  " + GameProgress.FillKeys("{Click}", shift) + " on a glowing object to open the wheel:\n" +
            "     move it  |  delete it  |  change its material\n\n" +
            "<b><color=#FF3DF2>MATERIALS</color></b>\n" +
            "ICE  slippery and fast   |   MUD  slow and sticky\n" +
            "BOOST  very fast   |   BOUNCY  higher jumps\n\n" +
            "<b><color=#FF3DF2>GLITCH ZONES</color></b>\n" +
            "Gravity zones flip or change gravity.\n" +
            "Inversion zones swap your left and right.\n" +
            "Pressure plates open doors - a heavy block works too.\n\n" +
            "<b><color=#FFB347>ESC</color></b> pauses the game.";

        UIKit.Label("Left", card.transform, left, 25, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(50f, -40f), new Vector2(680f, 620f), TextAnchor.UpperLeft, false);
        UIKit.Label("Right", card.transform, right, 25, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-50f, -40f), new Vector2(680f, 620f), TextAnchor.UpperLeft, false);

        UIKit.MakeButton("Back", p, "BACK", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 36f),
                         new Vector2(240f, 64f), 28, UIKit.Cyan, delegate { ShowPanel(mainPanel); });
    }

    // ================================================================ helpers
    void Rebuild()
    {
        // simplest way to refresh the cards after unlocking / resetting: reload the menu on the level select page
        GameProgress.OpenLevelSelectOnMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
