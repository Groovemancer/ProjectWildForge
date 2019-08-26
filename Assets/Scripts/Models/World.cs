using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class World : IXmlSerializable
{
    public Tile[,] Tiles { get; protected set; }

    public Tile[,] GetTiles()
    {
        return Tiles;
    }

    public List<Actor>      actors;
    public List<Structure>  structures;
    public List<Room>       rooms;
    public InventoryManager inventoryManager;

    // The pathfinding graph used to navigate our world map.
    public PathTileGraph tileGraph;

    Dictionary<string, Structure> structurePrototypes;
    public Dictionary<string, Job> structureJobPrototypes;

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    Action<Structure> cbStructureCreated;
    Action<Tile> cbTileObjectChanged;
    Action<Actor> cbActorCreated;
    Action<Inventory> cbInventoryCreated;

    const float PAUSE_GAME_SPEED        = 0.0f;
    const float NORMAL_GAME_SPEED       = 1.0f;
    const float FAST_GAME_SPEED         = 2.0f;
    const float VERY_FAST_GAME_SPEED    = 3.0f;

    float gameSpeed = 1.0f;
    float autsPerSec = 100f;

    // TODO: Most likely this will be replaced with a dedicated
    // class for managing job queues (plural!) that might also
    // be semi-static or self initializing or some damn thing.
    // For now, this is just a PUBLIC member of world
    public JobQueue jobQueue;

    public World(int width, int height)
    {
        // Creates an empty world.
        SetupWorld(width, height);

        // Make one actor
        CreateActor(GetTileAt(Width / 2, Height / 2));
    }

    /// <summary>
    /// Default constructor, used when loading a file.
    /// </summary>
    public World()
    {

    }

    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public void AddRoom(Room r)
    {
        rooms.Add(r);
    }

    public void DeleteRoom(Room r)
    {
        if (r == GetOutsideRoom())
        {
            Debug.LogError("Tried to delete the outside room.");
            return;
        }

        // Remove this room from our rooms list.
        rooms.Remove(r);

        // All tiles that belonged to this room should be re-assigned to
        // the outside.
        r.ReturnTilesToOutsideRoom();
    }

    void SetupWorld(int width, int height)
    {
        jobQueue = new JobQueue();
        Width = width;
        Height = height;

        Tiles = new Tile[Width, Height];

        rooms = new List<Room>();
        rooms.Add(new Room(this)); // Create the outside?

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tiles[x, y] = new Tile(this, x, y);
                Tiles[x, y].RegisterTileChangedCallback(OnTileChanged);
                Tiles[x, y].Room = rooms[0]; // Rooms 0 is always going to be outside, and that is our default room
            }
        }

        //Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateStructurePrototypes();

        actors              = new List<Actor>();
        structures          = new List<Structure>();
        inventoryManager    = new InventoryManager();
    }

    public void Update(float deltaTime)
    {
        float deltaAuts = autsPerSec * gameSpeed * deltaTime;

        foreach (Actor a in actors)
        {
            a.Update(deltaAuts);
        }

        foreach (Structure s in structures)
        {
            s.Update(deltaAuts);
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

    private void CreateStructurePrototypes()
    {
        // This will be replacd by a function that reads all of our structure data
        // from a text file in the future

        structurePrototypes = new Dictionary<string, Structure>();
        structureJobPrototypes = new Dictionary<string, Job>();

        structurePrototypes.Add("Wall",
            new Structure(
                "Wall",
                0,      // Impassable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                true    // Enclose rooms
            )
        );

        structureJobPrototypes.Add("Wall",
            new Job(null, "Wall", StructureActions.JobComplete_StructureBuilding,
            300,
            new Inventory[] {
                new Inventory("RawStone", 5, 0)
                }
            )
        );

        structurePrototypes.Add("Door",
            new Structure(
                "Door",
                1,      // Door Pathfinding Cost
                1,      // Width
                1,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                true    // Enclose rooms
            )
        );

        structurePrototypes["Door"].SetParameter("openness", 0f); // 0 = closed door, 1 = fully open door, in between is partially opened
        structurePrototypes["Door"].SetParameter("isOpening", 0);
        structurePrototypes["Door"].SetParameter("doorOpenTime", 25f); // Amount of AUTs to open door
        structurePrototypes["Door"].RegisterUpdateAction(StructureActions.Door_UpdateAction);
        structurePrototypes["Door"].IsEnterable = StructureActions.Door_IsEnterable;

        structurePrototypes.Add("Stockpile",
            new Structure(
                "Stockpile",
                1,      // Not Impassable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                false    // Enclose rooms
            )
        );

        structurePrototypes["Stockpile"].RegisterUpdateAction(StructureActions.Stockpile_UpdateAction);
        structurePrototypes["Stockpile"].tint = new Color32(255, 0, 255, 255);
        structureJobPrototypes.Add("Stockpile",
            new Job(
                null,
                "Stockpile",
                StructureActions.JobComplete_StructureBuilding,
                -1,
                null
            )
        );


        structurePrototypes.Add("WorkStation",
            new Structure(
                "WorkStation",
                1,      // Pathfinding Cost
                3,      // Width
                3,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                false    // Enclose rooms
            )
        );
        structurePrototypes["WorkStation"].jobSpotOffset = new Vector2(1, 0);
        structurePrototypes["WorkStation"].RegisterUpdateAction(StructureActions.WorkStation_UpdateAction);

        structurePrototypes.Add("OxygenGenerator",
            new Structure(
                "OxygenGenerator",
                10,      // Pathfinding Cost
                2,      // Width
                2,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileTypeData.Flag("Dirt") | TileTypeData.Flag("Floor") | TileTypeData.Flag("Grass") |
                    TileTypeData.Flag("RoughStone") | TileTypeData.Flag("Road"),
                false    // Enclose rooms
            )
        );
        structurePrototypes["OxygenGenerator"].RegisterUpdateAction(StructureActions.OxygenGenerator_UpdateAction);
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
                Tiles[x, y].Type = TileTypeData.GetByFlagName("Floor");

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceStructure("Wall", Tiles[x, y], false);
                    }
                }
            }
        }
    }

    public void RandomizeTiles()
    {
        Debug.Log("World::RandomizeTiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    Tiles[x, y].Type = TileTypeData.GetByFlagName("Dirt");
                }
                else
                {
                    Tiles[x, y].Type = TileTypeData.GetByFlagName("Grass");
                }
            }
        }
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0)
        {
            //Debug.LogError("World::GetTileAt Tile (" + x + ", " + y + ") is out of range.");
            return null;
        }

        return Tiles[x, y];
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
            // TODO: Not sure if I'll be using rooms just yet.
            Room.DoRoomFloodFill(structure.Tile);
        }

        if (cbStructureCreated != null)
        {
            cbStructureCreated(structure);
        }

        InvalidateTileGraph(); // Reset the pathfinding system

        return structure;
    }

    

    // This should be called whenever a change to the world
    // means that our old pathfinding info is invalid
    public void InvalidateTileGraph()
    {
        tileGraph = null;
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
    public void OnTileChanged(Tile t)
    {
        if (cbTileObjectChanged == null)
            return;

        cbTileObjectChanged(t);

        InvalidateTileGraph();
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
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Tiles[x, y].Type != TileTypeData.Instance.EmptyType)
                {
                    writer.WriteStartElement("Tile");
                    Tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
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

        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));

        SetupWorld(Width, Height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
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
        Inventory inv = new Inventory("RawStone", 50, 50);
        Tile t = GetTileAt(Width / 2, Height / 2);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }

        inv = new Inventory("RawStone", 50, 4);
        t = GetTileAt(Width / 2 + 2, Height / 2);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }

        inv = new Inventory("RawStone", 50, 3);
        t = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.Inventory);
        }
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
                Tiles[x, y].ReadXml(reader);
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

                Structure structure = PlaceStructure(reader.GetAttribute("objectType"), Tiles[x, y], false);
                structure.ReadXml(reader);
            } while (reader.ReadToNextSibling("Structure"));

            foreach (Structure strct in structures)
            {
                Room.DoRoomFloodFill(strct.Tile, true);
            }
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

                Actor actor = CreateActor(Tiles[x, y]);
                actor.ReadXml(reader);
            } while (reader.ReadToNextSibling("Actor"));
        }
    }

    #endregion
}
