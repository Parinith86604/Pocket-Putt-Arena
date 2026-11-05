using UnityEngine;
using UnityEngine.UI;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Professional, responsive Canvas-based gameplay UI for Pocket Putt Arena.
/// Inspired by Golf Battle (Reference Images 1, 2, and 3):
/// 1. Top-center stroke & par counter ("STROKES: 0 • PAR 2")
/// 2. Top-left vibrant casual blue rounded pause button ("||")
/// 3. Top-right player rank badge ("2nd ▼")
/// 4. Left-side collapsible course selector & controls panel (access to all 5 courses)
/// 5. Camera zoom (+/-) and reset buttons
/// 6. Hole completion victory card with "NEXT COURSE" and "REPLAY" buttons
/// Fully responsive on Desktop (Laptop Touchpad / Mouse) and Mobile (Touchscreen) browsers.
/// </summary>
public class GolfHUDController : MonoBehaviour
{
    public static GolfHUDController Instance { get; private set; }

    [Header("Controllers")]
    [SerializeField] private GolfBallController ballController;
    [SerializeField] private HoleCompletionController holeController;
    [SerializeField] private SmoothCameraFollow cameraFollow;

    [Header("HUD Canvas & Scaler")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private CanvasScaler canvasScaler;

    [Header("Gameplay HUD References")]
    [SerializeField] private Text strokeCountText;
    [SerializeField] private Text rankBadgeText;
    [SerializeField] private GameObject powerMeterContainer;
    [SerializeField] private Image powerMeterFill;
    [SerializeField] private Text powerPercentText;
    [SerializeField] private GameObject aimGuidanceText;

    [Header("Controls Panel & Buttons")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private Button controlsToggleBtn;
    [SerializeField] private Button zoomInBtn;
    [SerializeField] private Button zoomOutBtn;
    [SerializeField] private Button resetCamBtn;

    [Header("Completion Panel References")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Text completionTitleText;
    [SerializeField] private Text completionScoreText;
    [SerializeField] private Button nextCourseButton;
    [SerializeField] private Button replayButton;

    [Header("Visual Theme")]
    [SerializeField] private Color lowPowerColor = new Color(0.2f, 0.95f, 0.3f, 1f);   // Bright Green
    [SerializeField] private Color midPowerColor = new Color(1f, 0.85f, 0.1f, 1f);    // Amber Gold
    [SerializeField] private Color highPowerColor = new Color(1f, 0.25f, 0.15f, 1f);  // Coral Red

    private bool isControlsPanelOpen = false;
    private int currentStrokes = 0;
    private int currentPar = 2;
    private string currentCourseName = "Evergreen Valley";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        FindControllers();

        if (hudCanvas == null)
        {
            BuildDefaultCanvas();
        }

        // Initialize state
        if (powerMeterContainer != null) powerMeterContainer.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);
        if (aimGuidanceText != null) aimGuidanceText.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        UpdateStrokeText(0);
    }

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void FindControllers()
    {
        if (ballController == null) ballController = FindFirstObjectByType<GolfBallController>();
        if (holeController == null) holeController = FindFirstObjectByType<HoleCompletionController>();
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();
    }

    private void SubscribeEvents()
    {
        if (ballController != null)
        {
            ballController.OnStrokeTaken += HandleStrokeTaken;
            ballController.OnAimStarted += HandleAimStarted;
            ballController.OnAimPowerChanged += HandleAimPowerChanged;
            ballController.OnAimEnded += HandleAimEnded;
        }

        if (holeController != null)
        {
            holeController.OnLevelCompleted += HandleLevelCompleted;
            holeController.OnHoleReplayed += HandleHoleReplayed;
        }

        if (replayButton != null) replayButton.onClick.AddListener(OnReplayButtonClicked);
        if (nextCourseButton != null) nextCourseButton.onClick.AddListener(OnNextCourseButtonClicked);
        if (controlsToggleBtn != null) controlsToggleBtn.onClick.AddListener(ToggleControlsPanel);
        if (zoomInBtn != null) zoomInBtn.onClick.AddListener(OnZoomInClicked);
        if (zoomOutBtn != null) zoomOutBtn.onClick.AddListener(OnZoomOutClicked);
        if (resetCamBtn != null) resetCamBtn.onClick.AddListener(OnResetCamClicked);
    }

    private void UnsubscribeEvents()
    {
        if (ballController != null)
        {
            ballController.OnStrokeTaken -= HandleStrokeTaken;
            ballController.OnAimStarted -= HandleAimStarted;
            ballController.OnAimPowerChanged -= HandleAimPowerChanged;
            ballController.OnAimEnded -= HandleAimEnded;
        }

        if (holeController != null)
        {
            holeController.OnLevelCompleted -= HandleLevelCompleted;
            holeController.OnHoleReplayed -= HandleHoleReplayed;
        }

        if (replayButton != null) replayButton.onClick.RemoveListener(OnReplayButtonClicked);
        if (nextCourseButton != null) nextCourseButton.onClick.RemoveListener(OnNextCourseButtonClicked);
        if (controlsToggleBtn != null) controlsToggleBtn.onClick.RemoveListener(ToggleControlsPanel);
        if (zoomInBtn != null) zoomInBtn.onClick.RemoveListener(OnZoomInClicked);
        if (zoomOutBtn != null) zoomOutBtn.onClick.RemoveListener(OnZoomOutClicked);
        if (resetCamBtn != null) resetCamBtn.onClick.RemoveListener(OnResetCamClicked);
    }

    #region Public Course API

    public void UpdateCourseHeader(string courseName, int parScore)
    {
        currentCourseName = courseName;
        currentPar = parScore;
        currentStrokes = 0;
        UpdateStrokeText(0);

        if (completionPanel != null) completionPanel.SetActive(false);
    }

    public void UpdateStrokeText(int strokes)
    {
        currentStrokes = strokes;
        if (strokeCountText != null)
        {
            strokeCountText.text = $"STROKES: {strokes}   •   PAR {currentPar}";
        }
    }

    #endregion

    #region Event Callbacks

    private void HandleStrokeTaken(int totalStrokes)
    {
        UpdateStrokeText(totalStrokes);
        if (aimGuidanceText != null) aimGuidanceText.SetActive(false);
    }

    private void HandleAimStarted()
    {
        if (powerMeterContainer != null) powerMeterContainer.SetActive(true);
        if (aimGuidanceText != null) aimGuidanceText.SetActive(false);
        UpdatePowerVisual(0f);
    }

    private void HandleAimPowerChanged(float powerRatio)
    {
        UpdatePowerVisual(powerRatio);
    }

    private void HandleAimEnded()
    {
        if (powerMeterContainer != null) powerMeterContainer.SetActive(false);
        if (aimGuidanceText != null) aimGuidanceText.SetActive(true);
    }

    public void HideCompletionPanel()
    {
        if (completionPanel != null) completionPanel.SetActive(false);
    }

    private void HandleLevelCompleted(int finalScore)
    {
        if (finalScore < 1)
        {
            Debug.LogWarning($"[GolfHUDController] Blocked invalid completion UI with {finalScore} strokes.");
            return;
        }

        if (aimGuidanceText != null) aimGuidanceText.SetActive(false);
        if (powerMeterContainer != null) powerMeterContainer.SetActive(false);

        // Update rank badge on completion (Reference Image 2 shows 6th ▲ or 1st ▲)
        if (rankBadgeText != null)
        {
            rankBadgeText.text = finalScore <= currentPar ? "1st" : "2nd";
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);

            if (completionTitleText != null)
            {
                int diff = finalScore - currentPar;
                if (finalScore == 1) completionTitleText.text = "HOLE IN ONE!";
                else if (diff <= -2) completionTitleText.text = "EAGLE!";
                else if (diff == -1) completionTitleText.text = "BIRDIE!";
                else if (diff == 0) completionTitleText.text = "PAR!";
                else completionTitleText.text = "HOLE COMPLETED!";
            }

            if (completionScoreText != null)
            {
                completionScoreText.text = $"FINAL STROKES: {finalScore}   (PAR {currentPar})";
            }
        }
    }

