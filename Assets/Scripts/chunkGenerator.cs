using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;


public class chunkGenerator : MonoBehaviour
{
    public GameObject chunkPrefab;
    public GameObject refferenceObject;
    public int renderRadius = 5;
    public int chunkSize = 16;
    public float chunkScale = 1;
    public float UVscale = 1;
    public float perlinScale = 1.0f;
    public int seed = 0;
    public Gradient gradient;
    public List<Biome> Biomes;
    public float biomeAdjustment = 1.0f;
    public float maxTemp = 100.0f;
    public float maxHumidity = 100.0f;
    public float biomeScale = 1.0f;
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
    public Color fogColor = Color.white;
    public float fogStart = 0.0f;
    public Material basicMaterial;
    public bool UseBasicMaterial = true;
    public Color planeColor = Color.darkGreen;
    public Color slopeColor = Color.dimGray;
    public float slopeBias = 0.5f;
    public float slopeSharpness = 25.0f;
    private bool previousUseBasicMaterial = true;
    private Color previousPlaneColor = Color.darkGreen;
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
    private Gradient previousGradient;
    private CancellationTokenSource cancellationTokenSource;
    
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
        previousUseBasicMaterial = UseBasicMaterial;
        previousPlaneColor = planeColor;
        previousSlopeColor = slopeColor;
        previousSlopeBias = slopeBias;
        previousSlopeSharpness = slopeSharpness;
        previousAbsoluteHeight = absoluteHeight;
        previousFogStart = fogStart;
        refferencePosition = refferenceObject.transform.position / chunkScale / chunkSize;
        previousGradient = new Gradient();
        previousGradient.SetKeys(gradient.colorKeys, gradient.alphaKeys);
        SpawnChunks();
    }

    private bool equalColors(Color color1, Color color2, float tolerance = 0.01f) {
        return Mathf.Abs(color1.r - color2.r) < tolerance &&
               Mathf.Abs(color1.g - color2.g) < tolerance &&
               Mathf.Abs(color1.b - color2.b) < tolerance &&
               Mathf.Abs(color1.a - color2.a) < tolerance;
    }

    private bool compareFloats(float a, float b, float tolerance = 0.01f) {
        return Mathf.Abs(a - b) < tolerance;
    }

    public bool AreGradientsApproximatelyEqual(Gradient g1, Gradient g2, float tolerance = 0.001f)
    {
        if (g1.colorKeys.Length != g2.colorKeys.Length || g1.alphaKeys.Length != g2.alphaKeys.Length)
        {
            return false;
        }

        for (int i = 0; i < g1.colorKeys.Length; i++)
        {
            GradientColorKey key1 = g1.colorKeys[i];
            GradientColorKey key2 = g2.colorKeys[i];

            if (Mathf.Abs(key1.time - key2.time) > tolerance ||
                !equalColors(key1.color, key2.color, tolerance))
            {
                return false;
            }
        }

        for (int i = 0; i < g1.alphaKeys.Length; i++)
        {
            GradientAlphaKey key1 = g1.alphaKeys[i];
            GradientAlphaKey key2 = g2.alphaKeys[i];
            
            if (Mathf.Abs(key1.time - key2.time) > tolerance ||
                Mathf.Abs(key1.alpha - key2.alpha) > tolerance)
            {
                return false;
            }
        }
        return true;
    }

    private void UpdateAllChunks(Action<chunkScript> updateAction)
    {
        foreach (var chunk in chunkObjects.Values)
        {
            if (chunk.TryGetComponent(out chunkScript chunkComponent))
            {
                updateAction(chunkComponent);
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
    private void updateUseBasicMaterial(bool newUseBasicMaterial) { UpdateAllChunks(chunk => chunk.updateUsingBasicMaterial(newUseBasicMaterial)); }
    private void updatePlaneColor(Color newPlaneColor) { UpdateAllChunks(chunk => chunk.updatePlaneColor(newPlaneColor)); }
    private void updateSlopeColor(Color newSlopeColor) { UpdateAllChunks(chunk => chunk.updateSlopeColor(newSlopeColor)); }
    private void updateSlopeBias(float newSlopeBias) { UpdateAllChunks(chunk => chunk.updateSlopeBias(newSlopeBias)); }
    private void updateSlopeSharpness(float newSlopeSharpness) { UpdateAllChunks(chunk => chunk.updateSlopeSharpness(newSlopeSharpness)); }
    private void updateUVScale(float newUVScale) { UpdateAllChunks(chunk => chunk.updateUVScale(newUVScale)); }
    private void updateSharpness(float newSharpness) { UpdateAllChunks(chunk => chunk.updateSharpness(newSharpness)); }
    private void updateMaxHeight(float newMaxHeight) { UpdateAllChunks(chunk => chunk.updateMaxHeight(newMaxHeight)); }
    private void updateNoiseBias(float newNoiseBias) { UpdateAllChunks(chunk => chunk.updateNoiseBias(newNoiseBias)); }
    private void updateAbsoluteHeight(bool newAbsoluteHeight) { UpdateAllChunks(chunk => chunk.updateAbsoluteHeight(newAbsoluteHeight)); }
    private void updateGradient(Gradient newGradient) { UpdateAllChunks(chunk => chunk.updateGradient(newGradient)); }

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
        if (previousUseBasicMaterial != UseBasicMaterial)
        {
            updateUseBasicMaterial(UseBasicMaterial);
            previousUseBasicMaterial = UseBasicMaterial;
        }
        if (!equalColors(previousPlaneColor, planeColor))
        {
            updatePlaneColor(planeColor);
            previousPlaneColor = planeColor;
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
        if (!AreGradientsApproximatelyEqual(gradient, previousGradient))
        {
            updateGradient(gradient);
            previousGradient.SetKeys(gradient.colorKeys, gradient.alphaKeys);
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
        }
        foreach (var chunkCoord in chunksToRemove)
        {
            if (chunkObjects.TryGetValue(chunkCoord, out GameObject chunkObject))
            {
                chunkObjects.Remove(chunkCoord);
                generatedChunks.Remove(chunkCoord);
                Destroy(chunkObject);
            }
        }
    }

    private async void SpawnChunks()
    {
        List<Task> spawnTasks = new List<Task>();

        for (float i = Mathf.Ceil(refferencePosition.x) - renderRadius; i <= Mathf.Ceil(refferencePosition.x) + renderRadius; i++)
        {
            for (float j = Mathf.Ceil(refferencePosition.z) - renderRadius; j <= Mathf.Ceil(refferencePosition.z) + renderRadius; j++)
            {
                Vector2 chunkCoord = new Vector2(i, j);

                if (!generatedChunks.Contains(chunkCoord))
                {
                    generatedChunks.Add(chunkCoord);
                    Vector3 chunkPosition = new Vector3(i * chunkSize * chunkScale, 0, j * chunkSize * chunkScale);
                    GameObject chunkObject = Instantiate(chunkPrefab, chunkPosition, Quaternion.identity);
                    chunkScript chunk = chunkObject.GetComponent<chunkScript>();

                    if (chunk != null)
                    {
                        chunkObjects[chunkCoord] = chunkObject;
                        chunk.chunkSize = chunkSize;
                        chunk.chunkScale = chunkScale;
                        chunk.UVscale = UVscale;
                        chunk.perlinScale = perlinScale;
                        chunk.seed = seed;
                        chunk.Biomes = Biomes;
                        chunk.biomeAdjustment = biomeAdjustment;
                        chunk.maxTemp = maxTemp;
                        chunk.maxHumidity = maxHumidity;
                        chunk.biomeScale = biomeScale;
                        chunk.octaves = octaves;
                        chunk.persistence = persistence;
                        chunk.lacunarity = lacunarity;
                        chunk.frequency = frequency;
                        chunk.perlinOffsetX = perlinOffsetX + i * chunkSize;
                        chunk.perlinOffsetZ = perlinOffsetZ + j * chunkSize;
                        chunk.offsetX = offsetX + i * chunkSize * chunkScale;
                        chunk.offsetZ = offsetZ + j * chunkSize * chunkScale;
                        chunk.gradient = gradient;
                        chunk.amplitude = amplitude;
                        chunk.basicMaterial = basicMaterial;
                        chunk.UseBasicMaterial = UseBasicMaterial;
                        chunk.planeColor = planeColor;
                        chunk.slopeColor = slopeColor;
                        chunk.slopeBias = slopeBias;
                        chunk.slopeSharpness = slopeSharpness;
                        chunk.fogColor = fogColor;
                        chunk.fogStart = fogStart;
                        chunk.sharpness = sharpness;
                        chunk.maxHeight = maxHeight;
                        chunk.noiseBias = noiseBias;
                        chunk.absoluteHeight = absoluteHeight;
                        spawnTasks.Add(chunk.UpdateMeshAsync(cancellationTokenSource.Token));
                    }
                }
            }
        }
        await Task.WhenAll(spawnTasks);
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