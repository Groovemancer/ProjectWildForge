using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;
using ProjectWildForge.Rooms;
using UnityEngine;

[MoonSharpUserData]
public class World : IXmlSerializable
{
    private Tile[,,] tiles;

    public Tile[,,] GetTiles()
    {
        return tiles;
    }

    public List<Actor> actors;
    public List<Structure> structures;

    public RoomManager RoomManager { get; private set; }
    public InventoryManager InventoryManager { get; private set; }

    // The pathfinding graph used to navigate our world map.
    public Path_TileGraph tileGraph;
    private Path_RoomGraph roomGraph;

    public Dictionary<string, Structure> structurePrototypes;
    public Dictionary<string, Job> structureJobPrototypes;

    public int Width { get; protected set; }
    public int Height { get; protected set; }
    public int Depth { get; protected set; }

    public int Volume
    {
        get
        {
            return Width * Height * Depth;
        }
    }

    Action<Structure> cbStructureCreated;
    Action<Tile> cbTileObjectChanged;
    Action<Actor> cbActorCreated;
    Action<Inventory> cbInventoryCreated;

    public event Action<Tile> OnTileChanged;
    public event Action<Tile> OnTileTypeChanged;

    const float PAUSE_GAME_SPEED = 0.0f;
    const float NORMAL_GAME_SPEED = 1.0f;
    const float FAST_GAME_SPEED = 2.0f;
    const float VERY_FAST_GAME_SPEED = 3.0f;
    const float SUPER_FAST_GAME_SPEED = 4.0f;

    float gameSpeed = 1.0f;
    float autsPerSec = 100f;

    // TODO: Most likely this will be replaced with a dedicated
    // class for managing job queues (plural!) that might also
    // be semi-static or self initializing or some damn thing.
    // For now, this is just a PUBLIC member of world
    public JobQueue jobQueue;

    public static World Current { get; protected set; }

    public Path_TileGraph TileGraph
    {
        get
        {
            if (tileGraph == null)
            {
                tileGraph = new Path_TileGraph(this);
            }

            return tileGraph;
        }
    }

    public Path_RoomGraph RoomGraph
    {
        get
        {
            if (roomGraph == null)
            {
                roomGraph = new Path_RoomGraph(this);
            }

            return roomGraph;
        }
    }

    /// <summary>
    /// Gets or sets the world seed.
    /// </summary>
    /// <value>The world seed.</value>
    public int Seed { get; protected set; }

    public World(int width, int height, int depth)
    {
        // Creates an empty world.
        Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        RandomUtils.Initialize(DataManager.Instance.RandomSeed);

        // Creates an empty world.
        SetupWorld(width, height, depth);

        // Make one actor
        Actor initialActor = CreateActor(GetTileAt(Width / 2, Height / 2, 0));
        Actor initialActor2 = CreateActor(GetTileAt(Width / 2 + 2, Height / 2, 0));
        Actor initialActor3 = CreateActor(GetTileAt(Width / 2 + 4, Height / 2, 0));

        DetermineVisibility(initialActor.CurrTile);
    }

    /// <summary>
    /// Default constructor, used when loading a file.
    /// </summary>
    public World()
    {

    }

    private void SetupWorld(int width, int height, int depth)
    {
        jobQueue = new JobQueue();

        // Set the current world to be this world.
        // TODO: Do we need to do any cleanup of the old world?
        Current = this;

        Width = width;
        Height = height;
        Depth = depth;

        tiles = new Tile[Width, Height, Depth];

        RoomManager = new RoomManager();
        RoomManager.Adding += (room) => roomGraph = null;
        RoomManager.Removing += (room) => roomGraph = null;

        FillTilesArray();

        RegisterStructureCreated(OnStructureCreated);

        //Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateStructurePrototypes();

        actors = new List<Actor>();
        structures = new List<Structure>();
        InventoryManager = new InventoryManager();
    }

