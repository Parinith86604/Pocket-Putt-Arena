
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public class HUDClarityFix : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField]
    private Vector2 referenceResolution =
        new Vector2(1280f, 720f);

    [Header("Text Sizes")]
    [SerializeField] private int courseFontSize = 14;
    [SerializeField] private int difficultyFontSize = 11;
    [SerializeField] private int scoreFontSize = 20;
    [SerializeField] private int headingFontSize = 21;
    [SerializeField] private int resetFontSize = 15;

    [Header("Font")]
    [SerializeField] private Font preferredFont;

    private Font clearFont;

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.6f);

        CreateFont();
        FixCanvas();
        FixScoreBadge();
        FixDifficultyBanner();
        FixCourseText();
        FixCameraControls();

        Debug.Log("[HUDClarityFix] Compact HUD layout applied.");
    }

    private void CreateFont()
    {
        if (preferredFont != null)
        {
            clearFont = preferredFont;
            return;
        }

        try
        {
            clearFont = Font.CreateDynamicFontFromOSFont(
                new string[] { "Arial", "Segoe UI" },
                64
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HUDClarityFix] Font: " + e.Message);
        }

        if (clearFont == null)
        {
            clearFont = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
        }
    }

    // --------------------------------------------------
    // CANVAS SETTINGS
    // --------------------------------------------------

    private void FixCanvas()
    {
        Canvas[] canvases =
            FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
                continue;

            CanvasScaler scaler =
                canvas.GetComponent<CanvasScaler>();

            if (scaler == null)
                continue;

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution = referenceResolution;

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            canvas.pixelPerfect = true;
        }
    }

    // --------------------------------------------------
    // SCORE COUNTER
    // --------------------------------------------------

    private void FixScoreBadge()
    {
        Transform badge = FindObject("StrokeBadge");

        if (badge == null)
            return;

        RectTransform rect =
            badge.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            rect.anchoredPosition = new Vector2(0f, -16f);
            rect.sizeDelta = new Vector2(280f, 42f);
        }

        Image background = badge.GetComponent<Image>();

        if (background != null)
        {
            background.color =
                new Color(0.035f, 0.065f, 0.11f, 0.94f);

            background.raycastTarget = false;
        }

        Text score =
            badge.GetComponentInChildren<Text>(true);

        if (score != null)
        {
            ApplyFont(score, scoreFontSize);

            score.alignment = TextAnchor.MiddleCenter;
            score.color = Color.white;

            RectTransform textRect =
                score.GetComponent<RectTransform>();

            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4f, 2f);
                textRect.offsetMax = new Vector2(-4f, -2f);
            }
        }
    }

    // --------------------------------------------------
    // DIFFICULTY BANNER
    // --------------------------------------------------

    private void FixDifficultyBanner()
    {
        Transform banner =
            FindObject("CourseDifficultyBanner");

        if (banner == null)
            return;

        RectTransform rect =
            banner.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            rect.anchoredPosition = new Vector2(0f, -64f);
            rect.sizeDelta = new Vector2(270f, 24f);
        }

        Image background = banner.GetComponent<Image>();

        if (background != null)
        {
            background.color =
                new Color(0.035f, 0.065f, 0.11f, 0.88f);

            background.raycastTarget = false;
        }

        Text label =
            banner.GetComponentInChildren<Text>(true);

        if (label != null)
        {
            ApplyFont(label, 13);
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    // --------------------------------------------------
    // COURSE SELECTOR TEXT
    // --------------------------------------------------

    private void FixCourseText()
    {
        Text[] texts =
            FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Text label in texts)
        {
            if (label == null)
                continue;

            string value =
                label.text.Trim().ToUpperInvariant();

            if (value.Contains("SELECT COURSE"))
            {
                ApplyFont(label, headingFontSize);
                label.alignment = TextAnchor.MiddleCenter;
            }
            else if (IsCourseName(value))
            {
                ApplyFont(label, courseFontSize);

                label.alignment = TextAnchor.MiddleLeft;

                // HorizontalWrapMode has only Wrap and Overflow.
                label.horizontalOverflow =
                    HorizontalWrapMode.Overflow;

                label.verticalOverflow =
                    VerticalWrapMode.Truncate;

                RectTransform rect =
                    label.GetComponent<RectTransform>();

                if (rect != null)
                {
                    rect.offsetMin = new Vector2(7f, 2f);
                    rect.offsetMax = new Vector2(-82f, -2f);
                }

                // Remove Par information from compact selector rows.
                int parIndex = label.text.IndexOf(
                    "(Par",
                    System.StringComparison.OrdinalIgnoreCase
                );

                if (parIndex >= 0)
                {
                    label.text =
                        label.text.Substring(0, parIndex).Trim();
                }
            }
            else if (IsDifficulty(value))
            {
                ApplyFont(label, difficultyFontSize);

                label.alignment = TextAnchor.MiddleRight;

                RectTransform rect =
                    label.GetComponent<RectTransform>();

                if (rect != null)
                {
                    rect.anchorMin = new Vector2(1f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);

                    rect.anchoredPosition =
                        new Vector2(-7f, 0f);

                    rect.sizeDelta =
                        new Vector2(78f, 22f);
                }

                if (value == "MEDIUM")
                    label.color = new Color(0.30f, 1f, 0.55f);
                else if (value == "DIFFICULT")
                    label.color = new Color(1f, 0.76f, 0.20f);
                else
                    label.color = new Color(1f, 0.38f, 0.32f);
            }
        }
    }

    // --------------------------------------------------
    // CAMERA CONTROLS
    // --------------------------------------------------

    private void FixCameraControls()
    {
        Transform container =
            FindObject("CameraControlsContainer");

        if (container == null)
        {
            Debug.LogWarning(
                "[HUDClarityFix] CameraControlsContainer not found. " +
                "Existing camera button layout was left unchanged."
            );

            return;
        }

        Image containerImage =
            container.GetComponent<Image>();

        if (containerImage != null)
            containerImage.enabled = false;

        RectTransform containerRect =
            container.GetComponent<RectTransform>();

        if (containerRect != null)
        {
            containerRect.sizeDelta =
                new Vector2(140f, 84f);
        }

        SetCameraButton(
            container,
            "ZoomInBtn",
            new Vector2(-100f, 43f),
            new Vector2(42f, 34f),
            25
        );

        SetCameraButton(
            container,
            "ZoomOutBtn",
            new Vector2(-52f, 43f),
            new Vector2(42f, 34f),
            25
        );

        SetCameraButton(
            container,
            "ResetCamBtn",
            new Vector2(-76f, 3f),
            new Vector2(92f, 30f),
            resetFontSize
        );
    }

    private void SetCameraButton(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        Transform buttonTransform =
            parent.Find(objectName);

        if (buttonTransform == null)
            return;

        RectTransform rect =
            buttonTransform.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Text label =
            buttonTransform.GetComponentInChildren<Text>(true);

        if (label == null)
            return;

        ApplyFont(label, fontSize);
        label.alignment = TextAnchor.MiddleCenter;
    }

    // --------------------------------------------------
    // SHARED HELPERS
    // --------------------------------------------------

    private void ApplyFont(Text label, int size)
    {
        if (clearFont != null)
            label.font = clearFont;

        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.resizeTextForBestFit = false;
        label.raycastTarget = false;
    }

    private bool IsDifficulty(string value)
    {
        return value == "MEDIUM" ||
               value == "DIFFICULT" ||
               value == "MORE DIFFICULT";
    }

    private bool IsCourseName(string value)
    {
        string[] names =
        {
            "EVERGREEN VALLEY",
            "COPPER CANYON",
            "FROSTLIGHT RIDGE",
            "JADE TEMPLE GARDEN",
            "AZURE COAST",
            "VOLCANIC EMBER RUN",
            "MOONLIT CRATER",
            "REDWOOD RAPIDS",
            "SKY HARBOR",
            "CRYSTAL CAVERNS"
        };

        foreach (string name in names)
        {
            if (value.Contains(name))
                return true;
        }

        return false;
    }

    private Transform FindObject(string objectName)
    {
        Transform[] all =
            FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Transform item in all)
        {
            if (item != null && item.name == objectName)
                return item;
        }

        return null;
    }
}
