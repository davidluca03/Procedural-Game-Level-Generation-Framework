using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class spawnableObject
{
    public GameObject obj;
    public int seedOffset = 0;
    public float positionOffset = 0.0f;
    public float minScale = 1.0f;
    public float maxScale = 1.0f;
    public float minRotation = 0.0f;
    public float maxRotation = 0.0f;
    public float minDistance = 1.0f;
    public bool heightCondition = false;
    public float minHeight, maxHeight = 0.0f;
    public float heightBlendSize = 0.0f;
    public bool maxSlopeCondition = false;
    public float maxSlopeAngle = 90.0f;
    public bool randomSpawn = true;
    public float spawnChance = 0.5f;
    public bool noiseTresholdCondition = false;
    public float noiseSoftTreshold = 0.0f;
    public float noiseHardTreshold = 0.0f;
    public float noiseScale = 1.0f;
    public bool invertNoise = false;
    public bool ignoreCollisions = false;
    private FBM_Noise noise = null;

    private float GetLinearChance(float value, float hardTreshold, float softTreshold)
    {
        float min = Mathf.Min(hardTreshold, softTreshold);
        float max = Mathf.Max(hardTreshold, softTreshold);

        if (value < min) return 0f;
        if (value > max) return 1f;

        return Mathf.InverseLerp(min, max, value);
    }
    
    public bool confirmSpawn(float x, float y, float z, Vector3 normal, int seed)
    {
        Vector3 objDistance = new Vector3(x, y, z);
        float totalChance = 1.0f;

        int hashSeed = seed + Mathf.RoundToInt(x) * 101 + Mathf.RoundToInt(z) * 107 + seedOffset;
        
        if (maxSlopeCondition)
        {
            if (Vector3.Angle(normal, Vector3.up) > maxSlopeAngle)
                return false;
        }

        if (heightCondition)
        {
            float minHard = minHeight + heightBlendSize / 2.0f;
            float minSoft = minHeight - heightBlendSize / 2.0f;
            float maxHard = maxHeight - heightBlendSize / 2.0f;
            float maxSoft = maxHeight + heightBlendSize / 2.0f;

            if (y > maxSoft || y < minSoft)
            {
                return false;
            }
            
            if (y < minHard)
            {
                totalChance *= GetLinearChance(y, minSoft, minHard);
            }
            
            if (y > maxHard)
            {
                totalChance *= 1.0f - GetLinearChance(y, maxHard, maxSoft);
            }
        }

        if (randomSpawn)
        {
            totalChance *= spawnChance;
        }

        if (noiseTresholdCondition)
        {
            if (noise == null)
            {
                noise = new FBM_Noise(hashSeed, 1.0f, 0.5f, 1.0f, 2.0f, 4);
            }

            float noiseValue = noise.FBM_NoiseValue(x / noiseScale, z / noiseScale);

            if (invertNoise)
                noiseValue = 1.0f - noiseValue;

            totalChance *= GetLinearChance(noiseValue, noiseHardTreshold, noiseSoftTreshold);
        }

        if (totalChance <= DeterministicHash(hashSeed + 9))
            return false;

        return true;
    }

    private float DeterministicHash(int seed) {
        uint x = (uint)seed;
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = (x >> 16) ^ x;
        return x / 4294967296.0f;
    }
}