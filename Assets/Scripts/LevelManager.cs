using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Names and display info for the five levels (used by the menu, HUD and level builder).</summary>
public static class LevelCatalog
{
    public const string MenuScene = "MainMenu";

    public static readonly string[] SceneNames =
    {
        "L1_FirstDelivery", "L2_Slipstream", "L3_PhantomLane", "L4_Inversion", "L5_FinalDelivery"
    };

    public static readonly string[] Titles =
    {
        "FIRST DELIVERY", "SLIPSTREAM", "PHANTOM LANE", "INVERSION", "FINAL DELIVERY"
    };

    public static readonly string[] Subtitles =
    {
        "Learn the streets. Learn your glitches.",
        "Rewrite the world around you.",
        "Shift, flip, swap and shrink.",
        "Climb. Flip. Choose your road.",
        "The ramen is cooling. Perfect run: three minutes."
    };

    public static readonly string[] Tags =
    {
        "TUTORIAL  |  MOVEMENT  |  PHASE DASH",
        "MANIPULATION  |  LIFTS  |  MATERIALS",
        "DIMENSIONS  |  ZONES  |  SHRINK",
        "ROUTES  |  VERTICAL  |  PUZZLES",
        "THE GAUNTLET  |  ALL MECHANICS"
    };

    public static readonly int[] ParTimes = { 75, 90, 100, 130, 180 };

    public static string Scene(int level)
    {
        return SceneNames[Mathf.Clamp(level - 1, 0, SceneNames.Length - 1)];
    }
}

