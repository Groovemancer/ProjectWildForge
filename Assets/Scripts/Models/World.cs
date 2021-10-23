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
using UnityEngine.Rendering;

[MoonSharpUserData]
public class World : IXmlSerializable
{
    private Tile[,,] tiles;

    public Tile[,,] GetTiles()
    {
        return tiles;
    }

    /// <summary>
    /// Gets the room manager.
    /// </summary>
    /// <value>The room manager.</value>
    public RoomManager RoomManager { get; private set; }

    /// <summary>
    /// Gets the inventory manager.
    /// </summary>
    /// <value>The inventory manager.</value>
    public InventoryManager InventoryManager { get; private set; }

    /// <summary>
    /// Gets the structure manager.
    /// </summary>
    /// <value>The structure manager.</value>
    public StructureManager StructureManager { get; private set; }

    /// <summary>
    /// Gets the plant manager.
    /// </summary>
    /// <value>The plant manager.</value>
    public PlantManager PlantManager { get; private set; }

    /// <summary>
    /// Gets the actor manager.
    /// </summary>
    /// <value>The actor manager.</value>
    public ActorManager ActorManager { get; private set; }

    /// <summary>
    /// Gets the job manager.
    /// </summary>
    /// <value>The job manager.</value>
    public JobManager JobManager { get; private set; }

    /// <summary>
    /// Gets the light manager.
    /// </summary>
    /// <value>The light manager.</value>
    public LightManager LightManager { get; private set; }

    // The pathfinding graph used to navigate our world map.
    public Path_TileGraph tileGraph;
    private Path_RoomGraph roomGraph;

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
        Actor initialActor1 = ActorManager.Create(GetTileAt(Width / 2, Height / 2, 0),
            RandomUtils.ObjectFromList(PrototypeManager.Race.Keys.ToList(), "race_Elf"), null, RandomUtils.Boolean());
        Actor initialActor2 = ActorManager.Create(GetTileAt(Width / 2 + 2, Height / 2, 0),
            RandomUtils.ObjectFromList(PrototypeManager.Race.Keys.ToList(), "race_Elf"), null, RandomUtils.Boolean());
        //Actor initialActor3 = ActorManager.Create(GetTileAt(Width / 2 + 4, Height / 2, 0),
        //    RandomUtils.ObjectFromList(PrototypeManager.Race.Keys.ToList(), "race_Elf"), null, RandomUtils.Boolean());


        LightManager.AddPointLight("Point Light", new Vector3(50, 50, 0), MathUtils.HexToRGB(0xFFD7B6), 3, 6, 0.9f, true);

