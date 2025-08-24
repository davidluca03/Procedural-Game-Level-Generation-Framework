using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;

public class chunkGenerator : MonoBehaviour
{
    [Header("Chunk Settings")]
    public GameObject chunkPrefab;
    public GameObject refferenceObject;
    public int renderRadius = 5;
    public int chunkSize = 16;
    public float chunkScale = 1;
    public float objectRenderDistance = 0;

    [Header("Noise & Perlin Settings")]
    public int seed = 0;
    public float perlinScale = 1.0f;
    public int octaves = 1;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2.0f;
    public float frequency = 1.0f;
    public float perlinOffsetX = 0.0f;
    public float perlinOffsetZ = 0.0f;
    public float amplitude = 1.0f;
    public float sharpness = 1.0f;
    public float maxHeight = 1.0f;
    public float noiseBias = 0.0f;
    public bool absoluteHeight = false;

    [Header("Biome Settings")]
    public List<Biome> Biomes;
    public List<Biome> underWaterBiomes;
    public List<Biome> highlandsBiomes;
    public float highLandsTreshold = 100000.0f;
    public float biomeAdjustment = 1.0f;
    public float maxTemp = 100.0f;
    public float maxHumidity = 100.0f;
    public float biomeScale = 1.0f;
    public int biomeColorBlend = 1;
    public int blendSampleDistance = 1;

    [Header("Material & Visuals")]
    public Material basicMaterial;
    public Color slopeColor = Color.dimGray;
    public float slopeBias = 0.5f;
    public float slopeSharpness = 25.0f;

    [Header("Fog Settings")]
    public Color fogColor = Color.white;
    public float fogStart = 0.0f;

    [Header("UV Settings")]
    public float UVscale = 1;

    [Header("Water Settings")]
    public GameObject waterPrefab;
    public float waterLevel = 0.0f;
    public float waterUVScale = 1.0f;
    private Dictionary<Vector2, GameObject> waterObjects = new Dictionary<Vector2, GameObject>();
    private float previousWaterLevel = 0.0f;
    private float previousWaterUVScale = 1.0f;
    private Color previousSlopeColor = Color.dimGray;
    private float previousSlopeBias = 0.5f;
    private float previousSlopeSharpness = 25.0f;
    private HashSet<Vector2> generatedChunks = new HashSet<Vector2>();
    private Dictionary<Vector2, GameObject> chunkObjects = new Dictionary<Vector2, GameObject>();
    private Vector3 refferencePosition;
    private Color previousFogColor = Color.white;
    private float previousFogStart = 0.0f;
    private int previousSeed = 0;
    private int previousOctaves = 1;
    private float previousAmplitude = 1.0f;
    private float previousPerlinScale = 1.0f;
    private float previousPersistence = 0.5f;
    private float previousFrequency = 1.0f;
    private float previousLacunarity = 2.0f;
    private float previousUVScale = 1.0f;
    private float previousSharpness = 1.0f;
    private float previousMaxHeight = 1.0f;
    private float previousNoiseBias = 0.0f;
    private bool previousAbsoluteHeight = false;
    private float updateInterval = 1.0f;
    private float timer = 0.0f;
    private CancellationTokenSource cancellationTokenSource;

    public class PositionData
    {
        public Vector3 position;
        public float restrictionRadius;
    }

    void Start()
    {
        cancellationTokenSource = new CancellationTokenSource();
        previousAmplitude = amplitude;
        previousPerlinScale = perlinScale;
        previousPersistence = persistence;
        previousFrequency = frequency;
        previousLacunarity = lacunarity;
        previousUVScale = UVscale;
        previousFogColor = fogColor;
        previousSharpness = sharpness;
        previousMaxHeight = maxHeight;
        previousNoiseBias = noiseBias;
        previousSeed = seed;
        previousOctaves = octaves;
        previousSlopeColor = slopeColor;
        previousSlopeBias = slopeBias;
        previousSlopeSharpness = slopeSharpness;
        previousAbsoluteHeight = absoluteHeight;
        previousFogStart = fogStart;
        previousWaterLevel = waterLevel;
        previousWaterUVScale = waterUVScale;
        refferencePosition = refferenceObject.transform.position / chunkScale / chunkSize;
        SpawnChunks();
    }

