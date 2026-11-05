using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Stylized Golf Battle aiming and trajectory system for Pocket Putt Arena (Reference Images 1 & 3).
/// Displays:
/// 1. Idle dotted white circle around the ball made from discrete separated dots (Reference Image 1).
/// 2. Forward bright green directional chevrons in front of the ball (Reference Image 3).
/// 3. Dynamic pull-back power guide with white chevrons and colored glowing handle behind the ball.
/// Generates crisp procedural textures internally to guarantee flawless alpha rendering in WebGL.
/// </summary>
public class AimTrajectoryGuide : MonoBehaviour
{
    [Header("Visual Dimensions")]
    [SerializeField] private float maxHandleLength = 3.2f;
    [SerializeField] private float minHandleLength = 0.5f;
    [SerializeField] private float groundOffset = 0.035f;

    [Header("Power Gradient Colors")]
    [SerializeField] private Color lowPowerColor = new Color(0.20f, 0.95f, 0.25f, 1f);   // Bright Green
    [SerializeField] private Color midPowerColor = new Color(1.0f, 0.85f, 0.15f, 1f);    // Amber Yellow
    [SerializeField] private Color highPowerColor = new Color(1.0f, 0.15f, 0.12f, 1f);  // Vibrant Red

    // Textures (Procedural Fallback)
    private Texture2D dottedRingTexture;
    private Texture2D chevronTexture;
    private Texture2D knobTexture;

    // Components
    private GameObject dottedRingObj;
    private MeshRenderer dottedRingRenderer;

    private GameObject forwardChevronsContainer;
    private List<Transform> forwardChevrons = new List<Transform>();

    private GameObject pullbackContainer;
    private LineRenderer pullbackLine;
    private Transform pullbackHandleKnob;
    private Material handleMaterial;
    private Material knobMaterial;
    private List<Transform> pullbackChevrons = new List<Transform>();

    private bool isAimingActive = false;

    private void Awake()
    {
        GenerateProceduralTextures();
        BuildDottedRing();
        BuildForwardChevrons();
        BuildPullbackHandle();
        Hide();
    }

    #region Procedural Texture Generation