        DetermineVisibility(initialActor1.CurrTile);
    }

    /// <summary>
    /// Default constructor, used when loading a file.
    /// </summary>
    public World()
    {

    }

    private void SetupWorld(int width, int height, int depth)
    {
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
        roomGraph = new Path_RoomGraph(this);
        tileGraph = null;

        StructureManager = new StructureManager();
        StructureManager.Created += OnStructureCreated;
        structureJobPrototypes = new Dictionary<string, Job>();

        //Debug.Log("World created with " + (Width * Height) + " tiles.");

        PlantManager = new PlantManager();
        InventoryManager = new InventoryManager();
        ActorManager = new ActorManager();
        JobManager = new JobManager();
        LightManager = new LightManager();
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

    void LoadStructureLua()
    {
        //string filePath = Path.Combine(Application.streamingAssetsPath, "Scripts");
        //filePath = Path.Combine(filePath, "StructureActions.lua");

        // Instantiate the singleton
        //new StructureActions(filePath);
    }

    public void SetupPathfindingExample()
    {
        //Debug.Log("SetupPathfindingExample");

        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                Tile tile = tiles[x, y, 0];
                tile.SetTileType(TileTypeData.GetByFlagName("Floor"), false);
                if (tile.Plant != null)
                {
                    tile.Plant.Deconstruct();
                }

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        StructureManager.PlaceStructure("struct_StoneWall", tile, false);
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

        RandomTreePlacement();
    }

    public void DebugReplantTrees()
    {
        foreach (Tile tile in tiles)
        {
            if (tile.Plant != null)
            {
                tile.Plant.Deconstruct();
            }
        }

        RandomTreePlacement();
    }

    public void RandomTreePlacement()
    {
        const int kTreeAttempts = 2;

        for (int i = 0; i < kTreeAttempts; i++)
        {
            PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(Width, Height, 3.5f);
            foreach (Vector2 sample in poissonDiscSampler.Samples())
            {
                int x = Mathf.FloorToInt(sample.x);
                int y = Mathf.FloorToInt(sample.y);
                Plant plant = PlantManager.PlacePlant("plant_Dummy", GetTileAt(x, y, 0));
                if (plant != null)
                {
                    plant.SetRandomGrowthPercent(0.1f, 0.7f);
                }
                //DebugUtils.Log(string.Format("RandomTreePlacement {0}, {1}", x, y));
            }
        }
    }

    public Tile GetTileAt(int x, int y, int z)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0 || z >= Depth || z < 0)
        {
            //DebugUtils.LogError("World::GetTileAt Tile (" + x + ", " + y + ", " + z + ") is out of range.");
            return null;
        }

        return tiles[x, y, z];
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

    /// <summary>
    /// Reserves the structure's work spot, preventing it from being built on. Will not reserve a workspot inside of the structure.
    /// </summary>
    /// <param name="furniture">The structure whose workspot will be reserved.</param>
    /// <param name="tile">The tile on which the structure is located, for structures which don't have a tile, such as prototypes.</param>
    public void ReserveTileAsWorkSpot(Structure structure, Tile tile = null)
    {
        if (tile == null)
        {
            tile = structure.Tile;
        }

        // if it's an internal workspot bail before reserving.
        if (structure.Jobs.WorkSpotIsInternal())
        {
            return;
        }

        GetTileAt(
            tile.X + (int)structure.Jobs.WorkSpotOffset.x,
            tile.Y + (int)structure.Jobs.WorkSpotOffset.y,
            tile.Z)
            .ReservedAsWorkSpotBy.Add(structure);
    }

    /// <summary>
    /// Unreserves the structure's work spot, allowing it to be built on.
    /// </summary>
    /// <param name="structure">The structure whose workspot will be unreserved.</param>
    /// <param name="tile">The tile on which the structure is located, for structures which don't have a tile, such as prototypes.</param>
    public void UnreserveTileAsWorkSpot(Structure structure, Tile tile = null)
    {
        if (tile == null)
        {
            tile = structure.Tile;
        }

        World.Current.GetTileAt(
            tile.X + (int)structure.Jobs.WorkSpotOffset.x,
            tile.Y + (int)structure.Jobs.WorkSpotOffset.y,
            tile.Z)
            .ReservedAsWorkSpotBy.Remove(structure);
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
        foreach (Structure structure in StructureManager)
        {
            writer.WriteStartElement("Structure");
            structure.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Actors");
        foreach (Actor actor in ActorManager)
        {
            writer.WriteStartElement("Actor");
            actor.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        DebugUtils.LogChannel("World", "ReadXML");
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

        inv = new Inventory("inv_RawStone", 4, 50);
        t = GetTileAt(Width / 2 + 2, Height / 2, 0);
        InventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }

        inv = new Inventory("inv_RawStone", 3, 50);
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

                Structure structure = StructureManager.PlaceStructure(reader.GetAttribute("Type"), tiles[x, y, z], false);
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

                string race = reader.GetAttribute("Race");
                string name = reader.GetAttribute("Name");
                bool isFemale = bool.Parse(reader.GetAttribute("IsFemale"));
                string spriteName = reader.GetAttribute("SpriteName");

                Actor actor = ActorManager.Create(tiles[x, y, z], race, name, isFemale, spriteName);
                actor.ReadXml(reader);

            } while (reader.ReadToNextSibling("Actor"));
        }
    }

    #endregion
}