    private bool equalColors(Color color1, Color color2, float tolerance = 0.01f)
    {
        return Mathf.Abs(color1.r - color2.r) < tolerance &&
               Mathf.Abs(color1.g - color2.g) < tolerance &&
               Mathf.Abs(color1.b - color2.b) < tolerance &&
               Mathf.Abs(color1.a - color2.a) < tolerance;
    }

    private bool compareFloats(float a, float b, float tolerance = 0.01f)
    {
        return Mathf.Abs(a - b) < tolerance;
    }

    private void UpdateAllChunks(Action<chunkScript> updateAction)
    {
        List<GameObject> chunksToUpdate = new List<GameObject>(chunkObjects.Values);
        foreach (var chunk in chunksToUpdate)
        {
            if (chunk == null)
            {
                continue;
            }
            if (chunk.TryGetComponent(out chunkScript chunkComponent))
            {
                updateAction(chunkComponent);
            }
        }
    }

    private void UpdateAllWaterChunks(Action<WaterScript> updateAction)
    {
        List<GameObject> waterChunksToUpdate = new List<GameObject>(waterObjects.Values);
        foreach (var waterChunk in waterChunksToUpdate)
        {
            if (waterChunk == null)
            {
                continue;
            }
            if (waterChunk.TryGetComponent(out WaterScript waterComponent))
            {
                updateAction(waterComponent);
            }
        }
    }

    private void updateSeed(int newSeed) { UpdateAllChunks(chunk => chunk.updateSeed(newSeed)); }
    private void updateOctaves(int newOctaves) { UpdateAllChunks(chunk => chunk.updateOctaves(newOctaves)); }
    private void updateAmplitude(float newAmplitude) { UpdateAllChunks(chunk => chunk.updateAmplitude(newAmplitude)); }
    private void updatePersistence(float newPersistence) { UpdateAllChunks(chunk => chunk.updatePersistence(newPersistence)); }
    private void updateFrequency(float newFrequency) { UpdateAllChunks(chunk => chunk.updateFrequency(newFrequency)); }
    private void updateLacunarity(float newLacunarity) { UpdateAllChunks(chunk => chunk.updateLacunarity(newLacunarity)); }
    private void updatePerlinScale(float newPerlinScale) { UpdateAllChunks(chunk => chunk.updatePerlinScale(newPerlinScale)); }
    private void updateFogColor(Color newFogColor) { UpdateAllChunks(chunk => chunk.updateFogColor(newFogColor)); }
    private void updateFogStart(float newFogStart) { UpdateAllChunks(chunk => chunk.updateFogStart(newFogStart)); }
    private void updateSlopeColor(Color newSlopeColor) { UpdateAllChunks(chunk => chunk.updateSlopeColor(newSlopeColor)); }
    private void updateSlopeBias(float newSlopeBias) { UpdateAllChunks(chunk => chunk.updateSlopeBias(newSlopeBias)); }
    private void updateSlopeSharpness(float newSlopeSharpness) { UpdateAllChunks(chunk => chunk.updateSlopeSharpness(newSlopeSharpness)); }
    private void updateUVScale(float newUVScale) { UpdateAllChunks(chunk => chunk.updateUVScale(newUVScale)); }
    private void updateSharpness(float newSharpness) { UpdateAllChunks(chunk => chunk.updateSharpness(newSharpness)); }
    private void updateMaxHeight(float newMaxHeight) { UpdateAllChunks(chunk => chunk.updateMaxHeight(newMaxHeight)); }
    private void updateNoiseBias(float newNoiseBias) { UpdateAllChunks(chunk => chunk.updateNoiseBias(newNoiseBias)); }
    private void updateAbsoluteHeight(bool newAbsoluteHeight) { UpdateAllChunks(chunk => chunk.updateAbsoluteHeight(newAbsoluteHeight)); }
    private void updateWaterLevel(float newWaterLevel) { UpdateAllWaterChunks(waterChunk => waterChunk.updateWaterLevel(newWaterLevel)); }
    private void updateWaterUVScale(float newWaterUVScale) { UpdateAllWaterChunks(waterChunk => waterChunk.waterUVScale = newWaterUVScale); }

