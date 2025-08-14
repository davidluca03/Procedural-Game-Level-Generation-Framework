using UnityEngine;
using System;
using System.Linq;

[System.Serializable]
public class FBM_Noise
{
    private readonly int seed;
    private readonly float amplitude;
    private readonly float persistence;
    private readonly float frequency;
    private readonly float lacuranicy;
    private readonly int octaves;
    private readonly float sharpness;
    private readonly int[] p;

    public FBM_Noise(int seed, float amplitude, float persistence, float frequency, float lacuranicy, int octaves, float sharpness = 1)
    {
        this.seed = seed;
        this.amplitude = amplitude;
        this.persistence = persistence;
        this.frequency = frequency;
        this.lacuranicy = lacuranicy;
        this.octaves = octaves;
        this.sharpness = sharpness;

        this.p = new int[512];
        int[] permutation = new int[256];

        for (int i = 0; i < 256; i++)
        {
            permutation[i] = i;
        }

        System.Random r = new System.Random(seed);
        
        for (int i = 0; i < 256; i++)
        {
            int j = r.Next(256);
            int temp = permutation[i];
            permutation[i] = permutation[j];
            permutation[j] = temp;
        }

        for (int i = 0; i < 256; i++)
        {
            p[i] = p[i + 256] = permutation[i];
        }
    }

    private float Hermite(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private float Gradient(int hash, float x, float y)
    {
        switch (hash & 0xF)
        {
            case 0x0: return x + y;
            case 0x1: return -x + y;
            case 0x2: return x - y;
            case 0x3: return -x - y;
            case 0x4: return x + y;
            case 0x5: return -x + y;
            case 0x6: return x - y;
            case 0x7: return -x - y;
            case 0x8: return x;
            case 0x9: return -x;
            case 0xA: return x;
            case 0xB: return -x;
            case 0xC: return y;
            case 0xD: return -y;
            case 0xE: return y;
            case 0xF: return -y;
            default: return 0;
        }
    }

    public float PerlinNoise2D(float x, float y)
    {
        int X = (int)Mathf.Floor(x) & 255;
        int Y = (int)Mathf.Floor(y) & 255;

        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);

        float u = Hermite(x);
        float v = Hermite(y);

        int A = p[X] + Y;
        int B = p[X + 1] + Y;

        float res = Mathf.Lerp(
            Mathf.Lerp(Gradient(p[A], x, y), Gradient(p[B], x - 1, y), u),
            Mathf.Lerp(Gradient(p[A + 1], x, y - 1), Gradient(p[B + 1], x - 1, y - 1), u),
            v
        );

        return res;
    }

    public float FBM_NoiseValue(float x, float y, bool normalize = true)
    {
        float total = 0;
        float maxAmplitude = 0;
        float currentAmplitude = this.amplitude;
        float currentFrequency = this.frequency;

        for (int i = 0; i < octaves; i++)
        {
            total += PerlinNoise2D(x * currentFrequency, y * currentFrequency) * currentAmplitude;
            maxAmplitude += currentAmplitude;

            currentAmplitude *= persistence;
            currentFrequency *= lacuranicy;
        }

        total = Mathf.Pow((total / maxAmplitude + 1) / 2, sharpness) * 2 - 1;

        if (normalize)
        {
            return (total + 1f) / 2f;
        }
        else
        {
            return total;
        }
    }
}