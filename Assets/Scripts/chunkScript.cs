using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Unity.Collections;
using System.Xml.Serialization;
using Unity.VisualScripting;
using Unity.Multiplayer.Center.Common;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Color[] colors;
}

public class GPUInstanceData
{
    public Mesh mesh;
    public Material material;
    public List<Matrix4x4> matrices;

    public GPUInstanceData(Mesh mesh, Material material, List<Matrix4x4> matrices)
    {
        this.mesh = mesh;
        this.material = material;
        this.matrices = matrices;
    }
}

public class chunkScript : MonoBehaviour
{
    public int seed;
    private biomeMap biomeMap;
    private biomeMap underWaterBiomeMap;
    private biomeMap highLandsBiomeMap;
    public int biomeColorBlend = 0;
    public int blendSampleDistance = 0;
    public float biomeScale = 1.0f;
    public float biomeAdjustment = 1.0f;
    public float maxTemp = 100.0f;
    public float maxHumidity = 100.0f;
    public List<Biome> Biomes;
    public List<Biome> underWaterBiomes;
    public List<Biome> highlandsBiomes;
    public float highLandsTreshold = 100000.0f;
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
    public Color slopeColor = Color.dimGray;
    public float slopeBias = 0.5f;
    public float slopeSharpness = 25.0f;
    public Color fogColor = Color.white;
    public float fogStart = 0.0f;
    public float waterLevel = 0.0f;
    public float objectRenderDistance = 0;
    public Dictionary<string, GPUInstanceData> GPUInstances;
    private Renderer objectRenderer;
    private float[,] heightMap;
    private Dictionary<Color, float>[,] biomeColorMap;
    private Biome[,] biomeGrid;
    private FBM_Noise noise;
    private FBM_Noise tempNoise;
    private FBM_Noise humidityNoise;
    private CancellationTokenSource cancellationTokenSource;
    private float objectsPerChunk = 0.0f;
    private Mesh mesh;
    private bool addedObjects = false;
    private System.Random rng;
    private List<chunkGenerator.PositionData> localInstanceRegistry = new List<chunkGenerator.PositionData>();


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

    public void removeObjects()
    {
        if (GPUInstances != null)
            GPUInstances.Clear();

        localInstanceRegistry.Clear();
        addedObjects = false;
    }

