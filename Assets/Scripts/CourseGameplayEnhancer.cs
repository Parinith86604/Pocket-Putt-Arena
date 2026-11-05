
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public class CourseGameplayEnhancer : MonoBehaviour
{
    [Header("Visual Clarity")]
    [SerializeField] private bool improveRuntimeQuality = true;

    [SerializeField, Range(1f, 1.4f)]
    private float smallTextScale = 1.18f;

    private CourseManager courseManager;
    private CourseEnvironmentBuilder environmentBuilder;

    private Transform lastEnvironmentRoot;
    private int lastCourseId = -1;

    private void Awake()
    {
        RefreshReferences();
    }

    private void Start()
    {
        RefreshReferences();

        if (improveRuntimeQuality)
        {
            ApplyRuntimeQuality();
            ImproveHUDClarity();
        }
    }

    private void RefreshReferences()
    {
        if (courseManager == null)
        {
            courseManager = CourseManager.Instance;

            if (courseManager == null)
                courseManager =
                    FindFirstObjectByType<CourseManager>();
        }

        if (environmentBuilder == null)
        {
            if (courseManager != null)
            {
                environmentBuilder =
                    courseManager.GetComponent<CourseEnvironmentBuilder>();

                if (environmentBuilder == null)
                {
                    environmentBuilder =
                        courseManager.GetComponentInChildren<CourseEnvironmentBuilder>(
                            true
                        );
                }
            }

            if (environmentBuilder == null)
            {
                environmentBuilder =
                    FindFirstObjectByType<CourseEnvironmentBuilder>();
            }
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        RefreshReferences();

        if (environmentBuilder == null)
            return;

        Transform currentRoot = ResolveEnvironmentRoot();

        if (currentRoot == null)
            return;

        int currentCourseId = courseManager != null
            ? courseManager.ActiveCourseId
            : 1;

        if (currentRoot == lastEnvironmentRoot &&
            currentCourseId == lastCourseId)
            return;

        lastEnvironmentRoot = currentRoot;
        lastCourseId = currentCourseId;

        // Remove the old pink boost pad from every loaded course.
        RemoveOldBoostPads(currentRoot);

        // Make circular and loop obstacles physically solid.
        EnsureSolidRingObstacles(currentRoot);

        Debug.Log(
            $"[CourseGameplayEnhancer] Course {currentCourseId}: " +
            "old boost pads removed and obstacle collisions checked."
        );
    }

    private Transform ResolveEnvironmentRoot()
    {
        Transform found = FindChildRecursive(
            environmentBuilder.transform,
            "ProceduralEnvironment"
        );

        return found != null
            ? found
            : environmentBuilder.transform;
    }

    private Transform FindChildRecursive(
        Transform parent,
        string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result =
                FindChildRecursive(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    // ---------------------------------------------------------
    // REMOVE UNWANTED BOOST PADS
    // ---------------------------------------------------------

    private void RemoveOldBoostPads(Transform root)
    {
        if (root == null)
            return;

        Transform[] children =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            if (child.name == "Interactive_Pink_BoostPad")
            {
                Destroy(child.gameObject);

                Debug.Log(
                    "[CourseGameplayEnhancer] Removed old pink boost pad."
                );
            }
        }
    }

    // ---------------------------------------------------------
    // SOLID PHYSICAL RING / LOOP OBSTACLES
    // ---------------------------------------------------------

    private void EnsureSolidRingObstacles(Transform root)
    {
        if (root == null)
            return;

        MeshFilter[] meshes =
            root.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter meshFilter in meshes)
        {
            if (meshFilter == null ||
                meshFilter.sharedMesh == null)
                continue;

            if (!IsRingObstacle(meshFilter.transform, root))
                continue;

            EnsureMeshCollider(meshFilter);
        }

        // Also support circular obstacles drawn using LineRenderer.
        LineRenderer[] lines =
            root.GetComponentsInChildren<LineRenderer>(true);

        foreach (LineRenderer line in lines)
        {
            if (line == null ||
                line.positionCount < 2)
                continue;

            if (line.GetComponent<MeshFilter>() != null)
                continue;

            if (!IsRingObstacle(line.transform, root))
                continue;

            EnsureLineCollision(line);
        }
    }

    private bool IsRingObstacle(
        Transform obstacle,
        Transform environmentRoot)
    {
        Transform current = obstacle;

        while (current != null)
        {
            string objectName =
                current.name.ToLowerInvariant();

            if (objectName.Contains("ring") ||
                objectName.Contains("loop") ||
                objectName.Contains("hoop") ||
                objectName.Contains("circular"))
            {
                return true;
            }

            if (current == environmentRoot)
                break;

            current = current.parent;
        }

        return false;
    }

    private void EnsureMeshCollider(MeshFilter meshFilter)
    {
        GameObject obstacle = meshFilter.gameObject;

        Collider[] existingColliders =
            obstacle.GetComponents<Collider>();

        if (existingColliders.Length > 0)
        {
            foreach (Collider existing in existingColliders)
            {
                existing.enabled = true;
                existing.isTrigger = false;

                MeshCollider existingMesh =
                    existing as MeshCollider;

                if (existingMesh != null &&
                    existingMesh.sharedMesh == null)
                {
                    existingMesh.sharedMesh =
                        meshFilter.sharedMesh;
                }
            }

            return;
        }

        MeshCollider meshCollider =
            obstacle.AddComponent<MeshCollider>();

        Rigidbody attachedBody =
            obstacle.GetComponentInParent<Rigidbody>();

        // Static scenery uses a non-convex mesh.
        // Moving rigidbodies require a convex mesh.
        meshCollider.convex = attachedBody != null;
        meshCollider.isTrigger = false;
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        Debug.Log(
            $"[CourseGameplayEnhancer] Solid mesh collision added: {obstacle.name}"
        );
    }

    private void EnsureLineCollision(LineRenderer line)
    {
        int count = line.positionCount;

        if (count < 2)
            return;

        Vector3[] points = new Vector3[count];

        line.GetPositions(points);

        int segmentCount = line.loop
            ? count
            : count - 1;

        float radius = Mathf.Max(
            0.06f,
            Mathf.Max(line.startWidth, line.endWidth) * 0.5f
        );

        for (int i = 0; i < segmentCount; i++)
        {
            int next = (i + 1) % count;

            Vector3 start = line.useWorldSpace
                ? points[i]
                : line.transform.TransformPoint(points[i]);

            Vector3 end = line.useWorldSpace
                ? points[next]
                : line.transform.TransformPoint(points[next]);

            Vector3 direction = end - start;

            float length = direction.magnitude;

            if (length < 0.001f)
                continue;

            string colliderName =
                "SolidCollisionSegment_" + i;

            Transform existing =
                line.transform.Find(colliderName);

            if (existing != null)
                continue;

            GameObject segment =
                new GameObject(colliderName);

            segment.transform.SetParent(
                line.transform,
                true
            );

            segment.transform.position =
                (start + end) * 0.5f;

            segment.transform.rotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    direction.normalized
                );

            CapsuleCollider capsule =
                segment.AddComponent<CapsuleCollider>();

            capsule.direction = 1;
            capsule.radius = radius;
            capsule.height = length + radius * 2f;
            capsule.isTrigger = false;
        }

        Debug.Log(
            $"[CourseGameplayEnhancer] Solid line collision added: {line.name}"
        );
    }

    // ---------------------------------------------------------
    // VISUAL QUALITY
    // ---------------------------------------------------------

    private void ApplyRuntimeQuality()
    {
        QualitySettings.globalTextureMipmapLimit = 0;

        QualitySettings.anisotropicFiltering =
            AnisotropicFiltering.ForceEnable;

        QualitySettings.lodBias =
            Mathf.Max(QualitySettings.lodBias, 2f);

        QualitySettings.maximumLODLevel = 0;
    }

    private void ImproveHUDClarity()
    {
        Canvas[] canvases =
            FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
            {
                canvas.pixelPerfect = true;
            }
        }

        Font runtimeFont =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        Text[] texts =
            FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Text uiText in texts)
        {
            if (uiText == null)
                continue;

            if (runtimeFont != null)
                uiText.font = runtimeFont;

            if (uiText.fontSize <= 15)
            {
                uiText.fontSize = Mathf.Clamp(
                    Mathf.RoundToInt(
                        uiText.fontSize * smallTextScale
                    ),
                    15,
                    19
                );
            }

            uiText.supportRichText = true;
        }
    }
}