    private void GenerateProceduralTextures()
    {
        // 1. Discrete 16-Dot Selection Ring (Reference Image 1)
        dottedRingTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        dottedRingTexture.filterMode = FilterMode.Bilinear;
        dottedRingTexture.wrapMode = TextureWrapMode.Clamp;

        Color32[] ringPixels = new Color32[256 * 256];
        float cx = 128f, cy = 128f;
        float ringRadius = 88f;
        float dotRadius = 9.5f;
        int numDots = 16;

        // Calculate dot center positions
        Vector2[] dotCenters = new Vector2[numDots];
        for (int i = 0; i < numDots; i++)
        {
            float angle = (i * Mathf.PI * 2f) / numDots;
            dotCenters[i] = new Vector2(cx + Mathf.Cos(angle) * ringRadius, cy + Mathf.Sin(angle) * ringRadius);
        }

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float minDist = float.MaxValue;
                for (int i = 0; i < numDots; i++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), dotCenters[i]);
                    if (d < minDist) minDist = d;
                }

                if (minDist <= dotRadius)
                {
                    float alpha = Mathf.Clamp01(1f - (minDist / dotRadius));
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    byte a = (byte)(alpha * 255f);
                    ringPixels[y * 256 + x] = new Color32(255, 255, 255, a);
                }
                else
                {
                    ringPixels[y * 256 + x] = new Color32(0, 0, 0, 0);
                }
            }
        }
        dottedRingTexture.SetPixels32(ringPixels);
        dottedRingTexture.Apply();

        // 2. Upward Chevron Shape ^ (Reference Image 3)
        chevronTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        chevronTexture.filterMode = FilterMode.Bilinear;
        chevronTexture.wrapMode = TextureWrapMode.Clamp;

        Color32[] chevronPixels = new Color32[128 * 128];
        float chevCx = 64f;
        float chevThickness = 12f;
        float slope = 0.82f;

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float targetY = 32f + Mathf.Abs(x - chevCx) * slope;
                float dist = Mathf.Abs(y - targetY);

                if (x >= 14 && x <= 114 && y >= 16 && y <= 112 && dist <= chevThickness)
                {
                    float alpha = Mathf.Clamp01(1f - (dist / chevThickness));
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    byte a = (byte)(alpha * 255f);
                    chevronPixels[y * 128 + x] = new Color32(255, 255, 255, a);
                }
                else
                {
                    chevronPixels[y * 128 + x] = new Color32(0, 0, 0, 0);
                }
            }
        }
        chevronTexture.SetPixels32(chevronPixels);
        chevronTexture.Apply();

        // 3. Glowing Circular Handle Knob (Reference Image 3)
        knobTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        knobTexture.filterMode = FilterMode.Bilinear;
        knobTexture.wrapMode = TextureWrapMode.Clamp;

        Color32[] knobPixels = new Color32[128 * 128];
        float knobRadius = 56f;
        float knobThickness = 14f;

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
                if (dist <= knobRadius)
                {
                    float rimDist = Mathf.Abs(dist - (knobRadius - knobThickness * 0.5f));
                    if (rimDist <= knobThickness * 0.5f)
                    {
                        // Bright glowing outer ring
                        float ringAlpha = 1f - (rimDist / (knobThickness * 0.5f));
                        ringAlpha = Mathf.SmoothStep(0f, 1f, ringAlpha);
                        byte a = (byte)(Mathf.Clamp01(0.7f + ringAlpha * 0.3f) * 255f);
                        knobPixels[y * 128 + x] = new Color32(255, 255, 255, a);
                    }
                    else if (dist < (knobRadius - knobThickness))
                    {
                        // Translucent center
                        knobPixels[y * 128 + x] = new Color32(255, 255, 255, 45);
                    }
                    else
                    {
                        // Outer soft falloff
                        float edgeAlpha = Mathf.Clamp01(1f - ((dist - knobRadius + 3f) / 4f));
                        byte a = (byte)(edgeAlpha * 180f);
                        knobPixels[y * 128 + x] = new Color32(255, 255, 255, a);
                    }
                }
                else
                {
                    knobPixels[y * 128 + x] = new Color32(0, 0, 0, 0);
                }
            }
        }
        knobTexture.SetPixels32(knobPixels);
        knobTexture.Apply();
    }

    private Shader GetSpriteShader()
    {
        return Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
    }

    #endregion

    #region Hierarchy Construction

    private void BuildDottedRing()
    {
        dottedRingObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dottedRingObj.name = "SelectionDottedRing";
        dottedRingObj.transform.SetParent(transform, false);
        dottedRingObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        dottedRingObj.transform.localPosition = new Vector3(0f, groundOffset, 0f);
        dottedRingObj.transform.localScale = Vector3.one * 1.40f;

        DestroyImmediate(dottedRingObj.GetComponent<Collider>());

        Material mat = new Material(GetSpriteShader());
        mat.mainTexture = dottedRingTexture;
        mat.color = Color.white;
        mat.renderQueue = 3100;

        dottedRingRenderer = dottedRingObj.GetComponent<MeshRenderer>();
        dottedRingRenderer.sharedMaterial = mat;
        dottedRingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void BuildForwardChevrons()
    {
        forwardChevronsContainer = new GameObject("ForwardChevrons");
        forwardChevronsContainer.transform.SetParent(transform, false);

        Material chevronMat = new Material(GetSpriteShader());
        chevronMat.mainTexture = chevronTexture;
        chevronMat.color = new Color(0.20f, 0.95f, 0.25f, 0.95f); // Bright neon green
        chevronMat.renderQueue = 3100;

        int count = 3; // Reference Image 3 shows 3 forward green chevrons <<<
        for (int i = 0; i < count; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Quad);
            c.name = $"Chevron_{i + 1}";
            c.transform.SetParent(forwardChevronsContainer.transform, false);
            c.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            c.transform.localScale = Vector3.one * 0.45f;
            DestroyImmediate(c.GetComponent<Collider>());

            MeshRenderer mr = c.GetComponent<MeshRenderer>();
            mr.sharedMaterial = chevronMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            forwardChevrons.Add(c.transform);
        }
    }

    private void BuildPullbackHandle()
    {
        pullbackContainer = new GameObject("PullbackHandle");
        pullbackContainer.transform.SetParent(transform, false);

        // Line for the pull-back strip
        pullbackLine = pullbackContainer.AddComponent<LineRenderer>();
        pullbackLine.positionCount = 2;
        pullbackLine.useWorldSpace = true;
        pullbackLine.startWidth = 0.12f;
        pullbackLine.endWidth = 0.26f;
        pullbackLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pullbackLine.receiveShadows = false;

        handleMaterial = new Material(GetSpriteShader()) { color = highPowerColor, renderQueue = 3050 };
        pullbackLine.sharedMaterial = handleMaterial;

        // Circular knob at drag point (Reference Image 3)
        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Quad);
        knob.name = "PullbackKnob";
        knob.transform.SetParent(pullbackContainer.transform, false);
        knob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        knob.transform.localScale = Vector3.one * 1.05f;
        DestroyImmediate(knob.GetComponent<Collider>());

        knobMaterial = new Material(GetSpriteShader());
        knobMaterial.mainTexture = knobTexture;
        knobMaterial.color = highPowerColor;
        knobMaterial.renderQueue = 3105;

        MeshRenderer mr = knob.GetComponent<MeshRenderer>();
        mr.sharedMaterial = knobMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pullbackHandleKnob = knob.transform;

        // White chevron accents along the pull-back band pointing forward towards the ball (Reference Image 3)
        Material pullChevronMat = new Material(GetSpriteShader());
        pullChevronMat.mainTexture = chevronTexture;
        pullChevronMat.color = new Color(1f, 1f, 1f, 0.95f);
        pullChevronMat.renderQueue = 3102;

        int numPullChevrons = 3;
        for (int i = 0; i < numPullChevrons; i++)
        {
            GameObject pc = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pc.name = $"PullChevron_{i + 1}";
            pc.transform.SetParent(pullbackContainer.transform, false);
            pc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pc.transform.localScale = Vector3.one * 0.32f;
            DestroyImmediate(pc.GetComponent<Collider>());

            MeshRenderer pmr = pc.GetComponent<MeshRenderer>();
            pmr.sharedMaterial = pullChevronMat;
            pmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            pullbackChevrons.Add(pc.transform);
        }
    }

    #endregion

    private void Update()
    {
        // Rotate idle dotted ring slowly when waiting for shot (Reference Image 1)
        if (!isAimingActive && dottedRingObj != null && dottedRingObj.activeSelf)
        {
            dottedRingObj.transform.Rotate(0f, 0f, -22f * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Displays or hides the idle white dotted selection ring.
    /// </summary>
    public void SetSelectionRingVisible(bool visible)
    {
        if (dottedRingObj != null)
        {
            dottedRingObj.SetActive(visible && !isAimingActive);
        }
    }

    /// <summary>
    /// Updates the aim trajectory guide in real-time.
    /// </summary>
    public void UpdateAim(Vector3 ballPosition, Vector3 shootDirection, float powerRatio)
    {
        isAimingActive = true;
        if (dottedRingObj != null) dottedRingObj.SetActive(false);

        powerRatio = Mathf.Clamp01(powerRatio);
        Color currentColor = powerRatio < 0.5f
            ? Color.Lerp(lowPowerColor, midPowerColor, powerRatio * 2f)
            : Color.Lerp(midPowerColor, highPowerColor, (powerRatio - 0.5f) * 2f);

        // 1. Forward chevrons (<<< in front of ball, Reference Image 3)
        if (forwardChevronsContainer != null)
        {
            forwardChevronsContainer.SetActive(true);
            float forwardSpacing = 0.42f;
            Quaternion forwardRot = Quaternion.LookRotation(shootDirection, Vector3.up);

            for (int i = 0; i < forwardChevrons.Count; i++)
            {
                Vector3 chevronPos = ballPosition + (shootDirection * (0.34f + i * forwardSpacing));
                chevronPos.y = ballPosition.y + groundOffset;
                forwardChevrons[i].position = chevronPos;
                // Align chevron quad on the ground pointing forward
                forwardChevrons[i].rotation = Quaternion.Euler(90f, forwardRot.eulerAngles.y, 0f);
            }
        }

        // 2. Pullback handle (behind ball in direction opposite to shot)
        if (pullbackContainer != null)
        {
            pullbackContainer.SetActive(true);
            float currentLength = Mathf.Lerp(minHandleLength, maxHandleLength, powerRatio);
            Vector3 pullDir = -shootDirection.normalized;

            Vector3 startPt = new Vector3(ballPosition.x, ballPosition.y + groundOffset, ballPosition.z);
            Vector3 endPt = startPt + (pullDir * currentLength);

            pullbackLine.SetPosition(0, startPt);
            pullbackLine.SetPosition(1, endPt);
            pullbackLine.startColor = currentColor;
            pullbackLine.endColor = currentColor;

            if (handleMaterial != null) handleMaterial.color = currentColor;

            if (pullbackHandleKnob != null)
            {
                pullbackHandleKnob.position = new Vector3(endPt.x, endPt.y + 0.01f, endPt.z);
                if (knobMaterial != null) knobMaterial.color = currentColor;
            }

            // Position white chevrons along pullback handle pointing toward the ball (Reference Image 3)
            Quaternion pullChevronRot = Quaternion.LookRotation(shootDirection, Vector3.up);
            for (int i = 0; i < pullbackChevrons.Count; i++)
            {
                float frac = (i + 1f) / (pullbackChevrons.Count + 1f);
                Vector3 cPos = Vector3.Lerp(startPt, endPt, frac);
                cPos.y += 0.005f;
                pullbackChevrons[i].position = cPos;
                pullbackChevrons[i].rotation = Quaternion.Euler(90f, pullChevronRot.eulerAngles.y, 0f);
                // Scale chevron slightly wider with the tapered handle
                float scale = Mathf.Lerp(0.24f, 0.42f, frac);
                pullbackChevrons[i].localScale = Vector3.one * (scale * 0.72f);
            }
        }
    }

    /// <summary>
    /// Hides aiming guides and restores idle dotted selection ring.
    /// </summary>
    public void Hide()
    {
        isAimingActive = false;
        if (forwardChevronsContainer != null) forwardChevronsContainer.SetActive(false);
        if (pullbackContainer != null) pullbackContainer.SetActive(false);
        if (dottedRingObj != null) dottedRingObj.SetActive(true);
    }
}