    private void UpdateAttributesCheck()
    {
        if (!compareFloats(previousAmplitude, amplitude))
        {
            updateAmplitude(amplitude);
            previousAmplitude = amplitude;
        }
        if (!compareFloats(previousPersistence, persistence))
        {
            updatePersistence(persistence);
            previousPersistence = persistence;
        }
        if (!compareFloats(previousFrequency, frequency))
        {
            updateFrequency(frequency);
            previousFrequency = frequency;
        }
        if (!compareFloats(previousLacunarity, lacunarity))
        {
            updateLacunarity(lacunarity);
            previousLacunarity = lacunarity;
        }
        if (!compareFloats(previousPerlinScale, perlinScale))
        {
            updatePerlinScale(perlinScale);
            previousPerlinScale = perlinScale;
        }
        if (!compareFloats(previousUVScale, UVscale))
        {
            updateUVScale(UVscale);
            previousUVScale = UVscale;
        }
        if (!equalColors(previousFogColor, fogColor))
        {
            updateFogColor(fogColor);
            previousFogColor = fogColor;
        }
        if (!compareFloats(previousFogStart, fogStart))
        {
            updateFogStart(fogStart);
            previousFogStart = fogStart;
        }
        if (!equalColors(previousSlopeColor, slopeColor))
        {
            updateSlopeColor(slopeColor);
            previousSlopeColor = slopeColor;
        }
        if (!compareFloats(previousSlopeBias, slopeBias))
        {
            updateSlopeBias(slopeBias);
            previousSlopeBias = slopeBias;
        }
        if (!compareFloats(previousSlopeSharpness, slopeSharpness))
        {
            updateSlopeSharpness(slopeSharpness);
            previousSlopeSharpness = slopeSharpness;
        }
        if (!compareFloats(previousSharpness, sharpness))
        {
            updateSharpness(sharpness);
            previousSharpness = sharpness;
        }
        if (!compareFloats(previousMaxHeight, maxHeight))
        {
            updateMaxHeight(maxHeight);
            previousMaxHeight = maxHeight;
        }
        if (!compareFloats(previousNoiseBias, noiseBias))
        {
            updateNoiseBias(noiseBias);
            previousNoiseBias = noiseBias;
        }
        if (seed != previousSeed)
        {
            updateSeed(seed);
            previousSeed = seed;
        }
        if (previousOctaves != octaves)
        {
            updateOctaves(octaves);
            previousOctaves = octaves;
        }
        if (previousAbsoluteHeight != absoluteHeight)
        {
            updateAbsoluteHeight(absoluteHeight);
            previousAbsoluteHeight = absoluteHeight;
        }

        if (!compareFloats(previousWaterLevel, waterLevel))
        {
            updateWaterLevel(waterLevel);
            previousWaterLevel = waterLevel;
        }
        if (!compareFloats(previousWaterUVScale, waterUVScale))
        {
            updateWaterUVScale(waterUVScale);
            previousWaterUVScale = waterUVScale;
        }
    }

