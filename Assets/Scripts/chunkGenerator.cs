using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using UnityEngine.Analytics;
using System.Runtime.CompilerServices;

public class chunkGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject chunkPrefab;
    public GameObject refferenceObject;
    public int renderRadius = 5;
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
    public Color fogColor = Color.white;
    public float fogStart = 0.0f;
    public Material basicMaterial;
    public Boolean UseBasicMaterial = true;
    public Color planeColor = Color.darkGreen;
    public Color slopeColor = Color.dimGray;
    public float slopeBias = 0.5f;
    public float slopeSharpness = 25.0f;
    private Boolean previousUseBasicMaterial = true;
    private Color previousPlaneColor = Color.darkGreen;
    private Color previousSlopeColor = Color.dimGray;
    private float previousSlopeBias = 0.5f;
    private float previousSlopeSharpness = 25.0f;
    private HashSet<Vector2> generatedChunks = new HashSet<Vector2>();
    private Dictionary<Vector2, GameObject> chunkObjects = new Dictionary<Vector2, GameObject>();
    private Vector3 refferencePosition;
    private Color previousFogColor = Color.white;
    private float previousFogStart = 0.0f;
    private float previousOctaves = 1.0f;
    private float previousAmplitude = 1.0f;
    private float previousPerlinScale = 1.0f;
    private float previousPersistence = 0.5f;
    private float previousFrequency = 1.0f;
    private float previousLacunarity = 2.0f;
    private float previousUVScale = 1.0f;
    private float previousSharpness = 1.0f;
    private float previousMaxHeight = 1.0f;
    private float previousNoiseBias = 0.0f;
    private Boolean previousAbsoluteHeight = false;
    private float updateInterval = 1.0f;
    private float timer = 0.0f;
    void Start()
    {
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
        previousOctaves = octaves;
        previousUseBasicMaterial = UseBasicMaterial;
        previousPlaneColor = planeColor;
        previousSlopeColor = slopeColor;
        previousSlopeBias = slopeBias;
        previousSlopeSharpness = slopeSharpness;
        previousAbsoluteHeight = absoluteHeight;
        previousFogStart = fogStart;
    }

    private Boolean equalColors(Color color1, Color color2, float tolerance = 0.01f) {
        return Mathf.Abs(color1.r - color2.r) < tolerance &&
               Mathf.Abs(color1.g - color2.g) < tolerance &&
               Mathf.Abs(color1.b - color2.b) < tolerance &&
               Mathf.Abs(color1.a - color2.a) < tolerance;
    }

    private Boolean compareFloats(float a, float b, float tolerance = 0.01f) {
        return Mathf.Abs(a - b) < tolerance;
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

    private void updateOctaves(float newOctaves)
    {
        UpdateAllChunks(chunk => chunk.updateOctaves(newOctaves));
    }

    private void updateAmplitude(float newAmplitude)
    {
        UpdateAllChunks(chunk => chunk.updateAmplitude(newAmplitude));
    }

    private void updatePersistence(float newPersistence)
    {
        UpdateAllChunks(chunk => chunk.updatePersistence(newPersistence));
    }

    private void updateFrequency(float newFrequency)
    {
        UpdateAllChunks(chunk => chunk.updateFrequency(newFrequency));
    }

    private void updateLacunarity(float newLacunarity)
    {
        UpdateAllChunks(chunk => chunk.updateLacunarity(newLacunarity));
    }

    private void updatePerlinScale(float newPerlinScale)
    {
        UpdateAllChunks(chunk => chunk.updatePerlinScale(newPerlinScale));
    }

    private void updateFogColor(Color newFogColor)
    {
        UpdateAllChunks(chunk => chunk.updateFogColor(newFogColor));
    }

    private void updateFogStart(float newFogStart)
    {
        UpdateAllChunks(chunk => chunk.updateFogStart(newFogStart));
    }

    private void updateUseBasicMaterial(bool newUseBasicMaterial)
    {
        UpdateAllChunks(chunk => chunk.updateUsingBasicMaterial(newUseBasicMaterial));
    }

    private void updatePlaneColor(Color newPlaneColor)
    {
        UpdateAllChunks(chunk => chunk.updatePlaneColor(newPlaneColor));
    }

    private void updateSlopeColor(Color newSlopeColor)
    {
        UpdateAllChunks(chunk => chunk.updateSlopeColor(newSlopeColor));
    }

    private void updateSlopeBias(float newSlopeBias)
    {
        UpdateAllChunks(chunk => chunk.updateSlopeBias(newSlopeBias));
    }

    private void updateSlopeSharpness(float newSlopeSharpness)
    {
        UpdateAllChunks(chunk => chunk.updateSlopeSharpness(newSlopeSharpness));
    }

    private void updateUVScale(float newUVScale)
    {
        UpdateAllChunks(chunk => chunk.updateUVScale(newUVScale));
    }

    private void updateSharpness(float newSharpness)
    {
        UpdateAllChunks(chunk => chunk.updateSharpness(newSharpness));
    }

    private void updateMaxHeight(float newMaxHeight)
    {
        UpdateAllChunks(chunk => chunk.updateMaxHeight(newMaxHeight));
    }

    private void updateNoiseBias(float newNoiseBias)
    {
        UpdateAllChunks(chunk => chunk.updateNoiseBias(newNoiseBias));
    }

    private void updateAbsoluteHeight(Boolean newAbsoluteHeight)
    {
        UpdateAllChunks(chunk => chunk.updateAbsoluteHeight(newAbsoluteHeight));
    }

    private void UpdateAttributesCheck()
    {
        if (!compareFloats(previousAmplitude, amplitude))
        {
            updateAmplitude(amplitude);
            previousAmplitude = amplitude;
            Debug.Log("Amplitude updated to: " + amplitude);
        }

        if (!compareFloats(previousPersistence, persistence))
        {
            updatePersistence(persistence);
            previousPersistence = persistence;
            Debug.Log("Persistence updated to: " + persistence);
        }

        if (!compareFloats(previousFrequency, frequency))
        {
            updateFrequency(frequency);
            previousFrequency = frequency;
            Debug.Log("Frequency updated to: " + frequency);
        }

        if (!compareFloats(previousLacunarity, lacunarity))
        {
            updateLacunarity(lacunarity);
            previousLacunarity = lacunarity;
            Debug.Log("Lacunarity updated to: " + lacunarity);
        }

        if (!compareFloats(previousPerlinScale, perlinScale))
        {
            updatePerlinScale(perlinScale);
            previousPerlinScale = perlinScale;
            Debug.Log("Perlin scale updated to: " + perlinScale);
        }

        if (!compareFloats(previousUVScale, UVscale))
        {
            updateUVScale(UVscale);
            previousUVScale = UVscale;
            Debug.Log("UV scale updated to: " + UVscale);
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
            Debug.Log("Fog start updated to: " + fogStart);
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
            Debug.Log("Slope bias updated to: " + slopeBias);
        }

        if (!compareFloats(previousSlopeSharpness, slopeSharpness))
        {
            updateSlopeSharpness(slopeSharpness);
            previousSlopeSharpness = slopeSharpness;
            Debug.Log("Slope sharpness updated to: " + slopeSharpness);
        }

        if (!compareFloats(previousSharpness, sharpness))
        {
            updateSharpness(sharpness);
            previousSharpness = sharpness;
            Debug.Log("Sharpness updated to: " + sharpness);
        }

        if (!compareFloats(previousMaxHeight, maxHeight))
        {
            updateMaxHeight(maxHeight);
            previousMaxHeight = maxHeight;
            Debug.Log("Max height updated to: " + maxHeight);
        }

        if (!compareFloats(previousNoiseBias, noiseBias))
        {
            updateNoiseBias(noiseBias);
            previousNoiseBias = noiseBias;
            Debug.Log("Noise bias updated to: " + noiseBias);
        }

        if (!compareFloats(previousOctaves, octaves))
        {
            updateOctaves(octaves);
            previousOctaves = octaves;
            Debug.Log("Octaves updated to: " + octaves);
        }

        if (previousAbsoluteHeight != absoluteHeight)
        {
            updateAbsoluteHeight(absoluteHeight);
            previousAbsoluteHeight = absoluteHeight;
            Debug.Log("Absolute height updated to: " + absoluteHeight);
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

    void Update()
    {
        refferencePosition = refferenceObject.transform.position / chunkScale / chunkSize;

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
                        chunk.octaves = octaves;
                        chunk.persistence = persistence;
                        chunk.lacunarity = lacunarity;
                        chunk.frequency = frequency;
                        chunk.perlinOffsetX = perlinOffsetX + i * chunkSize;
                        chunk.perlinOffsetZ = perlinOffsetZ + j * chunkSize;
                        chunk.offsetX = offsetX + i * chunkSize * chunkScale;
                        chunk.offsetZ = offsetZ + j * chunkSize * chunkScale;
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
                    }
                }
            }
        }
        checkForUpdates();
        despawnChunks();
    }
}
