using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType { Dirt, RoughStone, Marsh, ShallowWater, Grass, Floor, Empty };

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
    InstalledObject installedObject;

    World world;
    public int X { get; protected set; }
    public int Y { get; protected set; }

    public Tile(World world, int x, int y)
    {
        this.world = world;
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
}

[Serializable]
public struct TileSprite
{
    [SerializeField]
    public TileType Type;

    [SerializeField]
    public Sprite Sprite;
}