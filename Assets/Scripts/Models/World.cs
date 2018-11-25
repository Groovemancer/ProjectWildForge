using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World
{
    Tile[,] tiles;

    Dictionary<string, InstalledObject> installedObjectPrototypes;

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    Action<InstalledObject> cbInstalledObjectCreated;
    Action<Tile> cbTileObjectChanged;

    public World(int width = 100, int height = 100)
    {
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

        CreateInstalledObjectPrototypes();
    }

    private void CreateInstalledObjectPrototypes()
    {
        installedObjectPrototypes = new Dictionary<string, InstalledObject>();

        installedObjectPrototypes.Add("Wall",
            InstalledObject.CreatePrototype(
                                    "Wall",
                                    0,      // Impassable
                                    1,      // Width
                                    1,      // Height
                                    true,    // Links to neighbors and "sort of" becomes part of a large object
                                    TileType.Dirt | TileType.Floor | TileType.Grass | TileType.RoughStone
                                )
        );

        Debug.Log("CreateInstalledObjectPrototypes:");
        foreach (KeyValuePair<string, InstalledObject> kvpair in installedObjectPrototypes)
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

    public void PlaceInstalledObject(string objectType, Tile t)
    {
        //TODO: This function assumes 1x1 tiles -- change this later!

        if (installedObjectPrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("installedObjectPrototypes doesn't contain a proto for key: " + objectType);
            return;
        }

        InstalledObject obj = InstalledObject.PlaceInstance(installedObjectPrototypes[objectType], t);

        if (obj == null)
        {
            // Failed to place object -- most likely there was already something there.
            return;
        }

        if (cbInstalledObjectCreated != null)
        {
            cbInstalledObjectCreated(obj);
        }
    }

    public void RegisterInstalledObjectCreated(Action<InstalledObject> callbackfunc)
    {
        cbInstalledObjectCreated += callbackfunc;
    }

    public void UnregisterInstalledObjectCreated(Action<InstalledObject> callbackfunc)
    {
        cbInstalledObjectCreated -= callbackfunc;
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
}
