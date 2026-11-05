using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages local CPU opponent balls with floating nameplates.
/// These are simulated competitors, not online multiplayer players.
/// </summary>
public class OpponentBallManager : MonoBehaviour
{
    [System.Serializable]
    public class OpponentData
    {
        public string playerName;
        public Vector3 startPosition;
        public Color primaryColor;
        public Color secondaryColor;
        public int currentStroke;
        public bool hasSunk;
    }

    [Header("Opponent Roster")]
    [SerializeField]
    private List<OpponentData> opponents = new List<OpponentData>()
    {
        new OpponentData
        {
            playerName = "Kasem",
            startPosition = new Vector3(3.2f, 0.1605f, 2.5f),
            primaryColor = new Color(0.92f, 0.18f, 0.22f),
            secondaryColor = Color.white,
            currentStroke = 1
        },
        new OpponentData
        {
            playerName = "Raul",
            startPosition = new Vector3(-1.1f, 0.1605f, 6.8f),
            primaryColor = new Color(0.18f, 0.45f, 0.92f),
            secondaryColor = Color.white,
            currentStroke = 2
        },
        new OpponentData
        {
            playerName = "Vincent",
            startPosition = new Vector3(4.1f, 0.1605f, -3.2f),
            primaryColor = new Color(0.20f, 0.75f, 0.28f),
            secondaryColor = Color.white,
            currentStroke = 1
        }
    };

    [Header("Visual Dimensions")]
    [SerializeField] private float ballDiameter = 0.315f;
    [SerializeField] private float nameTagHeight = 0.45f;

    private readonly List<GameObject> spawnedOpponentObjects = new List<GameObject>();
    private readonly List<TextMesh> nameplateMeshes = new List<TextMesh>();
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        SpawnOpponents();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Quaternion cameraRotation = mainCamera.transform.rotation;

        for (int i = 0; i < nameplateMeshes.Count; i++)
        {
            if (nameplateMeshes[i] != null)
                nameplateMeshes[i].transform.rotation = cameraRotation;
        }
    }

    public void SpawnOpponents()
    {
        ClearOpponents();

        for (int i = 0; i < opponents.Count; i++)
        {
            OpponentData data = opponents[i];
            if (data == null)
                continue;

            GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.name = $"Opponent_{data.playerName}";
            ballObject.transform.SetParent(transform, false);
            ballObject.transform.position = data.startPosition;
            ballObject.transform.localScale = Vector3.one * ballDiameter;

            SphereCollider sphere = ballObject.GetComponent<SphereCollider>();
            if (sphere != null)
                sphere.isTrigger = true;

            MeshRenderer ballRenderer = ballObject.GetComponent<MeshRenderer>();
            Shader ballShader = Shader.Find("Universal Render Pipeline/Lit");

            if (ballShader == null)
                ballShader = Shader.Find("Standard");

            if (ballRenderer != null && ballShader != null)
            {
                Material ballMaterial = new Material(ballShader);
                ballMaterial.color = data.primaryColor;

                if (ballMaterial.HasProperty("_Smoothness"))
                    ballMaterial.SetFloat("_Smoothness", 0.85f);

                if (ballMaterial.HasProperty("_Metallic"))
                    ballMaterial.SetFloat("_Metallic", 0.05f);

                ballRenderer.sharedMaterial = ballMaterial;
            }

            GameObject tagObject = new GameObject($"NameTag_{data.playerName}");
            tagObject.transform.SetParent(ballObject.transform, false);
            tagObject.transform.localPosition =
                new Vector3(0f, nameTagHeight / Mathf.Max(0.001f, ballDiameter), 0f);
            tagObject.transform.localScale = Vector3.one * 0.12f;

            TextMesh textMesh = tagObject.AddComponent<TextMesh>();
            Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (runtimeFont == null)
                runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 32);

            if (runtimeFont != null)
                textMesh.font = runtimeFont;

            textMesh.text = data.playerName;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.28f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyle.Bold;

            MeshRenderer nameRenderer = tagObject.GetComponent<MeshRenderer>();
            if (nameRenderer != null)
            {
                nameRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            spawnedOpponentObjects.Add(ballObject);
            nameplateMeshes.Add(textMesh);
        }
    }

    public void ClearOpponents()
    {
        for (int i = 0; i < spawnedOpponentObjects.Count; i++)
        {
            GameObject opponentObject = spawnedOpponentObjects[i];

            if (opponentObject == null)
                continue;

            if (Application.isPlaying)
                Destroy(opponentObject);
            else
                DestroyImmediate(opponentObject);
        }

        spawnedOpponentObjects.Clear();
        nameplateMeshes.Clear();
    }

    private void OnDestroy()
    {
        ClearOpponents();
    }
}
