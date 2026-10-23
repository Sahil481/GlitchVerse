using System.Collections;
using UnityEngine;

/// <summary>
/// Vex, the ramen guy. Stands at the end of the level; when the courier arrives, the ramen is handed over,
/// a dialogue plays (level-specific, then a reaction that depends on how hot the ramen still is) and the level completes.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeliveryNpc : MonoBehaviour
{
    public SpriteRenderer body;
    public Sprite ramenSprite;
    public Sprite npcPortrait, heroPortrait;
    public Transform sign;

    bool triggered;
    Vector3 bodyBase;
    float phase;
    NeonLight light;
    float signBase;

    void Start()
    {
        if (body != null) bodyBase = body.transform.localPosition;
        phase = Random.value * 6f;
        if (sign != null) signBase = sign.localPosition.y;
        light = GetComponentInChildren<NeonLight>();
    }

    void Update()
    {
        CheckOverlap();
        float t = Time.time;
        if (body != null)
        {
            float breathe = Mathf.Sin(t * 2f + phase);
            body.transform.localScale = new Vector3(1f - 0.008f * breathe, 1f + 0.014f * breathe, 1f);
            body.transform.localPosition = bodyBase + new Vector3(0f, 0.03f * breathe, 0f);
        }
        if (sign != null) sign.localPosition = new Vector3(sign.localPosition.x, signBase + 0.08f * Mathf.Sin(t * 2.4f), 0f);

        // turn to face the courier
        PlayerController p = FindPlayer();
        if (p != null && body != null)
        {
            body.flipX = p.transform.position.x < transform.position.x ? false : true;
        }
    }

    PlayerController cachedPlayer;
    PlayerController FindPlayer()
    {
        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerController>();
        return cachedPlayer;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        TryStart(p);
    }

    Collider2D myCol, playerCol;

    void CheckOverlap()
    {
        if (triggered) return;
        PlayerController p = FindPlayer();
        if (p == null) return;
        if (myCol == null) myCol = GetComponent<Collider2D>();
        if (playerCol == null) playerCol = p.GetComponent<Collider2D>();
        if (myCol != null && playerCol != null && myCol.bounds.Intersects(playerCol.bounds)) TryStart(p);
    }

    void TryStart(PlayerController p)
    {
        if (triggered) return;
        if (p == null || p.IsDead) return;
        LevelManager lm = LevelManager.Instance;
        if (lm == null || lm.CurrentState != LevelManager.State.Playing) return;
        triggered = true;
        StartCoroutine(Sequence(p, lm));
    }

    IEnumerator Sequence(PlayerController p, LevelManager lm)
    {
        lm.BeginDelivery();
        float timeLeft = lm.TimeLeft;
        float frac = lm.timeLimit > 0f ? timeLeft / lm.timeLimit : 0f;
        Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
        if (prb != null) prb.linearVelocity = Vector2.zero;

        if (CameraFx.Instance != null) CameraFx.Instance.SetCinematicZoom(-1.6f);
        if (light != null) light.Flash(2.5f);
        FxLib.Ring(transform.position + Vector3.up * 1.3f, FxLib.Magenta, 0.5f, 4f, 0.5f);

        // let the camera ease in for a beat
        yield return new WaitForSecondsRealtime(0.45f);

        // toss the ramen across
        yield return ThrowRamen(p.transform.position + Vector3.up * 0.4f, transform.position + Vector3.up * 1.3f);
        SfxHub.Play("delivered");
        PlayerVisuals.Celebrate = true;
        FxLib.Burst(transform.position + Vector3.up * 1.3f, FxLib.Amber, 30, 7f, 0.16f, 0.8f, 0.3f);
        FxLib.Burst(transform.position + Vector3.up * 1.3f, FxLib.Magenta, 20, 6f, 0.13f, 0.7f);
        if (CameraFx.Instance != null) CameraFx.Instance.Shake(0.12f, 0.25f);
        if (light != null) light.Flash(3.5f);

        DialogueUI dui = new GameObject("DialogueUI").AddComponent<DialogueUI>();
        dui.npcPortrait = npcPortrait;
        dui.heroPortrait = heroPortrait;
        yield return dui.Play(BuildLines(lm.levelIndex, frac, timeLeft, lm.Deaths));
        Destroy(dui.gameObject);

        if (CameraFx.Instance != null) CameraFx.Instance.SetCinematicZoom(0f);
        yield return null;
        lm.Complete();
    }

    IEnumerator ThrowRamen(Vector3 from, Vector3 to)
    {
        GameObject go = new GameObject("RamenThrow");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ramenSprite;
        sr.sharedMaterial = FxLib.SpriteMat;
        sr.sortingOrder = 40;
        float t = 0f, dur = 0.7f;
        float last = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.Clamp01(t);
            Vector3 pos = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, -k * 360f);
            go.transform.localScale = Vector3.one * (0.9f + 0.2f * Mathf.Sin(k * Mathf.PI));
            if (Time.unscaledTime - last > 0.04f)
            {
                last = Time.unscaledTime;
                FxLib.Burst(pos, FxLib.Amber, 1, 0.5f, 0.12f, 0.3f);
            }
            yield return null;
        }
        Destroy(go);
    }

    static DialogueUI.Line[] BuildLines(int level, float frac, float timeLeft, int deaths)
    {
        string[] greet =
        {
            "Whoa, hold up. Is that my ramen?",
            "Ha! Right on schedule. Slipstream suits you.",
            "You came through the phantom lane for noodles? Respect.",
            "Everything upside down and you still kept the broth level?",
            "The final run. Tell me you did not spill a drop."
        };
        string[] hero =
        {
            "Hot ramen, one fresh delivery. Glitches included.",
            "Wall to wall, no stops. Here you go.",
            "Phased through, shrank, squeezed in. Still warm.",
            "Gravity had other plans. The soup disagreed.",
            "Every glitch in this city. One bowl."
        };
        int i = Mathf.Clamp(level - 1, 0, 4);

        string react;
        if (frac > 0.5f) react = "Steam is still rolling off it. Fast does not cover it. You are a legend.";
        else if (frac > 0.2f) react = "Still hot. Another minute and I would have been mad. Not mad. Hungry-mad.";
        else react = "Lukewarm. Cutting it close, courier. I will microwave it and call it fresh.";

        string tail = deaths == 0 ? "And not a single wipeout? Okay, you are getting a tip." : "Rough trip, huh? Worth it. Slurp time.";

        return new[]
        {
            new DialogueUI.Line(true, greet[i]),
            new DialogueUI.Line(false, hero[i]),
            new DialogueUI.Line(true, react),
            new DialogueUI.Line(true, tail),
        };
    }
}
