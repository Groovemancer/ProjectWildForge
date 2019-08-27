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
    /// <summary>
    /// Custom parameters for this particular structure. We
    /// are using a dictionary because later, custom Lua functions will
    /// be able to use whatever parameters teh user/modder would like.
    /// Basically, the Lua code will bind to this dictionary.
    /// </summary>
    protected Dictionary<string, float> structureParameters;

    /// <summary>
    /// These actions are called every update. They get passed the structure
    /// they belong to, plus a deltaTime.
    /// </summary>
    protected Action<Structure, float> updateActions;

    public Func<Structure, Enterability> IsEnterable;

    List<Job> jobs;

    // If this structure gets worked by a person,
    // where is the correct spot for them to stand,
    // relative to the bottom-left tile of the sprite.
    // NOTE: This could even be something outside of the actual
    // structure tile itself!   (In fact, this will probably be common).
    public Vector2 jobSpotOffset = Vector2.zero;

    // If the job causes some kind of object to be spawned, where will it appear?
    public Vector2 jobSpawnSpotOffset = Vector2.zero;

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

    public uint AllowedTileTypes { get; protected set; }

    // This is a multiplier. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
    // Tile types and other environmental effects may be combined.
    // For example, a "rough tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    // would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
    // SPECIAL: If movementCost = 0, then this tile is impassable. (e.g. a wall).
    public float MovementCost { get; protected set; }

    public bool RoomEnclosure { get; protected set; }

    // For example, a sofa might be a 3x2 (actual graphics only appear to cover the 3x1 area, but the extra row is for leg room
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public Color tint = Color.white;

    public bool LinksToNeighbor { get; protected set; }

    public Action<Structure> cbOnChanged;
    public Action<Structure> cbOnRemoved;

    Func<Tile, bool> funcPositionValidation;

    // TODO: Implement larger objects
    // TODO: Implement object rotation

    // Empty constructor is used for serialization
    public Structure()
    {
        structureParameters = new Dictionary<string, float>();
        jobs = new List<Job>();
    }

    // Copy Constructor -- don't call this directly, unless we never
    // do ANY sub-classing. Instead use Clone(), which is more virtual.
    protected Structure(Structure other)
    {
        this.ObjectType = other.ObjectType;
        this.MovementCost = other.MovementCost;
        this.RoomEnclosure = other.RoomEnclosure;
        this.Width = other.Width;
        this.Height = other.Height;
        this.tint = other.tint;
        this.LinksToNeighbor = other.LinksToNeighbor;
        this.AllowedTileTypes = other.AllowedTileTypes;

        this.jobSpotOffset = other.jobSpotOffset;

        this.structureParameters = new Dictionary<string, float>(other.structureParameters);
        jobs = new List<Job>();

        if (other.updateActions != null)
            this.updateActions = (Action<Structure, float>)other.updateActions.Clone();

        if (other.funcPositionValidation != null)
            this.funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();

        this.IsEnterable = other.IsEnterable;
    }

    // Make a copy of the current structure. Sub-classes should
    // override this Clone() if a different (sub-classed) copy
    // constructor should be run.
    public virtual Structure Clone()
    {
        return new Structure(this);
    }

    // Create structure from parameters -- this will probably ONLY ever be used for prototype
    public Structure(string objectType, float movementCost = 1f,
        int width = 1, int height = 1, bool linksToNeighbor = false, uint allowedTileTypes = 1,
        bool roomEnclosure = false)
    {
        this.ObjectType = objectType;
        this.MovementCost = movementCost;
        this.RoomEnclosure = roomEnclosure;
        this.Width = width;
        this.Height = height;
        this.LinksToNeighbor = linksToNeighbor;
        this.AllowedTileTypes = allowedTileTypes;

        this.funcPositionValidation = this.DefaulIsValidPosition;

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

            t = World.current.GetTileAt(x, y + 1);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = World.current.GetTileAt(x + 1, y);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = World.current.GetTileAt(x, y - 1);
            if (t != null && t.Structure != null && t.Structure.cbOnChanged != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = World.current.GetTileAt(x - 1, y);
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
    // This will be replaced by validation checks fed to us from
    // Lua files that will be customizable for each structure.
    // For example, a door might specify that it needs two walls
    // to connect to.
    protected bool DefaulIsValidPosition(Tile t)
    {
        for (int x_off = t.X; x_off < (t.X + Width); x_off++)
        {
            for (int y_off = t.Y; y_off < (t.Y + Height); y_off++)
            {
                Tile t2 = World.current.GetTileAt(x_off, y_off);

                //Debug.Log("AllowedTypes: " + AllowedTileTypes);
                // Make sure tile is of allowed types
                // Make sure tile doesn't already have structure
                if ((AllowedTileTypes & t2.Type.Flag) != t2.Type.Flag && t2.Type != TileTypeData.Instance.AllType)
                {
                    //Debug.Log("Old IsValidPosition: false");
                    return false;
                }

                // Make sure tile doesn't already have a structure
                if (t2.Structure != null)
                {
                    return false;
                }
            }
        }

        //Debug.Log("Old IsValidPosition: true");
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

    public void RegisterOnRemovedCallback(Action<Structure> callbackFunc)
    {
        cbOnRemoved += callbackFunc;
    }

    public void UnregisterOnRemovedCallback(Action<Structure> callbackFunc)
    {
        cbOnRemoved -= callbackFunc;
    }

    /// <summary>
    /// Gets the custom structure parameter from a string key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public float GetParameter(string key, float defaultValue = 0)
    {
        if (structureParameters.ContainsKey(key) == false)
        {
            return defaultValue;
        }
        return structureParameters[key];
    }

    public void SetParameter(string key, float value)
    {
        structureParameters[key] = value;
    }

    public void ChangeParameter(string key, float value)
    {
        if (structureParameters.ContainsKey(key))
            structureParameters[key] += value;
    }

    /// <summary>
    /// Registers a function that will be called every Update.
    /// </summary>
    public void RegisterUpdateAction(Action<Structure, float> a)
    {
        updateActions += a;
    }

    public void UnregisterUpdateAction(Action<Structure, float> a)
    {
        updateActions -= a;
    }

    public int JobCount()
    {
        return jobs.Count;
    }

    public void AddJob(Job j)
    {
        j.structure = this;
        jobs.Add(j);
        j.RegisterJobStoppedCallback(OnJobStopped);
        World.current.jobQueue.Enqueue(j);
    }

    public void OnJobStopped(Job j)
    {
        RemoveJob(j);
    }

    protected void RemoveJob(Job j)
    {
        j.UnregisterJobStoppedCallback(OnJobStopped);
        jobs.Remove(j);
        j.structure = null;
    }

    protected void ClearJobs()
    {
        Job[] jobs_array = jobs.ToArray();
        foreach (Job j in jobs_array)
        {
            RemoveJob(j);
        }
    }

    public void CancelJobs()
    {
        Job[] jobs_array = jobs.ToArray();
        foreach (Job j in jobs_array)
        {
            j.CancelJob();
        }
    }

    public bool IsStockpile()
    {
        return ObjectType == "Stockpile";
    }

    public void Deconstruct()
    {
        Debug.Log("Deconstruct");

        Tile.UnplaceStructure();

        if (cbOnRemoved != null)
            cbOnRemoved(this);

        // Do we need to recalculate our rooms?
        if (RoomEnclosure)
        {
            // TODO: Not sure if I'll be using rooms just yet.
            Room.DoRoomFloodFill(this.Tile, false);
        }

        World.current.InvalidateTileGraph();

        // At this point, no DATA structures should be pointing to us, so we
        // should get garbage-collected.
    }

    public Tile GetJobSpotTile()
    {
        return World.current.GetTileAt(Tile.X + (int)jobSpotOffset.x, Tile.Y + (int)jobSpotOffset.y);
    }

    public Tile GetSpawnSpotTile()
    {
        return World.current.GetTileAt(Tile.X + (int)jobSpawnSpotOffset.x, Tile.Y + (int)jobSpawnSpotOffset.y);
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

    #endregion
}
