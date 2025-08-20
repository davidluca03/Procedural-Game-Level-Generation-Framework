using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class biomeMap
{
    public List<Biome> biomes = new List<Biome>();
    public float maxTemp = 100f;
    public float maxHumidity = 100f;
    private Biome[,] biomeGrid;
    private int gridSize = 32;

    public biomeMap(float maxTemp, float maxHumidity, List<Biome> biomes)
    {
        this.maxTemp = maxTemp;
        this.maxHumidity = maxHumidity;
        this.biomes = biomes;
        biomeGrid = new Biome[gridSize, gridSize];

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                float temp = i * maxTemp / (gridSize - 1);
                float hum = j * maxHumidity / (gridSize - 1);
                biomeGrid[i, j] = null;
                foreach (Biome biome in biomes)
                {
                    if (temp >= biome.minTemp && temp <= biome.maxTemp &&
                        hum >= biome.minHumidity && hum <= biome.maxHumidity)
                    {
                        biomeGrid[i, j] = biome;
                        break;
                    }
                }
            }
        }
    }

    public Biome getBiome(float temperature, float humidity)
    {
        int i = Mathf.Clamp(Mathf.RoundToInt(temperature * (gridSize - 1) / maxTemp), 0, gridSize - 1);
        int j = Mathf.Clamp(Mathf.RoundToInt(humidity * (gridSize - 1) / maxHumidity), 0, gridSize - 1);
        Biome biome = biomeGrid[i, j];
        if (biome != null)
        {
            return biome;
        }

        return null;
    }
}