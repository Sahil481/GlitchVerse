using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a GlitchVerse-style YOU DIED overlay at runtime.
/// Attach it to an empty GameObject in each playable level and assign PlayerController.
/// Level choices are read from Build Settings unless Level Scene Names is filled in.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("Optional explicit scene names. Leave empty to populate from scenes included in Build Settings.")]
    [SerializeField] private List<string> levelSceneNames = new List<string>();

    [Header("Behavior")]
    [SerializeField] private bool pauseGameOnDeath = true;

    [Header("Style")]
    [SerializeField] private Color overlayColor = new Color(0.008f, 0.006f, 0.018f, 0.94f);
    [SerializeField] private Color panelColor = new Color32(17, 16, 31, 255);
    [SerializeField] private Color accentColor = new Color32(255, 65, 105, 255);
    [SerializeField] private Color buttonColor = new Color32(44, 42, 70, 255);
    [SerializeField] private Color buttonHoverColor = new Color32(68, 55, 100, 255);

    private GameObject canvasRoot;
    private GameObject overlayRoot;
    private GameObject mainPanel;
    private GameObject levelPanel;
    private Text levelMessage;
    private Font uiFont;
    private GameObject topAccent;
    private GameObject bottomAccent;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float lastCanvasScaleFactor = -1f;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        EnsureEventSystem();
        BuildCanvasAndPanels();
        overlayRoot.SetActive(false);
        ApplyResponsiveLayout(true);
    }

    private void LateUpdate()
    {
        if (canvasRoot == null) return;

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight ||
            Mathf.Abs(scaleFactor - lastCanvasScaleFactor) > 0.001f)
        {
            ApplyResponsiveLayout(false);
        }
    }

    /// <summary>Scales the panels down as needed so the UI stays inside small windows and unusual aspect ratios.</summary>
    private void ApplyResponsiveLayout(bool force)
    {
        if (canvasRoot == null) return;

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        float scaleFactor = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
        if (!force && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight &&
            Mathf.Abs(scaleFactor - lastCanvasScaleFactor) <= 0.001f) return;

        // The panels are designed at 620 x 690 reference units. Scale them uniformly
        // only when the current window cannot fit them with a small safety margin.
        float availableWidth = Mathf.Max(1f, Screen.width - 40f);
        float availableHeight = Mathf.Max(1f, Screen.height - 40f);
        float fitScale = Mathf.Min(1f, availableWidth / (620f * scaleFactor),
                                  availableHeight / (690f * scaleFactor));
        fitScale = Mathf.Clamp(fitScale, 0.1f, 1f);

        if (mainPanel != null) mainPanel.transform.localScale = Vector3.one * fitScale;
        if (levelPanel != null) levelPanel.transform.localScale = Vector3.one * fitScale;
        if (topAccent != null) topAccent.transform.localScale = Vector3.one * fitScale;
        if (bottomAccent != null) bottomAccent.transform.localScale = Vector3.one * fitScale;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastCanvasScaleFactor = scaleFactor;
    }

    public void Show()
    {
        if (overlayRoot == null)
            BuildCanvasAndPanels();

        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
        overlayRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (pauseGameOnDeath) Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (pauseGameOnDeath) Time.timeScale = 1f;
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    public void RespawnPlayer()
    {
        Time.timeScale = 1f;
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (playerController != null)
            playerController.Respawn();
        else
            Debug.LogError("DeathScreenUI: No PlayerController is assigned or present in this scene.");

        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    public void OpenLevelSelector()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
        PopulateLevelButtons();
    }

    public void BackToDeathMenu()
    {
        levelPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            if (levelMessage != null)
                levelMessage.text = "Scene isn't in Build Settings / Build Profiles: " + sceneName;
            Debug.LogError("DeathScreenUI: Scene '" + sceneName + "' cannot be loaded. Add it to Build Settings / Build Profiles, or check its name.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildCanvasAndPanels()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        canvasRoot = new GameObject("DeathScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        overlayRoot = CreateRect("DeathOverlay", canvasRoot.transform, Vector2.zero, Vector2.zero, overlayColor);
        SetStretch(overlayRoot.GetComponent<RectTransform>());

        // Decorative accent bars behind the central cards.
        topAccent = CreateRect("TopGlitchAccent", overlayRoot.transform, new Vector2(600, 5), new Vector2(0, 330), accentColor);
        bottomAccent = CreateRect("BottomGlitchAccent", overlayRoot.transform, new Vector2(370, 3), new Vector2(0, -326), new Color(0.25f, 0.8f, 1f, 0.9f));

        mainPanel = CreateRect("DeathMainPanel", overlayRoot.transform, new Vector2(620, 690), Vector2.zero, panelColor);
        AddBorder(mainPanel.transform, new Color(1f, 1f, 1f, 0.08f));
        CreateText("SystemLabel", mainPanel.transform, "GLITCHVERSE   //   SIMULATION ERROR", 16, new Color(0.58f, 0.88f, 1f), new Vector2(0, 286), new Vector2(530, 30), FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateText("DeathTitle", mainPanel.transform, "YOU DIED", 58, Color.white, new Vector2(0, 213), new Vector2(540, 80), FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateText("DeathSubtitle", mainPanel.transform, "Reality has rejected your current timeline.", 20, new Color(0.93f, 0.93f, 1f), new Vector2(0, 155), new Vector2(530, 36), FontStyle.Normal, TextAnchor.MiddleCenter);

        CreateButton("RespawnButton", mainPanel.transform, "↻   RESPAWN", new Vector2(0, 65), new Vector2(430, 62), accentColor, new Color32(255, 85, 120, 255), RespawnPlayer, 22);
        CreateButton("LevelSelectorButton", mainPanel.transform, "▦   LEVEL SELECTOR", new Vector2(0, -18), new Vector2(430, 58), buttonColor, buttonHoverColor, OpenLevelSelector, 20);
        CreateButton("QuitButton", mainPanel.transform, "×   QUIT GAME", new Vector2(0, -96), new Vector2(430, 58), new Color32(35, 33, 49, 255), new Color32(85, 45, 70, 255), QuitGame, 20);
        CreateText("RetryHint", mainPanel.transform, "YOUR RUN ENDS HERE. YOUR PROGRESS DOESN'T.", 14, new Color(0.78f, 0.80f, 0.90f), new Vector2(0, -245), new Vector2(510, 26), FontStyle.Normal, TextAnchor.MiddleCenter);

        BuildLevelSelectorPanel();
    }

    private void BuildLevelSelectorPanel()
    {
        levelPanel = CreateRect("LevelSelectorPanel", overlayRoot.transform, new Vector2(620, 690), Vector2.zero, panelColor);
        AddBorder(levelPanel.transform, new Color(1f, 1f, 1f, 0.08f));
        CreateText("LevelSelectorTitle", levelPanel.transform, "CHOOSE YOUR LEVEL", 38, Color.white, new Vector2(0, 260), new Vector2(540, 62), FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateText("LevelSelectorSubtitle", levelPanel.transform, "Jump back into the simulation.", 20, new Color(0.93f, 0.93f, 1f), new Vector2(0, 210), new Vector2(520, 32), FontStyle.Normal, TextAnchor.MiddleCenter);
        levelMessage = CreateText("LevelSelectorMessage", levelPanel.transform, "", 14, new Color(1f, 0.67f, 0.73f), new Vector2(0, -226), new Vector2(520, 28), FontStyle.Normal, TextAnchor.MiddleCenter);
        CreateButton("BackButton", levelPanel.transform, "←   BACK", new Vector2(0, -278), new Vector2(430, 52), buttonColor, buttonHoverColor, BackToDeathMenu, 18);
        PopulateLevelButtons();
    }

    private void PopulateLevelButtons()
    {
        if (levelPanel == null) return;

        Transform list = levelPanel.transform.Find("LevelButtonList");
        if (list != null) Destroy(list.gameObject);
        levelMessage.text = "";

        GameObject listObject = new GameObject("LevelButtonList", typeof(RectTransform));
        listObject.transform.SetParent(levelPanel.transform, false);
        RectTransform listRect = listObject.GetComponent<RectTransform>();
        listRect.anchorMin = listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.pivot = new Vector2(0.5f, 0.5f);
        listRect.anchoredPosition = new Vector2(0, -10);
        listRect.sizeDelta = new Vector2(470, 365);

        List<string> scenes = GetLevelNames();
        if (scenes.Count == 0)
        {
            levelMessage.text = "Add your levels to Build Settings / Build Profiles.";
            return;
        }

        int visibleCount = Mathf.Min(scenes.Count, 6);
        for (int i = 0; i < visibleCount; i++)
        {
            string sceneName = scenes[i];
            bool isCurrent = sceneName == SceneManager.GetActiveScene().name;
            string label = string.Format("{0:00}   {1}{2}", i + 1, sceneName, isCurrent ? "   [CURRENT]" : "");
            float y = 142f - i * 55f;
            CreateButton("LevelButton_" + i, listObject.transform, label, new Vector2(0, y), new Vector2(430, 46), isCurrent ? new Color32(65, 54, 91, 255) : buttonColor, buttonHoverColor, () => LoadLevel(sceneName), 17);
        }

        if (scenes.Count > visibleCount)
            levelMessage.text = "Showing first 6 scenes. Add specific names in the Inspector if needed.";
    }

    private List<string> GetLevelNames()
    {
        List<string> result = new List<string>();
        foreach (string scene in levelSceneNames)
        {
            if (!string.IsNullOrWhiteSpace(scene) && !result.Contains(scene.Trim()))
                result.Add(scene.Trim());
        }

        if (result.Count > 0) return result;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(name) && !result.Contains(name))
                result.Add(name);
        }
        return result;
    }

    private GameObject CreateRect(string objectName, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private Text CreateText(string objectName, Transform parent, string content, int fontSize, Color color, Vector2 position, Vector2 size, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        // Dark shadow improves readability over the dim overlay and on varied displays.
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        shadow.useGraphicAlpha = true;

        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size, Color normalColor, Color highlightedColor, UnityEngine.Events.UnityAction action, int fontSize)
    {
        GameObject go = CreateRect(objectName, parent, size, position, normalColor);
        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = accentColor;
        colors.selectedColor = highlightedColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = go.GetComponent<Image>();
        button.onClick.AddListener(action);

        CreateText(objectName + "Label", go.transform, label, fontSize, Color.white, Vector2.zero, size - new Vector2(24, 8), FontStyle.Bold, TextAnchor.MiddleCenter);
        return button;
    }

    private void AddBorder(Transform parent, Color color)
    {
        GameObject border = CreateRect("PanelBorder", parent, new Vector2(620, 2), new Vector2(0, 342), color);
        border.transform.SetAsLastSibling();
    }

    private void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
