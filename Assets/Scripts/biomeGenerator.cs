using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class biomeGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Biome> biomes = new List<Biome>();
    public float maxTemp = 100f;
    public float maxHumidity = 100f;
    public int size = 16;
    public float scale = 1f;
    public float perlinScale = 1.0f;
    public Material biomeMaterial;
    public int tempSeed = 0;
    public int humditySeed = 0;
    public float adjustment = 1.0f;
    private biomeMap biomeMap;

    private Vector3[] createVertices(int size, float scale)
    {
        Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;
                vertices[index] = new Vector3((x - (float)size / 2) * scale,
                                                0,
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
                uvs[index] = new Vector2((float)x, (float)z);
            }
        }
        return uvs;
    }

    float Adjust(float noise, float adjustment)
    {
        noise = noise * 2 - 1;
        noise = Math.Sign(noise) * Mathf.Pow(Mathf.Abs(noise), adjustment);
        noise = (noise + 1) / 2;

        return noise;
    }

    private Color LerpColor(Color a, Color b, float t)
    {
        return (1 - t) * a + b;
    }

    private float Hermite(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private Color[] createColors(int size)
    {
        Color[] colors = new Color[(size + 1) * (size + 1)];
        FBM_Noise tempNoise = new FBM_Noise(tempSeed, 1f, 0.5f, 2f, 3f, 3);
        FBM_Noise humidityNoise = new FBM_Noise(humditySeed, 1f, 0.5f, 2f, 3f, 3);

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;

                float sampleX = (float)x / perlinScale;
                float sampleZ = (float)z / perlinScale;

                float tempNoiseVal = Adjust(tempNoise.FBM_NoiseValue(sampleX, sampleZ), adjustment);
                float humidityNoiseVal = Adjust(humidityNoise.FBM_NoiseValue(sampleX, sampleZ), adjustment);

                float tempValue = tempNoiseVal * maxTemp;
                float humidityValue = humidityNoiseVal * maxHumidity;

                colors[index] = biomeMap.getBiome(tempValue, humidityValue).Color;
            }
        }

        return colors;
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
        meshRenderer.sharedMaterial = biomeMaterial;

        mesh.vertices = createVertices(size, scale);
        mesh.triangles = createTriangles(size);
        mesh.uv = createUVs(size);
        mesh.colors = createColors(size);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }


    void Start()
    {
        biomeMap = new biomeMap(maxTemp, maxHumidity, biomes);
        //Debug.Log(biomeMap.biomes[0].Name);
        createMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}