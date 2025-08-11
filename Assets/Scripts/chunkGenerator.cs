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
    public Material basicMaterial;
    private HashSet<Vector2> generatedChunks = new HashSet<Vector2>();
    private Dictionary<Vector2, GameObject> chunkObjects = new Dictionary<Vector2, GameObject>();
    private Vector3 refferencePosition;

    void Start()
    {
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
                        chunk.material = basicMaterial;
                    }
                }
            }
        };
        despawnChunks();
    }
}
