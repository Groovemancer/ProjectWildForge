using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum TileType { Empty = 0x00, Dirt = 0x01, RoughStone = 0x02, Marsh = 0x04, ShallowWater = 0x08, Grass = 0x10, Floor = 0x20, All = 0x30 };

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
            if (cbTileChanged != null && _type != oldType)
                cbTileChanged(this);
        }
    }

    // The function we callback any time our tile data changes
    Action<Tile> cbTileChanged;

    LooseObject looseObject;

    public Building Building { get; protected set; }
    public Job PendingBuildingJob;

    public World World { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }

    public Tile(World world, int x, int y)
    {
        this.World = world;
        this.X = x;
        this.Y = y;
    }

    public void RegisterTileChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }

    public void UnregisterTileChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public bool PlaceBuilding(Building objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here before.
            Building = null;
            return true;
        }

        // objInstance isn't null
        if (Building != null)
        {
            Debug.LogError("Trying to assign an building to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!
        Building = objInstance;
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