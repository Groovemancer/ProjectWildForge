using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum TileType { Empty = 0, Dirt = 1, RoughStone = 2, Marsh = 4, ShallowWater = 8, Grass = 16, Floor = 32, All = 64 };

[Serializable]
public class Tile
{
    TileType _type = TileType.Empty;
    public TileType Type
    {
        get { return _type; }
        set
        {
            TileType oldType = _type;
            _type = value;

            // Call the callback and let things know we've changed.
            if (cbTileTypeChanged != null && _type != oldType)
                cbTileTypeChanged(this);
        }
    }

    // The function we callback any time our type changes
    Action<Tile> cbTileTypeChanged;

    LooseObject looseObject;

    public InstalledObject InstalledObject { get; protected set; }

    public World World { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }

    public Tile(World world, int x, int y)
    {
        this.World = world;
        this.X = x;
        this.Y = y;
    }

    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileTypeChanged += callback;
    }

    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileTypeChanged -= callback;
    }

    public bool PlaceInstalledObject(InstalledObject objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here before.
            InstalledObject = null;
            return true;
        }

        // objInstance isn't null
        if (InstalledObject != null)
        {
            Debug.LogError("Trying to assign an installed object to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!
        InstalledObject = objInstance;
        return true;
    }
}

[Serializable]
public struct TileSprite
{
    [SerializeField]
    public TileType Type;

    [SerializeField]
    public Sprite Sprite;
}