    private void HandleHoleReplayed()
    {
        if (completionPanel != null) completionPanel.SetActive(false);
        if (aimGuidanceText != null) aimGuidanceText.SetActive(true);
        UpdateStrokeText(0);

        if (rankBadgeText != null) rankBadgeText.text = "2nd";
    }

    private void OnReplayButtonClicked()
    {
        if (completionPanel != null) completionPanel.SetActive(false);
        if (CourseManager.Instance != null)
        {
            CourseManager.Instance.ReplayCurrentCourse();
        }
        else if (holeController != null)
        {
            holeController.ReplayHole();
        }
    }

    private void OnNextCourseButtonClicked()
    {
        if (completionPanel != null) completionPanel.SetActive(false);
        if (CourseManager.Instance != null)
        {
            CourseManager.Instance.LoadNextCourse();
        }
        else if (holeController != null)
        {
            holeController.ReplayHole();
        }
    }

    private void ToggleControlsPanel()
    {
        isControlsPanelOpen = !isControlsPanelOpen;
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(isControlsPanelOpen);
        }
    }

    private void OnZoomInClicked()
    {
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();
        if (cameraFollow != null) cameraFollow.ZoomIn();
    }

    private void OnZoomOutClicked()
    {
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();
        if (cameraFollow != null) cameraFollow.ZoomOut();
    }

    private void OnResetCamClicked()
    {
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();
        if (cameraFollow != null) cameraFollow.ResetToInitialPosition();
    }

    private void UpdatePowerVisual(float powerRatio)
    {
        powerRatio = Mathf.Clamp01(powerRatio);

        if (powerMeterFill != null)
        {
            powerMeterFill.fillAmount = powerRatio;
            Color powerColor = powerRatio < 0.5f
                ? Color.Lerp(lowPowerColor, midPowerColor, powerRatio * 2f)
                : Color.Lerp(midPowerColor, highPowerColor, (powerRatio - 0.5f) * 2f);
            powerMeterFill.color = powerColor;
        }

        if (powerPercentText != null)
        {
            powerPercentText.text = $"{Mathf.RoundToInt(powerRatio * 100f)}%";
        }
    }

    #endregion

    #region Dynamic UI Builder

    private void BuildDefaultCanvas()
    {
        GameObject canvasObj = new GameObject("GameplayHUD_Canvas");
        canvasObj.transform.SetParent(transform, false);

        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;

        canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (defaultFont == null)
        {
            defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        // 1. Top Stroke Counter & Par Badge
        BuildStrokeBadge(canvasObj, defaultFont);

        // 2. Power Meter
        BuildPowerMeter(canvasObj, defaultFont);

        // 3. Aim Guidance Text
        BuildAimGuidance(canvasObj, defaultFont);

        // 4. Left-Side Controls & Course Select Panel with Pause Button (Reference Images 1 & 2)
        BuildControlsPanel(canvasObj, defaultFont);

        // 5. Camera Controls (+ / - / Reset)
        BuildCameraControls(canvasObj, defaultFont);

        // 6. Level Completion Victory Card
        BuildCompletionPanel(canvasObj, defaultFont);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
    }

    private void BuildStrokeBadge(GameObject canvasObj, Font font)
    {
        GameObject strokeBadge = CreateUIObject("StrokeBadge", canvasObj.transform);
        RectTransform badgeRT = strokeBadge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.5f, 1f);
        badgeRT.anchorMax = new Vector2(0.5f, 1f);
        badgeRT.pivot = new Vector2(0.5f, 1f);
        badgeRT.anchoredPosition = new Vector2(0f, -24f);
        badgeRT.sizeDelta = new Vector2(360f, 54f);

        Image badgeBg = strokeBadge.AddComponent<Image>();
        badgeBg.color = new Color(0.06f, 0.10f, 0.18f, 0.88f);
        badgeBg.raycastTarget = false;

        GameObject strokeTextObj = CreateUIObject("StrokeText", strokeBadge.transform);
        RectTransform strokeTextRT = strokeTextObj.GetComponent<RectTransform>();
        strokeTextRT.anchorMin = Vector2.zero;
        strokeTextRT.anchorMax = Vector2.one;
        strokeTextRT.sizeDelta = Vector2.zero;

        strokeCountText = strokeTextObj.AddComponent<Text>();
        strokeCountText.font = font;
        strokeCountText.fontSize = 22;
        strokeCountText.fontStyle = FontStyle.Bold;
        strokeCountText.alignment = TextAnchor.MiddleCenter;
        strokeCountText.color = new Color(1f, 0.95f, 0.9f);
        strokeCountText.raycastTarget = false;
        strokeCountText.text = "STROKES: 0   •   PAR 2";

        // Build Top-Right Player Rank Badge (Reference Images 1, 2, 3)
        BuildRankBadge(canvasObj, font);
    }

    private void BuildRankBadge(GameObject canvasObj, Font font)
    {
        GameObject rankObj = CreateUIObject("PlayerRankBadge", canvasObj.transform);
        RectTransform rankRT = rankObj.GetComponent<RectTransform>();
        rankRT.anchorMin = new Vector2(1f, 1f);
        rankRT.anchorMax = new Vector2(1f, 1f);
        rankRT.pivot = new Vector2(1f, 1f);
        rankRT.anchoredPosition = new Vector2(-24f, -24f);
        rankRT.sizeDelta = new Vector2(150f, 52f);

        Image rankBg = rankObj.AddComponent<Image>();
        rankBg.color = new Color(0.08f, 0.28f, 0.58f, 0.88f); // Translucent navy blue
        rankBg.raycastTarget = false;

        // Rank Text (e.g. 2nd)
        GameObject textObj = CreateUIObject("RankText", rankObj.transform);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(0.65f, 1f);
        textRT.sizeDelta = Vector2.zero;

        rankBadgeText = textObj.AddComponent<Text>();
        rankBadgeText.font = font;
        rankBadgeText.fontSize = 26;
        rankBadgeText.fontStyle = FontStyle.Bold;
        rankBadgeText.alignment = TextAnchor.MiddleCenter;
        rankBadgeText.color = Color.white;
        rankBadgeText.raycastTarget = false;
        rankBadgeText.text = "2nd";

        // Status Triangle Button
        GameObject triObj = CreateUIObject("StatusTriangle", rankObj.transform);
        RectTransform triRT = triObj.GetComponent<RectTransform>();
        triRT.anchorMin = new Vector2(0.65f, 0.15f);
        triRT.anchorMax = new Vector2(0.92f, 0.85f);
        triRT.sizeDelta = Vector2.zero;

        Image triImg = triObj.AddComponent<Image>();
        triImg.color = new Color(0.12f, 0.55f, 0.95f, 1f); // Bright blue
        triImg.raycastTarget = false;

        GameObject triLabelObj = CreateUIObject("TriLabel", triObj.transform);
        RectTransform triLabelRT = triLabelObj.GetComponent<RectTransform>();
        triLabelRT.anchorMin = Vector2.zero;
        triLabelRT.anchorMax = Vector2.one;
        triLabelRT.sizeDelta = Vector2.zero;

        Text triLabel = triLabelObj.AddComponent<Text>();
        triLabel.font = font;
        triLabel.fontSize = 18;
        triLabel.fontStyle = FontStyle.Bold;
        triLabel.alignment = TextAnchor.MiddleCenter;
        triLabel.color = Color.white;
        triLabel.raycastTarget = false;
        triLabel.text = "v";
    }

    private void BuildPowerMeter(GameObject canvasObj, Font font)
    {
        GameObject meterContainer = CreateUIObject("PowerMeterContainer", canvasObj.transform);
        RectTransform meterRT = meterContainer.GetComponent<RectTransform>();
        meterRT.anchorMin = new Vector2(0.5f, 0f);
        meterRT.anchorMax = new Vector2(0.5f, 0f);
        meterRT.pivot = new Vector2(0.5f, 0f);
        meterRT.anchoredPosition = new Vector2(0f, 60f);
        meterRT.sizeDelta = new Vector2(400f, 24f);
        powerMeterContainer = meterContainer;

        GameObject bgObj = CreateUIObject("PowerMeter_BG", meterContainer.transform);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.85f);
        bgImg.raycastTarget = false;

        GameObject fillObj = CreateUIObject("PowerMeter_Fill", meterContainer.transform);
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        powerMeterFill = fillObj.AddComponent<Image>();
        powerMeterFill.type = Image.Type.Filled;
        powerMeterFill.fillMethod = Image.FillMethod.Horizontal;
        powerMeterFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        powerMeterFill.fillAmount = 0f;
        powerMeterFill.color = lowPowerColor;
        powerMeterFill.raycastTarget = false;

        GameObject textObj = CreateUIObject("PowerPercentText", meterContainer.transform);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 1f);
        textRT.anchorMax = new Vector2(0.5f, 1f);
        textRT.pivot = new Vector2(0.5f, 0f);
        textRT.anchoredPosition = new Vector2(0f, 6f);
        textRT.sizeDelta = new Vector2(100f, 26f);

        powerPercentText = textObj.AddComponent<Text>();
        powerPercentText.font = font;
        powerPercentText.fontSize = 18;
        powerPercentText.fontStyle = FontStyle.Bold;
        powerPercentText.alignment = TextAnchor.MiddleCenter;
        powerPercentText.color = Color.white;
        powerPercentText.raycastTarget = false;
        powerPercentText.text = "0%";
    }

    private void BuildAimGuidance(GameObject canvasObj, Font font)
    {
        GameObject guideObj = CreateUIObject("AimGuidanceText", canvasObj.transform);
        RectTransform guideRT = guideObj.GetComponent<RectTransform>();
        guideRT.anchorMin = new Vector2(0.5f, 0f);
        guideRT.anchorMax = new Vector2(0.5f, 0f);
        guideRT.pivot = new Vector2(0.5f, 0f);
        guideRT.anchoredPosition = new Vector2(0f, 110f);
        guideRT.sizeDelta = new Vector2(500f, 32f);
        aimGuidanceText = guideObj;

        Text guideText = guideObj.AddComponent<Text>();
        guideText.font = font;
        guideText.fontSize = 16;
        guideText.fontStyle = FontStyle.Bold;
        guideText.alignment = TextAnchor.MiddleCenter;
        guideText.color = new Color(1f, 1f, 1f, 0.85f);
        guideText.raycastTarget = false;
        guideText.text = "CLICK BALL & DRAG BACK TO AIM  •  RELEASE TO SHOOT";
    }

    private void BuildControlsPanel(GameObject canvasObj, Font font)
    {
        // 1. Top-Left Vibrant Blue Pause Button (Reference Images 1, 2, 3)
        GameObject toggleObj = CreateUIObject("ControlsToggleButton", canvasObj.transform);
        RectTransform toggleRT = toggleObj.GetComponent<RectTransform>();
        toggleRT.anchorMin = new Vector2(0f, 1f);
        toggleRT.anchorMax = new Vector2(0f, 1f);
        toggleRT.pivot = new Vector2(0f, 1f);
        toggleRT.anchoredPosition = new Vector2(24f, -24f);
        toggleRT.sizeDelta = new Vector2(54f, 52f);

        Image toggleImg = toggleObj.AddComponent<Image>();
        toggleImg.color = new Color(0.12f, 0.55f, 0.95f, 1.0f);
        controlsToggleBtn = toggleObj.AddComponent<Button>();

        // Crisp double vertical bars for pause icon "||"
        for (int b = -1; b <= 1; b += 2)
        {
            GameObject bar = CreateUIObject($"PauseBar_{(b > 0 ? "2" : "1")}", toggleObj.transform);
            RectTransform barRT = bar.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.5f, 0.5f);
            barRT.anchorMax = new Vector2(0.5f, 0.5f);
            barRT.anchoredPosition = new Vector2(b * 6f, 0f);
            barRT.sizeDelta = new Vector2(5f, 22f);

            Image barImg = bar.AddComponent<Image>();
            barImg.color = Color.white;
            barImg.raycastTarget = false;
        }

        // 2. Collapsible Left Panel Container
        GameObject panelObj = CreateUIObject("LeftControlsPanel", canvasObj.transform);
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0.5f);
        panelRT.anchorMax = new Vector2(0f, 0.5f);
        panelRT.pivot = new Vector2(0f, 0.5f);
        panelRT.anchoredPosition = new Vector2(24f, -10f);
        panelRT.sizeDelta = new Vector2(360f, 500f);
        controlsPanel = panelObj;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.10f, 0.16f, 0.96f);
        panelBg.raycastTarget = true;

        // Panel Title
        GameObject headerObj = CreateUIObject("PanelHeader", panelObj.transform);
        RectTransform headerRT = headerObj.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.5f, 1f);
        headerRT.anchorMax = new Vector2(0.5f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0f, -16f);
        headerRT.sizeDelta = new Vector2(320f, 36f);

        Text headerText = headerObj.AddComponent<Text>();
        headerText.font = font;
        headerText.fontSize = 20;
        headerText.fontStyle = FontStyle.Bold;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(0.2f, 0.85f, 1f);
        headerText.raycastTarget = false;
        headerText.text = "SELECT COURSE";

        // Five Course Selection Buttons
        string[] courses = new string[]
        {
            "1. Evergreen Valley (Par 2)",
            "2. Copper Canyon (Par 3)",
            "3. Frostlight Ridge (Par 3)",
            "4. Jade Temple Garden (Par 2)",
            "5. Azure Coast (Par 3)"
        };

        for (int i = 0; i < courses.Length; i++)
        {
            int courseIndex = i + 1;
            float yPos = -64f - (i * 48f);

            GameObject cBtnObj = CreateUIObject($"CourseBtn_{courseIndex}", panelObj.transform);
            RectTransform cBtnRT = cBtnObj.GetComponent<RectTransform>();
            cBtnRT.anchorMin = new Vector2(0.5f, 1f);
            cBtnRT.anchorMax = new Vector2(0.5f, 1f);
            cBtnRT.pivot = new Vector2(0.5f, 1f);
            cBtnRT.anchoredPosition = new Vector2(0f, yPos);
            cBtnRT.sizeDelta = new Vector2(320f, 40f);

            Image cBtnImg = cBtnObj.AddComponent<Image>();
            cBtnImg.color = new Color(0.12f, 0.28f, 0.45f, 0.9f);

            Button cBtn = cBtnObj.AddComponent<Button>();
            var colors = cBtn.colors;
            colors.normalColor = new Color(0.12f, 0.28f, 0.45f, 0.9f);
            colors.highlightedColor = new Color(0.18f, 0.45f, 0.72f, 1f);
            colors.pressedColor = new Color(0.10f, 0.60f, 0.95f, 1f);
            cBtn.colors = colors;

            cBtn.onClick.AddListener(() =>
            {
                HideCompletionPanel();
                if (CourseManager.Instance != null)
                {
                    CourseManager.Instance.LoadCourse(courseIndex);
                }
                ToggleControlsPanel();
            });

            GameObject lblObj = CreateUIObject("Label", cBtnObj.transform);
            RectTransform lblRT = lblObj.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.sizeDelta = Vector2.zero;

            Text lbl = lblObj.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 15;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = Color.white;
            lbl.raycastTarget = false;
            lbl.text = courses[i];
        }

        // Instructions Footer
        GameObject instrObj = CreateUIObject("ControlsInstructions", panelObj.transform);
        RectTransform instrRT = instrObj.GetComponent<RectTransform>();
        instrRT.anchorMin = new Vector2(0.5f, 0f);
        instrRT.anchorMax = new Vector2(0.5f, 0f);
        instrRT.pivot = new Vector2(0.5f, 0f);
        instrRT.anchoredPosition = new Vector2(0f, 16f);
        instrRT.sizeDelta = new Vector2(320f, 160f);

        Text instrText = instrObj.AddComponent<Text>();
        instrText.font = font;
        instrText.fontSize = 13;
        instrText.alignment = TextAnchor.UpperLeft;
        instrText.color = new Color(0.85f, 0.88f, 0.92f);
        instrText.raycastTarget = false;
        instrText.lineSpacing = 1.25f;
        instrText.text =
            "TOUCHPAD / DESKTOP CONTROLS:\n" +
            "• Aim & Shoot: Click ball & drag back\n" +
            "• Orbit Camera: Click & drag empty course\n" +
            "• Zoom Camera: 2-finger scroll or +/- buttons\n\n" +
            "MOBILE TOUCHSCREEN:\n" +
            "• Drag ball to shoot / swipe screen to orbit";
    }

    private void BuildCameraControls(GameObject canvasObj, Font font)
    {
        GameObject camControlsContainer = CreateUIObject("CameraControlsContainer", canvasObj.transform);
        RectTransform containerRT = camControlsContainer.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(1f, 0f);
        containerRT.anchorMax = new Vector2(1f, 0f);
        containerRT.pivot = new Vector2(1f, 0f);
        containerRT.anchoredPosition = new Vector2(-24f, 24f);
        containerRT.sizeDelta = new Vector2(160f, 110f);

        zoomInBtn = CreateIconButton(camControlsContainer.transform, "ZoomInBtn", new Vector2(-88f, 56f), new Vector2(64f, 48f), "+", font, 28);
        zoomInBtn.onClick.AddListener(OnZoomInClicked);

        zoomOutBtn = CreateIconButton(camControlsContainer.transform, "ZoomOutBtn", new Vector2(-16f, 56f), new Vector2(64f, 48f), "-", font, 28);
        zoomOutBtn.onClick.AddListener(OnZoomOutClicked);

        resetCamBtn = CreateIconButton(camControlsContainer.transform, "ResetCamBtn", new Vector2(-52f, 0f), new Vector2(136f, 44f), "RESET", font, 16);
        resetCamBtn.onClick.AddListener(OnResetCamClicked);
    }

    private Button CreateIconButton(Transform parent, string name, Vector2 pos, Vector2 size, string text, Font font, int fontSize)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 0f);
        btnRT.anchorMax = new Vector2(1f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = pos;
        btnRT.sizeDelta = size;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.10f, 0.22f, 0.12f, 0.90f);
        btnImg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.10f, 0.22f, 0.12f, 0.90f);
        colors.highlightedColor = new Color(0.16f, 0.38f, 0.20f, 1f);
        colors.pressedColor = new Color(0.22f, 0.55f, 0.28f, 1f);
        btn.colors = colors;

        GameObject textObj = CreateUIObject("BtnLabel", btnObj.transform);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        Text lbl = textObj.AddComponent<Text>();
        lbl.font = font;
        lbl.fontSize = fontSize;
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = new Color(0.95f, 0.98f, 1f);
        lbl.raycastTarget = false;
        lbl.text = text;

        return btn;
    }

    private void BuildCompletionPanel(GameObject canvasObj, Font font)
    {
        GameObject panelObj = CreateUIObject("LevelCompletionPanel", canvasObj.transform);
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(460f, 320f);
        completionPanel = panelObj;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.10f, 0.18f, 0.95f);
        panelBg.raycastTarget = true;

        GameObject titleObj = CreateUIObject("TitleText", panelObj.transform);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -30f);
        titleRT.sizeDelta = new Vector2(420f, 44f);

        completionTitleText = titleObj.AddComponent<Text>();
        completionTitleText.font = font;
        completionTitleText.fontSize = 32;
        completionTitleText.fontStyle = FontStyle.Bold;
        completionTitleText.alignment = TextAnchor.MiddleCenter;
        completionTitleText.color = new Color(1f, 0.85f, 0.2f);
        completionTitleText.raycastTarget = false;
        completionTitleText.text = "HOLE COMPLETED!";

        GameObject scoreObj = CreateUIObject("ScoreText", panelObj.transform);
        RectTransform scoreRT = scoreObj.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRT.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRT.pivot = new Vector2(0.5f, 0.5f);
        scoreRT.anchoredPosition = new Vector2(0f, 20f);
        scoreRT.sizeDelta = new Vector2(400f, 40f);

        completionScoreText = scoreObj.AddComponent<Text>();
        completionScoreText.font = font;
        completionScoreText.fontSize = 24;
        completionScoreText.fontStyle = FontStyle.Bold;
        completionScoreText.alignment = TextAnchor.MiddleCenter;
        completionScoreText.color = Color.white;
        completionScoreText.raycastTarget = false;
        completionScoreText.text = "TOTAL STROKES: 1";

        // NEXT COURSE Button (Vibrant Golf Battle Blue)
        GameObject nextBtnObj = CreateUIObject("NextCourseButton", panelObj.transform);
        RectTransform nextBtnRT = nextBtnObj.GetComponent<RectTransform>();
        nextBtnRT.anchorMin = new Vector2(0.5f, 0f);
        nextBtnRT.anchorMax = new Vector2(0.5f, 0f);
        nextBtnRT.pivot = new Vector2(0.5f, 0f);
        nextBtnRT.anchoredPosition = new Vector2(105f, 35f);
        nextBtnRT.sizeDelta = new Vector2(190f, 56f);

        Image nextBtnImg = nextBtnObj.AddComponent<Image>();
        nextBtnImg.color = new Color(0.12f, 0.55f, 0.95f, 1f);
        nextBtnImg.raycastTarget = true;

        nextCourseButton = nextBtnObj.AddComponent<Button>();
        var nextColors = nextCourseButton.colors;
        nextColors.normalColor = new Color(0.12f, 0.55f, 0.95f, 1f);
        nextColors.highlightedColor = new Color(0.18f, 0.65f, 1f, 1f);
        nextColors.pressedColor = new Color(0.08f, 0.40f, 0.80f, 1f);
        nextCourseButton.colors = nextColors;

        GameObject nextTextObj = CreateUIObject("NextBtnText", nextBtnObj.transform);
        RectTransform nextTextRT = nextTextObj.GetComponent<RectTransform>();
        nextTextRT.anchorMin = Vector2.zero;
        nextTextRT.anchorMax = Vector2.one;
        nextTextRT.sizeDelta = Vector2.zero;

        Text nextLabel = nextTextObj.AddComponent<Text>();
        nextLabel.font = font;
        nextLabel.fontSize = 18;
        nextLabel.fontStyle = FontStyle.Bold;
        nextLabel.alignment = TextAnchor.MiddleCenter;
        nextLabel.color = Color.white;
        nextLabel.raycastTarget = false;
        nextLabel.text = "NEXT COURSE";

        // REPLAY Button (Rich Emerald Green)
        GameObject btnObj = CreateUIObject("ReplayButton", panelObj.transform);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(-105f, 35f);
        btnRT.sizeDelta = new Vector2(190f, 56f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.65f, 0.35f, 1f);
        btnImg.raycastTarget = true;

        replayButton = btnObj.AddComponent<Button>();
        var colors = replayButton.colors;
        colors.normalColor = new Color(0.12f, 0.65f, 0.35f, 1f);
        colors.highlightedColor = new Color(0.16f, 0.78f, 0.42f, 1f);
        colors.pressedColor = new Color(0.08f, 0.50f, 0.26f, 1f);
        replayButton.colors = colors;

        GameObject btnTextObj = CreateUIObject("BtnText", btnObj.transform);
        RectTransform btnTextRT = btnTextObj.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;

        Text btnLabel = btnTextObj.AddComponent<Text>();
        btnLabel.font = font;
        btnLabel.fontSize = 18;
        btnLabel.fontStyle = FontStyle.Bold;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color = Color.white;
        btnLabel.raycastTarget = false;
        btnLabel.text = "REPLAY HOLE";
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    #endregion
}