// -------------------------------------------------------------
// UNITY EDITOR TOOLS
// -------------------------------------------------------------

#if UNITY_EDITOR

public static class PocketPuttCourseTools
{
    [MenuItem("Pocket Putt Arena/Remove Old Pink Boost Pads")]
    public static void RemoveOldPinkBoostPads()
    {
        Transform[] allTransforms =
            Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int removed = 0;

        foreach (Transform item in allTransforms)
        {
            if (item == null ||
                item.name != "Interactive_Pink_BoostPad" ||
                !item.gameObject.scene.IsValid())
                continue;

            Undo.DestroyObjectImmediate(item.gameObject);
            removed++;
        }

        Debug.Log(
            $"[Pocket Putt Arena] Removed {removed} old pink boost pads from loaded scenes."
        );
    }

    [MenuItem("Pocket Putt Arena/Set All Course Configs To Medium")]
    public static void SetAllCoursesToMedium()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:CourseConfig");

        int updated = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            CourseConfig config =
                AssetDatabase.LoadAssetAtPath<CourseConfig>(path);

            if (config == null)
                continue;

            config.difficulty = "Medium";

            EditorUtility.SetDirty(config);
            updated++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[Pocket Putt Arena] Set {updated} course configurations to Medium."
        );
    }

    [MenuItem("Pocket Putt Arena/Improve Course Texture Clarity")]
    public static void ImproveCourseTextureClarity()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Textures" }
            );

        int updated = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            importer.maxTextureSize =
                Mathf.Max(importer.maxTextureSize, 2048);

            importer.filterMode =
                FilterMode.Trilinear;

            importer.anisoLevel = 16;

            importer.textureCompression =
                TextureImporterCompression.CompressedHQ;

            importer.compressionQuality = 100;

            importer.SaveAndReimport();
            updated++;
        }

        Debug.Log(
            $"[Pocket Putt Arena] Updated clarity settings for {updated} textures."
        );
    }
}

#endif