/// <summary>
/// Runs one level: delivery countdown, shard / death counting, checkpoints, pause, win and time-up flow, scoring.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public enum State { Intro, Playing, Paused, Delivering, Won, TimeUp }

    public static LevelManager Instance { get; private set; }

    [Header("Level info (filled by the level builder)")]
    public int levelIndex = 1;
    public string title = "LEVEL";
    public string subtitle = "";
    public string objective = "Reach the delivery terminal.";
    public float timeLimit = 180f;
    public float parTime = 90f;
    public int totalShards = 0;
    public LevelHUD Hud;

    public State CurrentState { get; private set; }
    public float TimeLeft { get; private set; }
    public int Shards { get; private set; }
    public int Deaths { get; private set; }

    PlayerController player;
    RadialMenu radial;
    ManipulationController manip;
    int lastCheckpoint;

    public string ShiftKeyName
    {
        get
        {
            DimensionShift ds = FindFirstObjectByType<DimensionShift>();
            return GameProgress.KeyName(ds != null ? ds.toggleKey : KeyCode.R);
        }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        TimeLeft = timeLimit;
        CurrentState = State.Intro;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        PlayerController.PlayerRespawned -= OnPlayerRespawned;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        radial = FindFirstObjectByType<RadialMenu>();
        manip = FindFirstObjectByType<ManipulationController>();
        PlayerController.PlayerRespawned += OnPlayerRespawned;

        SceneFader.Ready();
        if (Hud != null) Hud.Init(this);
        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        // level 1 opens with the comic-book cutscene (once per play session)
        if (levelIndex == 1 && !ComicCutscene.Seen)
        {
            if (player != null) player.SetGameplayInputEnabled(false);
            bool finished = false;
            ComicCutscene.Play(delegate { finished = true; });
            while (!finished) yield return null;
        }
        yield return IntroRoutine();
    }

    IEnumerator IntroRoutine()
    {
        CurrentState = State.Intro;
        if (player != null) player.SetGameplayInputEnabled(false);
        if (Hud != null) Hud.ShowIntro(2.0f);

        float t = 0f;
        while (t < 2.0f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (player != null) player.SetGameplayInputEnabled(true);
        CurrentState = State.Playing;
    }

    void OnPlayerRespawned()
    {
        if (CurrentState == State.Playing || CurrentState == State.Intro) Deaths++;
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Playing:
                bool dead = player != null && player.IsDead;
                if (!dead)
                {
                    float before = TimeLeft;
                    TimeLeft -= Time.unscaledDeltaTime;
                    if (TimeLeft < 11f && Mathf.CeilToInt(before) != Mathf.CeilToInt(TimeLeft) && TimeLeft > 0f)
                        SfxHub.Play("tick_low", 0.8f, TimeLeft < 4f ? 1.35f : 1f);
                    if (TimeLeft <= 0f)
                    {
                        TimeLeft = 0f;
                        TimeUp();
                        break;
                    }
                }

                if (!dead && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
                {
                    bool wheelOpen = radial != null && radial.IsOpen;
                    bool moving = manip != null && manip.IsMoving;
                    if (!wheelOpen && !moving) Pause();
                }
                break;

            case State.Delivering:
                break;

            case State.Paused:
                Time.timeScale = 0f;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) Resume();
                break;

            case State.Won:
            case State.TimeUp:
                Time.timeScale = 0f;
                break;
        }

        if (Hud != null) Hud.Refresh();
    }

    // ------------------------------------------------------------ events from world objects
    public void CollectShard()
    {
        Shards++;
    }

    public bool TryActivateCheckpoint(int order)
    {
        if (order <= lastCheckpoint) return false;
        lastCheckpoint = order;
        return true;
    }

    // ------------------------------------------------------------ flow
    public void Pause()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Paused;
        Time.timeScale = 0f;
        Cursor.visible = true;
        SfxHub.Play("pause");
        if (Hud != null) Hud.ShowPause(true);
    }

    public void Resume()
    {
        if (CurrentState != State.Paused) return;
        CurrentState = State.Playing;
        Time.timeScale = 1f;
        SfxHub.Play("resume");
        if (Hud != null) Hud.ShowPause(false);
    }

    void TimeUp()
    {
        CurrentState = State.TimeUp;
        if (player != null) player.SetGameplayInputEnabled(false);
        Time.timeScale = 0f;
        Cursor.visible = true;
        SfxHub.Play("fail");
        if (Hud != null) Hud.ShowResult(false, ResultData(false));
    }

    /// <summary>Called by the NPC when the courier arrives: stops the clock and input while the hand-over plays.</summary>
    public void BeginDelivery()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Delivering;
        if (player != null) player.SetGameplayInputEnabled(false);
        if (Hud != null) Hud.Toast("ORDER DELIVERED");
    }

    public void Complete()
    {
        if (CurrentState != State.Playing && CurrentState != State.Delivering) return;
        CurrentState = State.Won;
        if (player != null) player.SetGameplayInputEnabled(false);

        LevelResult r = ResultData(true);
        r.newBest = GameProgress.CompleteLevel(levelIndex, r.timeUsed, r.score, r.stars);
        r.bestScore = GameProgress.BestScore(levelIndex);

        Time.timeScale = 0f;
        Cursor.visible = true;
        SfxHub.Play("win");
        if (Hud != null) Hud.ShowResult(true, r);
    }

    LevelResult ResultData(bool won)
    {
        LevelResult r = new LevelResult();
        r.won = won;
        r.timeUsed = timeLimit - TimeLeft;
        r.timeLeft = TimeLeft;
        r.shards = Shards;
        r.totalShards = totalShards;
        r.deaths = Deaths;
        r.timeBonus = won ? Mathf.RoundToInt(TimeLeft * 10f) : 0;
        r.shardBonus = Shards * 250;
        r.noDeathBonus = (won && Deaths == 0) ? 500 : 0;
        r.score = won ? r.timeBonus + r.shardBonus + r.noDeathBonus : 0;

        bool allShards = totalShards > 0 && Shards >= totalShards;
        if (!won) r.stars = 0;
        else if (allShards && r.timeUsed <= parTime) r.stars = 3;
        else if (allShards || r.timeUsed <= parTime * 1.45f) r.stars = 2;
        else r.stars = 1;
        return r;
    }

    // ------------------------------------------------------------ navigation (HUD buttons)
    public void Retry()
    {
        Time.timeScale = 1f;
        SceneFader.Load(SceneManager.GetActiveScene().name);
    }

    public bool HasNext { get { return levelIndex < GameProgress.LevelCount; } }

    public void NextLevel()
    {
        if (!HasNext) { LevelSelect(); return; }
        Time.timeScale = 1f;
        SceneFader.Load(LevelCatalog.Scene(levelIndex + 1));
    }

    public void LevelSelect()
    {
        Time.timeScale = 1f;
        GameProgress.OpenLevelSelectOnMenu = true;
        SceneFader.Load(LevelCatalog.MenuScene);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        GameProgress.OpenLevelSelectOnMenu = false;
        SceneFader.Load(LevelCatalog.MenuScene);
    }
}

public class LevelResult
{
    public bool won;
    public float timeUsed, timeLeft;
    public int shards, totalShards, deaths;
    public int timeBonus, shardBonus, noDeathBonus, score, stars, bestScore;
    public bool newBest;
}
