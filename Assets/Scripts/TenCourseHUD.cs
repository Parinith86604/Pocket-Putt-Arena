using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the ten-course selector and camera controls.
/// Desktop keeps the course list visible. Portrait/narrow screens use
/// a compact COURSES button so the list never blocks the completion card.
/// Attach this component to the same GameObject as CourseManager.
/// </summary>
[DisallowMultipleComponent]
public class TenCourseHUD : MonoBehaviour
{
    private static readonly string[] CourseNames =
    {
        "01  Evergreen Valley",
        "02  Copper Canyon",
        "03  Frostlight Ridge",
        "04  Jade Temple Garden",
        "05  Azure Coast",
        "06  Volcanic Ember Run",
        "07  Moonlit Crater",
        "08  Redwood Rapids",
        "09  Sky Harbor",
        "10  Crystal Caverns"
    };

    [Header("Desktop Panel Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(300f, 560f);
    [SerializeField] private Vector2 panelOffset = new Vector2(18f, 0f);

    [Header("Compact Screen Layout")]
    [SerializeField] private float compactScreenWidth = 900f;
    [SerializeField] private float compactPanelMaxWidth = 380f;
    [SerializeField] private float compactPanelMaxHeight = 620f;
    [SerializeField] private Vector2 compactTogglePosition = new Vector2(18f, -76f);
    [SerializeField] private Vector2 compactToggleSize = new Vector2(136f, 42f);

    [Header("Camera Controls")]
    [SerializeField] private Vector2 controlsMargin = new Vector2(24f, 24f);
    [SerializeField] private Font uiFont;

    private Canvas canvas;
    private CourseManager courseManager;
    private SmoothCameraFollow cameraFollow;

    private GameObject selectorPanel;
    private GameObject closeButtonObject;
    private GameObject mobileCourseToggleObject;
    private RectTransform cameraControlsRect;

    private readonly List<Image> courseButtonImages = new List<Image>();
    private readonly List<Button> courseButtons = new List<Button>();

    private bool isCompactLayout;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Start()
    {
        courseManager = CourseManager.Instance;

        if (courseManager == null)
            courseManager = FindFirstObjectByType<CourseManager>();

        cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();
        canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "[TenCourseHUD] No Canvas found. Make sure GolfHUDController creates its Canvas first."
            );
            return;
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        HideLegacyCourseAndCameraPanels();
        BuildCourseSelector();
        BuildCameraButtons();
        BuildMobileCourseToggle();
        ApplyResponsiveLayout(true);
        RefreshSelectedCourse();
    }

    private void Update()
    {
        if (canvas == null || selectorPanel == null)
            return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            ApplyResponsiveLayout(true);

        RefreshSelectedCourse();

        // Never let the course list cover the completion card on a phone.
        if (isCompactLayout)
        {
            bool completionVisible = IsCompletionPanelVisible();

            if (completionVisible)
            {
                if (selectorPanel.activeSelf)
                    selectorPanel.SetActive(false);

                if (mobileCourseToggleObject != null &&
                    mobileCourseToggleObject.activeSelf)
                {
                    mobileCourseToggleObject.SetActive(false);
                }
            }
            else if (mobileCourseToggleObject != null &&
                     !mobileCourseToggleObject.activeSelf)
            {
                mobileCourseToggleObject.SetActive(true);
            }
        }
    }

    private void HideLegacyCourseAndCameraPanels()
    {
        string[] namesToHide =
        {
            "LeftControlsPanel",
            "ControlsToggleButton",
            "CameraControlsContainer"
        };

        Transform[] all = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform item in all)
        {
            if (item == null)
                continue;

            foreach (string targetName in namesToHide)
            {
                if (item.name == targetName)
                {
                    item.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    // ---------------------------------------------------------
    // TEN-COURSE SELECTOR
    // ---------------------------------------------------------

    private void BuildCourseSelector()
    {
        selectorPanel = CreateUIObject("TenCourseSelectorPanel", canvas.transform);

        RectTransform panelRT = selectorPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0.5f);
        panelRT.anchorMax = new Vector2(0f, 0.5f);
        panelRT.pivot = new Vector2(0f, 0.5f);
        panelRT.anchoredPosition = panelOffset;
        panelRT.sizeDelta = panelSize;

        Image panelImage = selectorPanel.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.075f, 0.12f, 0.96f);

        GameObject header = CreateUIObject("CourseSelectorHeader", selectorPanel.transform);
        SetRect(
            header.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -18f),
            new Vector2(260f, 32f)
        );

        CreateText(
            header,
            "SELECT COURSE",
            21,
            TextAnchor.MiddleCenter,
            new Color(0.25f, 0.88f, 1f)
        );

        GameObject subtitle = CreateUIObject("CourseSelectorSubtitle", selectorPanel.transform);
        SetRect(
            subtitle.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -48f),
            new Vector2(260f, 22f)
        );

        CreateText(
            subtitle,
            "10 COURSES  •  MEDIUM DIFFICULTY",
            12,
            TextAnchor.MiddleCenter,
            new Color(0.75f, 0.84f, 0.92f)
        );

        GameObject viewport = CreateUIObject("CourseListViewport", selectorPanel.transform);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = new Vector2(0f, 0f);
        viewportRT.anchorMax = new Vector2(1f, 1f);
        viewportRT.offsetMin = new Vector2(12f, 42f);
        viewportRT.offsetMax = new Vector2(-12f, -78f);

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.02f, 0.045f, 0.075f, 0.4f);

        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateUIObject("CourseListContent", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(7, 7, 7, 7);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = selectorPanel.AddComponent<ScrollRect>();
        scroll.viewport = viewportRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 30f;

        for (int i = 0; i < CourseNames.Length; i++)
        {
            int courseId = i + 1;

            GameObject buttonObject = CreateUIObject(
                "CourseButton_" + courseId,
                content.transform
            );

            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 42f;
            layoutElement.minHeight = 42f;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.10f, 0.22f, 0.34f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.10f, 0.22f, 0.34f, 1f);
            colors.highlightedColor = new Color(0.16f, 0.39f, 0.57f, 1f);
            colors.pressedColor = new Color(0.08f, 0.52f, 0.78f, 1f);
            colors.selectedColor = new Color(0.10f, 0.42f, 0.62f, 1f);
            button.colors = colors;

            CreateText(
                buttonObject,
                CourseNames[i],
                15,
                TextAnchor.MiddleLeft,
                Color.white,
                new Vector2(13f, 0f)
            );

            button.onClick.AddListener(() => SelectCourse(courseId));

            courseButtons.Add(button);
            courseButtonImages.Add(buttonImage);
        }

