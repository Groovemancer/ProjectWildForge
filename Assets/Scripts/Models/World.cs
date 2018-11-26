using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World
{
    Tile[,] tiles;
    List<Actor> actors;

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

    public World(int width = 100, int height = 100)
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

        Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateStructurePrototypes();

        actors = new List<Actor>();
    }

    public void Update(float deltaTime)
    {
        float deltaAuts = autsPerSec* gameSpeed * deltaTime;

        foreach (Actor a in actors)
        {
            a.Update(deltaAuts);
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
        structurePrototypes = new Dictionary<string, Structure>();

        structurePrototypes.Add("Wall",
            Structure.CreatePrototype(
                "Wall",
                0,      // Impassable
                1,      // Width
                1,      // Height
                true,    // Links to neighbors and "sort of" becomes part of a large object
                TileType.Dirt | TileType.Floor | TileType.Grass | TileType.RoughStone
            )
        );

        Debug.Log("CreateStructurePrototypes:");
        foreach (KeyValuePair<string, Structure> kvpair in structurePrototypes)
        {
            Debug.Log("\tKey: " + kvpair.Key);
        }
    }

    public void SetupPathfindingExample()
    {
        Debug.Log("SetupPathfindingExample");

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
            Debug.LogError("World::GetTileAt Tile (" + x + ", " + y + ") is out of range.");
            return null;
        }

        return tiles[x, y];
    }

    public void PlaceStructure(string structureType, Tile t)
    {
        //TODO: This function assumes 1x1 tiles -- change this later!

        if (structurePrototypes.ContainsKey(structureType) == false)
        {
            Debug.LogError("structurePrototypes doesn't contain a proto for key: " + structureType);
            return;
        }

        Structure obj = Structure.PlaceInstance(structurePrototypes[structureType], t);

        if (obj == null)
        {
            // Failed to place object -- most likely there was already something there.
            return;
        }

        if (cbStructureCreated != null)
        {
            cbStructureCreated(obj);
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

    public void RegisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileObjectChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileObjectChanged -= callbackfunc;
    }

    public void OnTileChanged(Tile t)
    {
        if (cbTileObjectChanged == null)
            return;

        cbTileObjectChanged(t);
    }

    public bool IsStructurePlacementValid(string structureType, Tile t)
    {
        return structurePrototypes[structureType].IsValidPosition(t);
    }

    public Structure GetStructurePrototype( string objType)
    {
        if (structurePrototypes.ContainsKey(objType) == false)
        {
            Debug.LogError("No structure with type: " + objType);
            return null;
        }

        return structurePrototypes[objType];
    }
}
