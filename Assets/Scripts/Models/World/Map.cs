using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public BiomePreset[] biomes;
    public GameObject tilePrefab;

    [Header("Dimensions")]
    public int width = 50;
    public int height = 50;
    public float scale = 1.0f;
    public Vector2 offset;

    [Header("Height Map")]
    public Wave[] heightWaves;
    public float[,] heightMap;

    [Header("Moisture Map")]
    public Wave[] moistureWaves;
    public float[,] moistureMap;

    [Header("Heat Map")]
    public Wave[] heatWaves;
    public float[,] heatMap;

    void GenerateMap()
    {
        foreach (Wave wave in heightWaves)
        {
            wave.seed = RandomUtils.Range(0.0f, 1000f);
        }

        foreach (Wave wave in moistureWaves)
        {
            wave.seed = RandomUtils.Range(0.0f, 1000f);
        }

        foreach (Wave wave in heatWaves)
        {
            wave.seed = RandomUtils.Range(0.0f, 1000f);
        }

        // height map
        heightMap = NoiseGenerator.Generate(width, height, scale, heightWaves, offset);

        // moisture map
        moistureMap = NoiseGenerator.Generate(width, height, scale, moistureWaves, offset);

        // heat map
        heatMap = NoiseGenerator.Generate(width, height, scale, heatWaves, offset);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                tile.transform.parent = this.transform;
                BiomePreset biome = GetBiome(heightMap[x, y], moistureMap[x, y], heatMap[x, y]);
                tile.name = string.Format("Biome: {0} ({1}, {2})", biome.name, x, y);
                tile.GetComponent<SpriteRenderer>().sprite = biome.GetTileSprite();
            }
        }
    }

    private void Start()
    {
        GenerateMap();
    }

    BiomePreset GetBiome(float height, float moisture, float heat)
    {
        List<BiomeTempData> biomeTemp = new List<BiomeTempData>();
        BiomePreset biomeToReturn = null;

        foreach (BiomePreset biome in biomes)
        {
            if (biome.MatchCondition(height, moisture, heat))
            {
                biomeTemp.Add(new BiomeTempData(biome));
            }
        }

        float curVal = 0.0f;

        foreach (BiomeTempData biome in biomeTemp)
        {
            if (biomeToReturn == null)
            {
                biomeToReturn = biome.biome;
                curVal = biome.GetDiffValue(height, moisture, heat);
            }
            else
            {
                if (biome.GetDiffValue(height, moisture, heat) < curVal)
                {
                    biomeToReturn = biome.biome;
                    curVal = biome.GetDiffValue(height, moisture, heat);
                }
            }
        }

        if (biomeToReturn == null)
            biomeToReturn = biomes[0];

        return biomeToReturn;
    }
}
