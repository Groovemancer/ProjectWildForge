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
public class Structure : IXmlSerializable, ISelectable, IUpdatable, IPrototypable, IBuildable
{
    /// <summary>
    /// These actions are called every update. They get passed the structure
    /// they belong to, plus a deltaTime.
    /// </summary>
    //protected Action<Structure, float> updateActions;
    protected List<string> updateActions;

    protected string isEnterableAction;

    /// <summary>
    /// This action is called to get the progress info based on the furniture parameters.
    /// </summary>
    private string getProgressInfoNameAction;

    private HashSet<string> typeTags;

    private HashSet<BuildableComponent> components;

    private Dictionary<string, OrderAction> orderActions;

    private bool isOperating;

    // Did we have power in the last update?
    private bool prevUpdatePowerOn;

    private List<string> replaceableStructure = new List<string>();

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

    /// <summary>
    /// Gets a component that handles the jobs linked to the structure.
    /// </summary>
    public BuildableJobs Jobs { get; private set; }

    /// <summary>
    /// Gets or sets the parameters that is tied to the structure.
    /// </summary>
    public Parameter Parameters { get; set; }

    public string GetDefaultSpriteName()
    {
        if (!string.IsNullOrEmpty(DefaultSpriteName))
        {
            return DefaultSpriteName;
        }

        // Else return default Type string
        return Type;
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
        return Type;
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

        foreach (BuildableComponent component in components)
        {
            component.EveryFrameUpdate(deltaTime);
        }
    }

