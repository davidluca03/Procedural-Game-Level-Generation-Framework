using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class biomeMap
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Biome> biomes = new List<Biome>();
    public float maxTemp = 100f;
    public float maxHumidity = 100f;

    public biomeMap(float maxTemp, float maxHumidity, List<Biome> biomes)
    {
        this.maxTemp = maxTemp;
        this.maxHumidity = maxHumidity;
        this.biomes = biomes;
    }

    public Color getBiomeColor(float temperature, float humidity)
    {
        foreach (Biome biome in biomes)
        {
            if (temperature >= biome.minTemp && temperature <= biome.maxTemp &&
                humidity >= biome.minHumidity && humidity <= biome.maxHumidity)
            {
                Debug.Log(biome.Name);
                return biome.Color;
            }
        }
        Debug.Log(temperature + " " + humidity);
        return Color.black;
    }
}
