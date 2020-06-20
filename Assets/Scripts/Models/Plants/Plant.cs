using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using MoonSharp.Interpreter;

using UnityEngine;

using Animation;

// Plants that grow and can be harvested/felled (trees)

[MoonSharpUserData]
public class Plant : IXmlSerializable, ISelectable, IUpdatable, IPrototypable
{
    private bool isSelected;
    public bool IsSelected
    {
        get
        {
            return isSelected;
        }
        set
        {
            if (value != isSelected)
            {
                isSelected = value;
                if (SelectionChanged != null)
                    SelectionChanged(this);
            }
        }
    }

    public Bounds Bounds => throw new NotImplementedException();

    // This represents the BASE tile of the object -- but in practices, large objects may actually occupy
    // multiple tiles.
    public Tile Tile { get; set; }

    // What sort of tiles can this grow on...
    public uint AllowedTileTypes { get; protected set; }

    public string Name { get; protected set; }

    // This "objectType" will be queried by the visual system to know what sprite to render for this object
    public string Type { get; protected set; }

    // This is a multiplier. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
    // Tile types and other environmental effects may be combined.
    // For example, a "rough tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    // would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
    // SPECIAL: If movementCost = 0, then this tile is impassable. (e.g. a wall).
    public float MovementCost { get; protected set; }

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public float GrowthTime { get; protected set; }

    public float CurrentGrowth { get; protected set; }

    private float currentGrowthPctThreshold = 0f;

    public Dictionary<float, string> Sprites { get; protected set; }

    /// <summary>
    /// This event will trigger when the plant has been changed.
    /// </summary>
    public event Action<Plant> Changed;

    /// <summary>
    /// This event will trigger when the plant has been changed.
    /// </summary>
    public event Action<Plant> SelectionChanged;

    /// <summary>
    /// This event will trigger when the plant has been removed.
    /// </summary>
    public event Action<Plant> Removed;

    // Empty constructor is used for serialization
    public Plant()
    {
        Width = 1;
        Height = 1;
        this.CurrentGrowth = 0;
        this.Sprites = new Dictionary<float, string>();
    }

    // Copy Constructor -- don't call this directly, unless we never
    // do ANY sub-classing. Instead use Clone(), which is more virtual.
    protected Plant(Plant other)
    {
        Width = other.Width;
        Height = other.Height;
        this.Type = other.Type;
        this.Name = other.Name;
        this.MovementCost = other.MovementCost;
        this.GrowthTime = other.GrowthTime;
        this.CurrentGrowth = 0;
        this.AllowedTileTypes = other.AllowedTileTypes;

        this.Sprites = new Dictionary<float, string>();
        foreach (KeyValuePair<float, string> keyValPair in other.Sprites)
        {
            this.Sprites.Add(keyValPair.Key, keyValPair.Value);
        }
    }

    // Make a copy of the current plant. Sub-classes should
    // override this Clone() if a different (sub-classed) copy
    // constructor should be run.
    public virtual Plant Clone()
    {
        return new Plant(this);
    }

    // Create plant from parameters -- this will probably ONLY ever be used for prototype
    public Plant(string type, string name, float growthTime, float currentGrowth = 0)
    {
        this.Type = type;
        this.Name = name;
        this.GrowthTime = growthTime;
        this.CurrentGrowth = currentGrowth;
    }

    public string GetCurrentSprite()
    {
        float growthPct = CurrentGrowth / GrowthTime;

        string spriteName = Type;

        float currentThreshold = 0;
        foreach (var sprite in Sprites)
        {
            if (growthPct >= sprite.Key)
                currentThreshold = sprite.Key;
        }

        if (Sprites.ContainsKey(currentThreshold))
        {
            spriteName = Sprites[currentThreshold];
        }

        return spriteName;
    }

    public void SetRandomGrowthPercent(float min, float max)
    {
        if (min < 0.0f)
            min = 0.0f;

        if (max > 1.0f)
            max = 1.0f;

        CurrentGrowth = (GrowthTime * RandomUtils.Range(min, max));
    }