    /// <summary>
    /// This function is called to update the structure. This will also trigger EventsActions.
    /// </summary>
    /// <param name="deltaTime">The time since the last update was called.</param>
    public void FixedFrequencyUpdate(float deltaTime)
    {
        // requirements from components (gas, ...)
        bool canFunction = true;
        BuildableComponent.Requirements newRequirements = BuildableComponent.Requirements.None;
        foreach (BuildableComponent component in components)
        {
            bool componentCanFunction = component.CanFunction();
            canFunction &= componentCanFunction;

            // if it can't function, collect all stuff it needs (power, gas, ...) for icon signalization
            if (!componentCanFunction)
            {
                newRequirements |= component.Needs;
            }
        }

        // requirements were changed, force update of status icons
        if (Requirements != newRequirements)
        {
            Requirements = newRequirements;
            OnIsOperatingChanged(this);
        }

        IsOperating = canFunction;

        if (canFunction == false)
        {
            if (prevUpdatePowerOn)
            {
                EventActions.Trigger("OnPowerOff", this, deltaTime);
            }

            Jobs.PauseAll();
            prevUpdatePowerOn = false;
            return;
        }

        Jobs.ResumeAll();

        if (EventActions != null)
        {
            EventActions.Trigger("OnUpdate", this, deltaTime);
        }

        foreach (BuildableComponent component in components)
        {
            component.FixedFrequencyUpdate(deltaTime);
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
    public string Type { get; protected set; }

    /// <summary>
    /// Gets a list of structure Type this structure can be replaced with.
    /// This should most likely not be a list of strings.
    /// </summary>
    /// <value>A list of structure that this structure can be replaced with.</value>
    public List<string> ReplaceableStructure
    {
        get { return replaceableStructure; }
    }

    private string _Name = null;
    public string Name
    {
        get
        {
            if (_Name == null || _Name.Length == 0)
            {
                return Type;
            }
            return _Name;
        }
        set
        {
            _Name = value;
        }
    }

    public string Description { get; private set; }

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

    /// <summary>
    /// Checks whether structure has some custom progress info.
    /// </summary>
    public bool HasCustomProgressReport
    {
        get
        {
            return !string.IsNullOrEmpty(getProgressInfoNameAction);
        }
    }

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
    /// Flag with structure requirements (used for showing icon overlay, e.g. No power, ... ).
    /// </summary>
    public BuildableComponent.Requirements Requirements { get; protected set; }

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

    /// <summary>
    /// Gets a value indicating whether the furniture is operating or not.
    /// </summary>
    /// <value>Whether the furniture is operating or not.</value>
    public bool IsOperating
    {
        get
        {
            return isOperating;
        }

        private set
        {
            if (isOperating == value)
            {
                return;
            }

            isOperating = value;
            OnIsOperatingChanged(this);
        }
    }

    /// <summary>
    /// This flag is set if the structure is tasked to be destroyed.
    /// </summary>
    public bool IsBeingDestroyed { get; protected set; }
    public bool IsSelected { get; set; }

    /// <summary>
    /// This event will trigger when the structure has been changed.
    /// This is means that any change (parameters, job state etc) to the furniture will trigger this.
    /// </summary>
    public event Action<Structure> Changed;

    /// <summary>
    /// This event will trigger when the structure has been removed.
    /// </summary>
    public event Action<Structure> Removed;

    /// <summary>
    /// This event will trigger if <see cref="IsOperating"/> has been changed.
    /// </summary>
    public event Action<Structure> IsOperatingChanged;

    // TODO: Implement larger objects
    // TODO: Implement object rotation

    // Empty constructor is used for serialization
    public Structure()
    {
        Parameters = new Parameter();
        Jobs = new BuildableJobs(this);
        updateActions = new List<string>();
        EventActions = new EventActions();

        Tint = Color.white;
        VerticalDoor = false;

        Width = 1;
        Height = 1;
        typeTags = new HashSet<string>();
        LinksToNeighbors = string.Empty;
        components = new HashSet<BuildableComponent>();
        orderActions = new Dictionary<string, OrderAction>();
    }

    // Copy Constructor -- don't call this directly, unless we never
    // do ANY sub-classing. Instead use Clone(), which is more virtual.
    protected Structure(Structure other)
    {
        this.Type = other.Type;
        this.Name = other.Name;
        this.MovementCost = other.MovementCost;
        this.RoomEnclosure = other.RoomEnclosure;
        this.Width = other.Width;
        this.Height = other.Height;
        this.Tint = other.Tint;
        this.LinksToNeighbors = other.LinksToNeighbors;
        this.AllowedTileTypes = other.AllowedTileTypes;

        this.Parameters = new Parameter(other.Parameters);
        this.Jobs = new BuildableJobs(this, other.Jobs);
        this.typeTags = new HashSet<string>(other.typeTags);

        // add cloned components
        components = new HashSet<BuildableComponent>();
        foreach (BuildableComponent component in other.components)
        {
            components.Add(component.Clone());
        }

        // add cloned order actions
        orderActions = new Dictionary<string, OrderAction>();
        foreach (var orderAction in other.orderActions)
        {
            orderActions.Add(orderAction.Key, orderAction.Value.Clone());
        }

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
        this.getProgressInfoNameAction = other.getProgressInfoNameAction;

        RequiresSlowUpdate = EventActions.HasEvent("OnUpdate") || components.Any(c => c.RequiresSlowUpdate);
        RequiresFastUpdate = EventActions.HasEvent("OnFastUpdate") || components.Any(c => c.RequiresFastUpdate);

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
        this.Type = objectType;
        this.Name = name;
        this.MovementCost = movementCost;
        this.RoomEnclosure = roomEnclosure;
        this.Width = width;
        this.Height = height;
        this.LinksToNeighbors = linksToNeighbors;
        this.AllowedTileTypes = allowedTileTypes;

        Parameters = new Parameter();
        Jobs = new BuildableJobs(this);
        typeTags = new HashSet<string>();
        updateActions = new List<string>();
        isEnterableAction = "";
    }

    public static Structure PlaceInstance(Structure proto, Tile tile)
    {
        if (proto.IsValidPosition(tile) == false)
        {
            DebugUtils.LogErrorChannel("Structure", "PlaceInstance :: Position Validity Function returned FALSE. " + proto.Type + " " + tile.X + ", " + tile.Y + ", " + tile.Z);
            return null;
        }

        // We know our placement destination is valid.
        Structure structObj = proto.Clone();
        structObj.Tile = tile;

        // FIXME: This assumes we are 1x1!
        if (tile.PlaceStructure(structObj) == false)
        {
            // For some reason,we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        // need to update reference to structure and call Initialize (so components can place hooks on events there)
        foreach (BuildableComponent component in structObj.components)
        {
            component.Initialize(structObj);
        }

        if (structObj.LinksToNeighbors != string.Empty)
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
                    if (tileAt != null && tileAt.Structure != null && tileAt.Structure.Changed != null)
                    {
                        tileAt.Structure.Changed(tileAt.Structure);
                    }
                }
            }
        }

        // Let our workspot tile know it is reserved for us
        World.Current.ReserveTileAsWorkSpot(structObj);

        return structObj;
    }

