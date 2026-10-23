using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual and audio feedback for the existing DimensionShift component.
/// Attach this to the same GameObject as DimensionShift (normally the player).
/// Listens to DimensionShift.ShiftChanged; it does not implement dimension logic itself.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DimensionShift))]
public class DimensionShiftVFX : MonoBehaviour
{
    [Header("References (optional effects can be left empty)")]
    public SpriteRenderer playerSprite;
    public ParticleSystem shiftBurst;
    public AudioSource audioSource;
    public AudioClip enterShiftSound;
    public AudioClip exitShiftSound;
    [Tooltip("Optional full-screen UI Image. Stretch it to the Canvas edges and disable Raycast Target.")]
    public Image screenFlash;
    [Tooltip("Optional Unity UI Legacy Text for a brief REALITY SHIFTED / REALITY RESTORED message.")]
    public Text statusText;

    [Header("Player tint")]
    public Color shiftedTint = new Color(0.25f, 0.9f, 1f, 1f);
    public float tintTransitionDuration = 0.12f;

    [Header("Particle colors")]
    public Color enterParticleColor = new Color(0.25f, 0.9f, 1f, 1f);
    public Color exitParticleColor = new Color(1f, 0.25f, 0.85f, 1f);

    [Header("Afterimage glitch")]
    public bool createAfterimages = true;
    [Range(0f, 1f)] public float afterimageAlpha = 0.48f;
    [Min(0.01f)] public float afterimageLifetime = 0.18f;
    [Min(0f)] public float afterimageOffset = 0.12f;
    public Color enterAfterimageColor = new Color(0.2f, 0.95f, 1f, 1f);
    public Color exitAfterimageColor = new Color(1f, 0.2f, 0.8f, 1f);

    [Header("Screen flash (optional)")]
    public bool enableScreenFlash = true;
    [Range(0f, 1f)] public float flashMaxAlpha = 0.16f;
    [Min(0.01f)] public float flashDuration = 0.16f;
    public Color enterFlashColor = new Color(0.15f, 0.85f, 1f, 1f);
    public Color exitFlashColor = new Color(0.9f, 0.2f, 1f, 1f);

    [Header("Status notification (optional)")]
    public bool showStatusNotification = true;
    [Min(0.1f)] public float notificationDuration = 0.65f;
    public Color restoredTint = new Color(1f, 0.45f, 0.9f, 1f);

    private Color originalSpriteColor = Color.white;
    private Coroutine tintRoutine;
    private Coroutine flashRoutine;
    private Coroutine statusRoutine;
    private readonly List<GameObject> activeGhosts = new List<GameObject>();

    private void Awake()
    {
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerSprite != null)
            originalSpriteColor = playerSprite.color;