    private void FillTilesArray()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    tiles[x, y, z] = new Tile(x, y, z);
                    tiles[x, y, z].TileChanged += OnTileChangedCallback;
                    tiles[x, y, z].TileTypeChanged += OnTileTypeChangedCallback;
                    tiles[x, y, z].Room = RoomManager.OutsideRoom;
                }
            }
        }
    }

    public void Update(float deltaTime)
    {
        float deltaAuts = autsPerSec * gameSpeed * deltaTime;

        if (deltaAuts > 0)
        {
            foreach (Actor a in actors)
            {
                a.Update(deltaAuts);
            }

            foreach (Structure s in structures)
            {
                s.Update(deltaAuts);
            }
        }
    }

    public void SetGameSpeed(GameSpeed desiredSpeed)
    {
        switch (desiredSpeed)
        {
            case GameSpeed.Pause:
                gameSpeed = PAUSE_GAME_SPEED;
                break;
            case GameSpeed.Normal:
                gameSpeed = NORMAL_GAME_SPEED;
                break;
            case GameSpeed.Fast:
                gameSpeed = FAST_GAME_SPEED;
                break;
            case GameSpeed.VeryFast:
                gameSpeed = VERY_FAST_GAME_SPEED;
                break;
            case GameSpeed.SuperFast:
                gameSpeed = SUPER_FAST_GAME_SPEED;
                break;
        }
    }

    public Actor CreateActor(Tile t)
    {
        Actor a = new Actor(t);

        actors.Add(a);

        if (cbActorCreated != null)
            cbActorCreated(a);

        return a;
    }

    void LoadStructureLua()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Scripts");
        filePath = Path.Combine(filePath, "StructureActions.lua");

        // Instantiate the singleton
        new StructureActions(filePath);
    }

    private void CreateStructurePrototypes()
    {
        LoadStructureLua();

        structurePrototypes = new Dictionary<string, Structure>();
        structureJobPrototypes = new Dictionary<string, Job>();

        // READ STRUCTURE PROTOTYPE XML FILE HERE

        try
        {
            XmlDocument doc = new XmlDocument();

            string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
            filePath = Path.Combine(filePath, "Structures.xml");
            doc.Load(filePath);

            XmlNodeList structNodes = doc.SelectNodes("Structures/Structure");

            foreach (XmlNode structNode in structNodes)
            {
                Structure structurePrototype = new Structure();

                structurePrototype.CreateStructurePrototype(structNode);

                structurePrototypes.Add(structurePrototype.ObjectType, structurePrototype);

                // Building Job
                XmlNode buildJobNode = structNode.SelectSingleNode("BuildingJob");
                if (buildJobNode != null)
                {
                    float jobCost = float.Parse(buildJobNode.Attributes["jobCost"].InnerText);
                    XmlNodeList invNodes = buildJobNode.SelectNodes("Inventory");
                    List<Inventory> invReqs = new List<Inventory>();
                    foreach (XmlNode invNode in invNodes)
                    {
                        string invType = invNode.Attributes["objectType"].InnerText;
                        int invAmount = int.Parse(invNode.Attributes["amount"].InnerText);
                        invReqs.Add(new Inventory(invType, invAmount, 0));
                    }

                    World.Current.structureJobPrototypes.Add(structurePrototype.ObjectType,
                        new Job(null, structurePrototype.ObjectType, StructureActions.JobComplete_StructureBuilding,
                            jobCost, invReqs.ToArray()));
                }
                // Deconstruct Job
                // TODO

            }
            //DebugUtils.Log("Locale Entries Loaded: " + Instance.Data.Count);
        }
        catch (Exception e)
        {
            DebugUtils.DisplayError(e.ToString(), false);
            DebugUtils.LogException(e);
        }


        //structurePrototypes["struct_WoodDoor"].RegisterUpdateAction(StructureActions.Door_UpdateAction);
        //structurePrototypes["struct_WoodDoor"].IsEnterable = StructureActions.Door_IsEnterable;
        //structurePrototypes["struct_WorkStation"].RegisterUpdateAction(StructureActions.WorkStation_UpdateAction);
        //structurePrototypes["struct_Stockpile"].RegisterUpdateAction(StructureActions.Stockpile_UpdateAction);
        //structurePrototypes["struct_O2Generator"].RegisterUpdateAction(StructureActions.OxygenGenerator_UpdateAction);
    }

    /*
    private void CreateStructurePrototypes()
    {
        // This will be replacd by a function that reads all of our structure data
        // from a text file in the future

        structurePrototypes = new Dictionary<string, Structure>();
        structureJobPrototypes = new Dictionary<string, Job>();

        structurePrototypes.Add("struct_StoneWall",
            new Structure(
                "struct_StoneWall",
                0,      // Impassable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                true    // Enclose rooms
            )
        );
        structurePrototypes["struct_StoneWall"].Name = "Stone Wall";

        structureJobPrototypes.Add("struct_StoneWall",
            new Job(null, "struct_StoneWall", StructureActions.JobComplete_StructureBuilding,
            300,
            new Inventory[] {
                new Inventory("inv_RawStone", 5, 0)
                }
            )
        );

        structurePrototypes.Add("struct_Door",
            new Structure(
                "struct_Door",
                1,      // Door Pathfinding Cost
                1,      // Width
                1,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                true    // Enclose rooms
            )
        );

        structurePrototypes["struct_Door"].SetParameter("openness", 0f); // 0 = closed door, 1 = fully open door, in between is partially opened
        structurePrototypes["struct_Door"].SetParameter("isOpening", 0);
        structurePrototypes["struct_Door"].SetParameter("doorOpenTime", 25f); // Amount of AUTs to open door
        structurePrototypes["struct_Door"].RegisterUpdateAction(StructureActions.Door_UpdateAction);
        structurePrototypes["struct_Door"].IsEnterable = StructureActions.Door_IsEnterable;

        structurePrototypes.Add("struct_Stockpile",
            new Structure(
                "struct_Stockpile",
                1,      // Not Impassable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                false    // Enclose rooms
            )
        );

        structurePrototypes["struct_Stockpile"].RegisterUpdateAction(StructureActions.Stockpile_UpdateAction);
        structurePrototypes["struct_Stockpile"].tint = new Color32(255, 0, 255, 255);
        structureJobPrototypes.Add("struct_Stockpile",
            new Job(
                null,
                "struct_Stockpile",
                StructureActions.JobComplete_StructureBuilding,
                -1,
                null
            )
        );

        structurePrototypes.Add("struct_WorkStation",
            new Structure(
                "struct_WorkStation",
                1,      // Pathfinding Cost
                3,      // Width
                3,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                false    // Enclose rooms
            )
        );
        structurePrototypes["struct_WorkStation"].jobSpotOffset = new Vector2(1, 0);
        structurePrototypes["struct_WorkStation"].RegisterUpdateAction(StructureActions.WorkStation_UpdateAction);

        structurePrototypes.Add("struct_O2Generator",
            new Structure(
                "struct_O2Generator",
                10,      // Pathfinding Cost
                2,      // Width
                2,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                false    // Enclose rooms
            )
        );
        structurePrototypes["struct_O2Generator"].RegisterUpdateAction(StructureActions.OxygenGenerator_UpdateAction);
    }
    */

    public void SetupPathfindingExample()
    {
        //Debug.Log("SetupPathfindingExample");

        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y, 0].SetTileType(TileTypeData.GetByFlagName("Floor"), false);

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceStructure("struct_StoneWall", tiles[x, y, 0], false);
                    }
                }
            }
        }
    }

    private void DetermineVisibility(Tile start)
    {
        Queue<Tile> roomsToVisit = new Queue<Tile>();

        roomsToVisit.Enqueue(start);
        Tile current;
        do
        {
            current = roomsToVisit.Dequeue();

            bool canEnterCurrent = current.IsEnterable() != Enterability.Never;

            foreach (Tile neighbor in current.GetNeighbors(false, true, true))
            {
                if (neighbor == null || neighbor.CanSee)
                {
                    continue;
                }

                if (canEnterCurrent)
                {
                    neighbor.CanSee = true;
                    if (roomsToVisit.Contains(neighbor) == false)
                    {
                        roomsToVisit.Enqueue(neighbor);
                    }
                }
            }
        }
        while (roomsToVisit.Count > 0);
    }

    public void RandomizeTiles()
    {
        Debug.Log("World::RandomizeTiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (RandomUtils.Range(0, 3) == 0)
                {
                    tiles[x, y, 0].SetTileType(TileTypeData.GetByFlagName("Dirt"), false);
                }
                else
                {
                    tiles[x, y, 0].SetTileType(TileTypeData.GetByFlagName("Grass"), false);
                }
            }
        }
    }

    public Tile GetTileAt(int x, int y, int z)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0 || z >= Depth || z < 0)
        {
            //Debug.LogError("World::GetTileAt Tile (" + x + ", " + y + ") is out of range.");
            return null;
        }

        return tiles[x, y, z];
    }

    public Structure PlaceStructure(string structureType, Tile t, bool doRoomFloodFill = true)
    {
        //TODO: This function assumes 1x1 tiles -- change this later!

        if (structurePrototypes.ContainsKey(structureType) == false)
        {
            Debug.LogError("structurePrototypes doesn't contain a proto for key: " + structureType);
            return null;
        }

        Structure structure = Structure.PlaceInstance(structurePrototypes[structureType], t);

        if (structure == null)
        {
            // Failed to place object -- most likely there was already something there.
            return null;
        }

        structure.RegisterOnRemovedCallback(OnStructureRemoved);
        structures.Add(structure);

        // Do we need to recalculate our rooms?
        if (doRoomFloodFill && structure.RoomEnclosure)
        {
            RoomManager.DoRoomFloodFill(structure.Tile, true);
        }

        if (cbStructureCreated != null)
        {
            cbStructureCreated(structure);
        }

        //InvalidateTileGraph(); // Reset the pathfinding system

        return structure;
    }



    // This should be called whenever a change to the world
    // means that our old pathfinding info is invalid
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }

    public void RegenerateGraphAtTile(Tile tile)
    {
        if (tileGraph != null)
        {
            tileGraph.RegenerateGraphAtTile(tile);
        }
    }

    private void OnStructureCreated(Structure structure)
    {
        if (structure.MovementCost != 1)
        {
            // Since tiles return movement cost as their base cost multiplied
            // by the structure's movement cost, a structure movement cost
            // of exactly 1 doesn't impact our pathfinding system, so we can
            // occasionally avoid invalidating pathfinding graphs.
            // InvalidateTileGraph();    
            // Reset the pathfinding system
            if (tileGraph != null)
            {
                tileGraph.RegenerateGraphAtTile(structure.Tile);
            }
        }
    }

    public void RegisterStructureCreated(Action<Structure> callbackfunc)
    {
        cbStructureCreated += callbackfunc;
    }

    public void UnregisterStructureCreated(Action<Structure> callbackfunc)
    {
        cbStructureCreated -= callbackfunc;
    }

    public void RegisterActorCreated(Action<Actor> callbackfunc)
    {
        cbActorCreated += callbackfunc;
    }

    public void UnregisterActorCreated(Action<Actor> callbackfunc)
    {
        cbActorCreated -= callbackfunc;
    }

    public void RegisterInventoryCreated(Action<Inventory> callbackfunc)
    {
        cbInventoryCreated += callbackfunc;
    }

    public void UnregisterInventoryCreated(Action<Inventory> callbackfunc)
    {
        cbInventoryCreated -= callbackfunc;
    }

    public void RegisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileObjectChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileObjectChanged -= callbackfunc;
    }

    // Gets called whenever ANY tile changes
    private void OnTileChangedCallback(Tile t)
    {
        if (OnTileChanged == null)
            return;

        OnTileChanged(t);
    }

    private void OnTileTypeChangedCallback(Tile t)
    {
        if (OnTileTypeChanged == null)
        {
            return;
        }

        OnTileTypeChanged(t);

        if (tileGraph != null)
        {
            tileGraph.RegenerateGraphAtTile(t);
            tileGraph.RegenerateGraphAtTile(t.Down());
        }
    }

    public bool IsStructurePlacementValid(string structureType, Tile t)
    {
        return structurePrototypes[structureType].IsValidPosition(t);
    }

    public Structure GetStructurePrototype(string objType)
    {
        if (structurePrototypes.ContainsKey(objType) == false)
        {
            Debug.LogError("No structure with type: " + objType);
            return null;
        }

        return structurePrototypes[objType];
    }

    public void OnInventoryCreated(Inventory inv)
    {
        if (cbInventoryCreated != null)
            cbInventoryCreated(inv);
    }

    public void OnStructureRemoved(Structure strct)
    {
        structures.Remove(strct);
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
        // Save info here
        writer.WriteAttributeString("Seed", Seed.ToString());
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());
        writer.WriteAttributeString("Depth", Depth.ToString());

        writer.WriteStartElement("Rooms");
        foreach (Room room in RoomManager)
        {
            if (room.IsOutsideRoom())
                continue;   // Skip the outside room

            writer.WriteStartElement("Room");
            room.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    if (tiles[x, y, z].Type != TileTypeData.Instance.EmptyType)
                    {
                        writer.WriteStartElement("Tile");
                        tiles[x, y, z].WriteXml(writer);
                        writer.WriteEndElement();
                    }

                }
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Structures");
        foreach (Structure structure in structures)
        {
            writer.WriteStartElement("Structure");
            structure.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Actors");
        foreach (Actor actor in actors)
        {
            writer.WriteStartElement("Actor");
            actor.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        Debug.Log("ReadXML");
        // Load info here

        Seed = int.Parse(reader.GetAttribute("Seed"));
        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));
        Depth = int.Parse(reader.GetAttribute("Depth"));

        SetupWorld(Width, Height, Depth);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Rooms":
                    ReadXmlRooms(reader);
                    break;
                case "Tiles":
                    ReadXmlTiles(reader);
                    break;
                case "Structures":
                    ReadXmlStructures(reader);
                    break;
                case "Actors":
                    ReadXmlActors(reader);
                    break;
            }
        }

        // DEBUGGING ONLY! REMOVE ME LATER!
        // Create an Inventory Item
        Inventory inv = new Inventory("inv_RawStone", 50, 50);
        Tile t = GetTileAt(Width / 2, Height / 2, 0);
        InventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }

        inv = new Inventory("inv_RawStone", 50, 4);
        t = GetTileAt(Width / 2 + 2, Height / 2, 0);
        InventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }

        inv = new Inventory("inv_RawStone", 50, 3);
        t = GetTileAt(Width / 2 + 1, Height / 2 + 2, 0);
        InventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }

        tileGraph = new Path_TileGraph(this);
    }

    void ReadXmlTiles(XmlReader reader)
    {
        // We are in the "Tiles" element, so read elements until
        // we run out of "Tile" nodes.

        if (reader.ReadToDescendant("Tile"))
        {
            // We have at least one tile, so do something with it.
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                int z = int.Parse(reader.GetAttribute("Z"));
                tiles[x, y, z].ReadXml(reader);
            } while (reader.ReadToNextSibling("Tile"));
        }
    }

    void ReadXmlStructures(XmlReader reader)
    {
        if (reader.ReadToDescendant("Structure"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                int z = int.Parse(reader.GetAttribute("Z"));

                Structure structure = PlaceStructure(reader.GetAttribute("objectType"), tiles[x, y, z], false);
                structure.ReadXml(reader);
            } while (reader.ReadToNextSibling("Structure"));

            // TODO we don't need to do a flood fill on load, because we're getting room info from the save file
            /*
            foreach (Structure strct in structures)
            {
                Room.DoRoomFloodFill(strct.Tile, true);
            }
            */
        }
    }

    void ReadXmlRooms(XmlReader reader)
    {
        Debug.Log("ReadXmlRooms");
        if (reader.ReadToDescendant("Room"))
        {
            do
            {
                Room r = new Room();
                RoomManager.Add(r);
                r.ReadXml(reader);
            } while (reader.ReadToNextSibling("Room"));
        }
    }

    void ReadXmlActors(XmlReader reader)
    {
        if (reader.ReadToDescendant("Actor"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                int z = int.Parse(reader.GetAttribute("Z"));

                Actor actor = CreateActor(tiles[x, y, z]);
                actor.ReadXml(reader);
            } while (reader.ReadToNextSibling("Actor"));
        }
    }

    #endregion
}
