using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Celebratory visual presentation for successful putts inspired by Golf Battle (Reference Image 2).
/// Displays dynamic 3D floating "HOLE IN X!" animated text, glowing vertical light shaft,
/// and bursting triangular white confetti shards radiating from the cup.
/// </summary>
public class HoleCelebrationEffect : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private HoleCompletionController holeController;

    [Header("Colors & Visuals")]
    [SerializeField] private Color celebrationTextColor = Color.white;
    [SerializeField] private Color celebrationGlowColor = new Color(0.2f, 0.85f, 1f, 0.75f); // Cyan glow
    [SerializeField] private int confettiCount = 36;

    private GameObject celebrationContainer;
    private TextMesh celebrationTextMesh;
    private TextMesh celebrationShadowMesh;
    private MeshRenderer lightBurstRenderer;
    private List<Transform> confettiPieces = new List<Transform>();
    private List<Vector3> confettiVelocities = new List<Vector3>();
    private List<Vector3> confettiSpinAxes = new List<Vector3>();

    private void Start()
    {
        if (holeController == null)
        {
            holeController = FindFirstObjectByType<HoleCompletionController>();
        }

        if (holeController != null)
        {
            holeController.OnLevelCompleted += HandleLevelCompleted;
            holeController.OnHoleReplayed += HandleHoleReplayed;
        }

        BuildCelebrationHierarchy();
    }

    private void OnDestroy()
    {
        if (holeController != null)
        {
            holeController.OnLevelCompleted -= HandleLevelCompleted;
            holeController.OnHoleReplayed -= HandleHoleReplayed;
        }
    }

    private void BuildCelebrationHierarchy()
    {
        celebrationContainer = new GameObject("CelebrationPresentation");
        celebrationContainer.transform.SetParent(transform, false);
        celebrationContainer.transform.localPosition = new Vector3(0f, 0f, 0f);

        // 1. 3D Floating "HOLE IN X!" Text Mesh (Reference Image 2)
        GameObject textObj = new GameObject("FloatingHoleText");
        textObj.transform.SetParent(celebrationContainer.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        textObj.transform.localScale = Vector3.zero;

        // Shadow text layer
        GameObject shadowObj = new GameObject("TextShadow");
        shadowObj.transform.SetParent(textObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.04f, -0.04f, 0.04f);

        celebrationShadowMesh = shadowObj.AddComponent<TextMesh>();
        celebrationShadowMesh.text = "HOLE IN 1!";
        celebrationShadowMesh.fontSize = 44;
        celebrationShadowMesh.characterSize = 0.22f;
        celebrationShadowMesh.anchor = TextAnchor.MiddleCenter;
        celebrationShadowMesh.alignment = TextAlignment.Center;
        celebrationShadowMesh.color = new Color(0.05f, 0.25f, 0.65f, 0.95f); // Deep blue drop shadow
        celebrationShadowMesh.fontStyle = FontStyle.Bold;

        // Foreground text layer
        celebrationTextMesh = textObj.AddComponent<TextMesh>();
        celebrationTextMesh.text = "HOLE IN 1!";
        celebrationTextMesh.fontSize = 44;
        celebrationTextMesh.characterSize = 0.22f;
        celebrationTextMesh.anchor = TextAnchor.MiddleCenter;
        celebrationTextMesh.alignment = TextAlignment.Center;
        celebrationTextMesh.color = Color.white;
        celebrationTextMesh.fontStyle = FontStyle.Bold;
        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null)
            runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 44);

        if (runtimeFont != null)
        {
            celebrationShadowMesh.font = runtimeFont;
            celebrationTextMesh.font = runtimeFont;
        }

        // 2. Vertical Glowing Light Shaft
        GameObject shaftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaftObj.name = "LightBurstShaft";
        shaftObj.transform.SetParent(celebrationContainer.transform, false);
        shaftObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        shaftObj.transform.localScale = new Vector3(0.65f, 1.2f, 0.65f);
        Collider shaftCollider = shaftObj.GetComponent<Collider>();
        if (shaftCollider != null)
        {
            if (Application.isPlaying) Destroy(shaftCollider);
            else DestroyImmediate(shaftCollider);
        }

        Material glowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        glowMat.color = celebrationGlowColor;
        lightBurstRenderer = shaftObj.GetComponent<MeshRenderer>();
        lightBurstRenderer.sharedMaterial = glowMat;
        lightBurstRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 3. Build Triangular Confetti Shards
        Material confettiMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        confettiMat.color = Color.white;

        for (int i = 0; i < confettiCount; i++)
        {
            GameObject shard = CreateTriangleShardMesh($"Confetti_{i + 1}", confettiMat);
            shard.transform.SetParent(celebrationContainer.transform, false);
            shard.transform.localPosition = Vector3.zero;
            shard.SetActive(false);
            confettiPieces.Add(shard.transform);
        }

        celebrationContainer.SetActive(false);
    }

    private GameObject CreateTriangleShardMesh(string name, Material mat)
    {
        GameObject shard = new GameObject(name);
        MeshFilter mf = shard.AddComponent<MeshFilter>();
        MeshRenderer mr = shard.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Double-sided triangle shard
        Mesh mesh = new Mesh { name = "ConfettiTriangle" };
        float size = Random.Range(0.08f, 0.16f);
        Vector3[] verts = new Vector3[]
        {
            new Vector3(0f, size * 1.2f, 0f),
            new Vector3(-size * 0.7f, -size * 0.5f, 0f),
            new Vector3(size * 0.7f, -size * 0.5f, 0f),
            // reverse side
            new Vector3(0f, size * 1.2f, 0f),
            new Vector3(size * 0.7f, -size * 0.5f, 0f),
            new Vector3(-size * 0.7f, -size * 0.5f, 0f)
        };
        int[] tris = new int[] { 0, 1, 2, 3, 4, 5 };

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        return shard;
    }

    private void LateUpdate()
    {
        // Keep 3D celebration text facing the camera
        if (celebrationTextMesh != null && Camera.main != null)
        {
            celebrationTextMesh.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void HideCelebration()
    {
        StopAllCoroutines();
        if (celebrationContainer != null) celebrationContainer.SetActive(false);
    }

    private void HandleLevelCompleted(int totalStrokes)
    {
        if (totalStrokes < 1)
        {
            Debug.LogWarning($"[HoleCelebrationEffect] Ignored invalid celebration with {totalStrokes} strokes.");
            return;
        }

        string message = totalStrokes == 1 ? "HOLE IN ONE!" : $"HOLE IN {totalStrokes}!";
        if (celebrationTextMesh != null) celebrationTextMesh.text = message;
        if (celebrationShadowMesh != null) celebrationShadowMesh.text = message;

        StopAllCoroutines();
        StartCoroutine(PlayCelebrationRoutine());
    }

    private void HandleHoleReplayed()
    {
        HideCelebration();
    }

    private IEnumerator PlayCelebrationRoutine()
    {
        if (celebrationContainer == null) yield break;
        celebrationContainer.SetActive(true);

        Transform textTransform = celebrationTextMesh != null ? celebrationTextMesh.transform : null;
        if (textTransform != null) textTransform.localScale = Vector3.zero;

        // Reset and launch confetti shards
        confettiVelocities.Clear();
        confettiSpinAxes.Clear();

        for (int i = 0; i < confettiPieces.Count; i++)
        {
            Transform shard = confettiPieces[i];
            shard.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), 0.05f, Random.Range(-0.15f, 0.15f));
            shard.localScale = Vector3.one;
            shard.gameObject.SetActive(true);

            // Upward burst with outward angle
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(2.2f, 4.5f);
            float outwardSpeed = Random.Range(0.8f, 2.0f);
            Vector3 vel = new Vector3(Mathf.Cos(angle) * outwardSpeed, speed, Mathf.Sin(angle) * outwardSpeed);
            confettiVelocities.Add(vel);

            Vector3 spin = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-360f, 360f));
            confettiSpinAxes.Add(spin);
        }

        // Animate pop-in of text and light shaft
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Bounce scale for text: 0 -> 1.15 -> 1.0
            if (textTransform != null)
            {
                float textScale = (elapsed < 0.35f)
                    ? Mathf.Lerp(0f, 1.18f, elapsed / 0.35f)
                    : Mathf.Lerp(1.18f, 1.0f, (elapsed - 0.35f) / 0.35f);
                textTransform.localScale = Vector3.one * textScale;
            }

            // Light shaft pulsing glow and fade
            if (lightBurstRenderer != null)
            {
                float pulse = 0.5f + Mathf.PingPong(elapsed * 4f, 0.4f);
                float alpha = Mathf.Clamp01(1f - (elapsed / 1.8f));
                lightBurstRenderer.material.color = new Color(celebrationGlowColor.r, celebrationGlowColor.g, celebrationGlowColor.b, alpha * pulse);
            }

            // Update confetti particle simulation
            for (int i = 0; i < confettiPieces.Count; i++)
            {
                Transform shard = confettiPieces[i];
                if (!shard.gameObject.activeSelf) continue;

                Vector3 vel = confettiVelocities[i];
                vel.y -= 3.8f * Time.deltaTime; // gentle gravity
                confettiVelocities[i] = vel;

                shard.localPosition += vel * Time.deltaTime;
                shard.Rotate(confettiSpinAxes[i] * Time.deltaTime);

                // Fade out shard scale as it falls
                if (elapsed > 1.2f)
                {
                    float shrink = Mathf.Clamp01(1f - ((elapsed - 1.2f) / 1.3f));
                    shard.localScale = Vector3.one * shrink;
                }
            }

            yield return null;
        }
    }
}
