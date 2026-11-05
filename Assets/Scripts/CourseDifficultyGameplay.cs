
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies genuine gameplay difficulty to all ten courses.
/// 
/// Courses 01-03: Medium - existing layouts.
/// Courses 04-06: Difficult - tighter gates and barriers.
/// Courses 07-10: More Difficult - precision gates and extra obstacles.
///
/// Also displays difficulty on the course selector and current-course HUD.
/// Attach to the same GameObject as CourseManager.
/// </summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public class CourseDifficultyGameplay : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private CourseManager courseManager;
    [SerializeField] private CourseEnvironmentBuilder environmentBuilder;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Font uiFont;

    [Header("Difficulty Layout")]
    [SerializeField] private bool addPhysicalDifficultyObstacles = true;

    [Header("Difficulty Display")]
    [SerializeField] private bool displayCourseDifficulty = true;
    [SerializeField] private bool displayDifficultyOnCourseButtons = true;

    private static readonly string[] CourseNames =
    {
        "Evergreen Valley",
        "Copper Canyon",
        "Frostlight Ridge",
        "Jade Temple Garden",
        "Azure Coast",
        "Volcanic Ember Run",
        "Moonlit Crater",
        "Redwood Rapids",
        "Sky Harbor",
        "Crystal Caverns"
    };

    private Transform currentEnvironmentRoot;

    private int lastAppliedCourseId = -1;
    private int lastDisplayedCourseId = -1;

    private Text difficultyBannerText;

    private readonly List<Material> generatedMaterials =
        new List<Material>();

    // ---------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------

    private IEnumerator Start()
    {
        // Allow CourseManager and TenCourseHUD to initialise first.
        yield return null;

        FindReferences();

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
        }

        if (displayCourseDifficulty)
            BuildDifficultyBanner();

        if (displayDifficultyOnCourseButtons)
            AddDifficultyBadgesToSelector();

        SynchronizeCourse();
    }

    private void Update()
    {
        FindReferences();
        SynchronizeCourse();
    }

    private void FindReferences()
    {
        if (courseManager == null)
        {
            courseManager = CourseManager.Instance;

            if (courseManager == null)
            {
                courseManager =
                    FindFirstObjectByType<CourseManager>();
            }
        }

        if (environmentBuilder == null)
        {
            if (courseManager != null)
            {
                environmentBuilder =
                    courseManager.GetComponent<CourseEnvironmentBuilder>();
            }

            if (environmentBuilder == null)
            {
                environmentBuilder =
                    FindFirstObjectByType<CourseEnvironmentBuilder>();
            }
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }
    }

    // ---------------------------------------------------------
    // COURSE DIFFICULTY DEFINITIONS
    // ---------------------------------------------------------

    public static string GetDifficulty(int courseId)
    {
        if (courseId >= 1 && courseId <= 3)
            return "Medium";

        if (courseId >= 4 && courseId <= 6)
            return "Difficult";

        if (courseId >= 7 && courseId <= 10)
            return "More Difficult";

        return "Unknown";
    }

    private static Color GetDifficultyColor(int courseId)
    {
        if (courseId <= 3)
            return new Color(0.35f, 0.90f, 0.60f);

        if (courseId <= 6)
            return new Color(1f, 0.72f, 0.30f);

        return new Color(1f, 0.42f, 0.36f);
    }

    // ---------------------------------------------------------
    // ENVIRONMENT DETECTION
    // ---------------------------------------------------------

    private void SynchronizeCourse()
    {
        if (courseManager == null)
            return;

        int courseId = courseManager.ActiveCourseId;

        if (courseId < 1 || courseId > 10)
            return;

        if (displayCourseDifficulty &&
            courseId != lastDisplayedCourseId)
        {
            UpdateDifficultyBanner(courseId);
            lastDisplayedCourseId = courseId;
        }

        if (!addPhysicalDifficultyObstacles)
            return;

        if (environmentBuilder == null)
            return;

        Transform newEnvironment =
            environmentBuilder.transform.Find("ProceduralEnvironment");

        if (newEnvironment == null)
            return;

        if (newEnvironment == currentEnvironmentRoot &&
            courseId == lastAppliedCourseId)
        {
            return;
        }

        ReleaseGeneratedMaterials();

        currentEnvironmentRoot = newEnvironment;
        lastAppliedCourseId = courseId;

        BuildDifficultyLayout(
            currentEnvironmentRoot,
            courseId
        );
    }

    // ---------------------------------------------------------
    // APPLY ACTUAL BOARD DIFFICULTY
    // ---------------------------------------------------------

    private void BuildDifficultyLayout(
        Transform root,
        int courseId)
    {
        if (root == null)
            return;

        string layoutName =
            "DifficultyLayout_Course_" + courseId.ToString("00");

        if (root.Find(layoutName) != null)
            return;

        GameObject layoutObject =
            new GameObject(layoutName);

        layoutObject.transform.SetParent(root, false);

        switch (courseId)
        {
            // Existing medium routes are preserved.
            case 1:
            case 2:
            case 3:
                Debug.Log(
                    $"[CourseDifficultyGameplay] Course {courseId}: Medium layout retained."
                );
                break;

            case 4:
                BuildJadeDifficultLayout(layoutObject.transform);
                break;

            case 5:
                BuildAzureDifficultLayout(layoutObject.transform);
                break;

            case 6:
                BuildVolcanicDifficultLayout(layoutObject.transform);
                break;

            case 7:
                BuildMoonlitVeryDifficultLayout(layoutObject.transform);
                break;

            case 8:
                BuildRedwoodVeryDifficultLayout(layoutObject.transform);
                break;

            case 9:
                BuildSkyHarborVeryDifficultLayout(layoutObject.transform);
                break;

            case 10:
                BuildCrystalVeryDifficultLayout(layoutObject.transform);
                break;
        }

        Debug.Log(
            $"[CourseDifficultyGameplay] Applied {GetDifficulty(courseId)} physical layout to Course {courseId}."
        );
    }

    // ---------------------------------------------------------
    // COURSE 04: JADE TEMPLE - DIFFICULT
    // ---------------------------------------------------------

    private void BuildJadeDifficultLayout(Transform root)
    {
        Material jadeMaterial = CreateMaterial(
            new Color(0.28f, 0.39f, 0.27f)
        );

        // Narrow the three existing gate passages.
        AddGatePair(
            root, "Jade_NarrowGate_A",
            1.35f, -4.15f, 1.85f, 0.95f, jadeMaterial
        );

        AddGatePair(
            root, "Jade_NarrowGate_B",
            -1.35f, -0.15f, 1.85f, 0.95f, jadeMaterial
        );

        AddGatePair(
            root, "Jade_NarrowGate_C",
            0.95f, 3.85f, 1.80f, 0.95f, jadeMaterial
        );

        // An angled approach deflector forces a controlled line.
        CreateSolidBlock(
            root,
            "Jade_AngledApproachBlocker",
            new Vector3(-2.65f, 0.28f, -6.1f),
            new Vector3(2.5f, 0.56f, 0.28f),
            22f,
            jadeMaterial
        );
    }

    // ---------------------------------------------------------
    // COURSE 05: AZURE COAST - DIFFICULT
    // ---------------------------------------------------------

    private void BuildAzureDifficultLayout(Transform root)
    {
        Material wood = CreateMaterial(
            new Color(0.43f, 0.24f, 0.12f)
        );

        // The ball must alternate between right and left openings.
        AddSideGapBarrier(
            root, "Azure_ChallengeBarrier_A",
            -5.55f, true, 2.35f, 0.54f, wood
        );

        AddSideGapBarrier(
            root, "Azure_ChallengeBarrier_B",
            -1.45f, false, 2.35f, 0.54f, wood
        );

        AddSideGapBarrier(
            root, "Azure_ChallengeBarrier_C",
            2.65f, true, 2.35f, 0.54f, wood
        );
    }

    // ---------------------------------------------------------
    // COURSE 06: VOLCANIC EMBER - DIFFICULT
    // ---------------------------------------------------------

    private void BuildVolcanicDifficultLayout(Transform root)
    {
        Material basalt = CreateMaterial(
            new Color(0.20f, 0.16f, 0.17f)
        );

        // Narrower passages than the Azure boardwalk.
        AddSideGapBarrier(
            root, "Ember_NarrowBasalt_A",
            -5.65f, true, 1.95f, 0.72f, basalt
        );

        AddSideGapBarrier(
            root, "Ember_NarrowBasalt_B",
            -1.55f, false, 1.95f, 0.72f, basalt
        );

        AddSideGapBarrier(
            root, "Ember_NarrowBasalt_C",
            2.55f, true, 1.95f, 0.72f, basalt
        );
    }

    // ---------------------------------------------------------
    // COURSE 07: MOONLIT CRATER - MORE DIFFICULT
    // ---------------------------------------------------------

    private void BuildMoonlitVeryDifficultLayout(Transform root)
    {
        Material moonStone = CreateMaterial(
            new Color(0.42f, 0.49f, 0.59f)
        );

        AddGatePair(
            root, "Moon_PrecisionGate_A",
            1.15f, -5.75f, 1.55f, 1.05f, moonStone
        );

        AddGatePair(
            root, "Moon_PrecisionGate_B",
            -1.15f, -1.75f, 1.55f, 1.05f, moonStone
        );

        AddGatePair(
            root, "Moon_PrecisionGate_C",
            1.15f, 2.25f, 1.55f, 1.05f, moonStone
        );

        AddGatePair(
            root, "Moon_PrecisionGate_D",
            -1.15f, 5.85f, 1.55f, 1.05f, moonStone
        );
    }

    // ---------------------------------------------------------
    // COURSE 08: REDWOOD RAPIDS - MORE DIFFICULT
    // ---------------------------------------------------------

    private void BuildRedwoodVeryDifficultLayout(Transform root)
    {
        Material redwood = CreateMaterial(
            new Color(0.36f, 0.16f, 0.09f)
        );

        // Four alternating log barriers with narrow side passages.
        AddSideGapBarrier(
            root, "Redwood_LogChallenge_A",
            -5.7f, true, 1.85f, 0.72f, redwood
        );

        AddSideGapBarrier(
            root, "Redwood_LogChallenge_B",
            -1.65f, false, 1.85f, 0.72f, redwood
        );

        AddSideGapBarrier(
            root, "Redwood_LogChallenge_C",
            2.35f, true, 1.85f, 0.72f, redwood
        );

        AddSideGapBarrier(
            root, "Redwood_LogChallenge_D",
            5.75f, false, 1.85f, 0.72f, redwood
        );
    }

    // ---------------------------------------------------------
    // COURSE 09: SKY HARBOR - MORE DIFFICULT
    // ---------------------------------------------------------

    private void BuildSkyHarborVeryDifficultLayout(Transform root)
    {
        Material gold = CreateMaterial(
            new Color(0.78f, 0.55f, 0.19f),
            0.55f
        );

        // Alternating high-precision aerial gates.
        AddGatePair(
            root, "Sky_PrecisionGate_A",
            1.2f, -5.7f, 1.40f, 1.15f, gold
        );

        AddGatePair(
            root, "Sky_PrecisionGate_B",
            -1.2f, -1.65f, 1.40f, 1.15f, gold
        );

        AddGatePair(
            root, "Sky_PrecisionGate_C",
            1.2f, 2.4f, 1.40f, 1.15f, gold
        );

        AddGatePair(
            root, "Sky_PrecisionGate_D",
            -1.2f, 5.7f, 1.40f, 1.15f, gold
        );
    }

    // ---------------------------------------------------------
    // COURSE 10: CRYSTAL CAVERNS - MORE DIFFICULT
    // ---------------------------------------------------------

    private void BuildCrystalVeryDifficultLayout(Transform root)
    {
        Material crystal = CreateMaterial(
            new Color(0.42f, 0.26f, 0.69f),
            0.65f
        );

        // Five precision gates with the smallest playable clearances.
        AddGatePair(
            root, "Crystal_FinalGate_A",
            1.0f, -5.9f, 1.20f, 1.20f, crystal
        );

        AddGatePair(
            root, "Crystal_FinalGate_B",
            -1.0f, -3.0f, 1.20f, 1.20f, crystal
        );

        AddGatePair(
            root, "Crystal_FinalGate_C",
            1.0f, -0.1f, 1.20f, 1.20f, crystal
        );

        AddGatePair(
            root, "Crystal_FinalGate_D",
            -1.0f, 2.8f, 1.20f, 1.20f, crystal
        );

        AddGatePair(
            root, "Crystal_FinalGate_E",
            1.0f, 5.7f, 1.20f, 1.20f, crystal
        );
    }

    // ---------------------------------------------------------
    // OBSTACLE HELPERS
    // ---------------------------------------------------------

    private void AddGatePair(
        Transform parent,
        string name,
        float centerX,
        float z,
        float clearGap,
        float height,
        Material material)
    {
        const float postWidth = 0.32f;

        float offset =
            (clearGap + postWidth) * 0.5f;

        CreateSolidBlock(
            parent,
            name + "_Left",
            new Vector3(centerX - offset, height * 0.5f, z),
            new Vector3(postWidth, height, 0.42f),
            0f,
            material
        );

        CreateSolidBlock(
            parent,
            name + "_Right",
            new Vector3(centerX + offset, height * 0.5f, z),
            new Vector3(postWidth, height, 0.42f),
            0f,
            material
        );
    }

    private void AddSideGapBarrier(
        Transform parent,
        string name,
        float z,
        bool leaveRightSideOpen,
        float openingWidth,
        float height,
        Material material)
    {
        // Approximate playable course boundaries:
        // X = -4.15 to +4.15.
        const float halfBoardWidth = 4.15f;

        float totalWidth = halfBoardWidth * 2f;

        float barrierLength =
            totalWidth - openingWidth;

        float centerX =
            leaveRightSideOpen
                ? -openingWidth * 0.5f
                : openingWidth * 0.5f;

        CreateSolidBlock(
            parent,
            name,
            new Vector3(centerX, height * 0.5f, z),
            new Vector3(
                barrierLength,
                height,
                0.30f
            ),
            0f,
            material
        );
    }

    private GameObject CreateSolidBlock(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        float yaw,
        Material material)
    {
        GameObject obstacle =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        obstacle.name = objectName;

        obstacle.transform.SetParent(parent, false);
        obstacle.transform.localPosition = localPosition;

        obstacle.transform.localRotation =
            Quaternion.Euler(0f, yaw, 0f);

        obstacle.transform.localScale = localScale;

        MeshRenderer renderer =
            obstacle.GetComponent<MeshRenderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        BoxCollider collider =
            obstacle.GetComponent<BoxCollider>();

        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }

        return obstacle;
    }

    private Material CreateMaterial(
        Color color,
        float smoothness = 0.25f)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError(
                "[CourseDifficultyGameplay] No compatible shader found."
            );

            return null;
        }

        Material material = new Material(shader);

        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        generatedMaterials.Add(material);

        return material;
    }

    private void ReleaseGeneratedMaterials()
    {
        foreach (Material material in generatedMaterials)
        {
            if (material == null)
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }

        generatedMaterials.Clear();
    }

    // ---------------------------------------------------------
    // CURRENT COURSE DIFFICULTY BANNER
    // ---------------------------------------------------------

    private void BuildDifficultyBanner()
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning(
                "[CourseDifficultyGameplay] Canvas not found. Difficulty banner skipped."
            );
            return;
        }

        Transform existing =
            targetCanvas.transform.Find("CourseDifficultyBanner");

        GameObject banner;

        if (existing != null)
        {
            banner = existing.gameObject;
        }
        else
        {
            banner = new GameObject(
                "CourseDifficultyBanner",
                typeof(RectTransform),
                typeof(Image)
            );

            banner.transform.SetParent(
                targetCanvas.transform,
                false
            );
        }

        RectTransform rect =
            banner.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        rect.anchoredPosition =
            new Vector2(0f, -68f);

        rect.sizeDelta =
            new Vector2(365f, 34f);

        Image background =
            banner.GetComponent<Image>();

        background.color =
            new Color(0.025f, 0.065f, 0.10f, 0.92f);

        Transform labelTransform =
            banner.transform.Find("DifficultyLabel");

        GameObject labelObject;

        if (labelTransform == null)
        {
            labelObject = new GameObject(
                "DifficultyLabel",
                typeof(RectTransform),
                typeof(Text)
            );

            labelObject.transform.SetParent(
                banner.transform,
                false
            );
        }
        else
        {
            labelObject = labelTransform.gameObject;
        }

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(5f, 0f);
        labelRect.offsetMax = new Vector2(-5f, 0f);

        difficultyBannerText =
            labelObject.GetComponent<Text>();

        difficultyBannerText.font = uiFont;
        difficultyBannerText.fontSize = 13;
        difficultyBannerText.fontStyle = FontStyle.Bold;
        difficultyBannerText.alignment =
            TextAnchor.MiddleCenter;

        difficultyBannerText.raycastTarget = false;
        difficultyBannerText.supportRichText = true;
        difficultyBannerText.horizontalOverflow =
            HorizontalWrapMode.Overflow;
    }

    private void UpdateDifficultyBanner(int courseId)
    {
        if (difficultyBannerText == null)
            return;

        string courseName =
            CourseNames[courseId - 1];

        string difficulty =
            GetDifficulty(courseId);

        difficultyBannerText.text =
            $"COURSE {courseId:00}  •  {courseName.ToUpperInvariant()}  •  {difficulty.ToUpperInvariant()}";

        difficultyBannerText.color =
            GetDifficultyColor(courseId);
    }

    // ---------------------------------------------------------
    // DIFFICULTY LABELS ON EACH COURSE SELECTOR BUTTON
    // ---------------------------------------------------------

    private void AddDifficultyBadgesToSelector()
    {
        Transform[] all =
            FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int badgesAdded = 0;

        foreach (Transform item in all)
        {
            if (item == null ||
                !item.name.StartsWith("CourseButton_"))
            {
                continue;
            }

            string idText =
                item.name.Substring("CourseButton_".Length);

            int courseId;

            if (!int.TryParse(idText, out courseId))
                continue;

            if (courseId < 1 || courseId > 10)
                continue;

            if (item.Find("DifficultyBadge") != null)
                continue;

            // Reserve space for the course difficulty label.
            Transform oldLabel =
                item.Find("Label");

            if (oldLabel != null)
            {
                RectTransform oldRect =
                    oldLabel.GetComponent<RectTransform>();

                if (oldRect != null)
                {
                    oldRect.offsetMax =
                        new Vector2(-112f, oldRect.offsetMax.y);
                }
            }

            GameObject badge =
                new GameObject(
                    "DifficultyBadge",
                    typeof(RectTransform),
                    typeof(Text)
                );

            badge.transform.SetParent(item, false);

            RectTransform badgeRect =
                badge.GetComponent<RectTransform>();

            badgeRect.anchorMin =
                new Vector2(1f, 0.5f);

            badgeRect.anchorMax =
                new Vector2(1f, 0.5f);

            badgeRect.pivot =
                new Vector2(1f, 0.5f);

            badgeRect.anchoredPosition =
                new Vector2(-7f, 0f);

            badgeRect.sizeDelta =
                new Vector2(108f, 25f);

            Text badgeText =
                badge.GetComponent<Text>();

            badgeText.font = uiFont;
            badgeText.fontSize = 10;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment =
                TextAnchor.MiddleRight;

            badgeText.text =
                GetDifficulty(courseId).ToUpperInvariant();

            badgeText.color =
                GetDifficultyColor(courseId);

            badgeText.raycastTarget = false;
            badgeText.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            badgesAdded++;
        }

        Debug.Log(
            $"[CourseDifficultyGameplay] Difficulty labels added to {badgesAdded} course buttons."
        );
    }

    private void OnDestroy()
    {
        ReleaseGeneratedMaterials();
    }
}
