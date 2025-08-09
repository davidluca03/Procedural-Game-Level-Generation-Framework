using System;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;

public class chunkScript : MonoBehaviour
{

    public int chunkSize = 16;
    public float chunkScale = 1;
    public float UVscale = 1;
    public float perlinScale = 1.0f;
    public float octaves = 1;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;
    public float frequency = 1.0f;
    public float perlinOffsetX = 0.0f;
    public float perlinOffsetZ = 0.0f;
    public float offsetX = 0.0f;
    public float offsetZ = 0.0f;
    public float amplitude = 1.0f;
    public Material material;
    private Renderer objectRenderer;

    public void updateMeshVertices()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh.Clear();
            meshFilter.mesh.vertices = createVertices(chunkSize, chunkScale);
            meshFilter.mesh.triangles = createTriangles(chunkSize);
            meshFilter.mesh.uv = createUVs(chunkSize);

            meshFilter.mesh.RecalculateNormals();
            meshFilter.mesh.RecalculateBounds();
        }
    }
    public void Awake()
    {
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
        }
    }

    private float computeHeight(float x, float z, float freq, float amp)
    {
        float height = 0;
        float maxAmp = 0;
        float ampDecay = 1.0f;
        for (int i = 0; i < octaves; i++)
        {
            height += (Mathf.PerlinNoise(x * freq, z * freq) * 2 - 1) * ampDecay;
            maxAmp += ampDecay;
            freq *= lacunarity;
            amp *= persistence;
            ampDecay *= persistence;
        }
        //return (Math.Abs(Math.Abs(height / maxAmp) - 0.8f) - 0.5f) * amplitude;
        return (float) Math.Abs((Math.Pow(height / maxAmp, 2))) * amplitude;
    }

    private Vector3[] createVertices(int size, float scale)
    {
        Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;
                vertices[index] = new Vector3((x - (float)size / 2) * scale,
                                                computeHeight((float)(x + perlinOffsetX) / perlinScale, (float)(z + perlinOffsetZ) / perlinScale, frequency, amplitude),
                                                (z - (float)size / 2) * scale);
                //Debug.Log($"Vertex {index}: {vertices[index]} at position ({x}, {z}) with height {vertices[index].y}");
            }
        }
        return vertices;
    }

    private int[] createTriangles(int size)
    {
        int[] triangles = new int[size * size * 6];
        int indexCounter = 0;

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int topLeft = x * (size + 1) + z;
                int topRight = topLeft + 1;
                int bottomLeft = (x + 1) * (size + 1) + z;
                int bottomRight = bottomLeft + 1;

                triangles[indexCounter++] = topLeft;
                triangles[indexCounter++] = topRight;
                triangles[indexCounter++] = bottomLeft;

                triangles[indexCounter++] = topRight;
                triangles[indexCounter++] = bottomRight;
                triangles[indexCounter++] = bottomLeft;
            }
        }
        return triangles;
    }

    private Vector2[] createUVs(int size)
    {
        Vector2[] uvs = new Vector2[(size + 1) * (size + 1)];

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;
                uvs[index] = new Vector2(((float)x + perlinOffsetX) * UVscale, ((float)z + perlinOffsetZ) * UVscale);
            }
        }
        return uvs;
    }

    private void createMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        meshRenderer.material = material;

        mesh.vertices = createVertices(chunkSize, chunkScale);
        mesh.triangles = createTriangles(chunkSize);
        mesh.uv = createUVs(chunkSize);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        objectRenderer = GetComponent<Renderer>();
    }
    void Start()
    {
        createMesh();
    }

    public void setAttributes(int size, float scale, float uvScale, float perlinScale, float octaves,
        float persistence, float lacunarity, float frequency, float offsetX, float offsetZ,
        float amplitude)
    {
        this.chunkSize = size;
        this.chunkScale = scale;
        this.UVscale = uvScale;
        this.perlinScale = perlinScale;
        this.octaves = octaves;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
        this.frequency = frequency;
        this.offsetX = offsetX;
        this.offsetZ = offsetZ;
        this.amplitude = amplitude;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
