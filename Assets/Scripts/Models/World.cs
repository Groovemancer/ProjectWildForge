using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class World : IXmlSerializable
{
    Tile[,] tiles;
    public List<Actor> actors;
    public List<Structure> structures;

    // The pathfinding graph used to navigate our world map.
    public PathTileGraph tileGraph;

    Dictionary<string, Structure> structurePrototypes;

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    Action<Structure> cbStructureCreated;
    Action<Tile> cbTileObjectChanged;
    Action<Actor> cbActorCreated;

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
        Actor a = CreateActor(GetTileAt(Width / 2, Height / 2));
    }

    void SetupWorld(int width, int height)
    {
        jobQueue = new JobQueue();
        Width = width;
        Height = height;

        tiles = new Tile[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileChangedCallback(OnTileChanged);
            }
        }

        //Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateStructurePrototypes();

        actors = new List<Actor>();
        structures = new List<Structure>();
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

        structurePrototypes.Add("Wall",
            new Structure(
                "Wall",
                0,      // Impassable
                1,      // Width
                1,      // Height
                true,   // Links to neighbors and "sort of" becomes part of a large object
                TileType.Dirt | TileType.Floor | TileType.Grass | TileType.RoughStone | TileType.Road
            )
        );

        structurePrototypes.Add("Door",
            new Structure(
                "Door",
                1,      // Door Pathfinding Cost
                1,      // Width
                1,      // Height
                false,   // Links to neighbors and "sort of" becomes part of a large object
                TileType.Dirt | TileType.Floor | TileType.Grass | TileType.RoughStone | TileType.Road
            )
        );

        structurePrototypes["Door"].structureParameters["openness"] = 0f; // 0 = closed door, 1 = fully open door, in between is partially opened
        structurePrototypes["Door"].structureParameters["isOpening"] = 0;
        structurePrototypes["Door"].structureParameters["doorOpenTime"] = 15f; // Amount of AUTs to open door
        structurePrototypes["Door"].updateActions += StructureActions.Door_UpdateAction;
        structurePrototypes["Door"].IsEnterable = StructureActions.Door_IsEnterable;
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
                tiles[x, y].Type = TileType.Floor;

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceStructure("Wall", tiles[x, y]);
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
                    tiles[x, y].Type = TileType.Dirt;
                }
                else
                {
                    tiles[x, y].Type = TileType.Grass;
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

        return tiles[x, y];
    }

    public Structure PlaceStructure(string structureType, Tile t)
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

        structures.Add(structure);

        if (cbStructureCreated != null)
        {
            cbStructureCreated(structure);
        }

        InvalidateTileGraph();

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

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                     SAVING & LOADING
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public World()
    {
    }

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
                if (tiles[x, y].Type != TileType.Empty)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
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
                tiles[x, y].ReadXml(reader);
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

                Structure structure = PlaceStructure(reader.GetAttribute("objectType"), tiles[x, y]);
                structure.ReadXml(reader);
            } while (reader.ReadToNextSibling("Structure"));
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

                Actor actor = CreateActor(tiles[x, y]);
                actor.ReadXml(reader);
            } while (reader.ReadToNextSibling("Actor"));
        }
    }
}
