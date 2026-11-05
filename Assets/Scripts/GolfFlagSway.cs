using UnityEngine;

/// <summary>
/// Generates and animates a tournament-grade triangular golf flag with realistic wind sway.
/// Generates a lightweight, double-sided triangular mesh and applies smooth vertex waves,
/// eliminating expensive cloth simulations while looking dynamic and visible in WebGL.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GolfFlagSway : MonoBehaviour
{
    [Header("Flag Dimensions")]
    [Tooltip("Length of the flag extending horizontally from the pole.")]
    [SerializeField] private float flagLength = 1.15f;

    [Tooltip("Height of the flag along the flagpole.")]
    [SerializeField] private float flagHeight = 0.58f;

    [Tooltip("Flagpole offset so the flag connects cleanly to the pole edge.")]
    [SerializeField] private float poleOffset = 0.05f;

    [Header("Wind Animation Parameters")]
    [Tooltip("Speed of the wind oscillation.")]
    [SerializeField] private float waveSpeed = 4.2f;

    [Tooltip("Amplitude of the wind flutter at the tip.")]
    [SerializeField] private float waveAmplitude = 0.08f;

    [Tooltip("Frequency of spatial waves along the flag fabric.")]
    [SerializeField] private float waveFrequency = 3.5f;

    private MeshFilter meshFilter;
    private Mesh flagMesh;
    private Vector3[] baseVertices;
    private Vector3[] dynamicVertices;
    private int segmentsX = 8;
    private int segmentsY = 4;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        BuildTriangularFlagMesh();
    }

    private void BuildTriangularFlagMesh()
    {
        flagMesh = new Mesh();
        flagMesh.name = "DynamicTriangularFlag";

        int vertCount = (segmentsX + 1) * (segmentsY + 1) * 2; // Front and back faces
        baseVertices = new Vector3[vertCount];
        dynamicVertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];

        int vIndex = 0;

        // Front Face
        for (int y = 0; y <= segmentsY; y++)
        {
            float v = (float)y / segmentsY;
            for (int x = 0; x <= segmentsX; x++)
            {
                float u = (float)x / segmentsX;

                // Triangular taper: flag narrows from full flagHeight at the pole to a tip at the end
                float halfHeight = (flagHeight * 0.5f) * (1f - u * 0.85f);
                float yPos = Mathf.Lerp(-halfHeight, halfHeight, v);
                float xPos = poleOffset + u * flagLength;

                Vector3 pos = new Vector3(-xPos, yPos, 0f);
                baseVertices[vIndex] = pos;
                dynamicVertices[vIndex] = pos;
                uvs[vIndex] = new Vector2(u, v);
                normals[vIndex] = Vector3.forward;
                vIndex++;
            }
        }

        // Back Face (reverse normals for two-sided visibility)
        int backOffset = (segmentsX + 1) * (segmentsY + 1);
        for (int i = 0; i < backOffset; i++)
        {
            baseVertices[vIndex] = baseVertices[i];
            dynamicVertices[vIndex] = baseVertices[i];
            uvs[vIndex] = uvs[i];
            normals[vIndex] = -Vector3.forward;
            vIndex++;
        }

        // Triangles
        int quads = segmentsX * segmentsY;
        int[] triangles = new int[quads * 6 * 2];
        int tIndex = 0;

        // Front triangles
        for (int y = 0; y < segmentsY; y++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int bl = y * (segmentsX + 1) + x;
                int br = bl + 1;
                int tl = bl + (segmentsX + 1);
                int tr = tl + 1;

                triangles[tIndex++] = bl;
                triangles[tIndex++] = tl;
                triangles[tIndex++] = br;

                triangles[tIndex++] = br;
                triangles[tIndex++] = tl;
                triangles[tIndex++] = tr;
            }
        }

        // Back triangles (inverted winding)
        for (int y = 0; y < segmentsY; y++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int bl = backOffset + y * (segmentsX + 1) + x;
                int br = bl + 1;
                int tl = bl + (segmentsX + 1);
                int tr = tl + 1;

                triangles[tIndex++] = bl;
                triangles[tIndex++] = br;
                triangles[tIndex++] = tl;

                triangles[tIndex++] = br;
                triangles[tIndex++] = tr;
                triangles[tIndex++] = tl;
            }
        }

        flagMesh.vertices = baseVertices;
        flagMesh.uv = uvs;
        flagMesh.normals = normals;
        flagMesh.triangles = triangles;
        flagMesh.RecalculateBounds();

        meshFilter.sharedMesh = flagMesh;
    }

    private void Update()
    {
        if (flagMesh == null || baseVertices == null) return;

        float time = Time.time * waveSpeed;
        int vertPerFace = (segmentsX + 1) * (segmentsY + 1);

        for (int i = 0; i < vertPerFace; i++)
        {
            Vector3 basePos = baseVertices[i];
            // Amplitude scales with distance from pole (pole is fixed, tip flaps maximum)
            float distFromPole = Mathf.Clamp01((-basePos.x - poleOffset) / flagLength);
            float flutter = Mathf.Sin(time + distFromPole * waveFrequency) * (waveAmplitude * distFromPole);
            float secondary = Mathf.Sin(time * 1.7f + distFromPole * waveFrequency * 1.5f) * (waveAmplitude * 0.35f * distFromPole);

            Vector3 wavePos = new Vector3(basePos.x, basePos.y, basePos.z + flutter + secondary);
            dynamicVertices[i] = wavePos;
            dynamicVertices[i + vertPerFace] = wavePos; // Mirror back face
        }

        flagMesh.vertices = dynamicVertices;
        flagMesh.RecalculateNormals();
    }
}
