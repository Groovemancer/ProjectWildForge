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


// Structures are things like walls, doors, and furniture (e.g. table)

[MoonSharpUserData]
public class Structure : IXmlSerializable, IUpdatable
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
    //protected Action<Structure, float> updateActions;
    protected List<string> updateActions;

    protected string isEnterableAction;

    List<Job> jobs;

    public List<string> Tags;

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
        {
            //updateActions(this, deltaAuts);
            //StructureActions.CallFuncitonsWithStructure(updateActions.ToArray(), this, deltaAuts);
        }

        if (Animations != null)
        {
            //Animations.Update(Time.deltaTime);
        }
    }

    public Enterability IsEnterable()
    {
        if (string.IsNullOrEmpty(isEnterableAction))
        {
            return Enterability.Yes;
        }

        DynValue ret = FunctionsManager.Structure.Call(isEnterableAction, this);

        return (Enterability)ret.Number;
    }

    /// <summary>
    /// Gets or sets the structure animation.
    /// </summary>
    public StructureAnimation Animations { get; set; }

    public string GetDefaultSpriteName()
    {
        if (!string.IsNullOrEmpty(DefaultSpriteName))
        {
            return DefaultSpriteName;
        }

        // Else return default Type string
        return ObjectType;
    }

    /// <summary>
    /// Check if the furniture has a function to determine the sprite name and calls that function.
    /// </summary>
    /// <param name="explicitSpriteUsed">Out: true if explicit sprite was used, false if default type was used.</param>
    /// <returns>Name of the sprite.</returns>
    public string GetSpriteName(out bool explicitSpriteUsed)
    {
        explicitSpriteUsed = true;
        if (!string.IsNullOrEmpty(SpriteName))
        {
            return SpriteName;
        }

        // Try to get spritename from animation
        if (Animations != null)
        {
            return Animations.GetSpriteName();
        }

        // Else return default Type string
        explicitSpriteUsed = false;
        return ObjectType;
    }

    #region Update and Animation
    /// <summary>
    /// This function is called to update the furniture animation in lua.
    /// This will be called every frame and should be used carefully.
    /// </summary>
    /// <param name="deltaTime">The time since the last update was called.</param>
    public void EveryFrameUpdate(float deltaTime)
    {
        if (EventActions != null)
        {
            EventActions.Trigger("OnFastUpdate", this, deltaTime);
        }
    }

    /// <summary>
    /// This function is called to update the structure. This will also trigger EventsActions.
    /// </summary>
    /// <param name="deltaTime">The time since the last update was called.</param>
    public void FixedFrequencyUpdate(float deltaTime)
    {
        if (EventActions != null)
        {
            EventActions.Trigger("OnUpdate", this, deltaTime);
        }

        if (Animations != null)
        {
            Animations.Update(deltaTime);
        }
    }

    /// <summary>
    /// Set the animation state. Will only have an effect if stateName is different from current animation stateName.
    /// </summary>
    public void SetAnimationState(string stateName)
    {
        if (Animations == null)
        {
            return;
        }

        Animations.SetState(stateName);
    }

    /// <summary>
    /// Set the animation frame depending on a value. The currentvalue percent of the maxvalue will determine which frame is shown.
    /// </summary>
    public void SetAnimationProgressValue(float currentValue, float maxValue)
    {
        if (Animations == null)
        {
            return;
        }

        if (maxValue == 0)
        {
            DebugUtils.LogError("SetAnimationProgressValue maxValue is zero");
        }

        float percent = Mathf.Clamp01(currentValue / maxValue);
        Animations.SetProgressValue(percent);
    }
    #endregion


    // This represents the BASE tile of the object -- but in practices, large objects may actually occupy
    // multiple tiles.
    public Tile Tile { get; protected set; }

    // This "objectType" will be queried by the visual system to know what sprite to render for this object
    public string ObjectType { get; protected set; }

    private string _Name = null;
    public string Name
    {
        get
        {
            if (_Name == null || _Name.Length == 0)
            {
                return ObjectType;
            }
            return _Name;
        }
        set
        {
            _Name = value;
        }
    }

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

    /// <summary>
    /// Gets the tint used to change the color of the structure.
    /// </summary>
    /// <value>The Color of the structure.</value>
    public Color Tint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the door is Vertical or not.
    /// Should be false if the furniture is not a door.
    /// This field will most likely be moved to another class.
    /// </summary>
    /// <value>Whether the door is Vertical or not.</value>
    public bool VerticalDoor { get; set; }

    public string LinksToNeighbors { get; protected set; }

    /// <summary>
    /// Represents name of the sprite shown in menus.
    /// </summary>
    public string DefaultSpriteName { get; set; }

    /// <summary>
    /// Actual sprite name (can be null).
    /// </summary>
    public string SpriteName { get; set; }

    public bool RequiresFastUpdate { get; set; }

    public bool RequiresSlowUpdate { get; set; }

    /// <summary>
    /// Gets the EventAction for the current structure.
    /// These actions are called when an event is called. They get passed the structure
    /// they belong to, plus a deltaTime (which defaults to 0).
    /// </summary>
    /// <value>The event actions that is called on update.</value>
    public EventActions EventActions { get; private set; }

    public Bounds Bounds
    {
        get
        {
            return new Bounds(
                new Vector3(Tile.X - 0.5f + (Width / 2), Tile.Y - 0.5f + (Height / 2), 0),
                new Vector3(Width, Height));
        }
    }

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
        updateActions = new List<string>();
        EventActions = new EventActions();

        Tint = Color.white;
        VerticalDoor = false;

        Width = 1;
        Height = 1;
        Tags = new List<string>();
        LinksToNeighbors = string.Empty;
    }

    // Copy Constructor -- don't call this directly, unless we never
    // do ANY sub-classing. Instead use Clone(), which is more virtual.
    protected Structure(Structure other)
    {
        this.ObjectType = other.ObjectType;
        this.Name = other.Name;
        this.MovementCost = other.MovementCost;
        this.RoomEnclosure = other.RoomEnclosure;
        this.Width = other.Width;
        this.Height = other.Height;
        this.Tint = other.Tint;
        this.LinksToNeighbors = other.LinksToNeighbors;
        this.AllowedTileTypes = other.AllowedTileTypes;

        this.jobSpotOffset = other.jobSpotOffset;
        this.jobSpawnSpotOffset = other.jobSpawnSpotOffset;
        this.Tags = new List<string>();
        this.Tags.AddRange(other.Tags);

        this.structureParameters = new Dictionary<string, float>(other.structureParameters);
        jobs = new List<Job>();

        if (other.Animations != null)
        {
            Animations = other.Animations.Clone();
        }

        if (other.EventActions != null)
        {
            EventActions = other.EventActions.Clone();
        }

        if (other.updateActions != null)
            this.updateActions = new List<string>(other.updateActions);

        this.isEnterableAction = other.isEnterableAction;

        if (other.funcPositionValidation != null)
            this.funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();

        RequiresSlowUpdate = EventActions.HasEvent("OnUpdate");
        RequiresFastUpdate = EventActions.HasEvent("OnFastUpdate");

        //this.IsEnterable = other.IsEnterable;
    }

    // Make a copy of the current structure. Sub-classes should
    // override this Clone() if a different (sub-classed) copy
    // constructor should be run.
    public virtual Structure Clone()
    {
        return new Structure(this);
    }

    // Create structure from parameters -- this will probably ONLY ever be used for prototype
    public Structure(string objectType, string name, float movementCost = 1f,
        int width = 1, int height = 1, string linksToNeighbors = "", uint allowedTileTypes = 1,
        bool roomEnclosure = false)
    {
        this.ObjectType = objectType;
        this.Name = name;
        this.MovementCost = movementCost;
        this.RoomEnclosure = roomEnclosure;
        this.Width = width;
        this.Height = height;
        this.LinksToNeighbors = linksToNeighbors;
        this.AllowedTileTypes = allowedTileTypes;

        this.funcPositionValidation = this.DefaulIsValidPosition;

        structureParameters = new Dictionary<string, float>();
        Tags = new List<string>();
        updateActions = new List<string>();
        isEnterableAction = "";
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

        if (obj.LinksToNeighbors != string.Empty)
        {
            // This type of furniture links itself to its neighbors,
            // so we should inform our neighbors that they have a new
            // buddy. Just trigger their OnChangedCallback.
            int x = tile.X;
            int y = tile.Y;

            for (int xpos = x - 1; xpos < x + proto.Width + 1; xpos++)
            {
                for (int ypos = y - 1; ypos < y + proto.Height + 1; ypos++)
                {
                    Tile tileAt = World.Current.GetTileAt(xpos, ypos, tile.Z);
                    if (tileAt != null && tileAt.Structure != null && tileAt.Structure.cbOnChanged != null)
                    {
                        tileAt.Structure.cbOnChanged(tileAt.Structure);
                    }
                }
            }
        }

        return obj;
    }

    public bool IsExit()
    {
        if (RoomEnclosure && MovementCost > 0f)
        {
            return true;
        }

        return false;
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
    protected bool DefaulIsValidPosition(Tile tile)
    {
        if (Tags.Contains("OutdoorOnly"))
        {
            if (tile.Room == null || !tile.Room.IsOutsideRoom())
            {
                return false;
            }
        }

        for (int x_off = tile.X; x_off < (tile.X + Width); x_off++)
        {
            for (int y_off = tile.Y; y_off < (tile.Y + Height); y_off++)
            {
                Tile tile2 = World.Current.GetTileAt(x_off, y_off, tile.Z);

                //Debug.Log("AllowedTypes: " + AllowedTileTypes);
                // Make sure tile is of allowed types
                // Make sure tile doesn't already have structure
                if ((AllowedTileTypes & tile2.Type.Flag) != tile2.Type.Flag && tile2.Type != TileTypeData.Instance.AllType)
                {
                    //Debug.Log("Old IsValidPosition: false");
                    return false;
                }

                // Make sure tile doesn't already have a structure
                if (tile2.Structure != null)
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
    public float GetParameter(string key, float defaultValue)
    {
        if (structureParameters.ContainsKey(key) == false)
        {
            return defaultValue;
        }
        return structureParameters[key];
    }

    public float GetParameter(string key)
    {
        return GetParameter(key, 0);
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
    public void RegisterUpdateAction(string luaFunctionName)
    {
        updateActions.Add(luaFunctionName);
    }

    public void UnregisterUpdateAction(string luaFunctionName)
    {
        updateActions.Remove(luaFunctionName);
    }

    public int JobCount()
    {
        return jobs.Count;
    }

    public void AddJob(Job j)
    {
        j.Structure = this;
        jobs.Add(j);
        j.RegisterJobStoppedCallback(OnJobStopped);
        World.Current.jobQueue.Enqueue(j);
    }

    public void OnJobStopped(Job j)
    {
        RemoveJob(j);
    }

    protected void RemoveJob(Job j)
    {
        j.UnregisterJobStoppedCallback(OnJobStopped);
        jobs.Remove(j);
        j.Structure = null;
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

    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }

    public bool IsStockpile()
    {
        return Tags.Contains("Stockpile");
    }

    public void Deconstruct()
    {
        Debug.Log("Deconstruct");

        int x = Tile.X;
        int y = Tile.Y;
        int fwidth = 1;
        int fheight = 1;
        string linksToNeighbors = string.Empty;
        if (Tile.Structure != null)
        {
            Structure structure = Tile.Structure;
            fwidth = structure.Width;
            fheight = structure.Height;
            linksToNeighbors = structure.LinksToNeighbors;
            structure.CancelJobs();
        }

        Tile.UnplaceStructure();

        if (cbOnRemoved != null)
            cbOnRemoved(this);

        // Do we need to recalculate our rooms?
        if (RoomEnclosure)
        {
            World.Current.RoomManager.DoRoomFloodFill(Tile, false);
        }

        World.Current.RegenerateGraphAtTile(Tile);

        // We should inform our neighbours that they have just lost a
        // neighbor regardless of type.  
        // Just trigger their OnChangedCallback. 
        if (LinksToNeighbors != string.Empty)
        {
            for (int xpos = x - 1; xpos < x + fwidth + 1; xpos++)
            {
                for (int ypos = y - 1; ypos < y + fheight + 1; ypos++)
                {
                    Tile t = World.Current.GetTileAt(xpos, ypos, Tile.Z);
                    if (t != null && t.Structure != null && t.Structure.cbOnChanged != null)
                    {
                        t.Structure.cbOnChanged(t.Structure);
                    }
                }
            }
        }

        // At this point, no DATA structures should be pointing to us, so we
        // should get garbage-collected.
    }

    public Tile GetJobSpotTile()
    {
        return World.Current.GetTileAt(Tile.X + (int)jobSpotOffset.x, Tile.Y + (int)jobSpotOffset.y, Tile.Z);
    }

    public Tile GetSpawnSpotTile()
    {
        return World.Current.GetTileAt(Tile.X + (int)jobSpawnSpotOffset.x, Tile.Y + (int)jobSpawnSpotOffset.y, Tile.Z);
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
        ReadXmlParams(reader);
    }

    public void ReadXmlParams(XmlReader reader)
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

    #region Prototype Creation

    void LoadStructureLua()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Scripts");
        filePath = Path.Combine(filePath, "StructureActions.lua");

        string luaCode = File.ReadAllText(filePath);

        // Instantiate the singleton
        new StructureActions(luaCode);
    }

    public void CreateStructurePrototype(XmlNode structNode)
    {
        ObjectType = structNode.Attributes["objectType"].InnerText;
        Name = structNode.SelectSingleNode("Name").InnerText;
        MovementCost = float.Parse(structNode.SelectSingleNode("MoveCost").InnerText);
        Width = int.Parse(structNode.SelectSingleNode("Width").InnerText);
        Height = int.Parse(structNode.SelectSingleNode("Height").InnerText);
        if (structNode.SelectSingleNode("LinksToNeighbors") != null)
        {
            LinksToNeighbors = structNode.SelectSingleNode("LinksToNeighbors").InnerText;
        }
        RoomEnclosure = bool.Parse(structNode.SelectSingleNode("EnclosesRooms").InnerText);

        XmlNode defaultSpriteNode = structNode.SelectSingleNode("DefaultSpriteName");
        if (defaultSpriteNode != null)
        {
            DefaultSpriteName = defaultSpriteNode.InnerText;
        }

        XmlNode spriteNode = structNode.SelectSingleNode("SpriteName");
        if (spriteNode != null)
        {
            SpriteName = spriteNode.InnerText;
        }

        funcPositionValidation = DefaulIsValidPosition;

        string tileTags = structNode.SelectSingleNode("AllowedTiles").InnerText;

        string[] tileTypeTags = tileTags.Split('|');

        AllowedTileTypes = 0;
        foreach (string tileTypeTag in tileTypeTags)
        {
            AllowedTileTypes |= TileTypeData.Flag(tileTypeTag);
        }

        XmlNode jobOffsetNode = structNode.SelectSingleNode("JobOffset");
        jobSpotOffset = Vector2.zero;
        if (jobOffsetNode != null)
        {
            float[] arrJobOffset = jobOffsetNode.InnerText.Split(' ').Select(f => float.Parse(f)).ToArray();
            jobSpotOffset = new Vector2(arrJobOffset[0], arrJobOffset[1]);
        }

        XmlNode spawnOffsetNode = structNode.SelectSingleNode("JobSpawnOffset");
        jobSpawnSpotOffset = Vector2.zero;
        if (spawnOffsetNode != null)
        {
            float[] arrSpawnJobOffset = spawnOffsetNode.InnerText.Split(' ').Select(f => float.Parse(f)).ToArray();
            jobSpawnSpotOffset = new Vector2(arrSpawnJobOffset[0], arrSpawnJobOffset[1]);
        }

        XmlNode tintNode = structNode.SelectSingleNode("Tint");
        Tint = Color.white;
        if (tintNode != null)
        {
            string strTint = tintNode.InnerText;
            byte[] arrTint = strTint.Split(' ').Select(b => byte.Parse(b)).ToArray();

            Tint = new Color32(arrTint[0], arrTint[1], arrTint[2], arrTint[3]);
        }

        XmlNode tagsNode = structNode.SelectSingleNode("Tags");
        Tags = new List<string>();
        if (tagsNode != null)
        {
            XmlNodeList tagNodes = tagsNode.SelectNodes("Tag");
            foreach (XmlNode tagNode in tagNodes)
            {
                Tags.Add(tagNode.InnerText);
            }
        }

        XmlNode updateFnNode = structNode.SelectSingleNode("OnUpdate");
        if (updateFnNode != null)
        {
            string updateFuncName = updateFnNode.Attributes["FunctionName"].InnerText;
            RegisterUpdateAction(updateFuncName);
        }

        XmlNodeList eventActionNodeList = structNode.SelectNodes("EventAction");
        foreach (XmlNode eventActionNode in eventActionNodeList)
        {
            EventActions.ReadXml(eventActionNode);
        }

        XmlNode isEnterableFnNode = structNode.SelectSingleNode("IsEnterable");
        if (isEnterableFnNode != null)
        {
            string isEnterableFuncName = isEnterableFnNode.Attributes["FunctionName"].InnerText;
            isEnterableAction = isEnterableFuncName;
        }

        // Params
        structureParameters = new Dictionary<string, float>();
        XmlNode paramsNode = structNode.SelectSingleNode("Params");
        if (paramsNode != null)
        {
            XmlNodeList paramNodes = paramsNode.SelectNodes("Param");
            foreach (XmlNode paramNode in paramNodes)
            {
                string paramName = paramNode.Attributes["name"].InnerText;
                float paramValue = float.Parse(paramNode.Attributes["value"].InnerText);

                SetParameter(paramName, paramValue);
            }
        }

        // Animation
        Animations = ReadAnimations(structNode.SelectNodes("Animations/Animation"));
        //XmlNode animationsNode = structNode.SelectSingleNode("Animations");
        //if (animationsNode != null)
        //{
        //    XmlNodeList animationNodes = animationsNode.SelectNodes("Animations/Animation");

        //    Dictionary<string, SpritenameAnimation> animations = new Dictionary<string, SpritenameAnimation>();
        //    foreach (XmlNode animationNode in animationNodes)
        //    {
        //        SpritenameAnimation animation = new SpritenameAnimation();
        //        animation.ReadXml(animationNode);
        //        animations.Add(animation.State, animation);
        //    }
        //    Animations = new StructureAnimation(animations);
        //}

        jobs = new List<Job>();
    }

    public StructureAnimation ReadAnimations(XmlNodeList animationNodes)
    {
        if (animationNodes == null || animationNodes.Count == 0)
        {
            return null;
        }

        Dictionary<string, SpritenameAnimation> animations = new Dictionary<string, SpritenameAnimation>();

        foreach (XmlNode animationNode in animationNodes)
        {
            SpritenameAnimation animation = new SpritenameAnimation();
            animation.ReadXml(animationNode);
            animations.Add(animation.State, animation);
        }

        return new StructureAnimation(animations);
    }
    #endregion
}