    private void OnDestroy()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
            meshFilter.mesh = null;
        }

        if (GPUInstances != null)
            GPUInstances.Clear();
    }

    public async Task UpdateMeshAsync(CancellationToken cancellationToken)
    {
        noise = new FBM_Noise(seed, amplitude, persistence, frequency, lacunarity, octaves, sharpness);
        cancellationToken.ThrowIfCancellationRequested();

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        rng = new System.Random(seed);
        int tempSeed = rng.Next();
        int humditySeed = rng.Next();
        tempNoise = new FBM_Noise(tempSeed, 1f, 0.5f, 2f, 3f, 4);
        humidityNoise = new FBM_Noise(humditySeed, 1f, 0.5f, 2f, 3f, 4);
        biomeMap = new biomeMap(maxTemp, maxHumidity, Biomes);
        underWaterBiomeMap = new biomeMap(maxTemp, maxHumidity, underWaterBiomes);
        highLandsBiomeMap = new biomeMap(maxTemp, maxHumidity, highlandsBiomes);
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

        if (mesh != null)
            mesh.Clear();

        mesh = new Mesh();
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
        
    }

    public async void addObjects()
    {
        if (addedObjects == false && mesh != null)
        {
            addedObjects = true;
            var biomesCopy = new List<Biome>(Biomes);

            var localMeshKeys = new Dictionary<string, (Mesh mesh, Material material, spawnableObject obj)>();
            foreach (Biome biome in biomesCopy)
            {
                foreach (var obj in biome.objects)
                {
                    MeshFilter mf = obj.obj.GetComponent<MeshFilter>();
                    MeshRenderer mr = obj.obj.GetComponent<MeshRenderer>();
                    Mesh mesh = mf.sharedMesh;
                    Material material = mr.sharedMaterial;
                    string key = mesh.name + "_" + material.name;
                    
                    if (!localMeshKeys.ContainsKey(key))
                    {
                        localMeshKeys.Add(key, (mesh, material, obj));
                    }
                }
            }

            var waterBiomesCopy = new List<Biome>(underWaterBiomes);
            foreach (Biome waterBiome in waterBiomesCopy)
            {
                foreach (var obj in waterBiome.objects)
                {
                    MeshFilter mf = obj.obj.GetComponent<MeshFilter>();
                    MeshRenderer mr = obj.obj.GetComponent<MeshRenderer>();
                    Mesh mesh = mf.sharedMesh;
                    Material material = mr.sharedMaterial;
                    string key = mesh.name + "_" + material.name;

                    if (!localMeshKeys.ContainsKey(key))
                    {
                        localMeshKeys.Add(key, (mesh, material, obj));
                    }
                }
            }

            var highLandsBiomesCopy = new List<Biome>(highlandsBiomes);
            foreach (Biome highLandsBiome in highLandsBiomesCopy)
            {
                foreach (var obj in highLandsBiome.objects)
                {
                    MeshFilter mf = obj.obj.GetComponent<MeshFilter>();
                    MeshRenderer mr = obj.obj.GetComponent<MeshRenderer>();
                    Mesh mesh = mf.sharedMesh;
                    Material material = mr.sharedMaterial;
                    string key = mesh.name + "_" + material.name;

                    if (!localMeshKeys.ContainsKey(key))
                    {
                        localMeshKeys.Add(key, (mesh, material, obj));
                    }
                }
            }
            
            Vector3[] normals = mesh.normals.ToArray();
            Dictionary<string, List<(Matrix4x4 matrix, Vector3 normal)>> objectTransforms = await Task.Run(() => generateTransforms(chunkSize, normals, localMeshKeys));
            Dictionary<string, List<Matrix4x4>> validObjects = new Dictionary<string, List<Matrix4x4>>();

            foreach (var meshKey in localMeshKeys.Keys)
            {
                string key = meshKey;
                if (objectTransforms.TryGetValue(key, out List<(Matrix4x4 matrix, Vector3 normal)> transforms))
                {
                    spawnableObject randomObject = localMeshKeys[meshKey].obj;
                    foreach (var (matrix, normal) in transforms)
                    {
                        Vector3 spawnPos = matrix.GetColumn(3);
                        if (randomObject.confirmSpawn(spawnPos.x, spawnPos.y, spawnPos.z, normal, seed))
                        {
                            if (randomObject.ignoreCollisions || IsSpaceAvailableLocal(spawnPos, randomObject.minDistance))
                            {
                                if (!validObjects.ContainsKey(key))
                                {
                                    validObjects.Add(key, new List<Matrix4x4>());
                                }
                                validObjects[key].Add(matrix);

                                if (!randomObject.ignoreCollisions)
                                    localInstanceRegistry.Add(new chunkGenerator.PositionData { position = spawnPos, restrictionRadius = randomObject.minDistance });
                            }
                        }
                    }
                }
            }
            GPUInstances = await Task.Run(() => bindMeshToTransforms(validObjects, localMeshKeys));
        }
    }

    private bool IsSpaceAvailableLocal(Vector3 position, float radius)
    {
        foreach (var obj in localInstanceRegistry)
        {
            if (Vector3.Distance(obj.position, position) < obj.restrictionRadius + radius)
            {
                return false;
            }
        }
        return true;
    }


    public Dictionary<string, GPUInstanceData> bindMeshToTransforms(Dictionary<string, List<Matrix4x4>> validObjects, Dictionary<string, (Mesh mesh, Material material, spawnableObject obj)> meshKeys)
    {
        Dictionary<string, GPUInstanceData> result = new Dictionary<string, GPUInstanceData>();

        foreach (var meshKey in meshKeys.Keys)
        {
            if (validObjects.ContainsKey(meshKey))
            {
                result.Add(meshKey, new GPUInstanceData(meshKeys[meshKey].mesh, meshKeys[meshKey].material, validObjects[meshKey]));
            }
        }

        return result;
    }
    private Dictionary<string, List<(Matrix4x4 matrix, Vector3 normal)>> generateTransforms(int size, Vector3[] normals, Dictionary<string, (Mesh mesh, Material material, spawnableObject obj)> meshKeys)
    {
        Dictionary<string, List<(Matrix4x4 matrix, Vector3 normal)>> result = new Dictionary<string, List<(Matrix4x4 matrix, Vector3 normal)>>();

        if (GPUInstances == null)
        {
            GPUInstances = new Dictionary<string, GPUInstanceData>();
        }
        else
        {
            GPUInstances.Clear();
        }

        int hashSeed = seed + Mathf.RoundToInt(offsetX * 101) + Mathf.RoundToInt(offsetZ * 107);
        
        for (int i = 0; i < objectsPerChunk; i++)
        {
            float randomX = DeterministicHash(hashSeed + i * 2) * size;
            float randomZ = DeterministicHash(hashSeed + i * 3) * size;

            int xIdx = Mathf.FloorToInt(randomX);
            int zIdx = Mathf.FloorToInt(randomZ);

            xIdx = Mathf.Clamp(xIdx, 0, size);
            zIdx = Mathf.Clamp(zIdx, 0, size);

            List<spawnableObject> biomeObjectList = biomeGrid[xIdx, zIdx].sortObjects();
            if (biomeObjectList == null || biomeObjectList.Count == 0)
            {
                continue;
            }

            spawnableObject randomObject = biomeObjectList[(int)Math.Floor(DeterministicHash(hashSeed + i * 4) * biomeObjectList.Count)];
            if (randomObject == null || randomObject.obj == null)
            {
                continue;
            }

            float y = heightMap[xIdx, zIdx];
            Vector3 spawnPos = new Vector3((randomX - (float)size / 2) * chunkScale + offsetX * chunkSize * chunkScale, y, (randomZ - (float)size / 2) * chunkScale + offsetZ * chunkSize * chunkScale);
            string meshKey = meshKeys.FirstOrDefault(x => x.Value.obj == randomObject).Key;

            if (string.IsNullOrEmpty(meshKey)) {
                continue;
            }

            if (!result.ContainsKey(meshKey))
            {
                result.Add(meshKey, new List<(Matrix4x4, Vector3)>());
            }

            Vector3 displacement = Vector3.Normalize(new Vector3(DeterministicHash(hashSeed + i * 5) * 2 - 1, 0, DeterministicHash(hashSeed + i * 6) * 2 - 1)) * randomObject.positionOffset;
            spawnPos += displacement;

            float randomScaleVal = DeterministicHash(hashSeed + i * 7);
            float scale = (randomObject.maxScale - randomObject.minScale) * randomScaleVal + randomObject.minScale;
            Vector3 randomScale = new Vector3(scale, scale, scale);

            float randomRotationVal = DeterministicHash(hashSeed + i * 8);
            float rotation = (randomObject.maxRotation - randomObject.minRotation) * randomRotationVal + randomObject.minRotation;
            Quaternion randomRotation = Quaternion.Euler(0, rotation, 0);

            Vector3 normal = normals[xIdx * size + zIdx];

            result[meshKey].Add((Matrix4x4.TRS(spawnPos, randomRotation, randomScale), normal));
        }
        return result;
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
            
            objectRenderer.sharedMaterial.SetColor("_slopeColor", slopeColor);
            objectRenderer.sharedMaterial.SetFloat("_slopeBias", slopeBias);
            objectRenderer.sharedMaterial.SetFloat("_slopeSharpness", slopeSharpness);
        }
    }

    public void updateSeed(int newSeed) { seed = newSeed; RequestMeshUpdate(); }
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
    public void updateFogColor(Color newFogColor) { fogColor = newFogColor; UpdateMaterialProperties(); }
    public void updateFogStart(float newFogStart) { fogStart = newFogStart; UpdateMaterialProperties(); }
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
                float y_value = noise.FBM_NoiseValue(sampleX, sampleZ) * amplitude;
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

        float biomeWeight = 1.0f / ((size + 1) * (size + 1));

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
                float sampleX = ((float)x + perlinOffsetX) / biomeScale;
                float sampleZ = ((float)z + perlinOffsetZ) / biomeScale;

                float tempNoiseVal = Adjust(tempNoise.FBM_NoiseValue(sampleX, sampleZ), biomeAdjustment);
                float humidityNoiseVal = Adjust(humidityNoise.FBM_NoiseValue(sampleX, sampleZ), biomeAdjustment);

                float tempValue = tempNoiseVal * maxTemp;
                float humidityValue = humidityNoiseVal * maxHumidity;

                Biome biome;
                int index = x * (size + 1) + z;
                float u = (float)x / size;
                float v = (float)z / size;

                if (heightMap[x, z] > highLandsTreshold)
                {
                    biome = highLandsBiomeMap.getBiome(tempValue, humidityValue);
                }
                else if (heightMap[x, z] > waterLevel + 10.0f)
                {
                    biome = biomeMap.getBiome(tempValue, humidityValue);
                }
                else
                {
                    biome = underWaterBiomeMap.getBiome(tempValue, humidityValue);
                }

                biomeGrid[x, z] = biome;
                objectsPerChunk += biome.objectsPerChunk * biomeWeight;

                if (heightMap[x, z] > highLandsTreshold)
                {
                    colors[index] = biome.Color;
                }
                else if (heightMap[x, z] > waterLevel + 10)
                {
                    colors[index] = Color.Lerp(
                    Color.Lerp(cornerColors[0, 0], cornerColors[1, 0], u),
                    Color.Lerp(cornerColors[0, 1], cornerColors[1, 1], u),
                    v);
                }
                else
                {
                    colors[index] = biome.Color;
                }
            }
        }
        return colors;
    }

    public void Start()
    {
        biomeMap = new biomeMap(maxTemp, maxHumidity, Biomes);
        underWaterBiomeMap = new biomeMap(maxTemp, maxHumidity, underWaterBiomes);
        highLandsBiomeMap = new biomeMap(maxTemp, maxHumidity, highlandsBiomes);
        noise = new FBM_Noise(seed, amplitude, persistence, frequency, lacunarity, octaves, sharpness);

        rng = new System.Random(seed);
        int tempSeed = rng.Next();
        int humditySeed = rng.Next();
        tempNoise = new FBM_Noise(tempSeed, 1f, 0.5f, 2f, 3f, 4);
        humidityNoise = new FBM_Noise(humditySeed, 1f, 0.5f, 2f, 3f, 4);

        RequestMeshUpdate();
    }


    static int INSTANCE_LIMIT = 1023;
    void Update()
    {
        if (GPUInstances == null)
        {
            return;
        }

        foreach (var group in GPUInstances)
        {
            int numBatches = Mathf.CeilToInt((float)group.Value.matrices.Count / INSTANCE_LIMIT);
            for (int i = 0; i < numBatches; i++)
            {
                int start = i * INSTANCE_LIMIT;
                int count = Mathf.Min(INSTANCE_LIMIT, group.Value.matrices.Count - start);

                Graphics.DrawMeshInstanced(group.Value.mesh, 0, group.Value.material, group.Value.matrices.GetRange(start, count));
            }
        }
    }

    private float DeterministicHash(int seed) {
        uint x = (uint)seed;
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = (x >> 16) ^ x;
        return x / 4294967296.0f;
    }
}