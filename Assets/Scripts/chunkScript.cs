using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Unity.Collections;
using System.Xml.Serialization;
using Unity.VisualScripting;

public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Color[] colors;
}

public class chunkScript : MonoBehaviour
{
    public int seed;
    private biomeMap biomeMap;
    public int biomeColorBlend = 0;
    public int blendSampleDistance = 0;
    public float biomeScale = 1.0f;
    public float biomeAdjustment = 1.0f;
    public float maxTemp = 100.0f;
    public float maxHumidity = 100.0f;
    public List<Biome> Biomes;
    public int chunkSize = 16;
    public float chunkScale = 1;
    public float UVscale = 1;
    public float perlinScale = 1.0f;
    public int octaves = 1;
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
    public bool absoluteHeight = false;
    public Material basicMaterial;
    public bool UseBasicMaterial = true;
    public Gradient gradient;
    public Color planeColor = Color.darkGreen;
    public Color slopeColor = Color.dimGray;
    public float slopeBias = 0.5f;
    public float slopeSharpness = 25.0f;
    public Color fogColor = Color.white;
    public float fogStart = 0.0f;
    public float waterLevel = 0.0f;
    private Renderer objectRenderer;
    private float[,] heightMap;
    private Dictionary<Color, float>[,] biomeColorMap;
    private Biome[,] biomeGrid;
    private FBM_Noise noise;
    private FBM_Noise tempNoise;
    private FBM_Noise humidityNoise;
    private CancellationTokenSource cancellationTokenSource;
    private List<GameObject> objectList;

    private void OnEnable()
    {
        cancellationTokenSource = new CancellationTokenSource();
    }

