using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class Biome : IPrototypable
{
    public Biome()
    {
    }

    private Biome(Biome other)
    {
        Type = other.Type;
        Name = other.Name;
        TileTypes = new List<TileType>(other.TileTypes);
        MinHeight = other.MinHeight;
        MinMoisture = other.MinMoisture;
        MinHeat = other.MinHeat;
    }

    public string Type { get; set; }

    public string Name { get; set; }

    public List<TileType> TileTypes { get; set; }
    public float MinHeight { get; set; }
    public float MinMoisture { get; set; }
    public float MinHeat { get; set; }

    public TileType GetTileType()
    {
        return TileTypes[RandomUtils.Range(0, TileTypes.Count)];
    }

    public bool MatchCondition(float height, float moisture, float heat)
    {
        return height >= MinHeight && moisture >= MinMoisture && heat >= MinHeat;
    }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        DebugUtils.Log("Biome ReadXmlPrototype");

        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.SelectSingleNode("NameLocaleId").InnerText;

        string tileTags = rootNode.SelectSingleNode("TileTypes").InnerText;

        string[] tileTypeTags = tileTags.Split('|');
        TileTypes = new List<TileType>();
        foreach (string tileTypeTag in tileTypeTags)
        {
            TileTypes.Add(TileTypeData.GetByFlagName(tileTypeTag));
        }

        MinHeight = float.Parse(rootNode.SelectSingleNode("MinHeight").InnerText);
        MinMoisture = float.Parse(rootNode.SelectSingleNode("MinMoisture").InnerText);
        MinHeat = float.Parse(rootNode.SelectSingleNode("MinHeat").InnerText);
    }

    public Biome Clone()
    {
        return new Biome(this);
    }
}
