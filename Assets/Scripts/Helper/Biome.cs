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
}
