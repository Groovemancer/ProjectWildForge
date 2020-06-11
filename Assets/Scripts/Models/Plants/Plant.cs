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
	public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public Bounds Bounds => throw new NotImplementedException();

	// This represents the BASE tile of the object -- but in practices, large objects may actually occupy
	// multiple tiles.
	public Tile Tile { get; set; }

	// What sort of tiles can this grow on...
	public uint AllowedTileTypes { get; protected set; }

	public string Name { get; protected set; }

	// This "objectType" will be queried by the visual system to know what sprite to render for this object
	public string Type { get; protected set; }

	public float GrowthTime { get; protected set; }

	public float CurrentGrowth { get; protected set; }

	/// <summary>
	/// This event will trigger when the plant has been changed.
	/// </summary>
	public event Action<Plant> Changed;

	/// <summary>
	/// This event will trigger when the plant has been removed.
	/// </summary>
	public event Action<Plant> Removed;

	/// <summary>
	/// Represents name of the sprite shown in menus.
	/// </summary>
	public string DefaultSpriteName { get; set; }

	/// <summary>
	/// Actual sprite name (can be null).
	/// </summary>
	public string SpriteName { get; set; }

	// Empty constructor is used for serialization
	public Plant()
    {
		this.CurrentGrowth = 0;
	}

    // Copy Constructor -- don't call this directly, unless we never
    // do ANY sub-classing. Instead use Clone(), which is more virtual.
    protected Plant(Plant other)
    {
        this.Type = other.Type;
        this.Name = other.Name;
        this.GrowthTime = other.GrowthTime;
		this.CurrentGrowth = 0;
		this.AllowedTileTypes = other.AllowedTileTypes;
	}

    // Make a copy of the current plant. Sub-classes should
    // override this Clone() if a different (sub-classed) copy
    // constructor should be run.
    public virtual Plant Clone()
    {
        return new Plant(this);
    }

    // Create structure from parameters -- this will probably ONLY ever be used for prototype
    public Plant(string type, string name, float growthTime, float currentGrowth = 0)
    {
        this.Type = type;
        this.Name = name;
		this.GrowthTime = growthTime;
		this.CurrentGrowth = currentGrowth;
    }

	public string GetDefaultSpriteName()
	{
		if (!string.IsNullOrEmpty(DefaultSpriteName))
		{
			return DefaultSpriteName;
		}

		// Else return default Type string
		return Type;
	}

	public static Plant PlaceInstance(Plant proto, Tile tile)
	{
		if (proto.IsValidPosition(tile) == false)
		{
			DebugUtils.LogErrorChannel("Structure", "PlaceInstance :: Position Validity Function returned FALSE. " + proto.Type + " " + tile.X + ", " + tile.Y + ", " + tile.Z);
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

	/// <summary>
	/// Check if the position of the plant is valid or not.
	/// This is called when placing the plant.
	/// </summary>
	/// <param name="t">The base tile.</param>
	/// <returns>True if the tile is valid for the placement of the plant.</returns>
	public bool IsValidPosition(Tile tile)
	{
		//Debug.Log("AllowedTypes: " + AllowedTileTypes);
		// Make sure tile is of allowed types
		// Make sure tile doesn't already have structure
		if ((AllowedTileTypes & tile.Type.Flag) != tile.Type.Flag && tile.Type != TileTypeData.Instance.AllType)
		{
			//Debug.Log("Old IsValidPosition: false");
			return false;
		}

		// Make sure tile doesn't already have a plant
		if (tile.Plant != null)
		{
			return false;
		}

		//Debug.Log("Old IsValidPosition: true");
		return true;
	}


	public void EveryFrameUpdate(float deltaAuts)
	{
		throw new NotImplementedException();
	}

	public void FixedFrequencyUpdate(float deltaAuts)
	{
		CurrentGrowth += deltaAuts;

		DebugUtils.LogChannel("Plant", string.Format("FixedFrequencyUpdate CurrentGrowth: {0} | DeltaAuts: {1}", CurrentGrowth, deltaAuts));

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
		return Name;
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
		<Plant type = "plant_Dummy" >
			< Name > plant_Dummy </ Name >
			< GrowthTime > 3600000 </ GrowthTime > < !--Total amount of AUTs to fully grow -->
			<Health>300</Health> <!-- How much damage before destroyed.Does not Yield anything when destroyed-->
			<Harvest>
				<MinGrowthPerc>0.6</MinGrowthPerc> <!-- Minimum percent growth to harvest -->
				<Cost>1200</Cost> <!-- How much "work done" is needed to harvest -->
				<Skill>Forestry</Skill> <!-- What skill is needed to harvest -->
				<Yield> <!-- Items that are dropped when harvested -->
					<Inv type = "inv_RawWood" amount="25"/> <!-- Amount will be proportional to % grown when harvested -->
				</Yield>
			</Harvest>
		</Plant>
		*/

		Type = rootNode.Attributes["Type"].InnerText;
		Name = rootNode.SelectSingleNode("Name").InnerText;
		GrowthTime = float.Parse(rootNode.SelectSingleNode("GrowthTime").InnerText);

		string tileTags = rootNode.SelectSingleNode("AllowedTiles").InnerText;

		string[] tileTypeTags = tileTags.Split('|');
		AllowedTileTypes = 0;
		foreach (string tileTypeTag in tileTypeTags)
		{
			AllowedTileTypes |= TileTypeData.Flag(tileTypeTag);
		}
	}

    #endregion
}