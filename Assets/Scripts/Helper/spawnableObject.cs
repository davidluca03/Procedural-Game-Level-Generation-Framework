using System;
using UnityEngine;

[System.Serializable]
public class spawnableObject
{
    public GameObject obj;
    public float minScale = 1.0f;
    public float maxScale = 1.0f;
    public float minRotation = 0.0f;
    public float maxRotation = 0.0f;
    public int minDistance = 1;
    public bool heightCondition = false;
    public float minHeight, maxHeight = 0.0f;
    public bool maxSlopeCondition = false;
    public float maxSlopeAngle = 90.0f;
    public bool randomSpawn = true;
    public float spawnChance = 0.5f;

    public bool confirmSpawn(float x, float y, float z, Vector3 normal, int seed)
    {
        bool slopeCondition = true;
        bool randomSpawnCondition = true;

        if (maxSlopeCondition)
        {
            if (Vector3.Angle(normal, Vector3.up) > maxSlopeAngle)
                slopeCondition = false;
        }

        if (randomSpawn)
        {
            int hashSeed = seed + Mathf.RoundToInt(x) * 101 + Mathf.RoundToInt(y) * 103 + Mathf.RoundToInt(z) * 107;

            System.Random randomGenerator = new System.Random(hashSeed);

            float randomValue = (float)randomGenerator.NextDouble();

            randomSpawnCondition = randomValue < spawnChance;
        }

        return randomSpawnCondition && slopeCondition;
    }
}