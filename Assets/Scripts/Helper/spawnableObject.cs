using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[System.Serializable]
public class spawnableObject
{
    public GameObject obj;
    public int seedOffset = 0;
    public float minScale = 1.0f;
    public float maxScale = 1.0f;
    public float minRotation = 0.0f;
    public float maxRotation = 0.0f;
    public int minDistance = 1;
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
    private FBM_Noise noise = null;
    private System.Random r = null;

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

        if (r == null)
        {
            int hashSeed = seed + Mathf.RoundToInt(x) * 101 + Mathf.RoundToInt(y) * 103 + Mathf.RoundToInt(z) * 107 + seedOffset;
            r = new System.Random(hashSeed);
        }
        
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
                noise = new FBM_Noise(seed + seedOffset, 1.0f, 0.5f, 1.0f, 2.0f, 4);
            }

            float noiseValue = noise.FBM_NoiseValue(x / noiseScale, z / noiseScale);

            if (invertNoise)
                noiseValue = 1.0f - noiseValue;

            totalChance *= GetLinearChance(noiseValue, noiseHardTreshold, noiseSoftTreshold);
        }

        if (totalChance <= (float)r.NextDouble())
            return false;

        return true;
    }
}