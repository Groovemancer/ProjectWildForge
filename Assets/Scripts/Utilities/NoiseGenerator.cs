using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator
{
    public static float[,] Generate(int width, int height, float scale, Wave[] waves, Vector2 offset)
    {
        float[,] noiseMap = new float[width, height];

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                float sampelPosX = (float)x * scale + offset.x;
                float sampelPosY = (float)y * scale + offset.y;

                float normalization = 0.0f;

                foreach (Wave wave in waves)
                {
                    noiseMap[x, y] += wave.amplitude * Mathf.PerlinNoise(sampelPosX * wave.frequency + wave.seed, sampelPosY * wave.frequency + wave.seed);
                    normalization += wave.amplitude;
                }

                noiseMap[x, y] /= normalization;
            }
        }

        return noiseMap;
    }
}

[System.Serializable]
public class Wave
{
    public float seed;
    public float frequency;
    public float amplitude;
}