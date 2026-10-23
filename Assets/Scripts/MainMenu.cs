using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Main menu with level selector, controls page and progress info. Built entirely from code.</summary>
public class MainMenu : MonoBehaviour
{
    public Sprite heroSprite;
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
        SceneFader.Ready();
        if (heroSprite != null && mainPanel != null)
        {
            Image glow = UIKit.Icon("HeroGlow", mainPanel.transform, FxLib.DotSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                    new Vector2(540f, -20f), new Vector2(1000f, 1000f), new Color(0.16f, 0.94f, 1f, 0.28f));
            glow.transform.SetAsFirstSibling();
            heroImg = UIKit.Icon("Hero", mainPanel.transform, heroSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                 new Vector2(540f, -20f), new Vector2(560f, 980f), Color.white);
            heroImg.transform.SetSiblingIndex(1);
            heroGlow = glow;
        }

        if (GameProgress.OpenLevelSelectOnMenu) ShowPanel(levelsPanel);
        else ShowPanel(mainPanel);
        GameProgress.OpenLevelSelectOnMenu = false;
    }

    Image heroImg, heroGlow;

    void Update()
    {
        if (heroImg != null)
        {
            float t = Time.unscaledTime;
            heroImg.rectTransform.anchoredPosition = new Vector2(540f, -20f + Mathf.Sin(t * 1.6f) * 14f);
            heroImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 1.2f);
            if (heroGlow != null)
            {
                Color gc = heroGlow.color;
                gc.a = 0.24f + 0.08f * Mathf.Sin(t * 2.1f);
                heroGlow.color = gc;
            }
        }
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

        // left column: title, tagline, menu
        Vector2 tl = new Vector2(0f, 1f);
        if (titleSprite != null)
        {
            Image title = UIKit.Icon("Title", p, titleSprite, tl, tl, new Vector2(70f, -40f), new Vector2(1000f, 340f), Color.white);
            title.gameObject.AddComponent<SlideIn>().from = new Vector2(-80f, 0f);
        }
        else
        {
            UIKit.Label("TitleText", p, "GLITCHVERSE", 120, Color.white, tl, tl, new Vector2(80f, -80f), new Vector2(1000f, 160f), TextAnchor.MiddleLeft, true);
        }
        Text tag = UIKit.Label("Tagline", p, "THE WORLD IS BROKEN.  DELIVER THE RAMEN ANYWAY.", 26, new Color(UIKit.Cyan.r, UIKit.Cyan.g, UIKit.Cyan.b, 0.95f),
                               tl, tl, new Vector2(90f, -372f), new Vector2(1000f, 40f), TextAnchor.MiddleLeft, true);
        tag.gameObject.AddComponent<SlideIn>().delay = 0.15f;

        Vector2 sz = new Vector2(560f, 84f);
        Vector2 lc = new Vector2(0f, 0.5f);
        int next = Mathf.Max(1, Mathf.Min(GameProgress.HighestUnlocked, GameProgress.LevelCount));
        string playLabel = GameProgress.HasPlayed(1) ? "CONTINUE  -  LEVEL " + next : "START DELIVERY";
        string[] labels = { playLabel, "LEVEL SELECT", "HOW TO PLAY", "QUIT" };
        Color[] cols = { UIKit.Cyan, UIKit.Violet, UIKit.Amber, UIKit.Magenta };
        UnityEngine.Events.UnityAction[] acts =
        {
            delegate { SceneFader.Load(LevelCatalog.Scene(next)); },
            delegate { ShowPanel(levelsPanel); },
            delegate { ShowPanel(controlsPanel); },
            delegate { Quit(); }
        };
        for (int i = 0; i < 4; i++)
        {
            Button b = UIKit.MakeButton("Btn" + i, p, labels[i], lc, lc, new Vector2(110f, -70f - i * 108f), sz, 34, cols[i], acts[i]);
            SlideIn si = b.gameObject.AddComponent<SlideIn>();
            si.from = new Vector2(-160f, 0f);
            si.delay = 0.25f + i * 0.09f;
        }

        // progress chip, bottom left
        int stars = 0;
        for (int i = 1; i <= GameProgress.LevelCount; i++) stars += GameProgress.Stars(i);
        Image chip = UIKit.Panel("Chip", p, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 44f), new Vector2(420f, 62f),
                                 new Color(0.06f, 0.04f, 0.16f, 0.8f), true, new Color(UIKit.Amber.r, UIKit.Amber.g, UIKit.Amber.b, 0.7f));
        if (starOn != null) UIKit.Icon("ChipStar", chip.transform, starOn, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(40f, 40f), Color.white);
        UIKit.Label("ChipText", chip.transform, "STARS  " + stars + " / " + (GameProgress.LevelCount * 3), 26, UIKit.Amber,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(320f, 40f), TextAnchor.MiddleLeft, true);

        UIKit.Label("Credit", p, "GlitchVerse  |  Beat the clock. Break the rules of the world.", 20,
                    new Color(1f, 1f, 1f, 0.5f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 20f),
                    new Vector2(1000f, 30f), TextAnchor.MiddleRight, false);
    }

    void BuildLevels(Transform root)
    {
        levelsPanel = NewPanel("LevelsPanel", root);
        Transform p = levelsPanel.transform;

        UIKit.Label("Header", p, "SELECT DELIVERY", 80, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
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
                                 new Color(0.07f, 0.05f, 0.18f, 0.9f), true, new Color(accent.r, accent.g, accent.b, unlocked ? 0.95f : 0.3f));
        Button btn = card.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = card;
        ButtonFx fx = card.gameObject.AddComponent<ButtonFx>();
        fx.fill = card;
        fx.border = card.transform.Find("Border").GetComponent<Image>();
        fx.glow = card.transform.Find("Glow").GetComponent<Image>();
        fx.accent = accent;
        fx.hoverScale = 1.04f;
        fx.lift = 14f;
        fx.glowBase = unlocked ? 0.35f : 0.1f;
        SlideIn cin = card.gameObject.AddComponent<SlideIn>();
        cin.from = new Vector2(0f, -70f);
        cin.delay = 0.05f + level * 0.07f;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.interactable = unlocked;
        int captured = level;
        btn.onClick.AddListener(delegate { SceneFader.Load(LevelCatalog.Scene(captured)); });

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
            veil.sprite = UIKit.RoundedFill;
            veil.type = Image.Type.Sliced;
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

        Image card = UIKit.Panel("CardL", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, -30f),
                                 new Vector2(760f, 730f), UIKit.PanelDark, true, UIKit.Cyan);
        Image cardR = UIKit.Panel("CardR", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, -30f),
                                  new Vector2(760f, 730f), UIKit.PanelDark, true, UIKit.Magenta);

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
            "Bring the hot ramen to Vex before the countdown ends.\n" +
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

        UIKit.Label("Left", card.transform, left, 26, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(40f, -34f), new Vector2(690f, 660f), TextAnchor.UpperLeft, false);
        UIKit.Label("Right", cardR.transform, right, 26, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(40f, -34f), new Vector2(690f, 660f), TextAnchor.UpperLeft, false);

        UIKit.MakeButton("Back", p, "BACK", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 36f),
                         new Vector2(240f, 64f), 28, UIKit.Cyan, delegate { ShowPanel(mainPanel); });
    }

    // ================================================================ helpers
    void Rebuild()
    {
        // simplest way to refresh the cards after unlocking / resetting: reload the menu on the level select page
        GameProgress.OpenLevelSelectOnMenu = true;
        SceneFader.Load(SceneManager.GetActiveScene().name);
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
