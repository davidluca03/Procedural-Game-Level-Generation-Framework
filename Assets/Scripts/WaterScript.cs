using UnityEngine;
using System.Collections.Generic;

public class WaterScript : MonoBehaviour
{
    public float waterLevel = 0.0f;
    public Material waterMaterial;
    public int chunkSize = 16;
    public float chunkScale = 1;
    
    public float waterUVScale = 1;
    public float perlinOffsetX = 0.0f;
    public float perlinOffsetZ = 0.0f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        if (waterMaterial != null)
        {
            meshRenderer.sharedMaterial = waterMaterial;
        }
    }

    private void Start()
    {
        GenerateWaterMesh();
    }

    public void updateWaterLevel(float newWaterLevel)
    {
        waterLevel = newWaterLevel;
        GenerateWaterMesh();
    }

    private void OnDestroy()
    {
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
            meshFilter.mesh = null;
        }
    }

    private void GenerateWaterMesh()
    {
        MeshData data = GenerateMeshData();
        ApplyMeshDataToMesh(data);
    }
    
    private MeshData GenerateMeshData()
    {
        MeshData data = new MeshData();
        data.vertices = CreateVertices(chunkSize, chunkScale, waterLevel);
        data.triangles = CreateTriangles(chunkSize);
        data.uvs = CreateUVs(chunkSize);
        return data;
    }

    private void ApplyMeshDataToMesh(MeshData data)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        mesh.uv = data.uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
        }
        meshFilter.mesh = mesh;
    }

    private Vector3[] CreateVertices(int size, float scale, float height)
    {
        Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];
        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;
                vertices[index] = new Vector3((x - (float)size / 2) * scale, height, (z - (float)size / 2) * scale);
            }
        }
        return vertices;
    }

    private int[] CreateTriangles(int size)
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

    private Vector2[] CreateUVs(int size)
    {
        Vector2[] uvs = new Vector2[(size + 1) * (size + 1)];
        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;
                uvs[index] = new Vector2(((float)x + perlinOffsetX) * waterUVScale, ((float)z + perlinOffsetZ) * waterUVScale);
            }
        }
        return uvs;
    }

    public class MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
    }
}