    public bool IsExit()
    {
        if (RoomEnclosure && MovementCost > 0f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if the position of the structure is valid or not.
    /// This is called when placing the structure.
    /// TODO : Add some Lua special requierments.
    /// </summary>
    /// <param name="t">The base tile.</param>
    /// <returns>True if the tile is valid for the placement of the structure.</returns>
    public bool IsValidPosition(Tile tile)
    {
        if (typeTags.Contains("OutdoorOnly"))
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
        Changed += callbackFunc;
    }

    public void UnregisterOnChangedCallback(Action<Structure> callbackFunc)
    {
        Changed -= callbackFunc;
    }

    public void RegisterOnRemovedCallback(Action<Structure> callbackFunc)
    {
        Removed += callbackFunc;
    }

    public void UnregisterOnRemovedCallback(Action<Structure> callbackFunc)
    {
        Removed -= callbackFunc;
    }

    /// <summary>
    /// Accepts for storage.
    /// </summary>
    /// <returns>A list of RequestedItem which the Structure accepts for storage.</returns>
    public RequestedItem[] AcceptsForStorage()
    {
        if (HasTypeTag("Storage") == false)
        {
            DebugUtils.LogChannel("Stockpile_messages", "Someone is asking a non-stockpile to store stuff!?");
            return null;
        }

        // TODO: read this from structure params
        Dictionary<string, RequestedItem> itemsDict = new Dictionary<string, RequestedItem>();
        foreach (Inventory inventoryProto in PrototypeManager.Inventory.Values)
        {
            itemsDict[inventoryProto.Type] = new RequestedItem(inventoryProto.Type, 1, inventoryProto.MaxStackSize);
        }

        return itemsDict.Values.ToArray();
    }

    public string GetProgressInfo()
    {
        if (string.IsNullOrEmpty(getProgressInfoNameAction))
        {
            return string.Empty;
        }
        else
        {
            DynValue ret = FunctionsManager.Structure.Call(getProgressInfoNameAction, this);
            return ret.String;
        }
    }

    /// <summary>
    /// Gets component if present or null.
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
    /// <param name="componentName">Type of the component, e.g. PowerConnection, WorkShop.</param>
    /// <returns>Component or null.</returns>
    public T GetComponent<T>(string componentName) where T : BuildableComponent
    {
        if (components != null)
        {
            foreach (BuildableComponent component in components)
            {
                if (component.Type.Equals(componentName))
                {
                    return (T)component;
                }
            }
        }

        return null;
    }

    public BuildableComponent.Requirements GetPossibleRequirements()
    {
        BuildableComponent.Requirements requires = BuildableComponent.Requirements.None;

        foreach (BuildableComponent component in components)
        {
            requires |= component.Needs;
        }

        return requires;
    }

    public T GetOrderAction<T>() where T : OrderAction
    {
        OrderAction orderAction;
        if (orderActions.TryGetValue(typeof(T).Name, out orderAction))
        {
            return (T)orderAction;
        }
        else
        {
            return null;
        }
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

    public bool HasTypeTag(string tag)
    {
        return typeTags.Contains(tag);
    }

    public string[] GetTypeTags()
    {
        return typeTags.ToArray();
    }

    /// <summary>
    /// Returns LocalizationCode name for the furniture.
    /// </summary>
    /// <returns>LocalizationCode for the name of the furniture.</returns>
    public string GetName()
    {
        return StringUtils.GetLocalizedTextFiltered(Name);
    }

    public void Deconstruct()
    {
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
            structure.Jobs.CancelAll();
        }

        World.Current.UnreserveTileAsWorkSpot(this);

        Tile.UnplaceStructure();

        if (Removed != null)
        {
            Removed(this);
        }

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
                    if (t != null && t.Structure != null && t.Structure.Changed != null)
                    {
                        t.Structure.Changed(t.Structure);
                    }
                }
            }
        }