    public static Plant PlaceInstance(Plant proto, Tile tile)
    {
        if (proto.IsValidPosition(tile) == false)
        {
            //DebugUtils.LogErrorChannel("Plant", "PlaceInstance :: Position Validity Function returned FALSE. " + proto.Type + " " + tile.X + ", " + tile.Y + ", " + tile.Z);
            return null;
        }

        // We know our placement destination is valid.
        Plant plantObj = proto.Clone();
        plantObj.Tile = tile;

        // FIXME: This assumes we are 1x1!
        if (tile.PlacePlant(plantObj) == false)
        {
            // For some reason,we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        return plantObj;
    }

    public void Deconstruct()
    {
        Tile.UnplacePlant();

        if (Removed != null)
        {
            Removed(this);
        }

        // At this point, no DATA structures should be pointing to us, so we
        // should get garbage-collected.
    }

    /// <summary>
    /// Check if the position of the plant is valid or not.
    /// This is called when placing the plant.
    /// </summary>
    /// <param name="t">The base tile.</param>
    /// <returns>True if the tile is valid for the placement of the plant.</returns>
    public bool IsValidPosition(Tile tile)
    {
        for (int x_off = tile.X; x_off < (tile.X + Width); x_off++)
        {
            for (int y_off = tile.Y; y_off < (tile.Y + Height); y_off++)
            {
                Tile tile2 = World.Current.GetTileAt(x_off, y_off, tile.Z);

                if (tile2 == null)
                    break;

                // Make sure tile is of allowed types
                // Make sure tile doesn't already have a plant
                if ((AllowedTileTypes & tile2.Type.Flag) != tile2.Type.Flag && tile2.Type != TileTypeData.Instance.AllType)
                {
                    return false;
                }

                // Make sure tile doesn't already have a plant
                if (tile2.Plant != null)
                {
                    return false;
                }
            }
        }

        return true;
    }


    public void EveryFrameUpdate(float deltaAuts)
    {
        CurrentGrowth += deltaAuts;

        DebugUtils.LogChannel("Plant", string.Format("FixedFrequencyUpdate CurrentGrowth: {0} | DeltaAuts: {1}", CurrentGrowth, deltaAuts));

        float growthPct = CurrentGrowth / GrowthTime;

        float prevThreshold = currentGrowthPctThreshold;

        foreach (var sprite in Sprites)
        {
            if (growthPct >= sprite.Key)
                currentGrowthPctThreshold = sprite.Key;
        }

        if (currentGrowthPctThreshold != prevThreshold)
        {
            Changed(this);
        }

        if (CurrentGrowth >= GrowthTime)
        {
            CurrentGrowth = GrowthTime;
        }
    }

    public void FixedFrequencyUpdate(float deltaAuts)
    {
        CurrentGrowth += deltaAuts;

        //DebugUtils.LogChannel("Plant", string.Format("FixedFrequencyUpdate CurrentGrowth: {0} | DeltaAuts: {1}", CurrentGrowth, deltaAuts));

        float growthPct = CurrentGrowth / GrowthTime;

        float prevThreshold = currentGrowthPctThreshold;

        foreach (var sprite in Sprites)
        {
            if (growthPct >= sprite.Key)
                currentGrowthPctThreshold = sprite.Key;
        }

        if (currentGrowthPctThreshold != prevThreshold)
        {
            Changed(this);
        }

        if (CurrentGrowth >= GrowthTime)
        {
            CurrentGrowth = GrowthTime;
        }
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        throw new NotImplementedException();
    }

    public string GetDescription()
    {
        throw new NotImplementedException();
    }

    public string GetJobDescription()
    {
        throw new NotImplementedException();
    }

    public string GetName()
    {
        return StringUtils.GetLocalizedTextFiltered(Name);
    }

    #region Saving & Loading

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                     SAVING & LOADING
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", Tile.X.ToString());
        writer.WriteAttributeString("Y", Tile.Y.ToString());
        writer.WriteAttributeString("Z", Tile.Z.ToString());
        writer.WriteAttributeString("Type", Type);
        writer.WriteElementString("GrowthTime", GrowthTime.ToString());
        writer.WriteElementString("CurrentGrowth", CurrentGrowth.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Prototype Creation

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        DebugUtils.LogChannel("Plant", "ReadXmlPrototype");
        /*
		<Plant Type="plant_Dummy">
			<NameLocaleId>comment#plant_Dummy</NameLocaleId>
			<Width>1</Width>
			<Height>1</Height>
			<MoveCost>0</MoveCost>
			<GrowthTime>3600000</GrowthTime> <!-- Total amount of AUTs to fully grow -->
			<Health>300</Health> <!-- How much damage before destroyed. Does not Yield anything when destroyed -->
			<AllowedTiles>Dirt|Grass</AllowedTiles> <!-- The tile types that the plant can be placed/grow on -->
			<Sprites>
				<Sprite GrowPctThreshold="0.00">plant_TestTree_0</Sprite> <!-- When the percentage of total grow time exceeds the threshold, change to this sprite -->
				<Sprite GrowPctThreshold="0.25">plant_TestTree_1</Sprite>
				<Sprite GrowPctThreshold="0.50">plant_TestTree_2</Sprite>
				<Sprite GrowPctThreshold="0.75">plant_TestTree_3</Sprite>
			</Sprites>
			<Harvest>
				<MinGrowthPct>0.6</MinGrowthPct> <!-- Minimum percent growth to harvest -->
				<Cost>1200</Cost> <!-- How much "work done" is needed to harvest -->
				<Skill>Forestry</Skill> <!-- What skill is needed to harvest -->
				<Yield> <!-- Items that are dropped when harvested -->
					<Inv type="inv_RawWood" amount="25"/> <!-- Amount will be proportional to % grown when harvested -->
				</Yield>
			</Harvest>
		</Plant>
		*/
        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.SelectSingleNode("NameLocaleId").InnerText;
        Width = int.Parse(rootNode.SelectSingleNode("Width").InnerText);
        Height = int.Parse(rootNode.SelectSingleNode("Height").InnerText);
        MovementCost = float.Parse(rootNode.SelectSingleNode("MoveCost").InnerText);
        GrowthTime = float.Parse(rootNode.SelectSingleNode("GrowthTime").InnerText);

        string tileTags = rootNode.SelectSingleNode("AllowedTiles").InnerText;

        string[] tileTypeTags = tileTags.Split('|');
        AllowedTileTypes = 0;
        foreach (string tileTypeTag in tileTypeTags)
        {
            AllowedTileTypes |= TileTypeData.Flag(tileTypeTag);
        }

        Sprites = new Dictionary<float, string>();
        XmlNode spritesNode = rootNode.SelectSingleNode("Sprites");
        if (spritesNode != null)
        {
            foreach (XmlNode spriteNode in spritesNode.SelectNodes("Sprite"))
            {
                float growthThreshold = float.Parse(spriteNode.Attributes["GrowPctThreshold"].InnerText);
                string spriteName = spriteNode.InnerText;

                Sprites.Add(growthThreshold, spriteName);
            }
        }
    }

    public void ApplySelection()
    {
        if (Changed != null)
        {
            Changed(this);
        }
    }

    #endregion
}