using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-level interface, built from code: delivery countdown, shard counter, tutorial tips, toasts,
/// level intro card, pause menu and the delivered / time-up result screen.
/// (The top-left corner is left free for the dimension energy bar.)
/// </summary>
public class LevelHUD : MonoBehaviour
{
    public Sprite shardIcon, starOn, starOff;

    LevelManager lm;
    Canvas canvas;

    // timer
    RectTransform timerRoot;
    Text timerText, timerLabel;
    Image timerBar;
    // shards / level label
    Text shardText, levelLabel;
    // tip
    CanvasGroup tipGroup;
    Text tipTitle, tipBody;
    object tipOwner;
    float tipAlphaTarget;
    // toast
    CanvasGroup toastGroup;
    Text toastText;
    float toastTimer;
    // intro
    CanvasGroup introGroup;
    float introTimer, introDuration;
    // pause / result
    GameObject pausePanel, resultPanel;
    Text resultTitle, resultNew;
    Text[] rowValues = new Text[5];
    Image[] starImages = new Image[3];
    Button nextBtn;
    Text hintText;

    public void Init(LevelManager manager)
    {
        lm = manager;
        Build();
    }

    // ================================================================ build
    void Build()
    {
        UIKit.EnsureEventSystem();
        canvas = UIKit.CreateCanvas("LevelHUD_Canvas", 500);
        canvas.transform.SetParent(transform, false);
        Transform root = canvas.transform;

        // ---- timer (top centre)
        Image timerPanel = UIKit.Panel("Timer", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f),
                                       new Vector2(340f, 104f), UIKit.PanelDark, true, new Color(UIKit.Cyan.r, UIKit.Cyan.g, UIKit.Cyan.b, 0.7f));
        timerRoot = timerPanel.rectTransform;
        timerLabel = UIKit.Label("TimerLabel", timerRoot, "RAMEN IS COOLING", 17, UIKit.TextSoft, new Vector2(0.5f, 1f),
                                 new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(320f, 24f), TextAnchor.MiddleCenter, false);
        timerText = UIKit.Label("TimerText", timerRoot, "03:00", 56, Color.white, new Vector2(0.5f, 0.5f),
                                new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(320f, 70f), TextAnchor.MiddleCenter, true);
        Image barBg = UIKit.Panel("BarBG", timerRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f),
                                  new Vector2(300f, 7f), new Color(1f, 1f, 1f, 0.12f), false, Color.clear);
        timerBar = UIKit.Panel("BarFill", barBg.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                               new Vector2(300f, 7f), UIKit.Cyan, false, Color.clear);
        barBg.sprite = UIKit.White;
        timerBar.sprite = UIKit.White;
        timerBar.type = Image.Type.Filled;
        timerBar.fillMethod = Image.FillMethod.Horizontal;
        timerBar.fillOrigin = 0;

        // ---- level label + shards (top right)
        Image infoPanel = UIKit.Panel("Info", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -44f),
                                      new Vector2(360f, 104f), UIKit.PanelDark, true, new Color(UIKit.Magenta.r, UIKit.Magenta.g, UIKit.Magenta.b, 0.6f));
        levelLabel = UIKit.Label("LevelLabel", infoPanel.transform, "LEVEL " + lm.levelIndex + "  " + lm.title, 20, UIKit.TextSoft,
                                 new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(340f, 28f),
                                 TextAnchor.MiddleCenter, true);
        if (shardIcon != null)
            UIKit.Icon("ShardIcon", infoPanel.transform, shardIcon, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                       new Vector2(-75f, 12f), new Vector2(46f, 46f), Color.white);
        shardText = UIKit.Label("ShardText", infoPanel.transform, "0 / 0", 38, UIKit.Amber, new Vector2(0.5f, 0f),
                                new Vector2(0.5f, 0f), new Vector2(25f, 12f), new Vector2(150f, 46f), TextAnchor.MiddleLeft, true);

        // ---- bottom hint
        hintText = UIKit.Label("Hint", root, "ESC  PAUSE", 17, new Color(1f, 1f, 1f, 0.45f), new Vector2(1f, 0f),
                               new Vector2(1f, 0f), new Vector2(-48f, 36f), new Vector2(240f, 24f), TextAnchor.MiddleRight, false);

        // ---- tip card (bottom centre)
        Image tip = UIKit.Panel("Tip", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f),
                                new Vector2(900f, 190f), new Color(0.04f, 0.03f, 0.11f, 0.94f), true,
                                new Color(UIKit.Cyan.r, UIKit.Cyan.g, UIKit.Cyan.b, 0.8f));
        tipGroup = tip.gameObject.AddComponent<CanvasGroup>();
        tipGroup.alpha = 0f;
        tipGroup.blocksRaycasts = false;
        tipTitle = UIKit.Label("TipTitle", tip.transform, "", 30, UIKit.Cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                               new Vector2(0f, -16f), new Vector2(860f, 40f), TextAnchor.MiddleCenter, true);
        tipBody = UIKit.Label("TipBody", tip.transform, "", 26, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                              new Vector2(0f, 18f), new Vector2(840f, 120f), TextAnchor.MiddleCenter, false);

        // ---- toast
        Image toast = UIKit.Panel("Toast", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f),
                                  new Vector2(420f, 50f), UIKit.PanelDark, true, new Color(UIKit.Mint.r, UIKit.Mint.g, UIKit.Mint.b, 0.8f));
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastText = UIKit.Label("ToastText", toast.transform, "", 24, UIKit.Mint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                Vector2.zero, new Vector2(400f, 44f), TextAnchor.MiddleCenter, true);

        BuildIntro(root);
        BuildPause(root);
        BuildResult(root);
        Refresh();
    }

    void BuildIntro(Transform root)
    {
        Image dim = UIKit.Panel("Intro", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                new Color(0.02f, 0.01f, 0.07f, 0.72f), false, Color.clear);
        UIKit.Stretch(dim.rectTransform);
        dim.raycastTarget = false;
        introGroup = dim.gameObject.AddComponent<CanvasGroup>();
        introGroup.alpha = 0f;
        introGroup.blocksRaycasts = false;

        UIKit.Label("IntroNum", dim.transform, "LEVEL " + lm.levelIndex, 34, UIKit.Magenta, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 50f), TextAnchor.MiddleCenter, true);
        UIKit.Label("IntroTitle", dim.transform, lm.title, 96, Color.white, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1500f, 120f), TextAnchor.MiddleCenter, true);
        UIKit.Label("IntroSub", dim.transform, lm.subtitle, 30, UIKit.TextSoft, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1300f, 44f), TextAnchor.MiddleCenter, false);
        UIKit.Label("IntroObj", dim.transform, "DELIVER VEX'S HOT RAMEN IN " + GameProgress.FormatTime(lm.timeLimit),
                    28, UIKit.Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f),
                    new Vector2(1300f, 44f), TextAnchor.MiddleCenter, true);
    }

    void BuildPause(Transform root)
    {
        Image dim = UIKit.Panel("PausePanel", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                new Color(0.02f, 0.01f, 0.07f, 0.8f), false, Color.clear);
        UIKit.Stretch(dim.rectTransform);
        pausePanel = dim.gameObject;

        Image card = UIKit.Panel("Card", dim.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                                 new Vector2(560f, 600f), UIKit.PanelDark, true, UIKit.Violet);
        UIKit.Label("PauseTitle", card.transform, "PAUSED", 64, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -36f), new Vector2(500f, 80f), TextAnchor.MiddleCenter, true);
        Vector2 sz = new Vector2(400f, 68f);
        UIKit.MakeButton("Resume", card.transform, "RESUME", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), sz, 30, UIKit.Cyan, lm.Resume);
        UIKit.MakeButton("Restart", card.transform, "RESTART LEVEL", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), sz, 30, UIKit.Amber, lm.Retry);
        UIKit.MakeButton("Levels", card.transform, "LEVEL SELECT", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), sz, 30, UIKit.Violet, lm.LevelSelect);
        UIKit.MakeButton("Menu", card.transform, "MAIN MENU", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -420f), sz, 30, UIKit.Magenta, lm.MainMenu);
        pausePanel.SetActive(false);
    }

    void BuildResult(Transform root)
    {
        Image dim = UIKit.Panel("ResultPanel", root, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                new Color(0.02f, 0.01f, 0.07f, 0.86f), false, Color.clear);
        UIKit.Stretch(dim.rectTransform);
        resultPanel = dim.gameObject;

        Image card = UIKit.Panel("Card", dim.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                                 new Vector2(820f, 880f), UIKit.PanelDark, true, UIKit.Cyan);
        resultTitle = UIKit.Label("ResultTitle", card.transform, "DELIVERED!", 76, UIKit.Mint, new Vector2(0.5f, 1f),
                                  new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(760f, 96f), TextAnchor.MiddleCenter, true);

        for (int i = 0; i < 3; i++)
        {
            starImages[i] = UIKit.Icon("Star" + i, card.transform, starOff, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                       new Vector2((i - 1) * 120f, -140f), new Vector2(104f, 104f), Color.white);
        }

        string[] labels = { "TIME USED", "TIME BONUS", "SHARDS", "NO-DEATH BONUS", "SCORE" };
        for (int i = 0; i < labels.Length; i++)
        {
            float y = -290f - i * 52f;
            bool big = i == labels.Length - 1;
            UIKit.Label("RowL" + i, card.transform, labels[i], big ? 34 : 26, big ? UIKit.Amber : UIKit.TextSoft, new Vector2(0.5f, 1f),
                        new Vector2(0f, 1f), new Vector2(-330f, y), new Vector2(380f, 46f), TextAnchor.MiddleLeft, big);
            rowValues[i] = UIKit.Label("RowV" + i, card.transform, "", big ? 40 : 28, big ? UIKit.Amber : Color.white, new Vector2(0.5f, 1f),
                                       new Vector2(1f, 1f), new Vector2(330f, y), new Vector2(380f, 46f), TextAnchor.MiddleRight, true);
        }

        resultNew = UIKit.Label("NewBest", card.transform, "", 28, UIKit.Magenta, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                new Vector2(0f, -560f), new Vector2(700f, 40f), TextAnchor.MiddleCenter, true);

        Vector2 sz = new Vector2(350f, 66f);
        nextBtn = UIKit.MakeButton("Next", card.transform, "NEXT LEVEL", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 150f), sz, 28, UIKit.Mint, lm.NextLevel);
        UIKit.MakeButton("Retry", card.transform, "RETRY", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 150f), sz, 28, UIKit.Amber, lm.Retry);
        UIKit.MakeButton("Levels", card.transform, "LEVEL SELECT", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 60f), sz, 28, UIKit.Violet, lm.LevelSelect);
        UIKit.MakeButton("Menu", card.transform, "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 60f), sz, 28, UIKit.Magenta, lm.MainMenu);
        resultPanel.SetActive(false);
    }

    // ================================================================ public API
    public void ShowIntro(float duration)
    {
        introDuration = duration;
        introTimer = duration;
    }

    public void ShowPause(bool show)
    {
        if (pausePanel != null) pausePanel.SetActive(show);
    }

    public void Toast(string text)
    {
        if (toastText == null) return;
        toastText.text = text;
        toastTimer = 2.0f;
    }

    public void ShowTip(object owner, string title, string body)
    {
        if (tipGroup == null) return;
        tipOwner = owner;
        tipTitle.text = title;
        tipBody.text = GameProgress.FillKeys(body, lm != null ? lm.ShiftKeyName : "R");
        tipAlphaTarget = 1f;
    }

    public void HideTip(object owner)
    {
        if (tipOwner == owner) tipAlphaTarget = 0f;
    }

    public void ShowResult(bool won, LevelResult r)
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        resultPanel.SetActive(true);

        resultTitle.text = won ? "DELIVERED!" : "RAMEN WENT COLD";
        resultTitle.color = won ? UIKit.Mint : UIKit.Red;

        rowValues[0].text = GameProgress.FormatTime(r.timeUsed);
        rowValues[1].text = won ? "+" + r.timeBonus : "-";
        rowValues[2].text = r.shards + " / " + r.totalShards + "   +" + r.shardBonus;
        rowValues[3].text = won ? (r.noDeathBonus > 0 ? "+" + r.noDeathBonus : "0  (" + r.deaths + " deaths)") : "-";
        rowValues[4].text = won ? r.score.ToString() : "0";

        if (won) resultNew.text = r.newBest ? "NEW BEST SCORE!" : "BEST  " + r.bestScore;
        else resultNew.text = "Vex is not happy. Cold noodles. Try again.";

        nextBtn.gameObject.SetActive(won && lm.HasNext);
        StartCoroutine(RevealStars(won ? r.stars : 0));
    }

    IEnumerator RevealStars(int count)
    {
        for (int i = 0; i < 3; i++)
        {
            starImages[i].sprite = starOff;
            starImages[i].rectTransform.localScale = Vector3.one;
        }
        for (int i = 0; i < count; i++)
        {
            float wait = 0.35f;
            while (wait > 0f) { wait -= Time.unscaledDeltaTime; yield return null; }
            starImages[i].sprite = starOn;
            SfxHub.Play("star");
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.25f;
                float s = 1f + 0.4f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                starImages[i].rectTransform.localScale = Vector3.one * s;
                yield return null;
            }
            starImages[i].rectTransform.localScale = Vector3.one;
        }
    }

    // ================================================================ per frame
    Vector2 timerBase;
    bool timerBaseSet;

    public void Refresh()
    {
        if (lm == null || timerText == null) return;
        float dt = Time.unscaledDeltaTime;

        // timer
        float left = lm.TimeLeft;
        timerText.text = GameProgress.FormatTime(Mathf.Ceil(left));
        float frac = lm.timeLimit > 0f ? Mathf.Clamp01(left / lm.timeLimit) : 0f;
        timerBar.fillAmount = frac;
        // hot ramen: orange-amber when fresh, fading to cold blue-cyan as time runs out
        Color c = Color.Lerp(new Color(0.55f, 0.8f, 1f), UIKit.Amber, Mathf.SmoothStep(0f, 1f, frac * 1.3f));
        float pulse = 1f;
        if (left <= 20f)
        {
            c = Color.Lerp(UIKit.Cyan, UIKit.Red, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
            pulse = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 8f);
        }
        pulse += 0.10f * TimerPressure.Beat * TimerPressure.Urgency;
        if (timerRoot != null)
        {
            if (!timerBaseSet) { timerBase = timerRoot.anchoredPosition; timerBaseSet = true; }
            float shake = left <= 10f && left > 0f ? 4f : 0f;
            timerRoot.anchoredPosition = timerBase + new Vector2(Random.Range(-shake, shake), Random.Range(-shake, shake));
        }
        if (timerLabel != null)
            timerLabel.text = frac > 0.5f ? "RAMEN IS HOT" : (frac > 0.2f ? "RAMEN IS WARM" : "RAMEN IS COOLING");
        timerText.color = Color.Lerp(Color.white, c, 0.6f);
        timerBar.color = c;
        timerRoot.localScale = Vector3.one * pulse;

        // shards
        shardText.text = lm.Shards + " / " + lm.totalShards;

        // tip fade
        if (tipGroup != null) tipGroup.alpha = Mathf.MoveTowards(tipGroup.alpha, tipAlphaTarget, dt * 4f);

        // toast
        if (toastTimer > 0f) toastTimer -= dt;
        if (toastGroup != null) toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, toastTimer > 0f ? 1f : 0f, dt * 5f);

        // intro
        if (introTimer > 0f)
        {
            introTimer -= dt;
            float a = introTimer > 0.45f ? 1f : Mathf.Clamp01(introTimer / 0.45f);
            float fadeIn = Mathf.Clamp01((introDuration - introTimer) / 0.25f);
            introGroup.alpha = Mathf.Min(a, fadeIn);
        }
        else if (introGroup != null) introGroup.alpha = 0f;
    }
}
