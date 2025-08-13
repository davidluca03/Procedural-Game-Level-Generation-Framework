using UnityEngine;

[System.Serializable]
public class Biome
{
    public string Name;
    public Color Color;
    public float minTemp, maxTemp;
    public float minHumidity, maxHumidity;
    public float perlinScale;
}
