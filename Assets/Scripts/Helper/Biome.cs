using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Biome
{
    public string name;
    public Color Color;
    public float minTemp, maxTemp;
    public float minHumidity, maxHumidity;
    public List<spawnableObject> objects;
    public int objectsPerChunk = 40;
    private bool isSorted = false;

    public List<spawnableObject> sortObjects()
    {
        if (!isSorted)
        {
            isSorted = true;
            this.objects.Sort((a, b) => b.minDistance.CompareTo(a.minDistance));
        }
        return this.objects;
    }
}
