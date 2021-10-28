using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class WorldGenerator
{
    private static List<Biome> biomes = new List<Biome>();

    //public int width = 100;
    //public int height = 100;
    //public float scale = 1.0f;
    //public Vector2 offset;

    //public List<Wave> heightWaves;
    //public float[,] heightMap;

    //public List<Wave> moistureWaves;
    //public float[,] moistureMap;

    //public List<Wave> heatWaves;
    //public float[,] heatMap;

    public static World GenerateWorldFromSettings()
    {
        World world = null;
        bool loaded = false;
        //World world = new World(100, 100, 5);
        DebugUtils.LogChannel("WorldGenerator", "Loading World Settings...");
        try
        {
            XmlDocument doc = new XmlDocument();

            string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
            filePath = Path.Combine(filePath, "WorldSettings.xml");
            doc.Load(filePath);

            XmlNode worldSettingsNode = doc.SelectSingleNode("WorldSettings");

            int width = int.Parse(worldSettingsNode.SelectSingleNode("Width").InnerText);
            int height = int.Parse(worldSettingsNode.SelectSingleNode("Height").InnerText);
            int depth = int.Parse(worldSettingsNode.SelectSingleNode("Depth").InnerText);

            world = new World(width, height, depth);

            XmlNodeList biomeNodes = worldSettingsNode.SelectNodes("Biomes/Biome");
            foreach (XmlNode biomeNode in biomeNodes)
            {
                Biome biome = new Biome();
                biome.Type = biomeNode.Attributes["Type"].InnerText;
                DebugUtils.LogChannel("WorldGenerator", "Biome Type: " + biome.Type);
                biome.Name = biomeNode.SelectSingleNode("NameLocaleId").InnerText;
                
                string tileTags = biomeNode.SelectSingleNode("TileTypes").InnerText;

                string[] tileTypeTags = tileTags.Split('|');
                biome.TileTypes = new List<TileType>();
                foreach (string tileTypeTag in tileTypeTags)
                {
                    biome.TileTypes.Add(TileTypeData.GetByFlagName(tileTypeTag));
                }

                biome.MinHeight = float.Parse(biomeNode.SelectSingleNode("MinHeight").InnerText);
                biome.MinMoisture = float.Parse(biomeNode.SelectSingleNode("MinMoisture").InnerText);
                biome.MinHeat = float.Parse(biomeNode.SelectSingleNode("MinHeat").InnerText);

                biomes.Add(biome);
            }
            
            // Height Waves
            List<Wave> heightWaves = new List<Wave>();
            
            XmlNodeList heightWaveNodes = worldSettingsNode.SelectNodes("HeightWaves/Wave");
            foreach (XmlNode waveNode in heightWaveNodes)
            {
                Wave wave = new Wave();
                wave.seed = RandomUtils.Range(0.0f, 1000f);
                wave.frequency = float.Parse(waveNode.Attributes["Frequency"].InnerText);
                wave.amplitude = float.Parse(waveNode.Attributes["Amplitude"].InnerText);
                heightWaves.Add(wave);
            }

            DebugUtils.LogChannel("WorldGenerator", "HeightWaves Loaded: " + heightWaves.Count);

            float[,] heightMap = NoiseGenerator.Generate(width, height, 1, heightWaves.ToArray(), Vector2.zero);
            
            // Moisture Waves
            List<Wave> moistureWaves = new List<Wave>();

            XmlNodeList moistureWaveNodes = worldSettingsNode.SelectNodes("MoistureWaves/Wave");
            foreach (XmlNode waveNode in moistureWaveNodes)
            {
                Wave wave = new Wave();
                wave.seed = RandomUtils.Range(0.0f, 1000f);
                wave.frequency = float.Parse(waveNode.Attributes["Frequency"].InnerText);
                wave.amplitude = float.Parse(waveNode.Attributes["Amplitude"].InnerText);
                moistureWaves.Add(wave);
            }

            DebugUtils.LogChannel("WorldGenerator", "MoistureWaves Loaded: " + moistureWaves.Count);

            float[,] moistureMap = NoiseGenerator.Generate(width, height, 1, moistureWaves.ToArray(), Vector2.zero);

            // Heat Waves
            List<Wave> heatWaves = new List<Wave>();

            XmlNodeList heatWaveNodes = worldSettingsNode.SelectNodes("HeatWaves/Wave");
            foreach (XmlNode waveNode in heatWaveNodes)
            {
                Wave wave = new Wave();
                wave.seed = RandomUtils.Range(0.0f, 1000f);
                wave.frequency = float.Parse(waveNode.Attributes["Frequency"].InnerText);
                wave.amplitude = float.Parse(waveNode.Attributes["Amplitude"].InnerText);
                heatWaves.Add(wave);
            }

            DebugUtils.LogChannel("WorldGenerator", "HeatWaves Loaded: " + heatWaves.Count);

            float[,] heatMap = NoiseGenerator.Generate(width, height, 1, heatWaves.ToArray(), Vector2.zero);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    Biome biome = GetBiome(heightMap[x, y], moistureMap[x, y], heatMap[x, y]);

                    world.SetTileType(x, y, 0, biome.GetTileType(), false);
                }
            }

            loaded = true;

            DebugUtils.LogChannel("WorldGenerator", "Biomes Loaded: " + biomes.Count);
        }
        catch (Exception e)
        {
            DebugUtils.DisplayError(e.ToString(), false);
            DebugUtils.LogException(e);
        }

        if (!loaded)
        {
            DebugUtils.LogChannel("WorldGenerator", "Not Loaded?!");
            world = new World(100, 100, 5);
        }

        return world;
    }

    private static Biome GetBiome(float height, float moisture, float heat)
    {
        List<BiomeTempData> biomeTemp = new List<BiomeTempData>();
        Biome biomeToReturn = null;

        foreach (Biome biome in biomes)
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
