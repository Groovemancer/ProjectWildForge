using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World
{
    Tile[,] tiles;

    Dictionary<string, Building> buildingPrototypes;

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    Action<Building> cbBuildingCreated;
    Action<Tile> cbTileObjectChanged;

    // TODO: Most likely this will be replaced with a dedicated
    // class for managing job queues (plural!) that might also
    // be semi-static or self initializing or some damn thing.
    // For now, this is just a PUBLIC member of world
    public Queue<Job> jobQueue;

    public World(int width = 100, int height = 100)
    {
        jobQueue = new Queue<Job>();

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

        CreateBuildingPrototypes();
    }

    private void CreateBuildingPrototypes()
    {
        buildingPrototypes = new Dictionary<string, Building>();

        buildingPrototypes.Add("Wall",
            Building.CreatePrototype(
                "Wall",
                0,      // Impassable
                1,      // Width
                1,      // Height
                true,    // Links to neighbors and "sort of" becomes part of a large object
                TileType.Dirt | TileType.Floor | TileType.Grass | TileType.RoughStone
            )
        );

        Debug.Log("CreateBuildingPrototypes:");
        foreach (KeyValuePair<string, Building> kvpair in buildingPrototypes)
        {
            Debug.Log("\tKey: " + kvpair.Key);
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

    public void PlaceBuilding(string buildingType, Tile t)
    {
        //TODO: This function assumes 1x1 tiles -- change this later!

        if (buildingPrototypes.ContainsKey(buildingType) == false)
        {
            Debug.LogError("buildingPrototypes doesn't contain a proto for key: " + buildingType);
            return;
        }

        Building obj = Building.PlaceInstance(buildingPrototypes[buildingType], t);

        if (obj == null)
        {
            // Failed to place object -- most likely there was already something there.
            return;
        }

        if (cbBuildingCreated != null)
        {
            cbBuildingCreated(obj);
        }
    }

    public void RegisterBuildingCreated(Action<Building> callbackfunc)
    {
        cbBuildingCreated += callbackfunc;
    }

    public void UnregisterBuildingCreated(Action<Building> callbackfunc)
    {
        cbBuildingCreated -= callbackfunc;
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

    public bool IsBuildingPlacementValid(string buildingType, Tile t)
    {
        Debug.Log("Building Type: " + buildingType);
        Debug.Log("Tile: " + t);
        return buildingPrototypes[buildingType].IsValidPosition(t);
    }
}