        GameObject footer = CreateUIObject("CourseSelectorFooter", selectorPanel.transform);
        SetRect(
            footer.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 7f),
            new Vector2(260f, 25f)
        );

        CreateText(
            footer,
            "SCROLL TO VIEW ALL 10 COURSES",
            12,
            TextAnchor.MiddleCenter,
            new Color(0.72f, 0.82f, 0.9f)
        );

        // Close control is only shown while the compact selector is open.
        closeButtonObject = CreateUIObject("CourseSelectorCloseButton", selectorPanel.transform);
        RectTransform closeRT = closeButtonObject.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-10f, -10f);
        closeRT.sizeDelta = new Vector2(34f, 34f);

        Image closeImage = closeButtonObject.AddComponent<Image>();
        closeImage.color = new Color(0.12f, 0.30f, 0.44f, 1f);

        Button closeButton = closeButtonObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(CloseCourseSelector);

        CreateText(
            closeButtonObject,
            "X",
            18,
            TextAnchor.MiddleCenter,
            Color.white
        );
    }

    private void BuildMobileCourseToggle()
    {
        mobileCourseToggleObject = CreateUIObject(
            "MobileCourseSelectorToggle",
            canvas.transform
        );

        RectTransform rt = mobileCourseToggleObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = compactTogglePosition;
        rt.sizeDelta = compactToggleSize;

        Image image = mobileCourseToggleObject.AddComponent<Image>();
        image.color = new Color(0.035f, 0.075f, 0.12f, 0.94f);

        Button button = mobileCourseToggleObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.035f, 0.075f, 0.12f, 0.94f);
        colors.highlightedColor = new Color(0.10f, 0.30f, 0.48f, 1f);
        colors.pressedColor = new Color(0.08f, 0.43f, 0.58f, 1f);
        button.colors = colors;

        CreateText(
            mobileCourseToggleObject,
            "COURSES  ▼",
            16,
            TextAnchor.MiddleCenter,
            new Color(0.25f, 0.88f, 1f)
        );

        button.onClick.AddListener(ToggleCourseSelector);
    }

    private void ToggleCourseSelector()
    {
        if (selectorPanel == null)
            return;

        if (selectorPanel.activeSelf)
        {
            CloseCourseSelector();
        }
        else if (!IsCompletionPanelVisible())
        {
            selectorPanel.SetActive(true);
            selectorPanel.transform.SetAsLastSibling();
        }
    }

    private void CloseCourseSelector()
    {
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
    }

    private void SelectCourse(int courseId)
    {
        if (courseManager == null)
            courseManager = CourseManager.Instance;

        if (courseManager == null)
        {
            Debug.LogError("[TenCourseHUD] CourseManager is not available.");
            return;
        }

        if (GolfHUDController.Instance != null)
            GolfHUDController.Instance.HideCompletionPanel();

        courseManager.LoadCourse(courseId);
        RefreshSelectedCourse();

        if (isCompactLayout)
            CloseCourseSelector();
    }

    private void RefreshSelectedCourse()
    {
        if (courseManager == null)
            courseManager = CourseManager.Instance;

        if (courseManager == null)
            return;

        int activeId = courseManager.ActiveCourseId;

        for (int i = 0; i < courseButtonImages.Count; i++)
        {
            if (courseButtonImages[i] == null)
                continue;

            courseButtonImages[i].color =
                (i + 1 == activeId)
                    ? new Color(0.08f, 0.43f, 0.58f, 1f)
                    : new Color(0.10f, 0.22f, 0.34f, 1f);
        }
    }

    // ---------------------------------------------------------
    // RESPONSIVE LAYOUT
    // ---------------------------------------------------------

    private void ApplyResponsiveLayout(bool resetCompactPanel)
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        isCompactLayout =
            Screen.height > Screen.width ||
            Screen.width < compactScreenWidth;

        RectTransform panelRT = selectorPanel.GetComponent<RectTransform>();

        if (isCompactLayout)
        {
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float logicalWidth = Screen.width / scale;
            float logicalHeight = Screen.height / scale;

            float width = Mathf.Max(
                240f,
                Mathf.Min(compactPanelMaxWidth, logicalWidth - 28f)
            );

            float height = Mathf.Max(
                300f,
                Mathf.Min(compactPanelMaxHeight, logicalHeight - 80f)
            );

            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(width, height);

            if (resetCompactPanel)
                selectorPanel.SetActive(false);

            if (mobileCourseToggleObject != null)
                mobileCourseToggleObject.SetActive(!IsCompletionPanelVisible());

            if (closeButtonObject != null)
                closeButtonObject.SetActive(true);
        }
        else
        {
            panelRT.anchorMin = new Vector2(0f, 0.5f);
            panelRT.anchorMax = new Vector2(0f, 0.5f);
            panelRT.pivot = new Vector2(0f, 0.5f);
            panelRT.anchoredPosition = panelOffset;
            panelRT.sizeDelta = panelSize;
            selectorPanel.SetActive(true);

            if (mobileCourseToggleObject != null)
                mobileCourseToggleObject.SetActive(false);

            if (closeButtonObject != null)
                closeButtonObject.SetActive(false);
        }

        ApplyCameraControlsLayout();
    }

    private bool IsCompletionPanelVisible()
    {
        // GameObject.Find returns the panel only while it is active.
        return GameObject.Find("LevelCompletionPanel") != null;
    }

    // ---------------------------------------------------------
    // CAMERA BUTTONS
    // ---------------------------------------------------------

    private void BuildCameraButtons()
    {
        GameObject container = CreateUIObject(
            "TenCourseCameraControls",
            canvas.transform
        );

        cameraControlsRect = container.GetComponent<RectTransform>();
        cameraControlsRect.anchorMin = new Vector2(1f, 0f);
        cameraControlsRect.anchorMax = new Vector2(1f, 0f);
        cameraControlsRect.pivot = new Vector2(1f, 0f);
        cameraControlsRect.anchoredPosition = new Vector2(-controlsMargin.x, controlsMargin.y);
        cameraControlsRect.sizeDelta = new Vector2(232f, 58f);

        Image background = container.AddComponent<Image>();
        background.color = new Color(0.035f, 0.075f, 0.12f, 0.88f);

        Button plus = CreateCameraButton(
            container.transform,
            "ZoomInButton",
            new Vector2(8f, 5f),
            new Vector2(48f, 48f),
            "+"
        );

        Button minus = CreateCameraButton(
            container.transform,
            "ZoomOutButton",
            new Vector2(64f, 5f),
            new Vector2(48f, 48f),
            "−"
        );

        Button reset = CreateCameraButton(
            container.transform,
            "ResetCameraButton",
            new Vector2(120f, 5f),
            new Vector2(106f, 48f),
            "RESET"
        );

        plus.onClick.AddListener(() =>
        {
            FindCamera();
            if (cameraFollow != null)
                cameraFollow.ZoomIn();
        });

        minus.onClick.AddListener(() =>
        {
            FindCamera();
            if (cameraFollow != null)
                cameraFollow.ZoomOut();
        });

        reset.onClick.AddListener(() =>
        {
            FindCamera();
            if (cameraFollow != null)
                cameraFollow.ResetToInitialPosition();
        });
    }

    private void ApplyCameraControlsLayout()
    {
        if (cameraControlsRect == null)
            return;

        if (isCompactLayout)
        {
            cameraControlsRect.sizeDelta = new Vector2(186f, 48f);
            cameraControlsRect.anchoredPosition = new Vector2(-12f, 12f);

            SetCameraButtonLayout(
                cameraControlsRect,
                "ZoomInButton",
                new Vector2(6f, 6f),
                new Vector2(36f, 36f),
                22
            );

            SetCameraButtonLayout(
                cameraControlsRect,
                "ZoomOutButton",
                new Vector2(48f, 6f),
                new Vector2(36f, 36f),
                22
            );

            SetCameraButtonLayout(
                cameraControlsRect,
                "ResetCameraButton",
                new Vector2(90f, 6f),
                new Vector2(90f, 36f),
                13
            );
        }
        else
        {
            cameraControlsRect.sizeDelta = new Vector2(232f, 58f);
            cameraControlsRect.anchoredPosition =
                new Vector2(-controlsMargin.x, controlsMargin.y);

            SetCameraButtonLayout(
                cameraControlsRect,
                "ZoomInButton",
                new Vector2(8f, 5f),
                new Vector2(48f, 48f),
                27
            );

            SetCameraButtonLayout(
                cameraControlsRect,
                "ZoomOutButton",
                new Vector2(64f, 5f),
                new Vector2(48f, 48f),
                27
            );

            SetCameraButtonLayout(
                cameraControlsRect,
                "ResetCameraButton",
                new Vector2(120f, 5f),
                new Vector2(106f, 48f),
                14
            );
        }
    }

    private void SetCameraButtonLayout(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        Transform buttonTransform = parent.Find(objectName);

        if (buttonTransform == null)
            return;

        RectTransform rt = buttonTransform.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        Text label = buttonTransform.GetComponentInChildren<Text>(true);
        if (label != null)
            label.fontSize = fontSize;
    }

    private Button CreateCameraButton(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        string label)
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);

        RectTransform rt = buttonObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.10f, 0.34f, 0.22f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.10f, 0.34f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.17f, 0.50f, 0.31f, 1f);
        colors.pressedColor = new Color(0.07f, 0.25f, 0.16f, 1f);
        button.colors = colors;

        CreateText(
            buttonObject,
            label,
            label == "RESET" ? 14 : 27,
            TextAnchor.MiddleCenter,
            Color.white
        );

        return button;
    }

    private void FindCamera()
    {
        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<SmoothCameraFollow>();
    }

    // ---------------------------------------------------------
    // UI HELPERS
    // ---------------------------------------------------------

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void SetRect(
        RectTransform rt,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    private Text CreateText(
        GameObject parent,
        string value,
        int fontSize,
        TextAnchor alignment,
        Color color,
        Vector2? leftPadding = null)
    {
        GameObject textObject = CreateUIObject("Label", parent.transform);

        RectTransform rt = textObject.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        Vector2 padding = leftPadding ?? Vector2.zero;
        rt.offsetMin = new Vector2(padding.x, padding.y);
        rt.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        return text;
    }
}