        // At this point, no DATA structures should be pointing to us, so we
        // should get garbage-collected.
    }

    private void OnIsOperatingChanged(Structure structure)
    {
        Action<Structure> handler = IsOperatingChanged;
        if (handler != null)
        {
            handler(structure);
        }
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

        Parameters.ToXml(writer);
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
                string v = reader.GetAttribute("value");
                Parameters[k].SetValue(v);
            } while (reader.ReadToNextSibling("Param"));
        }
    }

    #endregion

    #region Prototype Creation

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.SelectSingleNode("NameLocaleId").InnerText;
        Description = PrototypeReader.ReadXml(string.Empty, rootNode.SelectSingleNode("Description"));
        MovementCost = float.Parse(rootNode.SelectSingleNode("MoveCost").InnerText);
        Width = int.Parse(rootNode.SelectSingleNode("Width").InnerText);
        Height = int.Parse(rootNode.SelectSingleNode("Height").InnerText);
        if (rootNode.SelectSingleNode("LinksToNeighbors") != null)
        {
            LinksToNeighbors = rootNode.SelectSingleNode("LinksToNeighbors").InnerText;
        }
        RoomEnclosure = bool.Parse(rootNode.SelectSingleNode("EnclosesRooms").InnerText);

        //XmlNode defaultSpriteNode = rootNode.SelectSingleNode("DefaultSpriteName");
        //if (defaultSpriteNode != null)
        //{
        //    DefaultSpriteName = defaultSpriteNode.InnerText;
        //}

        //XmlNode spriteNode = rootNode.SelectSingleNode("SpriteName");
        //if (spriteNode != null)
        //{
        //    SpriteName = spriteNode.InnerText;
        //}

        string tileTags = rootNode.SelectSingleNode("AllowedTiles").InnerText;

        string[] tileTypeTags = tileTags.Split('|');

        AllowedTileTypes = 0;
        foreach (string tileTypeTag in tileTypeTags)
        {
            AllowedTileTypes |= TileTypeData.Flag(tileTypeTag);
        }

        Jobs.ReadOffsets(rootNode.SelectSingleNode("Jobs"));

        XmlNode tintNode = rootNode.SelectSingleNode("Tint");
        Tint = Color.white;
        if (tintNode != null)
        {
            string strTint = tintNode.InnerText;
            byte[] arrTint = strTint.Split(' ').Select(b => byte.Parse(b)).ToArray();

            Tint = new Color32(arrTint[0], arrTint[1], arrTint[2], arrTint[3]);
        }

        XmlNode tagsNode = rootNode.SelectSingleNode("Tags");
        typeTags = new HashSet<string>();
        if (tagsNode != null)
        {
            XmlNodeList tagNodes = tagsNode.SelectNodes("Tag");
            foreach (XmlNode tagNode in tagNodes)
            {
                typeTags.Add(tagNode.InnerText);
            }
        }

        XmlNode updateFnNode = rootNode.SelectSingleNode("OnUpdate");
        if (updateFnNode != null)
        {
            string updateFuncName = updateFnNode.Attributes["FunctionName"].InnerText;
            RegisterUpdateAction(updateFuncName);
        }

        EventActions.ReadXml(rootNode.SelectNodes("EventAction"));

        isEnterableAction = PrototypeReader.ReadXml(isEnterableAction, rootNode.SelectSingleNode("IsEnterableAction"));

        getProgressInfoNameAction = PrototypeReader.ReadXml(getProgressInfoNameAction, rootNode.SelectSingleNode("GetProgressInfoNameAction"));

        // Params
        Parameters.FromXml(rootNode.SelectSingleNode("Params"));

        // Animation
        Animations = ReadAnimations(rootNode.SelectNodes("Animations/Animation"));

        orderActions = PrototypeReader.ReadOrderActions(rootNode.SelectSingleNode("OrderActions"));

        XmlNodeList componentNodes = rootNode.SelectNodes("Component");
        foreach (XmlNode componentNode in componentNodes)
        {
            BuildableComponent component = BuildableComponent.FromXml(componentNode);
            if (component != null)
            {
                component.InitializePrototype(this);
                components.Add(component);
                if (component.IsValid() == false)
                {
                    DebugUtils.LogErrorChannel("BuildableComponent", "Error parsing " + component.GetType() + " for " + Type);
                }
            }
        }

        // TODO Implement Order Actions
        /*
        // Building Job
        XmlNode buildJobNode = rootNode.SelectSingleNode("BuildingJob");
        if (buildJobNode != null)
        {
            float jobCost = float.Parse(buildJobNode.Attributes["jobCost"].InnerText);
            XmlNodeList invNodes = buildJobNode.SelectNodes("Inventory");
            List<Inventory> invReqs = new List<Inventory>();
            foreach (XmlNode invNode in invNodes)
            {
                string invType = invNode.Attributes["Type"].InnerText;
                int invAmount = int.Parse(invNode.Attributes["amount"].InnerText);
                invReqs.Add(new Inventory(invType, 0, invAmount));
            }

            World.Current.structureJobPrototypes.Add(Type,
                new Job(null, Type, StructureActions.JobComplete_StructureBuilding,
                    jobCost, invReqs.ToArray()));
        }
        // Deconstruct Job
        // TODO
        */
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

    public string GetDescription()
    {
        return Description;
    }

    public string GetJobDescription()
    {
        return string.Empty;
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        // try to get some info from components
        foreach (BuildableComponent component in components)
        {
            IEnumerable<string> desc = component.GetDescription();
            if (desc != null)
            {
                foreach (string inf in desc)
                {
                    yield return inf;
                }
            }
        }

        // TODO: Implement Health system
        /*
        if (health != null)
        {
            yield return health.TextForSelectionPanel();
        }
        */

        yield return GetProgressInfo();
    }

    #endregion
}
