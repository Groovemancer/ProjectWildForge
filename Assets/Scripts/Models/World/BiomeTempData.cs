using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeTempData
{
    public Biome biome;

    public BiomeTempData(Biome preset)
    {
        biome = preset;
    }

    public float GetDiffValue(float height, float moisture, float heat)
    {
        return (height - biome.MinHeight) + (moisture - biome.MinMoisture) + (heat - biome.MinHeat);
    }
}