using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes the delivery timer stressful. In levels it replaces the calm music with a driving battle track,
/// layers in a frantic "panic" track as the clock runs down, adds a heartbeat that speeds up, and draws
/// pulsing red screen edges, a big final-10-seconds countdown and warning callouts.
/// Attaches itself to any scene that has a LevelManager.
/// </summary>
public class TimerPressure : MonoBehaviour
{
    /// <summary>0..1 pulse that spikes on every heartbeat (the HUD timer uses it too).</summary>
    public static float Beat;
    /// <summary>0 = relaxed, 1 = almost out of time.</summary>
    public static float Urgency;

    static readonly Color Red = new Color(1f, 0.1f, 0.22f, 1f);

    LevelManager lm;
    AudioSource battle, panic;
    Image edge, flash;
    Text bigNumber, callout;
    float beatTimer, bigT, calloutT, volMul, startedAt;
    int lastSecond = -1, killChecks = 90;
    bool alarmed, said60, said30, musicStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Beat = 0f; Urgency = 0f; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, mode) => Ensure();
        Ensure();
    }

    static void Ensure()
    {
        Beat = 0f; Urgency = 0f;
        if (LevelManager.Instance == null) return;
        if (FindFirstObjectByType<TimerPressure>() != null) return;
        new GameObject("TimerPressure").AddComponent<TimerPressure>();
    }

    void Start()
    {
        lm = LevelManager.Instance;
        BuildUi();

        battle = NewSource("GlitchVerse/Music/battle_loop");
        panic = NewSource("GlitchVerse/Music/panic_layer");
        if (battle != null && panic != null)
        {
            double t = AudioSettings.dspTime + 0.15;
            battle.PlayScheduled(t);
            panic.PlayScheduled(t);
            musicStarted = true;
        }
        startedAt = Time.unscaledTime;
    }

    AudioSource NewSource(string path)
    {
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip == null) return null;
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = clip; s.loop = true; s.volume = 0f; s.spatialBlend = 0f; s.playOnAwake = false; s.ignoreListenerPause = true;
        return s;
    }

    void BuildUi()
    {
        Canvas c = UIKit.CreateCanvas("TimerPressureCanvas", 450);
        c.transform.SetParent(transform, false);
        Vector2 mid = new Vector2(0.5f, 0.5f);

        RectTransform er = UIKit.Rect("Edge", c.transform, Vector2.zero, mid, Vector2.zero, Vector2.zero);
        UIKit.Stretch(er);
        edge = er.gameObject.AddComponent<Image>();
        edge.sprite = MakeEdge();
        edge.raycastTarget = false;
        edge.color = new Color(Red.r, Red.g, Red.b, 0f);

        RectTransform fr = UIKit.Rect("Flash", c.transform, Vector2.zero, mid, Vector2.zero, Vector2.zero);
        UIKit.Stretch(fr);
        flash = fr.gameObject.AddComponent<Image>();
        flash.sprite = UIKit.White;
        flash.raycastTarget = false;
        flash.color = new Color(Red.r, Red.g, Red.b, 0f);

        bigNumber = UIKit.Label("Big", c.transform, "", 420, Red, mid, mid, new Vector2(0f, 20f), new Vector2(900f, 520f), TextAnchor.MiddleCenter, true);
        Outline o = bigNumber.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.05f, 0f, 0.1f, 0.9f); o.effectDistance = new Vector2(8f, -8f);
        bigNumber.color = new Color(Red.r, Red.g, Red.b, 0f);

        callout = UIKit.Label("Callout", c.transform, "", 64, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter, true);
        Outline o2 = callout.gameObject.AddComponent<Outline>();
        o2.effectColor = new Color(0.4f, 0f, 0.1f, 0.9f); o2.effectDistance = new Vector2(4f, -4f);
        callout.color = new Color(1f, 1f, 1f, 0f);
    }

    static Sprite MakeEdge()
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
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0.4f, 1.15f, r)));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        if (lm == null) { lm = LevelManager.Instance; if (lm == null) return; }
        float dt = Time.unscaledDeltaTime;
        LevelManager.State st = lm.CurrentState;
        bool playing = st == LevelManager.State.Playing && Time.timeScale > 0.01f;

        // silence the calm menu music that the level scene also carries
        if (killChecks > 0)
        {
            killChecks--;
            foreach (MusicPlayer mp in FindObjectsByType<MusicPlayer>(FindObjectsSortMode.None))
            {
                AudioSource s = mp.GetComponent<AudioSource>();
                if (s != null && s.isPlaying) s.Stop();
                mp.enabled = false;
            }
        }

        // ---- urgency: starts rising in the last 45 s (or the last 30% of a short level)
        float window = Mathf.Min(45f, lm.timeLimit * 0.3f);
        float left = lm.TimeLeft;
        float target = (st == LevelManager.State.Playing || st == LevelManager.State.Paused) ? Mathf.Clamp01(1f - left / Mathf.Max(1f, window)) : 0f;
        Urgency = Mathf.MoveTowards(Urgency, target, dt * 0.5f);
        float u = Urgency;

        // ---- music
        if (musicStarted)
        {
            float want = 0.1f;                                         // intro: low tension bed
            if (st == LevelManager.State.Playing) want = 1f;
            else if (st == LevelManager.State.Delivering) want = 0.45f;
            else if (st == LevelManager.State.Paused) want = 0.3f;
            else if (st == LevelManager.State.Won || st == LevelManager.State.TimeUp) want = 0f;
            if (Time.timeScale <= 0.01f && st == LevelManager.State.Playing) want = 0.3f;   // death screen
            volMul = Mathf.MoveTowards(volMul, want, dt * (want < volMul ? 1.5f : 0.7f));
            float master = SfxHub.Volume;
            battle.volume = 0.5f * volMul * Mathf.Lerp(0.85f, 1f, u) * Mathf.Min(1f, master + 0.2f);
            panic.volume = 0.55f * volMul * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.12f) / 0.7f)) * Mathf.Min(1f, master + 0.2f);
            float pitch = 1f + 0.06f * u;
            battle.pitch = pitch; panic.pitch = pitch;
        }

        // ---- heartbeat + beat pulse
        Beat = Mathf.Max(0f, Beat - dt * 5.5f);
        if (playing && u > 0.04f)
        {
            beatTimer -= dt;
            if (beatTimer <= 0f)
            {
                beatTimer = Mathf.Lerp(1.1f, 0.36f, u);
                Beat = 1f;
                SfxHub.Play("heartbeat", 0.35f + 0.65f * u, 0.95f + 0.25f * u);
                if (CameraFx.Instance != null) CameraFx.Instance.Punch(-0.06f * u);
                if (PostFx.Instance != null) PostFx.Instance.Kick(0.04f + 0.12f * u, 0.15f + 0.4f * u);
            }
        }
        else beatTimer = 0.2f;

        // ---- callouts and the final countdown
        if (playing)
        {
            if (!said60 && lm.timeLimit >= 120f && left <= 60f && left > 50f) { said60 = true; Say("ONE MINUTE LEFT", new Color(1f, 0.85f, 0.3f)); }
            if (!said30 && lm.timeLimit >= 60f && left <= 30f && left > 20f) { said30 = true; Say("30 SECONDS!", new Color(1f, 0.55f, 0.25f)); SfxHub.Play("alarm", 0.35f, 0.8f); }
            if (!alarmed && left <= 10f && left > 0f)
            {
                alarmed = true;
                Say("HURRY!!", Red);
                SfxHub.Play("alarm", 0.6f, 1f);
                if (CameraFx.Instance != null) CameraFx.Instance.Shake(0.25f, 0.4f);
            }
            int sec = Mathf.CeilToInt(left);
            if (left <= 10f && left > 0f && sec != lastSecond)
            {
                lastSecond = sec;
                bigNumber.text = sec.ToString();
                bigT = 1f;
                flash.color = new Color(Red.r, Red.g, Red.b, 0.2f);
                if (CameraFx.Instance != null) CameraFx.Instance.Shake(0.12f + (10 - sec) * 0.015f, 0.25f);
            }
        }

        // big number animation
        bigT = Mathf.Max(0f, bigT - dt * 1.1f);
        float e = 1f - bigT;
        float bs = Mathf.Lerp(1.9f, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(e * 4f), 3f));
        bigNumber.rectTransform.localScale = Vector3.one * bs;
        bigNumber.color = new Color(Red.r, Red.g, Red.b, 0.6f * Mathf.Clamp01(bigT * 1.6f));
        flash.color = new Color(Red.r, Red.g, Red.b, flash.color.a * Mathf.Exp(-9f * dt));

        calloutT = Mathf.Max(0f, calloutT - dt);
        float ca = calloutT > 0f ? Mathf.Clamp01(Mathf.Min(calloutT * 3f, (2f - (2f - calloutT)) * 1f)) : 0f;
        Color cc = callout.color; cc.a = ca; callout.color = cc;
        callout.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Clamp01(calloutT - 1.6f) * 2.5f);

        // ---- red edge pulse
        float edgeA = u * (0.12f + 0.4f * Beat);
        if (left <= 10f && playing) edgeA += 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 22f));
        edge.color = new Color(Red.r, Red.g, Red.b, Mathf.Clamp01(edgeA));
    }

    void Say(string text, Color color)
    {
        callout.text = text;
        callout.color = color;
        calloutT = 2f;
        SfxHub.Play("ui_open", 0.8f, 0.7f);
    }

    void OnDestroy()
    {
        Beat = 0f; Urgency = 0f;
    }
}
