using Unity.VisualScripting.FullSerializer;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Unity.Collections;


[System.Serializable]
public class FBM_Noise
{
    public int seed;
    public float amplitude;
    public float persistence;
    public float frequency;
    public float lacuranicy;
    public float octaves;
    private int[] p;


    public FBM_Noise(int seed, float amplitude, float persistence, float frequency, float lacuranicy, float octaves)
    {
        this.seed = seed;
        this.amplitude = amplitude;
        this.persistence = persistence;
        this.frequency = frequency;
        this.lacuranicy = lacuranicy;
        this.octaves = octaves;

        System.Random r = new System.Random(seed);

        List<int> nums = Enumerable.Range(0, 256).ToList();

        for (int i = 255; i >= 0; i--)
        {
            int j = r.Next(i + 1);

            int temp = nums[i];
            nums[i] = nums[j];
            nums[j] = temp;
        }

        this.p = new int[512];
        nums.CopyTo(p, 0);
        nums.CopyTo(p, 256);
    }

    public float Hermite(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    public float Lerp(float a, float b, float t)
    {
        return (1 - t) * a + t * b;
    }

    public float Gradient(int hash, float x, float y)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : 0);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    public float PerlinNoise2D(float x, float y)
    {
        int X = (int)Mathf.Floor(x) & 255;
        int Y = (int)Mathf.Floor(y) & 255;

        x -= (float)Mathf.Floor(x);
        y -= (float)Mathf.Floor(y);

        float u = Hermite(x);
        float v = Hermite(y);

        int AA = p[p[X] + Y];
        int AB = p[p[X] + Y + 1];
        int BA = p[p[X + 1] + Y];
        int BB = p[p[X + 1] + Y + 1];

        float dot_AA = Gradient(AA, x, y);
        float dot_AB = Gradient(AB, x, y - 1);
        float dot_BA = Gradient(BA, x - 1, y);
        float dot_BB = Gradient(BB, x - 1, y - 1);

        float lerpX1 = Lerp(dot_AA, dot_BA, u);
        float lerpX2 = Lerp(dot_AB, dot_BB, u);

        return Lerp(lerpX1, lerpX2, v);
    }

    public float FBM_NoiseValue(float x, float y, bool normalize = true)
    {
        float maxAmp = 0;
        float totalAmp = 0;

        float currentAmp = this.amplitude;
        float currentFreq = this.frequency;

        for (int i = 0; i < octaves; i++)
        {
            totalAmp += PerlinNoise2D(x * currentFreq, y * currentFreq) * currentAmp;
            maxAmp += currentAmp;

            currentAmp *= persistence;
            currentFreq *= lacuranicy;
        }

        if (normalize)
            return (totalAmp / maxAmp + 1f) / 2f;

        return totalAmp / maxAmp;
    }
}
