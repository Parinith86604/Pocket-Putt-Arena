
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural environment and obstacle builder for Pocket Putt Arena.
/// Generates ten distinct stylized 3D mini-golf environments and playable layouts.
/// Courses 01-05 retain their original themes. Courses 06-10 add new environments:
/// Volcanic Ember Run, Moonlit Crater, Redwood Rapids, Sky Harbor and Crystal Caverns.
/// </summary>
[ExecuteAlways]
public class CourseEnvironmentBuilder : MonoBehaviour
{
    [Header("Active Theme")]
    [SerializeField] private int activeCourseIndex = 1;
    public int ActiveCourseIndex => activeCourseIndex;

    [Header("Shared Materials")]
    [SerializeField] private Material woodMaterial;
    [SerializeField] private Material greenCheckerMaterial;

    private Material originalGroundMaterial;

    private Transform envRoot;
    private readonly List<Material> generatedMaterials = new List<Material>();
    private readonly List<Mesh> generatedMeshes = new List<Mesh>();

    private void Start()
    {
        if (Application.isPlaying)
        {
            ApplyTheme(activeCourseIndex);
        }
    }

    /// <summary>
    /// Applies a specific course theme (1 to 10).
    /// </summary>
    public void ApplyTheme(int courseIndex)
    {
        activeCourseIndex = Mathf.Clamp(courseIndex, 1, 10);

        // Clean up all previous procedural environment objects immediately.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("ProceduralEnvironment") ||
                child.name.StartsWith("Destroying"))
            {
                child.name = "Destroying";
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        ReleaseGeneratedAssets();

        GameObject rootObj = new GameObject("ProceduralEnvironment");
        rootObj.transform.SetParent(transform, false);
        rootObj.transform.localPosition = Vector3.zero;
        rootObj.transform.localRotation = Quaternion.identity;
        rootObj.transform.localScale = Vector3.one;
        envRoot = rootObj.transform;

        ResolveMaterials();

        switch (activeCourseIndex)
        {
            case 1:
                BuildEvergreenValley();
                break;
            case 2:
                BuildCopperCanyon();
                break;
            case 3:
                BuildFrostlightRidge();
                break;
            case 4:
                BuildJadeTemple();
                break;
            case 5:
                BuildAzureCoast();
                break;
            case 6:
                BuildVolcanicEmber();
                break;
            case 7:
                BuildMoonlitCrater();
                break;
            case 8:
                BuildRedwoodRapids();
                break;
            case 9:
                BuildSkyHarbor();
                break;
            case 10:
                BuildCrystalCaverns();
                break;
        }

        BuildDistinctPlayableLayout(activeCourseIndex);
        ApplyGroundSurfaceStyle(activeCourseIndex);
    }

    // ---------------------------------------------------------
    // MATERIAL HANDLING
    // ---------------------------------------------------------

    private void ResolveMaterials()
    {
        // Convert imported/source materials to known compatible Lit shaders.
        // This prevents incompatible imported shaders from rendering magenta in URP.
        Material sourceWood = woodMaterial != null
            ? woodMaterial
            : Resources.Load<Material>("M_Wall");

        Material sourceGreen = greenCheckerMaterial != null
            ? greenCheckerMaterial
            : Resources.Load<Material>("M_CourseGreen");

        woodMaterial = CreateSafeLitMaterial(
            sourceWood,
            "M_Wall_RuntimeSafe",
            new Color(0.48f, 0.26f, 0.12f),
            0.28f,
            0f
        );

        greenCheckerMaterial = CreateSafeLitMaterial(
            sourceGreen,
            "M_CourseGreen_RuntimeSafe",
            new Color(0.32f, 0.74f, 0.16f),
            0.30f,
            0f
        );
    }

    private Material CreateSafeLitMaterial(
        Material source,
        string materialName,
        Color fallbackColor,
        float fallbackSmoothness,
        float fallbackMetallic)
    {
        Shader litShader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (litShader == null)
            litShader = Shader.Find("Standard");

        if (litShader == null)
        {
            Debug.LogError(
                "[CourseEnvironmentBuilder] Could not find URP/Lit or Standard shader. " +
                "Generated course materials cannot be created."
            );

            return null;
        }

        Color color = source != null
            ? source.color
            : fallbackColor;

        if (LooksLikeErrorMagenta(color) ||
            (source != null &&
             source.shader != null &&
             source.shader.name.ToLowerInvariant().Contains("error")))
        {
            color = fallbackColor;
        }

        Material safe = new Material(litShader);
        safe.name = materialName;
        safe.color = color;

        if (safe.HasProperty("_BaseColor"))
            safe.SetColor("_BaseColor", color);

        if (safe.HasProperty("_Smoothness"))
        {
            float smoothness =
                source != null && source.HasProperty("_Smoothness")
                    ? source.GetFloat("_Smoothness")
                    : fallbackSmoothness;

            safe.SetFloat("_Smoothness", smoothness);
        }

        if (safe.HasProperty("_Metallic"))
        {
            float metallic =
                source != null && source.HasProperty("_Metallic")
                    ? source.GetFloat("_Metallic")
                    : fallbackMetallic;

            safe.SetFloat("_Metallic", metallic);
        }

        if (source != null)
        {
            Texture albedo = null;

            if (source.HasProperty("_BaseMap"))
                albedo = source.GetTexture("_BaseMap");

            if (albedo == null && source.HasProperty("_MainTex"))
                albedo = source.GetTexture("_MainTex");

            if (albedo != null)
            {
                if (safe.HasProperty("_BaseMap"))
                    safe.SetTexture("_BaseMap", albedo);

                if (safe.HasProperty("_MainTex"))
                    safe.SetTexture("_MainTex", albedo);
            }

            if (source.HasProperty("_BumpMap"))
            {
                Texture normal = source.GetTexture("_BumpMap");

                if (normal != null && safe.HasProperty("_BumpMap"))
                {
                    safe.SetTexture("_BumpMap", normal);
                    safe.EnableKeyword("_NORMALMAP");
                }
            }
        }

        generatedMaterials.Add(safe);
        return safe;
    }

    private bool LooksLikeErrorMagenta(Color color)
    {
        return color.r > 0.65f &&
               color.b > 0.55f &&
               color.g < 0.45f;
    }

    private void ApplyGroundSurfaceStyle(int courseIndex)
    {
        GameObject groundObject = GameObject.Find("Course_Ground");

        if (groundObject == null)
            return;

        Renderer groundRenderer =
            groundObject.GetComponent<Renderer>();

        if (groundRenderer == null)
            return;

        if (originalGroundMaterial == null)
        {
            originalGroundMaterial =
                groundRenderer.sharedMaterial != null
                    ? groundRenderer.sharedMaterial
                    : greenCheckerMaterial;
        }

        // Keep the original classic green checker for Courses 01-05.
        if (courseIndex <= 5)
        {
            if (originalGroundMaterial != null)
                groundRenderer.sharedMaterial = originalGroundMaterial;

            return;
        }

        if (originalGroundMaterial == null)
            return;

        Color tint;

        switch (courseIndex)
        {
            case 6:
                tint = new Color(1.00f, 0.72f, 0.66f);
                break;
            case 7:
                tint = new Color(0.78f, 0.88f, 1.00f);
                break;
            case 8:
                tint = new Color(0.82f, 1.00f, 0.78f);
                break;
            case 9:
                tint = new Color(0.78f, 0.94f, 1.00f);
                break;
            case 10:
                tint = new Color(0.91f, 0.79f, 1.00f);
                break;
            default:
                tint = Color.white;
                break;
        }

        Material themedGround =
            new Material(originalGroundMaterial);

        themedGround.name =
            $"Course_{courseIndex}_Checkerboard";

        themedGround.color = tint;

        if (themedGround.HasProperty("_BaseColor"))
            themedGround.SetColor("_BaseColor", tint);

        generatedMaterials.Add(themedGround);
        groundRenderer.sharedMaterial = themedGround;
    }

    private Material CreateLitMaterial(
        Color color,
        float smoothness = 0.2f,
        float metallic = 0.0f)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError(
                "[CourseEnvironmentBuilder] No compatible Lit shader was found."
            );