    private void OnDisable()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }

    private void OnDestroy()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
            meshFilter.mesh = null;
        }

        if (objectList != null)
        {
            foreach (GameObject obj in objectList) 
            {
                if (obj != null) 
                {
                    Destroy(obj);
                }
            }
        }
    }

    public async Task UpdateMeshAsync(CancellationToken cancellationToken)
    {
        noise = new FBM_Noise(seed, amplitude, persistence, frequency, lacunarity, octaves, sharpness);
        cancellationToken.ThrowIfCancellationRequested();

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        MeshData meshData = await Task.Run(() => GenerateMeshData());

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        ApplyMeshDataToMesh(meshData);
        UpdateMaterialProperties();
    }

    public void RequestMeshUpdate()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
        }
        else
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        UpdateMeshAsync(cancellationTokenSource.Token);
    }
    
    private MeshData GenerateMeshData()
    {
        MeshData data = new MeshData();
        data.vertices = createVertices(chunkSize, chunkScale);
        data.triangles = createTriangles(chunkSize);
        data.uvs = createUVs(chunkSize);
        data.colors = createColors(chunkSize);
        return data;
    }

    private void ApplyMeshDataToMesh(MeshData data)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        mesh.uv = data.uvs;
        mesh.colors = data.colors;
        mesh.RecalculateNormals();

        if (meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
        }
        meshFilter.mesh = mesh;
        spawnObjects(chunkSize, mesh.vertices, mesh.normals);
    }

    private void UpdateMaterialProperties()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (basicMaterial != null)
        {
            meshRenderer.sharedMaterial = basicMaterial;
        }

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && objectRenderer.sharedMaterial != null)
        {
            objectRenderer.sharedMaterial.SetColor("_fogColor", fogColor);
            objectRenderer.sharedMaterial.SetFloat("_fogStart", fogStart);
            if (UseBasicMaterial)
            {
                objectRenderer.sharedMaterial.SetColor("_planeColor", planeColor);
                objectRenderer.sharedMaterial.SetColor("_slopeColor", slopeColor);
                objectRenderer.sharedMaterial.SetFloat("_slopeBias", slopeBias);
                objectRenderer.sharedMaterial.SetFloat("_slopeSharpness", slopeSharpness);
            }
        }
    }

    public void updateSeed(int newSeed)
    {
        seed = newSeed;
        System.Random r = new System.Random(seed);
        int tempSeed = r.Next();
        int humditySeed = r.Next();
        tempNoise = new FBM_Noise(tempSeed, 1f, 0.5f, 2f, 3f, 4);
        humidityNoise = new FBM_Noise(humditySeed, 1f, 0.5f, 2f, 3f, 4);
        RequestMeshUpdate();
    }
    public void updateOctaves(int newOctaves) { octaves = newOctaves; RequestMeshUpdate(); }
    public void updateAmplitude(float newAmplitude) { amplitude = newAmplitude; RequestMeshUpdate(); }
    public void updatePersistence(float newPersistence) { persistence = newPersistence; RequestMeshUpdate(); }
    public void updateFrequency(float newFrequency) { frequency = newFrequency; RequestMeshUpdate(); }
    public void updateLacunarity(float newLacunarity) { lacunarity = newLacunarity; RequestMeshUpdate(); }
    public void updatePerlinScale(float newPerlinScale) { perlinScale = newPerlinScale; RequestMeshUpdate(); }
    public void updateUVScale(float newUVScale) { UVscale = newUVScale; RequestMeshUpdate(); }
    public void updateSharpness(float newSharpness) { sharpness = newSharpness; RequestMeshUpdate(); }
    public void updateMaxHeight(float newMaxHeight) { maxHeight = newMaxHeight; RequestMeshUpdate(); }
    public void updateAbsoluteHeight(bool newAbsoluteHeight) { absoluteHeight = newAbsoluteHeight; RequestMeshUpdate(); }
    public void updateNoiseBias(float newNoiseBias) { noiseBias = newNoiseBias; RequestMeshUpdate(); }
    public void updateGradient(Gradient newGradient) { gradient = newGradient; RequestMeshUpdate(); }
    public void updateFogColor(Color newFogColor) { fogColor = newFogColor; UpdateMaterialProperties(); }
    public void updateFogStart(float newFogStart) { fogStart = newFogStart; UpdateMaterialProperties(); }
    public void updateUsingBasicMaterial(bool newUseBasicMaterial) { UseBasicMaterial = newUseBasicMaterial; UpdateMaterialProperties(); }
    public void updatePlaneColor(Color newPlaneColor) { planeColor = newPlaneColor; UpdateMaterialProperties(); }
    public void updateSlopeColor(Color newSlopeColor) { slopeColor = newSlopeColor; UpdateMaterialProperties(); }
    public void updateSlopeBias(float newSlopeBias) { slopeBias = newSlopeBias; UpdateMaterialProperties(); }
    public void updateSlopeSharpness(float newSlopeSharpness) { slopeSharpness = newSlopeSharpness; UpdateMaterialProperties(); }
    
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
        if (absoluteHeight)
        {
            return (float)Math.Abs(Math.Pow(height / maxAmp, sharpness)) * amplitude;
        }
        else
        {
            return (float)Math.Pow(height / maxAmp, sharpness) * amplitude;
        }
    }

    private Vector3[] createVertices(int size, float scale)
    {
        Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];
        heightMap = new float[size + 1, size + 1];
        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;
                float sampleX = (x + perlinOffsetX) / perlinScale;
                float sampleZ = (z + perlinOffsetZ) / perlinScale;
                float y_value = noise.FBM_NoiseValue(sampleX, sampleZ, false) * amplitude;
                heightMap[x, z] = y_value;
                vertices[index] = new Vector3((x - (float)size / 2) * scale, y_value, (z - (float)size / 2) * scale);
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

    private void spawnObjects(int size, Vector3[] vertices, Vector3[] normals)
    {
        if (objectList == null)
        {
            objectList = new List<GameObject>();
        }
        else
        {
            foreach (GameObject oldObj in objectList)
            {
                if (oldObj != null)
                {
                    Destroy(oldObj);
                }
            }
            objectList.Clear();
        }
        
        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                Vector3 spawnPos = vertices[x * (size + 1) + z] + this.transform.position;
                
                List<spawnableObject> biomeObjectList = biomeGrid[x, z].objects;

                if (biomeObjectList != null)
                {
                    foreach (spawnableObject obj in biomeObjectList)
                    {
                        if (obj != null && obj.obj != null)
                        {
                            if (obj.confirmSpawn(
                                this.transform.position.x + spawnPos.x, 
                                this.transform.position.y + spawnPos.y, 
                                this.transform.position.z + spawnPos.z,
                                normals[x * (size + 1) + z],
                                seed) &&
                                this.transform.position.y + spawnPos.y > waterLevel + 2.0f &&
                                x % obj.minDistance == 0 && z % obj.minDistance == 0)
                            {
                                GameObject newObject = Instantiate(obj.obj, spawnPos, Quaternion.identity);
                                objectList.Add(newObject);

                                float randomScale = UnityEngine.Random.Range(obj.minScale, obj.maxScale);
                                newObject.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                                float randomRotation = UnityEngine.Random.Range(obj.minRotation, obj.maxRotation);
                                newObject.transform.Rotate(0, randomRotation, 0);
                            }
                            
                        }
                    }
                }
            }
        }
    }

    float Adjust(float noise, float adjustment)
    {
        noise = noise * 2 - 1;
        noise = Math.Sign(noise) * Mathf.Pow(Mathf.Abs(noise), adjustment);
        noise = (noise + 1) / 2;
        return noise;
    }

    private Color[] createColors(int size)
    {
        biomeGrid = new Biome[size + 1, size + 1];
        Color[] colors = new Color[(size + 1) * (size + 1)];
        Color[,] cornerColors = new Color[2, 2];

        float blendArea = Mathf.Pow(2 * biomeColorBlend + 1, 2);
        float weight = 1.0f / blendArea;

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                cornerColors[i, j] = Color.black;

                for (int m = i * size - biomeColorBlend * blendSampleDistance; m <= i * size + biomeColorBlend * blendSampleDistance; m += blendSampleDistance)
                {
                    for (int n = j * size - biomeColorBlend * blendSampleDistance; n <= j * size + biomeColorBlend * blendSampleDistance; n += blendSampleDistance)
                    {
                        float sampleX = ((float)m + perlinOffsetX) / biomeScale;
                        float sampleZ = ((float)n + perlinOffsetZ) / biomeScale;

                        float tempNoiseVal = Adjust(tempNoise.FBM_NoiseValue(sampleX, sampleZ), biomeAdjustment);
                        float humidityNoiseVal = Adjust(humidityNoise.FBM_NoiseValue(sampleX, sampleZ), biomeAdjustment);

                        float tempValue = tempNoiseVal * maxTemp;
                        float humidityValue = humidityNoiseVal * maxHumidity;

                        Biome biome = biomeMap.getBiome(tempValue, humidityValue);
                        cornerColors[i, j] += weight * biome.Color;
                    }
                }
            }
        }

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                int index = x * (size + 1) + z;

                float sampleX = ((float)x + perlinOffsetX) / biomeScale;
                float sampleZ = ((float)z + perlinOffsetZ) / biomeScale;

                float tempNoiseVal = Adjust(tempNoise.FBM_NoiseValue(sampleX, sampleZ), biomeAdjustment);
                float humidityNoiseVal = Adjust(humidityNoise.FBM_NoiseValue(sampleX, sampleZ), biomeAdjustment);

                float tempValue = tempNoiseVal * maxTemp;
                float humidityValue = humidityNoiseVal * maxHumidity;

                Biome biome = biomeMap.getBiome(tempValue, humidityValue);
                biomeGrid[x, z] = biome;

                float u = (float)x / size;
                float v = (float)z / size;

                colors[index] = Color.Lerp(
                    Color.Lerp(cornerColors[0, 0], cornerColors[1, 0], u),
                    Color.Lerp(cornerColors[0, 1], cornerColors[1, 1], u),
                    v
                );
            }
        }
        return colors;
    }

    public void Start()
    {
        biomeMap = new biomeMap(maxTemp, maxHumidity, Biomes);
        noise = new FBM_Noise(seed, amplitude, persistence, frequency, lacunarity, octaves, sharpness);

        System.Random r = new System.Random(seed);
        int tempSeed = r.Next();
        int humditySeed = r.Next();
        tempNoise = new FBM_Noise(tempSeed, 1f, 0.5f, 2f, 3f, 4);
        humidityNoise = new FBM_Noise(humditySeed, 1f, 0.5f, 2f, 3f, 4);

        RequestMeshUpdate();
    }
}