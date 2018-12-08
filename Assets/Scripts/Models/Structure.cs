using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

// Structures are things like walls, doors, and furniture (e.g. table)

public class Structure : IXmlSerializable
{
    public Dictionary<string, float> structureParameters;
    public Action<Structure, float> updateActions;

    public Func<Structure, Enterability> IsEnterable;

    public void Update(float deltaAuts)
    {
        if (updateActions != null)
            updateActions(this, deltaAuts);
    }

    // This represents the BASE tile of the object -- but in practices, large objects may actually occupy
    // multiple tiles.
    public Tile Tile { get; protected set; }

    // This "objectType" will be queried by the visual system to know what sprite to render for this object
    public string ObjectType { get; protected set; }

    public TileType AllowedTileTypes { get; protected set; }

    // This is a multiplier. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
    // Tile types and other environmental effects may be combined.
    // For example, a "rough tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    // would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
    // SPECIAL: If movementCost = 0, then this tile is impassable. (e.g. a wall).
    public float MovementCost { get; protected set; }

    public bool RoomEnclosure { get; protected set; }

    // For example, a sofa might be a 3x2 (actual graphics only appear to cover the 3x1 area, but the extra row is for leg room
    int width;
    int height;

    public bool LinksToNeighbor { get; protected set; }

    public Action<Structure> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

    // TODO: Implement larger objects
    // TODO: Implement object rotation

    // Empty constructor is used for serialization
    public Structure()
    {
        structureParameters = new Dictionary<string, float>();
    }

    // Copy Constructor
    protected Structure(Structure other)
    {
        this.ObjectType = other.ObjectType;
        this.MovementCost = other.MovementCost;
        this.RoomEnclosure = other.RoomEnclosure;
        this.width = other.width;
        this.height = other.height;
        this.LinksToNeighbor = other.LinksToNeighbor;
        this.AllowedTileTypes = other.AllowedTileTypes;

        this.structureParameters = new Dictionary<string, float>(other.structureParameters);

        if (other.updateActions != null)
            this.updateActions = (Action<Structure, float>)other.updateActions.Clone();

        this.IsEnterable = other.IsEnterable;
    }

    public virtual Structure Clone()
    {
        return new Structure(this);
    }

    // Create structure from parameters -- this will probably ONLY ever be used for prototype
    public Structure(string objectType, float movementCost = 1f,
        int width = 1, int height = 1, bool linksToNeighbor = false, TileType allowedTileTypes = TileType.All,
        bool roomEnclosure = false)
    {
        this.ObjectType = objectType;
        this.MovementCost = movementCost;
        this.RoomEnclosure = roomEnclosure;
        this.width = width;
        this.height = height;
        this.LinksToNeighbor = linksToNeighbor;
        this.AllowedTileTypes = allowedTileTypes;

        this.funcPositionValidation = this.__IsValidPosition;

        structureParameters = new Dictionary<string, float>();
    }

    public static Structure PlaceInstance(Structure proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Position Validity Function returned False.");
            return null;
        }

        // We know our placement destination is valid.
        Structure obj = proto.Clone();
        obj.Tile = tile;

        // FIXME: This assumes we are 1x1!
        if (tile.PlaceStructure(obj) == false)
        {
            // For some reason,we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        if (obj.LinksToNeighbor)
        {
            // This type of furniture links itself to its neighbors,
            // so we should inform our neighbors that they have a new
            // buddy. Just trigger their OnChangedCallback.
            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = tile.World.GetTileAt(x, y + 1);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = tile.World.GetTileAt(x + 1, y);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = tile.World.GetTileAt(x, y - 1);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = tile.World.GetTileAt(x - 1, y);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }
        }

        return obj;
    }

    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    // FIXME: These functions should never be called directly,
    // so they probably shouldn't be public functions
    public bool __IsValidPosition(Tile t)
    {
        Debug.Log("AllowedTypes: " + AllowedTileTypes);
        // Make sure tile is of allowed types
        // Make sure tile doesn't already have structure
        if ((AllowedTileTypes & t.Type) != t.Type && t.Type != TileType.All)
        {
            Debug.Log("Old IsValidPosition: false");
            return false;
        }

        Debug.Log("Old IsValidPosition: true");
        return true;
    }

    public bool __IsValidPosition_Door(Tile t)
    {
        // Make sure we have a pair of E/W walls or N/S walls

        return true;
    }

    public void RegisterOnChangedCallback(Action<Structure> callbackFunc)
    {
        cbOnChanged += callbackFunc;
    }

    public void UnregisterOnChangedCallback(Action<Structure> callbackFunc)
    {
        cbOnChanged -= callbackFunc;
    }



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
        writer.WriteAttributeString("objectType", ObjectType);

        foreach (string k in structureParameters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", structureParameters[k].ToString());
            Debug.Log("Write Param name: " + k + " val: " + structureParameters[k].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        // X, Y, and objectType has already been set, and we should already
        // be assigned to a tile. So just read extra data.

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                Debug.Log("Read Param name: " + k);
                float v = float.Parse(reader.GetAttribute("value"));
                structureParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }
}