            return null;
        }

        Material mat = new Material(shader);
        mat.name = "GeneratedCourseMaterial";
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);

        generatedMaterials.Add(mat);
        return mat;
    }

    private void ReleaseGeneratedAssets()
    {
        for (int i = 0; i < generatedMaterials.Count; i++)
        {
            Material material = generatedMaterials[i];

            if (material == null)
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }

        generatedMaterials.Clear();

        for (int i = 0; i < generatedMeshes.Count; i++)
        {
            Mesh mesh = generatedMeshes[i];

            if (mesh == null)
                continue;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }

        generatedMeshes.Clear();
    }

    // ---------------------------------------------------------
    // COURSE 01: EVERGREEN VALLEY
    // ---------------------------------------------------------

    private void BuildEvergreenValley()
    {
        GameObject lake =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        lake.name = "ScenicLake";
        lake.transform.SetParent(envRoot, false);
        lake.transform.localPosition =
            new Vector3(-13.5f, -0.15f, 2.0f);

        lake.transform.localScale =
            new Vector3(1.2f, 1f, 2.8f);

        DestroyImmediate(lake.GetComponent<Collider>());

        lake.GetComponent<MeshRenderer>().sharedMaterial =
            CreateLitMaterial(
                new Color(0.15f, 0.72f, 0.82f, 0.85f),
                0.92f
            );

        Material hillMat =
            CreateLitMaterial(new Color(0.28f, 0.68f, 0.18f), 0.15f);

        CreateHill(
            "Hill_Left",
            new Vector3(-16f, -1.2f, 8f),
            new Vector3(14f, 4.5f, 18f),
            hillMat
        );

        CreateHill(
            "Hill_Back",
            new Vector3(0f, -1.5f, 22f),
            new Vector3(28f, 6.0f, 16f),
            hillMat
        );

        CreateHill(
            "Hill_Right",
            new Vector3(15f, -1.2f, 5f),
            new Vector3(15f, 4.2f, 18f),
            hillMat
        );

        Material trunkMat =
            CreateLitMaterial(new Color(0.42f, 0.24f, 0.10f), 0.2f);

        Material foliageMat =
            CreateLitMaterial(new Color(0.12f, 0.48f, 0.16f), 0.2f);

        Vector3[] treePositions = new Vector3[]
        {
            new Vector3(-9.5f, 0f, -4.5f),
            new Vector3(-10.5f, 0.4f, 0.5f),
            new Vector3(-8.8f, 0.3f, 4.2f),
            new Vector3(-11.2f, 1.0f, 8.5f),
            new Vector3(-13.0f, 1.8f, 12.0f),
            new Vector3(-7.5f, 0.2f, 11.5f),
            new Vector3(-4.5f, 1.2f, 15.5f),
            new Vector3(-1.8f, 1.5f, 16.5f),
            new Vector3(1.5f, 1.4f, 17.0f),
            new Vector3(4.8f, 1.1f, 15.8f),
            new Vector3(8.5f, 0.2f, 9.5f),
            new Vector3(10.2f, 0.8f, 5.0f),
            new Vector3(9.0f, 0.3f, 0.0f),
            new Vector3(11.0f, 1.0f, -3.5f),
            new Vector3(8.2f, 0.1f, -7.0f)
        };

        for (int i = 0; i < treePositions.Length; i++)
        {
            float scaleVar =
                0.85f + ((i * 17) % 35) * 0.01f;

            CreateLowPolyPineTree(
                $"PineTree_{i + 1}",
                treePositions[i],
                scaleVar,
                trunkMat,
                foliageMat
            );
        }

        Material rockMat =
            CreateLitMaterial(new Color(0.60f, 0.62f, 0.60f), 0.15f);

        Vector3[] rockPositions = new Vector3[]
        {
            new Vector3(-6.2f, 0.1f, 7.5f),
            new Vector3(-6.8f, 0.2f, 9.2f),
            new Vector3(-4.8f, 0.1f, 11.5f),
            new Vector3(5.8f, 0.1f, 6.2f),
            new Vector3(6.5f, 0.2f, 8.8f),
            new Vector3(-5.8f, 0.1f, -5.5f),
            new Vector3(5.5f, 0.1f, -6.2f)
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            CreateFacetedRock(
                $"Boulder_{i + 1}",
                rockPositions[i],
                Vector3.one * (1.2f + (i % 3) * 0.35f),
                rockMat
            );
        }

        BuildLoopTheLoop(
            new Vector3(-3.2f, 2.2f, 3.5f),
            2.0f,
            1.6f
        );

        BuildCurvedWallDetails();
    }

    // ---------------------------------------------------------
    // COURSE 02: COPPER CANYON
    // ---------------------------------------------------------

    private void BuildCopperCanyon()
    {
        GameObject wash =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        wash.name = "DryCanyonWash";
        wash.transform.SetParent(envRoot, false);
        wash.transform.localPosition =
            new Vector3(-13.5f, -0.15f, 2.0f);

        wash.transform.localScale =
            new Vector3(1.2f, 1f, 2.8f);

        DestroyImmediate(wash.GetComponent<Collider>());

        wash.GetComponent<MeshRenderer>().sharedMaterial =
            CreateLitMaterial(
                new Color(0.55f, 0.35f, 0.20f),
                0.1f
            );

        Material cliffMat =
            CreateLitMaterial(new Color(0.73f, 0.38f, 0.18f), 0.12f);

        CreateHill(
            "Mesa_Left",
            new Vector3(-16f, -1.0f, 6f),
            new Vector3(14f, 6.5f, 20f),
            cliffMat
        );

        CreateHill(
            "Mesa_Back",
            new Vector3(0f, -1.2f, 22f),
            new Vector3(30f, 8.0f, 16f),
            cliffMat
        );

        CreateHill(
            "Mesa_Right",
            new Vector3(15f, -1.0f, 5f),
            new Vector3(15f, 6.0f, 18f),
            cliffMat
        );

        Material cactusMat =
            CreateLitMaterial(new Color(0.24f, 0.52f, 0.18f), 0.2f);

        Vector3[] cactusPositions = new Vector3[]
        {
            new Vector3(-9.5f, 0f, -4.0f),
            new Vector3(-10.5f, 0.4f, 1.5f),
            new Vector3(-8.5f, 0.3f, 5.5f),
            new Vector3(-11.0f, 1.0f, 10.0f),
            new Vector3(8.5f, 0.2f, 8.5f),
            new Vector3(10.0f, 0.6f, 3.0f),
            new Vector3(9.2f, 0.3f, -2.5f),
            new Vector3(8.0f, 0.1f, -6.5f)
        };

        for (int i = 0; i < cactusPositions.Length; i++)
        {
            CreateSaguaroCactus(
                $"Cactus_{i + 1}",
                cactusPositions[i],
                1.0f + (i % 3) * 0.25f,
                cactusMat
            );
        }

        Material redRockMat =
            CreateLitMaterial(new Color(0.78f, 0.40f, 0.22f), 0.15f);

        Vector3[] rockPositions = new Vector3[]
        {
            new Vector3(-6.0f, 0.1f, 7.0f),
            new Vector3(-6.5f, 0.2f, 9.5f),
            new Vector3(5.5f, 0.1f, 6.0f),
            new Vector3(6.2f, 0.2f, 9.0f),
            new Vector3(-5.5f, 0.1f, -5.0f),
            new Vector3(5.2f, 0.1f, -5.5f)
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            CreateFacetedRock(
                $"CanyonRock_{i + 1}",
                rockPositions[i],
                Vector3.one * (1.3f + (i % 3) * 0.4f),
                redRockMat
            );
        }

        BuildCanyonArchway(
            new Vector3(3.25f, 0f, 5.6f),
            cliffMat
        );

        BuildBankedBumpers(
            new Vector3(4.0f, 0.25f, -1.6f),
            new Vector3(-4.0f, 0.25f, 2.0f)
        );
    }

    // ---------------------------------------------------------
    // COURSE 03: FROSTLIGHT RIDGE
    // ---------------------------------------------------------

    private void BuildFrostlightRidge()
    {
        GameObject iceLake =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        iceLake.name = "GlacialIceLake";
        iceLake.transform.SetParent(envRoot, false);
        iceLake.transform.localPosition =
            new Vector3(-13.5f, -0.15f, 2.0f);

        iceLake.transform.localScale =
            new Vector3(1.2f, 1f, 2.8f);

        DestroyImmediate(iceLake.GetComponent<Collider>());

        iceLake.GetComponent<MeshRenderer>().sharedMaterial =
            CreateLitMaterial(
                new Color(0.62f, 0.88f, 0.98f),
                0.96f,
                0.1f
            );

        Material snowMat =
            CreateLitMaterial(new Color(0.92f, 0.96f, 0.98f), 0.25f);

        CreateHill(
            "Peak_Left",
            new Vector3(-16f, -1.0f, 8f),
            new Vector3(14f, 7.5f, 18f),
            snowMat
        );

        CreateHill(
            "Peak_Back",
            new Vector3(0f, -1.2f, 22f),
            new Vector3(30f, 9.0f, 16f),
            snowMat
        );

        CreateHill(
            "Peak_Right",
            new Vector3(15f, -1.0f, 5f),
            new Vector3(15f, 7.0f, 18f),
            snowMat
        );

        CreateHill(
            "GlacierSpire_Left",
            new Vector3(-19f, -2.2f, 17f),
            new Vector3(8f, 13f, 10f),
            snowMat
        );

        CreateHill(
            "GlacierSpire_Centre",
            new Vector3(1f, -2.4f, 27f),
            new Vector3(11f, 15f, 9f),
            snowMat
        );

        CreateHill(
            "GlacierSpire_Right",
            new Vector3(19f, -2.0f, 14f),
            new Vector3(9f, 12f, 11f),
            snowMat
        );

        Material trunkMat =
            CreateLitMaterial(new Color(0.35f, 0.22f, 0.15f), 0.2f);

        Material frostedFoliageMat =
            CreateLitMaterial(new Color(0.18f, 0.42f, 0.45f), 0.25f);

        Vector3[] treePositions = new Vector3[]
        {
            new Vector3(-9.5f, 0f, -4.5f),
            new Vector3(-10.5f, 0.4f, 0.5f),
            new Vector3(-8.8f, 0.3f, 4.2f),
            new Vector3(-11.2f, 1.0f, 8.5f),
            new Vector3(-4.5f, 1.2f, 15.5f),
            new Vector3(1.5f, 1.4f, 17.0f),
            new Vector3(4.8f, 1.1f, 15.8f),
            new Vector3(8.5f, 0.2f, 9.5f),
            new Vector3(10.2f, 0.8f, 5.0f),
            new Vector3(9.0f, 0.3f, 0.0f),
            new Vector3(8.2f, 0.1f, -7.0f)
        };

        for (int i = 0; i < treePositions.Length; i++)
        {
            CreateFrostedPineTree(
                $"FrostedPine_{i + 1}",
                treePositions[i],
                0.9f + (i % 3) * 0.2f,
                trunkMat,
                frostedFoliageMat,
                snowMat
            );
        }

        Material slateMat =
            CreateLitMaterial(new Color(0.48f, 0.58f, 0.65f), 0.2f);

        Vector3[] rockPositions = new Vector3[]
        {
            new Vector3(-6.2f, 0.1f, 7.5f),
            new Vector3(-6.8f, 0.2f, 9.2f),
            new Vector3(5.8f, 0.1f, 6.2f),
            new Vector3(6.5f, 0.2f, 8.8f),
            new Vector3(-5.8f, 0.1f, -5.5f),
            new Vector3(5.5f, 0.1f, -6.2f)
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            CreateFacetedRock(
                $"SnowRock_{i + 1}",
                rockPositions[i],
                Vector3.one * (1.2f + (i % 3) * 0.35f),
                slateMat
            );
        }

        BuildSnowRampJump(
            new Vector3(3.15f, 0.0f, 5.0f),
            snowMat
        );
    }

    // ---------------------------------------------------------
    // COURSE 04: JADE TEMPLE GARDEN
    // ---------------------------------------------------------

    private void BuildJadeTemple()
    {
        GameObject pond =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        pond.name = "ZenKoiPond";
        pond.transform.SetParent(envRoot, false);
        pond.transform.localPosition =
            new Vector3(-13.5f, -0.15f, 2.0f);

        pond.transform.localScale =
            new Vector3(1.2f, 1f, 2.8f);

        DestroyImmediate(pond.GetComponent<Collider>());

        pond.GetComponent<MeshRenderer>().sharedMaterial =
            CreateLitMaterial(
                new Color(0.12f, 0.62f, 0.62f),
                0.92f
            );

        Material mossMat =
            CreateLitMaterial(new Color(0.18f, 0.52f, 0.22f), 0.18f);

        CreateHill(
            "ZenHill_Left",
            new Vector3(-16f, -1.2f, 8f),
            new Vector3(14f, 4.5f, 18f),
            mossMat
        );

        CreateHill(
            "ZenHill_Back",
            new Vector3(0f, -1.5f, 22f),
            new Vector3(28f, 5.5f, 16f),
            mossMat
        );

        CreateHill(
            "ZenHill_Right",
            new Vector3(15f, -1.2f, 5f),
            new Vector3(15f, 4.2f, 18f),
            mossMat
        );

        Material cherryTrunkMat =
            CreateLitMaterial(new Color(0.32f, 0.20f, 0.14f), 0.2f);

        Material blossomMat =
            CreateLitMaterial(new Color(0.96f, 0.58f, 0.72f), 0.25f);

        Material jadeFoliageMat =
            CreateLitMaterial(new Color(0.10f, 0.42f, 0.20f), 0.25f);

        Vector3[] blossomPositions = new Vector3[]
        {
            new Vector3(-9.5f, 0f, -4.0f),
            new Vector3(-10.5f, 0.4f, 1.5f),
            new Vector3(-11.2f, 1.0f, 8.5f),
            new Vector3(-4.5f, 1.2f, 15.5f),
            new Vector3(1.5f, 1.4f, 17.0f),
            new Vector3(8.5f, 0.2f, 9.5f),
            new Vector3(10.2f, 0.8f, 5.0f),
            new Vector3(8.2f, 0.1f, -7.0f)
        };

        for (int i = 0; i < blossomPositions.Length; i++)
        {
            bool isPink = (i % 2 == 0);

            CreateCherryBlossomTree(
                $"ZenTree_{i + 1}",
                blossomPositions[i],
                0.95f + (i % 3) * 0.2f,
                cherryTrunkMat,
                isPink ? blossomMat : jadeFoliageMat
            );
        }

        Material stoneMat =
            CreateLitMaterial(new Color(0.45f, 0.50f, 0.48f), 0.18f);

        Vector3[] rockPositions = new Vector3[]
        {
            new Vector3(-6.2f, 0.1f, 7.5f),
            new Vector3(5.8f, 0.1f, 6.2f),
            new Vector3(-5.8f, 0.1f, -5.5f),
            new Vector3(5.5f, 0.1f, -6.2f)
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            CreateFacetedRock(
                $"ZenStone_{i + 1}",
                rockPositions[i],
                Vector3.one * (1.1f + (i % 2) * 0.3f),
                stoneMat
            );
        }

        BuildZenBridge(new Vector3(4.6f, 0f, 2.0f));
    }

    // ---------------------------------------------------------
    // COURSE 05: AZURE COAST
    // ---------------------------------------------------------

    private void BuildAzureCoast()
    {
        GameObject ocean =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        ocean.name = "TropicalOcean";
        ocean.transform.SetParent(envRoot, false);
        ocean.transform.localPosition =
            new Vector3(-14.5f, -0.2f, 2.0f);

        ocean.transform.localScale =
            new Vector3(1.5f, 1f, 3.2f);

        DestroyImmediate(ocean.GetComponent<Collider>());

        ocean.GetComponent<MeshRenderer>().sharedMaterial =
            CreateLitMaterial(
                new Color(0.08f, 0.74f, 0.84f),
                0.95f
            );

        Material sandMat =
            CreateLitMaterial(new Color(0.92f, 0.80f, 0.55f), 0.15f);

        CreateHill(
            "Dune_Left",
            new Vector3(-16f, -1.2f, 8f),
            new Vector3(14f, 4.0f, 18f),
            sandMat
        );

        CreateHill(
            "Dune_Back",
            new Vector3(0f, -1.5f, 22f),
            new Vector3(28f, 5.5f, 16f),
            sandMat
        );

        CreateHill(
            "Dune_Right",
            new Vector3(15f, -1.2f, 5f),
            new Vector3(15f, 4.0f, 18f),
            sandMat
        );

        Material palmTrunkMat =
            CreateLitMaterial(new Color(0.48f, 0.32f, 0.18f), 0.2f);

        Material frondMat =
            CreateLitMaterial(new Color(0.18f, 0.65f, 0.22f), 0.25f);

        Vector3[] palmPositions = new Vector3[]
        {
            new Vector3(-9.5f, 0f, -4.5f),
            new Vector3(-10.5f, 0.4f, 0.5f),
            new Vector3(-8.8f, 0.3f, 4.2f),
            new Vector3(-11.2f, 1.0f, 8.5f),
            new Vector3(-4.5f, 1.2f, 15.5f),
            new Vector3(1.5f, 1.4f, 17.0f),
            new Vector3(4.8f, 1.1f, 15.8f),
            new Vector3(8.5f, 0.2f, 9.5f),
            new Vector3(10.2f, 0.8f, 5.0f),
            new Vector3(9.0f, 0.3f, 0.0f),
            new Vector3(8.2f, 0.1f, -7.0f)
        };

        for (int i = 0; i < palmPositions.Length; i++)
        {
            CreatePalmTree(
                $"PalmTree_{i + 1}",
                palmPositions[i],
                1.0f + (i % 3) * 0.25f,
                palmTrunkMat,
                frondMat
            );
        }

        Material seaRockMat =
            CreateLitMaterial(new Color(0.55f, 0.48f, 0.42f), 0.18f);

        Vector3[] rockPositions = new Vector3[]
        {
            new Vector3(-6.2f, 0.1f, 7.5f),
            new Vector3(5.8f, 0.1f, 6.2f),
            new Vector3(-5.8f, 0.1f, -5.5f),
            new Vector3(5.5f, 0.1f, -6.2f)
        };

        for (int i = 0; i < rockPositions.Length; i++)
        {
            CreateFacetedRock(
                $"SeaRock_{i + 1}",
                rockPositions[i],
                Vector3.one * (1.2f + (i % 2) * 0.35f),
                seaRockMat
            );
        }

        BuildBoardwalkJump(
            new Vector3(3.2f, 0f, 4.8f),
            sandMat
        );
    }

    // ---------------------------------------------------------
    // COURSES 06-10: NEW ENVIRONMENTS
    // ---------------------------------------------------------

    private void BuildVolcanicEmber()
    {
        Material basalt =
            CreateLitMaterial(new Color(0.16f, 0.13f, 0.14f), 0.28f, 0.12f);

        Material lava =
            CreateLitMaterial(new Color(1f, 0.24f, 0.035f), 0.72f, 0.05f);

        Material ash =
            CreateLitMaterial(new Color(0.34f, 0.29f, 0.27f), 0.18f);

        CreateHill(
            "Volcano_Left",
            new Vector3(-16f, -1.4f, 8f),
            new Vector3(14f, 5f, 18f),
            basalt
        );

        CreateHill(
            "Volcano_Back",
            new Vector3(0f, -1.4f, 23f),
            new Vector3(28f, 8f, 16f),
            ash
        );

        CreateHill(
            "Volcano_Right",
            new Vector3(16f, -1.3f, 6f),
            new Vector3(15f, 5.5f, 19f),
            basalt
        );

        CreateRouteBlock(
            "LavaChannel_Left",
            new Vector3(-8f, -0.02f, 2f),
            new Vector3(3.4f, 0.06f, 20f),
            0f,
            lava
        );

        CreateRouteBlock(
            "LavaChannel_Right",
            new Vector3(8f, -0.02f, 2f),
            new Vector3(3.4f, 0.06f, 20f),
            0f,
            lava
        );

        Vector3[] rocks =
        {
            new Vector3(-6.4f, 0.15f, 6.5f),
            new Vector3(6.5f, 0.15f, 8.0f),
            new Vector3(-6.0f, 0.15f, -5.5f),
            new Vector3(6.2f, 0.15f, -5.8f)
        };

        for (int i = 0; i < rocks.Length; i++)
        {
            CreateFacetedRock(
                $"BasaltBoulder_{i + 1}",
                rocks[i],
                Vector3.one * 1.35f,
                basalt
            );
        }
    }

    private void BuildMoonlitCrater()
    {
        Material moonStone =
            CreateLitMaterial(new Color(0.39f, 0.43f, 0.49f), 0.3f, 0.08f);

        Material craterDark =
            CreateLitMaterial(new Color(0.19f, 0.23f, 0.29f), 0.22f);

        Material lunarBlue =
            CreateLitMaterial(new Color(0.22f, 0.52f, 0.72f), 0.72f, 0.15f);

        CreateHill(
            "LunarCrater_Left",
            new Vector3(-16f, -1.2f, 8f),
            new Vector3(14f, 5f, 18f),
            moonStone
        );

        CreateHill(
            "LunarCrater_Back",
            new Vector3(0f, -1.5f, 23f),
            new Vector3(28f, 7f, 17f),
            craterDark
        );

        CreateHill(
            "LunarCrater_Right",
            new Vector3(16f, -1.2f, 7f),
            new Vector3(15f, 5f, 18f),
            moonStone
        );

        CreateFacetedRock(
            "MoonRock_Left",
            new Vector3(-6.4f, 0.1f, 8.5f),
            new Vector3(1.6f, 1.25f, 1.5f),
            moonStone
        );

        CreateFacetedRock(
            "MoonRock_Right",
            new Vector3(6.3f, 0.1f, 4.5f),
            new Vector3(1.7f, 1.2f, 1.6f),
            moonStone
        );

        CreateFacetedRock(
            "MoonRock_Back",
            new Vector3(-5.7f, 0.1f, -5.5f),
            new Vector3(1.4f, 1.1f, 1.4f),
            craterDark
        );

        CreateRouteBlock(
            "LunarEnergyGate_Left",
            new Vector3(-3.9f, 0.45f, 1.0f),
            new Vector3(0.34f, 0.9f, 2.6f),
            12f,
            lunarBlue
        );

        CreateRouteBlock(
            "LunarEnergyGate_Right",
            new Vector3(3.9f, 0.45f, -2.4f),
            new Vector3(0.34f, 0.9f, 2.6f),
            -12f,
            lunarBlue
        );
    }

    private void BuildRedwoodRapids()
    {
        Material redwood =
            CreateLitMaterial(new Color(0.36f, 0.16f, 0.10f), 0.25f);

        Material foliage =
            CreateLitMaterial(new Color(0.08f, 0.34f, 0.18f), 0.2f);

        Material river =
            CreateLitMaterial(new Color(0.05f, 0.55f, 0.72f), 0.88f, 0.05f);

        Material riverStone =
            CreateLitMaterial(new Color(0.37f, 0.43f, 0.39f), 0.25f);

        CreateHill(
            "RedwoodBank_Left",
            new Vector3(-16f, -1.2f, 8f),
            new Vector3(14f, 5f, 18f),
            foliage
        );

        CreateHill(
            "RedwoodBank_Back",
            new Vector3(0f, -1.5f, 23f),
            new Vector3(28f, 7f, 16f),
            redwood
        );

        CreateHill(
            "RedwoodBank_Right",
            new Vector3(16f, -1.2f, 6f),
            new Vector3(15f, 5f, 18f),
            foliage
        );

        GameObject riverPlane =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        riverPlane.name = "RedwoodRapids";
        riverPlane.transform.SetParent(envRoot, false);
        riverPlane.transform.localPosition =
            new Vector3(-13.5f, -0.10f, 2f);

        riverPlane.transform.localScale =
            new Vector3(1.15f, 1f, 2.8f);

        DestroyImmediate(riverPlane.GetComponent<Collider>());

        riverPlane.GetComponent<MeshRenderer>().sharedMaterial =
            river;

        Vector3[] treePositions =
        {
            new Vector3(-9.5f, 0f, -4.5f),
            new Vector3(-10.5f, 0.3f, 2f),
            new Vector3(-8.8f, 0.2f, 7f),
            new Vector3(9.2f, 0.2f, 8f),
            new Vector3(10.1f, 0.4f, 2f),
            new Vector3(8.5f, 0f, -6f)
        };

        for (int i = 0; i < treePositions.Length; i++)
        {
            CreateLowPolyPineTree(
                $"RedwoodTree_{i + 1}",
                treePositions[i],
                1.25f + (i % 2) * 0.2f,
                redwood,
                foliage
            );
        }

        CreateFacetedRock(
            "RapidsRock_A",
            new Vector3(-6.2f, 0.1f, 7f),
            Vector3.one * 1.4f,
            riverStone
        );

        CreateFacetedRock(
            "RapidsRock_B",
            new Vector3(6.0f, 0.1f, 5f),
            Vector3.one * 1.5f,
            riverStone
        );
    }

    private void BuildSkyHarbor()
    {
        Material cloud =
            CreateLitMaterial(new Color(0.86f, 0.94f, 1f), 0.55f);

        Material skyStone =
            CreateLitMaterial(new Color(0.40f, 0.60f, 0.72f), 0.4f, 0.1f);

        Material gold =
            CreateLitMaterial(new Color(0.92f, 0.67f, 0.22f), 0.58f, 0.28f);

        CreateHill(
            "SkyIsland_Left",
            new Vector3(-16f, -0.8f, 8f),
            new Vector3(14f, 4.5f, 18f),
            skyStone
        );

        CreateHill(
            "SkyIsland_Back",
            new Vector3(0f, -1.0f, 23f),
            new Vector3(28f, 6f, 16f),
            cloud
        );

        CreateHill(
            "SkyIsland_Right",
            new Vector3(16f, -0.8f, 6f),
            new Vector3(15f, 4.5f, 18f),
            skyStone
        );

        CreateHill(
            "CloudBank_Left",
            new Vector3(-12f, 0.1f, -2f),
            new Vector3(5f, 1.8f, 5f),
            cloud
        );

        CreateHill(
            "CloudBank_Right",
            new Vector3(12f, 0.1f, 10f),
            new Vector3(5f, 1.8f, 5f),
            cloud
        );

        CreateRouteBlock(
            "SkyHarbor_GoldBridge_A",
            new Vector3(-2.0f, 0.32f, -3.0f),
            new Vector3(4.8f, 0.55f, 0.28f),
            22f,
            gold
        );

        CreateRouteBlock(
            "SkyHarbor_GoldBridge_B",
            new Vector3(2.0f, 0.32f, 1.0f),
            new Vector3(4.8f, 0.55f, 0.28f),
            -22f,
            gold
        );

        CreateRouteBlock(
            "SkyHarbor_Pillar_Left",
            new Vector3(-5.8f, 0.55f, 5.5f),
            new Vector3(0.7f, 1.1f, 0.7f),
            0f,
            skyStone
        );

        CreateRouteBlock(
            "SkyHarbor_Pillar_Right",
            new Vector3(5.8f, 0.55f, -5.5f),
            new Vector3(0.7f, 1.1f, 0.7f),
            0f,
            skyStone
        );
    }

    private void BuildCrystalCaverns()
    {
        Material cavern =
            CreateLitMaterial(new Color(0.19f, 0.15f, 0.30f), 0.38f, 0.16f);

        Material amethyst =
            CreateLitMaterial(new Color(0.48f, 0.24f, 0.76f), 0.65f, 0.2f);

        Material cyanCrystal =
            CreateLitMaterial(new Color(0.10f, 0.68f, 0.80f), 0.75f, 0.18f);

        CreateHill(
            "CrystalCavern_Left",
            new Vector3(-16f, -1.2f, 8f),
            new Vector3(14f, 5f, 18f),
            cavern
        );

        CreateHill(
            "CrystalCavern_Back",
            new Vector3(0f, -1.5f, 23f),
            new Vector3(28f, 7f, 16f),
            cavern
        );

        CreateHill(
            "CrystalCavern_Right",
            new Vector3(16f, -1.2f, 6f),
            new Vector3(15f, 5f, 18f),
            cavern
        );

        Vector3[] crystalPositions =
        {
            new Vector3(-6.1f, 0.8f, 7.0f),
            new Vector3(6.0f, 0.9f, 5.0f),
            new Vector3(-5.8f, 0.7f, -5.4f),
            new Vector3(5.8f, 0.8f, -6.0f),
            new Vector3(-6.2f, 0.7f, 1.0f),
            new Vector3(6.2f, 0.7f, -1.0f)
        };

        for (int i = 0; i < crystalPositions.Length; i++)
        {
            GameObject crystal =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            crystal.name = $"CavernCrystal_{i + 1}";
            crystal.transform.SetParent(envRoot, false);
            crystal.transform.localPosition = crystalPositions[i];

            crystal.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    i * 27f,
                    8f * (i % 2 == 0 ? 1f : -1f)
                );

            crystal.transform.localScale =
                new Vector3(
                    0.65f,
                    1.25f + (i % 3) * 0.3f,
                    0.65f
                );

            crystal.GetComponent<MeshRenderer>().sharedMaterial =
                (i % 2 == 0) ? amethyst : cyanCrystal;
        }
    }

    // ---------------------------------------------------------
    // DISTINCT PLAYABLE COURSE LAYOUTS
    // ---------------------------------------------------------

    private void BuildDistinctPlayableLayout(int courseId)
    {
        switch (courseId)
        {
            case 1:
                BuildForestLogChicane();
                break;
            case 2:
                BuildCopperSwitchbacks();
                break;
            case 3:
                BuildGlacierSlalom();
                break;
            case 4:
                BuildJadeGardenGates();
                break;
            case 5:
                BuildAzureBoardwalkRoute();
                break;
            case 6:
                BuildVolcanicSwitchbacks();
                break;
            case 7:
                BuildLunarCraterRoute();
                break;
            case 8:
                BuildRedwoodRapidsRoute();
                break;
            case 9:
                BuildSkyHarborRoute();
                break;
            case 10:
                BuildCrystalCavernRoute();
                break;
        }
    }

    private GameObject CreateRouteBlock(
        string objectName,
        Vector3 position,
        Vector3 size,
        float yaw,
        Material material)
    {
        GameObject block =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        block.name = objectName;
        block.transform.SetParent(envRoot, false);
        block.transform.localPosition = position;
        block.transform.localRotation =
            Quaternion.Euler(0f, yaw, 0f);

        block.transform.localScale = size;

        MeshRenderer renderer =
            block.GetComponent<MeshRenderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider =
            block.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }

        return block;
    }

    private void BuildForestLogChicane()
    {
        Material logMat =
            CreateLitMaterial(new Color(0.39f, 0.20f, 0.09f), 0.22f);

        CreateRouteBlock(
            "Forest_FallenLog_A",
            new Vector3(-2.25f, 0.31f, -3.65f),
            new Vector3(4.0f, 0.62f, 0.25f),
            24f,
            logMat
        );

        CreateRouteBlock(
            "Forest_FallenLog_B",
            new Vector3(2.35f, 0.31f, 0.15f),
            new Vector3(4.0f, 0.62f, 0.25f),
            -24f,
            logMat
        );
    }

    private void BuildCopperSwitchbacks()
    {
        Material canyonWallMat =
            CreateLitMaterial(new Color(0.65f, 0.29f, 0.14f), 0.16f);

        CreateRouteBlock(
            "Canyon_Switchback_Wall_A",
            new Vector3(-1.45f, 0.34f, -4.25f),
            new Vector3(8.8f, 0.68f, 0.32f),
            0f,
            canyonWallMat
        );

        CreateRouteBlock(
            "Canyon_Switchback_Wall_B",
            new Vector3(1.45f, 0.34f, -0.25f),
            new Vector3(8.8f, 0.68f, 0.32f),
            0f,
            canyonWallMat
        );

        CreateRouteBlock(
            "Canyon_Switchback_Wall_C",
            new Vector3(-1.45f, 0.34f, 3.75f),
            new Vector3(8.8f, 0.68f, 0.32f),
            0f,
            canyonWallMat
        );

        CreateRouteBlock(
            "Canyon_RockShoulder_Left",
            new Vector3(-5.2f, 0.42f, 0.9f),
            new Vector3(1.0f, 0.84f, 1.3f),
            -14f,
            canyonWallMat
        );

        CreateRouteBlock(
            "Canyon_RockShoulder_Right",
            new Vector3(5.15f, 0.42f, -2.1f),
            new Vector3(1.0f, 0.84f, 1.3f),
            16f,
            canyonWallMat
        );
    }

    private void BuildGlacierSlalom()
    {
        Material iceBankMat =
            CreateLitMaterial(new Color(0.48f, 0.78f, 0.91f), 0.72f, 0.08f);

        CreateRouteBlock(
            "Glacier_IceBank_A",
            new Vector3(-2.35f, 0.32f, -3.55f),
            new Vector3(4.9f, 0.64f, 0.28f),
            38f,
            iceBankMat
        );

        CreateRouteBlock(
            "Glacier_IceBank_B",
            new Vector3(2.25f, 0.32f, -0.05f),
            new Vector3(4.6f, 0.64f, 0.28f),
            -42f,
            iceBankMat
        );

        CreateRouteBlock(
            "Glacier_IceBank_C",
            new Vector3(-1.65f, 0.32f, 3.65f),
            new Vector3(4.2f, 0.64f, 0.28f),
            31f,
            iceBankMat
        );

        Material iceRockMat =
            CreateLitMaterial(new Color(0.63f, 0.79f, 0.86f), 0.32f);

        CreateRouteBlock(
            "Glacier_RockBumper_Left",
            new Vector3(-5.9f, 0.36f, 5.9f),
            new Vector3(0.8f, 0.72f, 1.0f),
            28f,
            iceRockMat
        );

        CreateRouteBlock(
            "Glacier_RockBumper_Right",
            new Vector3(5.9f, 0.36f, -6.8f),
            new Vector3(0.8f, 0.72f, 1.0f),
            -24f,
            iceRockMat
        );
    }

    private void BuildJadeGardenGates()
    {
        Material jadeStoneMat =
            CreateLitMaterial(new Color(0.36f, 0.43f, 0.34f), 0.24f);

        BuildGardenGate(
            "Jade_ToriiGate_A",
            1.35f,
            -4.15f,
            jadeStoneMat
        );

        BuildGardenGate(
            "Jade_ToriiGate_B",
            -1.35f,
            -0.15f,
            jadeStoneMat
        );

        BuildGardenGate(
            "Jade_ToriiGate_C",
            0.95f,
            3.85f,
            jadeStoneMat
        );

        CreateDecorativeStone(
            "Jade_SteppingStone_Left",
            new Vector3(-4.9f, 0.08f, 0.1f),
            new Vector3(1.0f, 0.16f, 0.7f),
            jadeStoneMat
        );

        CreateDecorativeStone(
            "Jade_SteppingStone_Right",
            new Vector3(4.8f, 0.08f, 4.9f),
            new Vector3(0.9f, 0.16f, 0.65f),
            jadeStoneMat
        );
    }

    private void BuildGardenGate(
        string name,
        float centreX,
        float z,
        Material material)
    {
        const float halfGap = 1.45f;

        CreateRouteBlock(
            name + "_Post_L",
            new Vector3(centreX - halfGap, 0.66f, z),
            new Vector3(0.36f, 1.32f, 0.42f),
            0f,
            material
        );

        CreateRouteBlock(
            name + "_Post_R",
            new Vector3(centreX + halfGap, 0.66f, z),
            new Vector3(0.36f, 1.32f, 0.42f),
            0f,
            material
        );

        CreateRouteBlock(
            name + "_Lintel",
            new Vector3(centreX, 1.42f, z),
            new Vector3(3.35f, 0.18f, 0.42f),
            0f,
            material
        );
    }

    private void CreateDecorativeStone(
        string name,
        Vector3 position,
        Vector3 size,
        Material material)
    {
        GameObject stone =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        stone.name = name;
        stone.transform.SetParent(envRoot, false);
        stone.transform.localPosition = position;
        stone.transform.localScale = size;

        Collider collider = stone.GetComponent<Collider>();

        if (collider != null)
            DestroyImmediate(collider);

        MeshRenderer renderer =
            stone.GetComponent<MeshRenderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void BuildAzureBoardwalkRoute()
    {
        Material boardwalkRailMat =
            CreateLitMaterial(new Color(0.50f, 0.29f, 0.13f), 0.28f);

        BuildBoardwalkRailSection(
            "Azure_Pier_A",
            new Vector3(-1.25f, 0.30f, -4.15f),
            3.45f,
            2.8f,
            -12f,
            boardwalkRailMat
        );

        BuildBoardwalkRailSection(
            "Azure_Pier_B",
            new Vector3(1.25f, 0.30f, -0.15f),
            3.45f,
            2.8f,
            12f,
            boardwalkRailMat
        );

        BuildBoardwalkRailSection(
            "Azure_Pier_C",
            new Vector3(-0.80f, 0.30f, 3.85f),
            3.45f,
            2.8f,
            -10f,
            boardwalkRailMat
        );

        CreateRouteBlock(
            "Azure_DockBumper_A",
            new Vector3(-3.2f, 0.24f, -1.95f),
            new Vector3(1.4f, 0.48f, 0.24f),
            18f,
            boardwalkRailMat
        );

        CreateRouteBlock(
            "Azure_DockBumper_B",
            new Vector3(3.2f, 0.24f, 2.0f),
            new Vector3(1.4f, 0.48f, 0.24f),
            -18f,
            boardwalkRailMat
        );
    }

    private void BuildBoardwalkRailSection(
        string name,
        Vector3 centre,
        float railLength,
        float laneWidth,
        float yaw,
        Material material)
    {
        Quaternion rotation =
            Quaternion.Euler(0f, yaw, 0f);

        Vector3 lateral =
            rotation * Vector3.right;

        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 position =
                centre + lateral * (laneWidth * 0.5f * side);

            position.y = 0.30f;

            CreateRouteBlock(
                name + (side < 0 ? "_Rail_L" : "_Rail_R"),
                position,
                new Vector3(0.20f, 0.60f, railLength),
                yaw,
                material
            );
        }
    }

    // Course 06: alternating basalt barriers create a medium switchback.
    private void BuildVolcanicSwitchbacks()
    {
        Material basalt =
            CreateLitMaterial(new Color(0.18f, 0.14f, 0.15f), 0.25f, 0.12f);

        Material ember =
            CreateLitMaterial(new Color(0.92f, 0.24f, 0.04f), 0.58f, 0.08f);

        CreateRouteBlock(
            "Ember_BasaltWall_A",
            new Vector3(-1.5f, 0.34f, -4.0f),
            new Vector3(8.0f, 0.68f, 0.32f),
            0f,
            basalt
        );

        CreateRouteBlock(
            "Ember_BasaltWall_B",
            new Vector3(1.5f, 0.34f, 0.0f),
            new Vector3(8.0f, 0.68f, 0.32f),
            0f,
            basalt
        );

        CreateRouteBlock(
            "Ember_LavaDivider",
            new Vector3(-1.5f, 0.18f, 4.0f),
            new Vector3(3.2f, 0.36f, 0.28f),
            0f,
            ember
        );
    }

    // Course 07: a slalom around broad low crater bumpers.
    private void BuildLunarCraterRoute()
    {
        Material moon =
            CreateLitMaterial(new Color(0.42f, 0.47f, 0.55f), 0.32f, 0.08f);

        CreateRouteBlock(
            "LunarCraterRim_A",
            new Vector3(-2.2f, 0.28f, -3.5f),
            new Vector3(3.6f, 0.56f, 0.32f),
            28f,
            moon
        );

        CreateRouteBlock(
            "LunarCraterRim_B",
            new Vector3(2.0f, 0.28f, 0.0f),
            new Vector3(3.6f, 0.56f, 0.32f),
            -28f,
            moon
        );

        CreateRouteBlock(
            "LunarCraterRim_C",
            new Vector3(-1.5f, 0.28f, 3.7f),
            new Vector3(3.2f, 0.56f, 0.32f),
            24f,
            moon
        );
    }

    // Course 08: staggered fallen redwood trunks and a stone bumper.
    private void BuildRedwoodRapidsRoute()
    {
        Material trunk =
            CreateLitMaterial(new Color(0.37f, 0.17f, 0.09f), 0.22f);

        Material stone =
            CreateLitMaterial(new Color(0.42f, 0.46f, 0.43f), 0.18f);

        CreateRouteBlock(
            "Redwood_FallenTrunk_A",
            new Vector3(-2.0f, 0.28f, -3.8f),
            new Vector3(4.1f, 0.56f, 0.28f),
            25f,
            trunk
        );

        CreateRouteBlock(
            "Redwood_FallenTrunk_B",
            new Vector3(2.0f, 0.28f, 0.1f),
            new Vector3(4.1f, 0.56f, 0.28f),
            -25f,
            trunk
        );

        CreateRouteBlock(
            "Redwood_RiverStone",
            new Vector3(-3.6f, 0.32f, 4.6f),
            new Vector3(1.1f, 0.64f, 1.1f),
            18f,
            stone
        );
    }

    // Course 09: paired sky bridges form alternating lanes.
    private void BuildSkyHarborRoute()
    {
        Material gold =
            CreateLitMaterial(new Color(0.88f, 0.65f, 0.24f), 0.55f, 0.22f);

        Material sky =
            CreateLitMaterial(new Color(0.38f, 0.65f, 0.79f), 0.5f, 0.12f);

        BuildBoardwalkRailSection(
            "SkyBridge_A",
            new Vector3(-1.0f, 0.30f, -3.7f),
            3.3f,
            2.9f,
            -14f,
            gold
        );

        BuildBoardwalkRailSection(
            "SkyBridge_B",
            new Vector3(1.0f, 0.30f, 0.2f),
            3.3f,
            2.9f,
            14f,
            sky
        );

        CreateRouteBlock(
            "SkyBridge_CrossBumper",
            new Vector3(-2.5f, 0.28f, 4.2f),
            new Vector3(2.1f, 0.56f, 0.28f),
            -18f,
            gold
        );
    }

    // Course 10: staggered crystal banks create a medium-difficulty route.
    private void BuildCrystalCavernRoute()
    {
        Material purple =
            CreateLitMaterial(new Color(0.46f, 0.24f, 0.69f), 0.62f, 0.16f);

        Material cyan =
            CreateLitMaterial(new Color(0.10f, 0.63f, 0.75f), 0.68f, 0.16f);

        CreateRouteBlock(
            "CrystalBank_A",
            new Vector3(-2.0f, 0.34f, -4.0f),
            new Vector3(3.8f, 0.68f, 0.34f),
            32f,
            purple
        );

        CreateRouteBlock(
            "CrystalBank_B",
            new Vector3(2.0f, 0.34f, -0.2f),
            new Vector3(3.8f, 0.68f, 0.34f),
            -32f,
            cyan
        );

        CreateRouteBlock(
            "CrystalBank_C",
            new Vector3(-1.5f, 0.34f, 3.7f),
            new Vector3(3.3f, 0.68f, 0.34f),
            27f,
            purple
        );
    }

    // ---------------------------------------------------------
    // SHARED PROCEDURAL HELPERS
    // ---------------------------------------------------------

    private void CreateHill(
        string name,
        Vector3 pos,
        Vector3 scale,
        Material mat)
    {
        GameObject hill =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        hill.name = name;
        hill.transform.SetParent(envRoot, false);
        hill.transform.localPosition = pos;
        hill.transform.localScale = scale;

        Collider collider = hill.GetComponent<Collider>();

        if (collider != null)
            DestroyImmediate(collider);

        hill.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private void CreateLowPolyPineTree(
        string name,
        Vector3 pos,
        float scale,
        Material trunkMat,
        Material foliageMat)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(envRoot, false);
        tree.transform.localPosition = pos;
        tree.transform.localScale = Vector3.one * scale;

        GameObject trunk =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);

        trunk.transform.localPosition =
            new Vector3(0f, 0.75f, 0f);

        trunk.transform.localScale =
            new Vector3(0.24f, 0.75f, 0.24f);

        DestroyImmediate(trunk.GetComponent<Collider>());

        trunk.GetComponent<MeshRenderer>().sharedMaterial =
            trunkMat;

        int tiers = 3;

        for (int t = 0; t < tiers; t++)
        {
            GameObject tier =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            tier.name = $"FoliageTier_{t + 1}";
            tier.transform.SetParent(tree.transform, false);

            float y = 1.25f + t * 0.75f;

            tier.transform.localPosition =
                new Vector3(0f, y, 0f);

            float radius = 1.35f - t * 0.32f;

            tier.transform.localScale =
                new Vector3(radius, 0.45f, radius);

            DestroyImmediate(tier.GetComponent<Collider>());

            tier.GetComponent<MeshRenderer>().sharedMaterial =
                foliageMat;
        }
    }

    private void CreateFrostedPineTree(
        string name,
        Vector3 pos,
        float scale,
        Material trunkMat,
        Material foliageMat,
        Material snowMat)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(envRoot, false);
        tree.transform.localPosition = pos;
        tree.transform.localScale = Vector3.one * scale;

        GameObject trunk =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);

        trunk.transform.localPosition =
            new Vector3(0f, 0.75f, 0f);

        trunk.transform.localScale =
            new Vector3(0.24f, 0.75f, 0.24f);

        DestroyImmediate(trunk.GetComponent<Collider>());

        trunk.GetComponent<MeshRenderer>().sharedMaterial =
            trunkMat;

        int tiers = 3;

        for (int t = 0; t < tiers; t++)
        {
            GameObject tier =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            tier.name = $"FrostedTier_{t + 1}";
            tier.transform.SetParent(tree.transform, false);

            float y = 1.25f + t * 0.75f;

            tier.transform.localPosition =
                new Vector3(0f, y, 0f);

            float radius = 1.35f - t * 0.32f;

            tier.transform.localScale =
                new Vector3(radius, 0.45f, radius);

            DestroyImmediate(tier.GetComponent<Collider>());

            tier.GetComponent<MeshRenderer>().sharedMaterial =
                foliageMat;

            GameObject snowCap =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            snowCap.name = $"SnowCap_{t + 1}";
            snowCap.transform.SetParent(tier.transform, false);

            snowCap.transform.localPosition =
                new Vector3(0f, 0.5f, 0f);

            snowCap.transform.localScale =
                new Vector3(0.92f, 0.2f, 0.92f);

            DestroyImmediate(snowCap.GetComponent<Collider>());

            snowCap.GetComponent<MeshRenderer>().sharedMaterial =
                snowMat;
        }
    }

    private void CreateSaguaroCactus(
        string name,
        Vector3 pos,
        float scale,
        Material mat)
    {
        GameObject cactus = new GameObject(name);
        cactus.transform.SetParent(envRoot, false);
        cactus.transform.localPosition = pos;
        cactus.transform.localScale = Vector3.one * scale;

        GameObject trunk =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        trunk.name = "CactusTrunk";
        trunk.transform.SetParent(cactus.transform, false);

        trunk.transform.localPosition =
            new Vector3(0f, 1.25f, 0f);

        trunk.transform.localScale =
            new Vector3(0.35f, 1.25f, 0.35f);

        DestroyImmediate(trunk.GetComponent<Collider>());

        trunk.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject armL =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        armL.name = "ArmL";
        armL.transform.SetParent(cactus.transform, false);

        armL.transform.localPosition =
            new Vector3(-0.45f, 1.35f, 0f);

        armL.transform.localRotation =
            Quaternion.Euler(0f, 0f, 90f);

        armL.transform.localScale =
            new Vector3(0.22f, 0.35f, 0.22f);

        DestroyImmediate(armL.GetComponent<Collider>());

        armL.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject armLUp =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        armLUp.name = "ArmLUp";
        armLUp.transform.SetParent(cactus.transform, false);

        armLUp.transform.localPosition =
            new Vector3(-0.75f, 1.75f, 0f);

        armLUp.transform.localScale =
            new Vector3(0.22f, 0.45f, 0.22f);

        DestroyImmediate(armLUp.GetComponent<Collider>());

        armLUp.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private void CreateCherryBlossomTree(
        string name,
        Vector3 pos,
        float scale,
        Material trunkMat,
        Material foliageMat)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(envRoot, false);
        tree.transform.localPosition = pos;
        tree.transform.localScale = Vector3.one * scale;

        GameObject trunk =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        trunk.name = "BonsaiTrunk";
        trunk.transform.SetParent(tree.transform, false);

        trunk.transform.localPosition =
            new Vector3(0f, 0.85f, 0f);

        trunk.transform.localRotation =
            Quaternion.Euler(0f, 0f, 10f);

        trunk.transform.localScale =
            new Vector3(0.26f, 0.85f, 0.26f);

        DestroyImmediate(trunk.GetComponent<Collider>());

        trunk.GetComponent<MeshRenderer>().sharedMaterial =
            trunkMat;

        for (int i = 0; i < 3; i++)
        {
            GameObject puff =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            puff.name = $"BlossomPuff_{i + 1}";
            puff.transform.SetParent(tree.transform, false);

            puff.transform.localPosition =
                new Vector3(
                    (i - 1) * 0.45f,
                    1.75f + (i % 2) * 0.35f,
                    (i % 2 == 0 ? 0.2f : -0.2f)
                );

            puff.transform.localScale =
                Vector3.one * (1.1f + (i % 2) * 0.25f);

            DestroyImmediate(puff.GetComponent<Collider>());

            puff.GetComponent<MeshRenderer>().sharedMaterial =
                foliageMat;
        }
    }

    private void CreatePalmTree(
        string name,
        Vector3 pos,
        float scale,
        Material trunkMat,
        Material frondMat)
    {
        GameObject palm = new GameObject(name);
        palm.transform.SetParent(envRoot, false);
        palm.transform.localPosition = pos;
        palm.transform.localScale = Vector3.one * scale;

        GameObject trunk =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        trunk.name = "PalmTrunk";
        trunk.transform.SetParent(palm.transform, false);

        trunk.transform.localPosition =
            new Vector3(0.25f, 1.5f, 0f);

        trunk.transform.localRotation =
            Quaternion.Euler(0f, 0f, -8f);

        trunk.transform.localScale =
            new Vector3(0.25f, 1.5f, 0.25f);

        DestroyImmediate(trunk.GetComponent<Collider>());

        trunk.GetComponent<MeshRenderer>().sharedMaterial =
            trunkMat;

        for (int f = 0; f < 5; f++)
        {
            float ang = f * (360f / 5f);

            GameObject frond =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            frond.name = $"Frond_{f + 1}";
            frond.transform.SetParent(palm.transform, false);

            frond.transform.localPosition =
                new Vector3(0.5f, 2.9f, 0f);

            frond.transform.localRotation =
                Quaternion.Euler(25f, ang, 0f);

            frond.transform.localScale =
                new Vector3(0.35f, 0.05f, 1.25f);

            DestroyImmediate(frond.GetComponent<Collider>());

            frond.GetComponent<MeshRenderer>().sharedMaterial =
                frondMat;
        }
    }

    private void CreateFacetedRock(
        string name,
        Vector3 pos,
        Vector3 scale,
        Material mat)
    {
        GameObject rock =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        rock.name = name;
        rock.transform.SetParent(envRoot, false);
        rock.transform.localPosition = pos;

        rock.transform.localRotation =
            Quaternion.Euler(
                pos.x * 25f,
                pos.z * 18f,
                pos.y * 30f
            );

        rock.transform.localScale =
            new Vector3(
                scale.x * 1.35f,
                scale.y * 0.85f,
                scale.z * 1.15f
            );

        MeshFilter mf = rock.GetComponent<MeshFilter>();

        if (mf != null && mf.sharedMesh != null)
            mf.sharedMesh = CreateFlatShadedMesh(mf.sharedMesh);

        rock.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private Mesh CreateFlatShadedMesh(Mesh original)
    {
        Vector3[] origVerts = original.vertices;
        int[] origTris = original.triangles;

        Vector3[] newVerts = new Vector3[origTris.Length];
        int[] newTris = new int[origTris.Length];

        for (int i = 0; i < origTris.Length; i++)
        {
            newVerts[i] = origVerts[origTris[i]];
            newTris[i] = i;
        }

        Mesh flatMesh = new Mesh
        {
            name = "FacetedRockMesh",
            vertices = newVerts,
            triangles = newTris
        };

        flatMesh.RecalculateNormals();
        generatedMeshes.Add(flatMesh);

        return flatMesh;
    }

    // ---------------------------------------------------------
    // THEMATIC OBSTACLES
    // ---------------------------------------------------------

    public void BuildLoopTheLoop(
        Vector3 position,
        float radius,
        float width)
    {
        GameObject loopObj =
            new GameObject("Obstacle_WoodenLoop");

        loopObj.transform.SetParent(envRoot, false);
        loopObj.transform.localPosition = position;
        loopObj.transform.localRotation =
            Quaternion.Euler(0f, 90f, 0f);

        int segments = 24;
        float thickness = 0.28f;

        GameObject trackObj =
            new GameObject("LoopTrack_Interior");

        trackObj.transform.SetParent(loopObj.transform, false);

        MeshFilter mfTrack = trackObj.AddComponent<MeshFilter>();
        MeshRenderer mrTrack = trackObj.AddComponent<MeshRenderer>();

        mrTrack.sharedMaterial =
            greenCheckerMaterial != null
                ? greenCheckerMaterial
                : woodMaterial;

        GameObject shellObj =
            new GameObject("LoopShell_Exterior");

        shellObj.transform.SetParent(loopObj.transform, false);

        MeshFilter mfShell = shellObj.AddComponent<MeshFilter>();
        MeshRenderer mrShell = shellObj.AddComponent<MeshRenderer>();

        mrShell.sharedMaterial = woodMaterial;

        List<Vector3> trackVerts = new List<Vector3>();
        List<Vector2> trackUVs = new List<Vector2>();
        List<int> trackTris = new List<int>();

        List<Vector3> shellVerts = new List<Vector3>();
        List<Vector2> shellUVs = new List<Vector2>();
        List<int> shellTris = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float theta =
                (i / (float)segments) * Mathf.PI * 2f;

            float cos = Mathf.Cos(theta);
            float sin = Mathf.Sin(theta);

            float rInner = radius;
            float rOuter = radius + thickness;

            float xL = -width * 0.5f;
            float xR = width * 0.5f;

            trackVerts.Add(
                new Vector3(xL, sin * rInner, cos * rInner)
            );

            trackVerts.Add(
                new Vector3(xR, sin * rInner, cos * rInner)
            );

            trackUVs.Add(new Vector2(0f, i / 2f));
            trackUVs.Add(new Vector2(1f, i / 2f));

            shellVerts.Add(
                new Vector3(xL, sin * rOuter, cos * rOuter)
            );

            shellVerts.Add(
                new Vector3(xR, sin * rOuter, cos * rOuter)
            );

            shellUVs.Add(new Vector2(0f, i / 2f));
            shellUVs.Add(new Vector2(1f, i / 2f));

            if (i < segments)
            {
                int baseIdx = i * 2;

                trackTris.Add(baseIdx);
                trackTris.Add(baseIdx + 2);
                trackTris.Add(baseIdx + 1);

                trackTris.Add(baseIdx + 1);
                trackTris.Add(baseIdx + 2);
                trackTris.Add(baseIdx + 3);

                shellTris.Add(baseIdx);
                shellTris.Add(baseIdx + 1);
                shellTris.Add(baseIdx + 2);

                shellTris.Add(baseIdx + 1);
                shellTris.Add(baseIdx + 3);
                shellTris.Add(baseIdx + 2);
            }
        }

        Mesh trackMesh =
            new Mesh { name = "LoopTrackMesh" };

        trackMesh.SetVertices(trackVerts);
        trackMesh.SetUVs(0, trackUVs);
        trackMesh.SetTriangles(trackTris, 0);
        trackMesh.RecalculateNormals();

        mfTrack.sharedMesh = trackMesh;
        generatedMeshes.Add(trackMesh);

        // Static mesh collision lets the golf ball physically contact
        // the loop track instead of passing through the rendered ring.
        MeshCollider trackCollider =
            trackObj.AddComponent<MeshCollider>();

        trackCollider.sharedMesh = trackMesh;
        trackCollider.convex = false;
        trackCollider.isTrigger = false;

        Mesh shellMesh =
            new Mesh { name = "LoopShellMesh" };

        shellMesh.SetVertices(shellVerts);
        shellMesh.SetUVs(0, shellUVs);
        shellMesh.SetTriangles(shellTris, 0);
        shellMesh.RecalculateNormals();

        mfShell.sharedMesh = shellMesh;
        generatedMeshes.Add(shellMesh);

        MeshCollider shellCollider =
            shellObj.AddComponent<MeshCollider>();

        shellCollider.sharedMesh = shellMesh;
        shellCollider.convex = false;
        shellCollider.isTrigger = false;
    }

    private void BuildCanyonArchway(
        Vector3 position,
        Material mat)
    {
        GameObject arch = new GameObject("Obstacle_CanyonArch");

        arch.transform.SetParent(envRoot, false);
        arch.transform.localPosition = position;

        GameObject pillarL =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        pillarL.name = "PillarL";
        pillarL.transform.SetParent(arch.transform, false);

        pillarL.transform.localPosition =
            new Vector3(-2.2f, 1.25f, 0f);

        pillarL.transform.localScale =
            new Vector3(1.2f, 2.5f, 1.2f);

        pillarL.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject pillarR =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        pillarR.name = "PillarR";
        pillarR.transform.SetParent(arch.transform, false);

        pillarR.transform.localPosition =
            new Vector3(2.2f, 1.25f, 0f);

        pillarR.transform.localScale =
            new Vector3(1.2f, 2.5f, 1.2f);

        pillarR.GetComponent<MeshRenderer>().sharedMaterial = mat;

        GameObject lintel =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        lintel.name = "Lintel";
        lintel.transform.SetParent(arch.transform, false);

        lintel.transform.localPosition =
            new Vector3(0f, 2.7f, 0f);

        lintel.transform.localScale =
            new Vector3(5.6f, 0.8f, 1.4f);

        lintel.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // ---------------------------------------------------------
    // FIXED CANYON BANKED BUMPERS
    // ---------------------------------------------------------

    private void BuildBankedBumpers(
        Vector3 pos1,
        Vector3 pos2)
    {
        // Dedicated brown URP/Lit material.
        // Do not use the imported M_Wall material here.
        Material bumperWood = CreateLitMaterial(
            new Color(0.39f, 0.20f, 0.09f),
            0.28f,
            0.02f
        );

        if (bumperWood == null)
        {
            Debug.LogError(
                "[CourseEnvironmentBuilder] Could not create the Canyon bumper material."
            );

            return;
        }

        GameObject b1 =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        b1.name = "BankedBumper_1";
        b1.transform.SetParent(envRoot, false);
        b1.transform.localPosition = pos1;

        b1.transform.localRotation =
            Quaternion.Euler(0f, 35f, 0f);

        b1.transform.localScale =
            new Vector3(0.45f, 0.6f, 2.4f);

        MeshRenderer renderer1 =
            b1.GetComponent<MeshRenderer>();

        if (renderer1 != null)
            renderer1.sharedMaterial = bumperWood;

        Collider collider1 = b1.GetComponent<Collider>();

        if (collider1 != null)
        {
            collider1.enabled = true;
            collider1.isTrigger = false;
        }

        GameObject b2 =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        b2.name = "BankedBumper_2";
        b2.transform.SetParent(envRoot, false);
        b2.transform.localPosition = pos2;

        b2.transform.localRotation =
            Quaternion.Euler(0f, -35f, 0f);

        b2.transform.localScale =
            new Vector3(0.45f, 0.6f, 2.4f);

        MeshRenderer renderer2 =
            b2.GetComponent<MeshRenderer>();

        if (renderer2 != null)
            renderer2.sharedMaterial = bumperWood;

        Collider collider2 = b2.GetComponent<Collider>();

        if (collider2 != null)
        {
            collider2.enabled = true;
            collider2.isTrigger = false;
        }
    }

    private void BuildSnowRampJump(
        Vector3 position,
        Material mat)
    {
        GameObject ramp =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        ramp.name = "Obstacle_SnowRamp";
        ramp.transform.SetParent(envRoot, false);
        ramp.transform.localPosition = position;

        ramp.transform.localRotation =
            Quaternion.Euler(-14f, 0f, 0f);

        ramp.transform.localScale =
            new Vector3(2.8f, 0.35f, 2.2f);

        ramp.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private void BuildZenBridge(Vector3 position)
    {
        GameObject bridge =
            new GameObject("Obstacle_ZenBridge");

        bridge.transform.SetParent(envRoot, false);
        bridge.transform.localPosition = position;

        Material redWoodMat =
            CreateLitMaterial(
                new Color(0.82f, 0.18f, 0.15f),
                0.35f
            );

        GameObject floor =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        floor.name = "BridgeFloor";
        floor.transform.SetParent(bridge.transform, false);

        floor.transform.localPosition =
            new Vector3(0f, 0.25f, 0f);

        floor.transform.localScale =
            new Vector3(2.6f, 0.25f, 3.2f);

        floor.GetComponent<MeshRenderer>().sharedMaterial =
            redWoodMat;

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject rail =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            rail.name =
                $"Railing_{(side > 0 ? "R" : "L")}";

            rail.transform.SetParent(bridge.transform, false);

            rail.transform.localPosition =
                new Vector3(side * 1.35f, 0.65f, 0f);

            rail.transform.localScale =
                new Vector3(0.18f, 0.55f, 3.2f);

            rail.GetComponent<MeshRenderer>().sharedMaterial =
                redWoodMat;
        }
    }

    private void BuildBoardwalkJump(
        Vector3 position,
        Material sandMat)
    {
        GameObject jump =
            new GameObject("Obstacle_BoardwalkJump");

        jump.transform.SetParent(envRoot, false);
        jump.transform.localPosition = position;

        GameObject bunker =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        bunker.name = "SandBunker";
        bunker.transform.SetParent(jump.transform, false);

        bunker.transform.localPosition =
            new Vector3(0f, 0.05f, 0f);

        bunker.transform.localScale =
            new Vector3(3.2f, 0.1f, 3.2f);

        bunker.GetComponent<MeshRenderer>().sharedMaterial =
            sandMat;

        GameObject ramp =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        ramp.name = "LaunchRamp";
        ramp.transform.SetParent(jump.transform, false);

        ramp.transform.localPosition =
            new Vector3(0f, 0.3f, -0.6f);

        ramp.transform.localRotation =
            Quaternion.Euler(-12f, 0f, 0f);

        ramp.transform.localScale =
            new Vector3(2.4f, 0.22f, 1.8f);

        ramp.GetComponent<MeshRenderer>().sharedMaterial =
            woodMaterial;
    }

    private void BuildCurvedWallDetails()
    {
        Vector3[] cornerPositions = new Vector3[]
        {
            new Vector3(-4.5f, 0.25f, -9.5f),
            new Vector3(4.5f, 0.25f, -9.5f),
            new Vector3(-4.5f, 0.25f, 9.5f),
            new Vector3(4.5f, 0.25f, 9.5f)
        };

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            GameObject corner =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            corner.name = $"CornerBumper_{i + 1}";
            corner.transform.SetParent(envRoot, false);
            corner.transform.localPosition = cornerPositions[i];

            corner.transform.localScale =
                new Vector3(1.1f, 0.6f, 1.1f);

            corner.GetComponent<MeshRenderer>().sharedMaterial =
                woodMaterial;
        }
    }
}