        if (screenFlash != null)
        {
            screenFlash.raycastTarget = false;
            Color c = screenFlash.color;
            c.a = 0f;
            screenFlash.color = c;
        }

        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        DimensionShift.ShiftChanged += OnShiftChanged;
    }

    private void OnDisable()
    {
        DimensionShift.ShiftChanged -= OnShiftChanged;

        if (playerSprite != null)
            playerSprite.color = originalSpriteColor;

        if (screenFlash != null)
        {
            Color c = screenFlash.color;
            c.a = 0f;
            screenFlash.color = c;
        }

        // Remove temporary afterimages if this component/player is disabled mid-effect.
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            if (activeGhosts[i] != null)
                Destroy(activeGhosts[i]);
        }
        activeGhosts.Clear();

        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    private void OnShiftChanged(bool shifted)
    {
        if (playerSprite != null)
        {
            if (tintRoutine != null) StopCoroutine(tintRoutine);
            tintRoutine = StartCoroutine(AnimateTint(shifted ? shiftedTint : originalSpriteColor));
        }

        PlayShiftSound(shifted);
        PlayParticleBurst(shifted);

        if (createAfterimages && playerSprite != null)
            CreateAfterimagePair(shifted);

        if (enableScreenFlash && screenFlash != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashScreen(shifted ? enterFlashColor : exitFlashColor));
        }

        if (showStatusNotification && statusText != null)
        {
            if (statusRoutine != null) StopCoroutine(statusRoutine);
            statusRoutine = StartCoroutine(ShowStatus(shifted));
        }
    }

    private IEnumerator AnimateTint(Color targetColor)
    {
        Color startColor = playerSprite.color;
        float duration = Mathf.Max(0.01f, tintTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration && playerSprite != null)
        {
            elapsed += Time.unscaledDeltaTime;
            playerSprite.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (playerSprite != null)
            playerSprite.color = targetColor;

        tintRoutine = null;
    }

    private void PlayShiftSound(bool shifted)
    {
        if (audioSource == null) return;

        AudioClip clip = shifted ? enterShiftSound : exitShiftSound;
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayParticleBurst(bool shifted)
    {
        if (shiftBurst == null) return;

        ParticleSystem.MainModule main = shiftBurst.main;
        main.startColor = shifted ? enterParticleColor : exitParticleColor;

        if (playerSprite != null)
            shiftBurst.transform.position = playerSprite.bounds.center;
        else
            shiftBurst.transform.position = transform.position;

        shiftBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        shiftBurst.Play(true);
    }

    private void CreateAfterimagePair(bool shifted)
    {
        Color tint = shifted ? enterAfterimageColor : exitAfterimageColor;
        CreateAfterimage(Vector3.left * afterimageOffset, tint);
        CreateAfterimage(Vector3.right * afterimageOffset, tint);
    }

    private void CreateAfterimage(Vector3 worldOffset, Color tint)
    {
        if (playerSprite == null || playerSprite.sprite == null) return;

        GameObject ghostObject = new GameObject("DimensionShift_Afterimage");
        ghostObject.transform.SetPositionAndRotation(
            playerSprite.transform.position + worldOffset,
            playerSprite.transform.rotation);
        ghostObject.transform.localScale = playerSprite.transform.lossyScale;

        SpriteRenderer ghost = ghostObject.AddComponent<SpriteRenderer>();
        ghost.sprite = playerSprite.sprite;
        ghost.flipX = playerSprite.flipX;
        ghost.flipY = playerSprite.flipY;
        ghost.sortingLayerID = playerSprite.sortingLayerID;
        ghost.sortingOrder = playerSprite.sortingOrder - 1;
        ghost.maskInteraction = playerSprite.maskInteraction;
        ghost.drawMode = playerSprite.drawMode;
        ghost.size = playerSprite.size;
        tint.a = afterimageAlpha;
        ghost.color = tint;

        // Safety timer means a ghost is removed even if its fade coroutine is interrupted.
        activeGhosts.Add(ghostObject);
        Destroy(ghostObject, Mathf.Max(0.02f, afterimageLifetime) + 0.05f);
        StartCoroutine(FadeAfterimage(ghost, Mathf.Max(0.02f, afterimageLifetime)));
    }

    private IEnumerator FadeAfterimage(SpriteRenderer ghost, float duration)
    {
        float elapsed = 0f;
        Color initial = ghost.color;

        while (elapsed < duration && ghost != null)
        {
            elapsed += Time.unscaledDeltaTime;
            Color c = initial;
            c.a = Mathf.Lerp(initial.a, 0f, Mathf.Clamp01(elapsed / duration));
            ghost.color = c;
            yield return null;
        }

        if (ghost != null)
            activeGhosts.Remove(ghost.gameObject);
    }

    private IEnumerator FlashScreen(Color flashColor)
    {
        Color c = flashColor;
        c.a = flashMaxAlpha;
        screenFlash.color = c;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, flashDuration);
        while (elapsed < duration && screenFlash != null)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(flashMaxAlpha, 0f, Mathf.Clamp01(elapsed / duration));
            screenFlash.color = c;
            yield return null;
        }

        if (screenFlash != null)
        {
            c.a = 0f;
            screenFlash.color = c;
        }
        flashRoutine = null;
    }

    private IEnumerator ShowStatus(bool shifted)
    {
        statusText.gameObject.SetActive(true);
        statusText.text = shifted ? "REALITY SHIFTED" : "REALITY RESTORED";
        Color textColor = shifted ? shiftedTint : restoredTint;
        textColor.a = 1f;
        statusText.color = textColor;

        RectTransform rect = statusText.rectTransform;
        Vector3 targetScale = Vector3.one;
        rect.localScale = Vector3.one * 0.88f;
        float introDuration = 0.12f;
        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(Vector3.one * 0.88f, targetScale,
                Mathf.Clamp01(elapsed / introDuration));
            yield return null;
        }
        rect.localScale = targetScale;

        yield return new WaitForSecondsRealtime(notificationDuration);

        float fadeDuration = 0.18f;
        elapsed = 0f;
        Color start = statusText.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color c = start;
            c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
            statusText.color = c;
            yield return null;
        }

        if (statusText != null)
            statusText.gameObject.SetActive(false);
        statusRoutine = null;
    }
}