    private void checkForUpdates()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0.0f;
            UpdateAttributesCheck();
        }
    }

    private void despawnChunks()
    {
        List<Vector2> chunksToRemove = new List<Vector2>();
        foreach (var chunkCoord in chunkObjects.Keys)
        {
            if (Mathf.Abs(chunkCoord.x - refferencePosition.x) > renderRadius ||
                Mathf.Abs(chunkCoord.y - refferencePosition.z) > renderRadius)
            {
                chunksToRemove.Add(chunkCoord);
            }
            else if (Mathf.Abs(chunkCoord.x - refferencePosition.x) > objectRenderDistance ||
                Mathf.Abs(chunkCoord.y - refferencePosition.z) > objectRenderDistance)
            {
                chunkObjects[chunkCoord].GetComponent<chunkScript>().removeObjects();
            }
            else
            {
                chunkObjects[chunkCoord].GetComponent<chunkScript>().addObjects();
            }
        }

        if (chunksToRemove.Count > 0)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            if (chunkObjects.TryGetValue(chunkCoord, out GameObject chunkObject))
            {
                chunkObjects.Remove(chunkCoord);
                generatedChunks.Remove(chunkCoord);
                Destroy(chunkObject);
            }
            if (waterObjects.TryGetValue(chunkCoord, out GameObject waterObject))
            {
                waterObjects.Remove(chunkCoord);
                Destroy(waterObject);
            }
        }
    }

    private void SpawnChunks()
{
    for (int i = Mathf.FloorToInt(refferencePosition.x) - renderRadius; 
         i <= Mathf.FloorToInt(refferencePosition.x) + renderRadius; i++)
    {
        for (int j = Mathf.FloorToInt(refferencePosition.z) - renderRadius; 
             j <= Mathf.FloorToInt(refferencePosition.z) + renderRadius; j++)
        {
            Vector2 chunkCoord = new Vector2(i, j);

            if (!generatedChunks.Contains(chunkCoord))
            {
                generatedChunks.Add(chunkCoord);

                Vector3 chunkPosition = new Vector3(
                    i * chunkSize * chunkScale,
                    0,
                    j * chunkSize * chunkScale
                );

                GameObject chunkObject = Instantiate(chunkPrefab, chunkPosition, Quaternion.identity);
                chunkScript chunk = chunkObject.GetComponent<chunkScript>();

                if (chunk != null)
                {
                    chunkObjects[chunkCoord] = chunkObject;
                    chunkObject.isStatic = true;

                    chunk.chunkSize = chunkSize;
                    chunk.chunkScale = chunkScale;
                    chunk.UVscale = UVscale;
                    chunk.perlinScale = perlinScale;
                    chunk.seed = seed;
                    chunk.Biomes = Biomes;
                    chunk.underWaterBiomes = underWaterBiomes;
                    chunk.highlandsBiomes = highlandsBiomes;
                    chunk.highLandsTreshold = highLandsTreshold;
                    chunk.biomeAdjustment = biomeAdjustment;
                    chunk.biomeColorBlend = biomeColorBlend;
                    chunk.blendSampleDistance = blendSampleDistance;
                    chunk.maxTemp = maxTemp;
                    chunk.maxHumidity = maxHumidity;
                    chunk.biomeScale = biomeScale;
                    chunk.octaves = octaves;
                    chunk.persistence = persistence;
                    chunk.lacunarity = lacunarity;
                    chunk.frequency = frequency;
                    chunk.perlinOffsetX = perlinOffsetX + i * chunkSize;
                    chunk.perlinOffsetZ = perlinOffsetZ + j * chunkSize;
                    chunk.offsetX = chunkCoord.x;
                    chunk.offsetZ = chunkCoord.y;
                    chunk.amplitude = amplitude;
                    chunk.basicMaterial = basicMaterial;
                    chunk.slopeColor = slopeColor;
                    chunk.slopeBias = slopeBias;
                    chunk.slopeSharpness = slopeSharpness;
                    chunk.fogColor = fogColor;
                    chunk.fogStart = fogStart;
                    chunk.sharpness = sharpness;
                    chunk.maxHeight = maxHeight;
                    chunk.noiseBias = noiseBias;
                    chunk.absoluteHeight = absoluteHeight;
                    chunk.waterLevel = waterLevel;

                    _ = chunk.UpdateMeshAsync(cancellationTokenSource.Token);
                }

                if (waterPrefab != null)
                {
                    GameObject waterObject = Instantiate(waterPrefab, chunkPosition, Quaternion.identity);
                    WaterScript waterChunk = waterObject.GetComponent<WaterScript>();
                    if (waterChunk != null)
                    {
                        waterObjects[chunkCoord] = waterObject;
                        waterChunk.chunkSize = chunkSize;
                        waterChunk.chunkScale = chunkScale;
                        waterChunk.waterLevel = waterLevel;
                        waterChunk.waterUVScale = waterUVScale;
                        waterChunk.perlinOffsetX = perlinOffsetX + i * chunkSize;
                        waterChunk.perlinOffsetZ = perlinOffsetZ + j * chunkSize;
                    }
                }
            }
        }
    }
}

    void OnDisable()
    {
        cancellationTokenSource.Cancel();
    }

    void Update()
    {
        refferencePosition = refferenceObject.transform.position / chunkScale / chunkSize;
        SpawnChunks();
        checkForUpdates();
        despawnChunks();
    }
}



