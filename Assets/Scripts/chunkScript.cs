using System;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;

public class chunkScript : MonoBehaviour
{

    public int seed;
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
    public float sharpness = 1.0f;
    public float maxHeight = 1.0f;
    public float noiseBias = 0.0f;
    public Boolean absoluteHeight = false;
    public Material basicMaterial;
    public Boolean UseBasicMaterial = true;
    public Color planeColor = Color.darkGreen;
    public Color slopeColor = Color.dimGray;
    public float slopeBias = 0.5f;
    public float slopeSharpness = 25.0f;
    public Color fogColor = Color.white;
    public float fogStart = 0.0f;
    private Renderer objectRenderer;
    private FBM_Noise noise;


    private void OnDestroy()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
            meshFilter.mesh = null;
        }
    }

    public void updateMeshVertices()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            meshFilter.mesh.Clear();

            Vector3[] vertices = createVertices(chunkSize, chunkScale);
            int[] triangles = createTriangles(chunkSize);
            Vector2[] uvs = createUVs(chunkSize);

            meshFilter.mesh.vertices = vertices;
            meshFilter.mesh.triangles = triangles;
            meshFilter.mesh.uv = uvs;

            meshFilter.mesh.RecalculateNormals();
            meshFilter.mesh.RecalculateBounds();
        }
    }

    public void updateOctaves(float newOctaves)
    {
        octaves = newOctaves;
        updateMeshVertices();
    }

    public void updateAmplitude(float newAmplitude)
    {
        amplitude = newAmplitude;
        updateMeshVertices();
    }

    public void updatePersistence(float newPersistence)
    {
        persistence = newPersistence;
        updateMeshVertices();
    }

    public void updateFrequency(float newFrequency)
    {
        frequency = newFrequency;
        updateMeshVertices();
    }

    public void updateLacunarity(float newLacunarity)
    {
        lacunarity = newLacunarity;
        updateMeshVertices();
    }

    public void updatePerlinScale(float newPerlinScale)
    {
        perlinScale = newPerlinScale;
        updateMeshVertices();
    }

    public void updateUVScale(float newUVScale) 
    {
        UVscale = newUVScale;
        updateMeshVertices();
    }

    public void updateSharpness(float newSharpness)
    {
        sharpness = newSharpness;
        updateMeshVertices();
    }

    public void updateMaxHeight(float newMaxHeight)
    {
        maxHeight = newMaxHeight;
        updateMeshVertices();
    }

    public void updateAbsoluteHeight(Boolean newAbsoluteHeight)
    {
        absoluteHeight = newAbsoluteHeight;
        updateMeshVertices();
    }

    public void updateNoiseBias(float newNoiseBias)
    {
        noiseBias = newNoiseBias;
        updateMeshVertices();
    }
    
    public void updateFogColor(Color newFogColor)
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        objectRenderer.sharedMaterial.SetColor("_fogColor", newFogColor);
    }

    public void updateFogStart(float newFogStart)
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        objectRenderer.sharedMaterial.SetFloat("_fogStart", newFogStart);
    }

    public void updateUsingBasicMaterial(Boolean newUseBasicMaterial)
    {
        UseBasicMaterial = newUseBasicMaterial;
        //TODO
    }

    public void updatePlaneColor(Color newPlaneColor)
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        objectRenderer.sharedMaterial.SetColor("_planeColor", newPlaneColor);
    }

    public void updateSlopeColor(Color newSlopeColor)
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        objectRenderer.sharedMaterial.SetColor("_slopeColor", newSlopeColor);
    }

    public void updateSlopeBias(float newSlopeBias)
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        objectRenderer.sharedMaterial.SetFloat("_slopeBias", newSlopeBias);
    }

    public void updateSlopeSharpness(float newSlopeSharpness)
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        objectRenderer.sharedMaterial.SetFloat("_slopeSharpness", newSlopeSharpness);
    }

    public void Awake()
    {
        if (basicMaterial == null)
        {
            basicMaterial = new Material(Shader.Find("Standard"));
            basicMaterial.color = Color.white;
        }
    }

    private float computeHeight(float x, float z, float freq, float amp)
    {
        float height = 0;
        float maxAmp = 0;
        float ampDecay = 1.0f;
        for (int i = 0; i < octaves; i++)
        {
            height += Mathf.Min(Mathf.PerlinNoise(x * freq, z * freq) * 2 - 1 + noiseBias, maxHeight) * ampDecay;
            maxAmp += ampDecay;
            freq *= lacunarity;
            amp *= persistence;
            ampDecay *= persistence;
        }
        //return (Math.Abs(Math.Abs(height / maxAmp) - 0.8f) - 0.5f) * amplitude;
        if (absoluteHeight)
        {
            return (float)Math.Abs(Math.Pow(height / maxAmp, sharpness)) * amplitude;
        }
        else
        {
            return (float) Math.Pow(height / maxAmp, sharpness) * amplitude; 
        }
        
    }

    private Vector3[] createVertices(int size, float scale)
    {
        Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;

                float sampleX = (x + perlinOffsetX) / perlinScale;
                float sampleZ = (z + perlinOffsetZ) / perlinScale;
                float y_value = noise.FBM_NoiseValue(sampleX, sampleZ, false) * amplitude;
                vertices[index] = new Vector3((x - (float)size / 2) * scale,
                                                y_value,
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
        meshRenderer.sharedMaterial = basicMaterial;

        mesh.vertices = createVertices(chunkSize, chunkScale);
        mesh.triangles = createTriangles(chunkSize);
        mesh.uv = createUVs(chunkSize);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        objectRenderer = GetComponent<Renderer>();
        objectRenderer.sharedMaterial.SetColor("_fogColor", fogColor);

        if (UseBasicMaterial)
        {
            objectRenderer.sharedMaterial = basicMaterial;
            objectRenderer.sharedMaterial.SetColor("_planeColor", planeColor);
            objectRenderer.sharedMaterial.SetColor("_slopeColor", slopeColor);
            objectRenderer.sharedMaterial.SetFloat("_slopeBias", slopeBias);
            objectRenderer.sharedMaterial.SetFloat("_slopeSharpness", slopeSharpness);
        }
    }
    void Start()
    {
        noise = new FBM_Noise(seed, amplitude, persistence, frequency, lacunarity, octaves);
        createMesh();
    }

    public void setAttributes(int size, float scale, float uvScale, float perlinScale, float octaves,
        float persistence, float lacunarity, float frequency, float offsetX, float offsetZ,
        float amplitude, Color fogColor, float sharpness)
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
        this.fogColor = fogColor;
        this.sharpness = sharpness;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
