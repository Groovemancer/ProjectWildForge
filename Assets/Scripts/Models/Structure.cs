using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Structures are things like walls, doors, and furniture (e.g. table)

public class Structure
{
    // This represents the BASE tile of the object -- but in practices, large objects may actually occupy
    // multiple tiles.
    public Tile Tile { get; protected set; }

    // This "objectType" will be queried by the visual system to know what sprite to render for this object
    public string ObjectType { get; protected set; }

    public TileType AllowedTileTypes { get; protected set; }

    // This is a multiplier. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
    // Tile types and other environmental effects may be combined.
    // For example, a "rough tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    // would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
    // SPECIAL: If movementCost = 0, then this tile is impassable. (e.g. a wall).
    public float MovementCost { get; protected set; }

    // For example, a sofa might be a 3x2 (actual graphics only appear to cover the 3x1 area, but the extra row is for leg room
    int width;
    int height;

    public bool LinksToNeighbor { get; protected set; }

    Action<Structure> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

    // TODO: Implement larger objects
    // TODO: Implement object rotation

    protected Structure() { }
    
    public static Structure CreatePrototype(string objectType, float movementCost = 1f,
        int width = 1, int height = 1, bool linksToNeighbor = false, TileType allowedTileTypes = TileType.All)
    {
        Structure obj = new Structure();

        obj.ObjectType = objectType;
        obj.MovementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.LinksToNeighbor = linksToNeighbor;
        obj.AllowedTileTypes = allowedTileTypes;

        obj.funcPositionValidation = obj.__IsValidPosition;

        return obj;
    }

    public static Structure PlaceInstance(Structure proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Position Validity Function returned False.");
            return null;
        }

        // We know our placement destination is valid.

        Structure obj = new Structure();

        obj.ObjectType = proto.ObjectType;
        obj.MovementCost = proto.MovementCost;
        obj.width = proto.width;
        obj.height = proto.height;
        obj.LinksToNeighbor = proto.LinksToNeighbor;
        obj.AllowedTileTypes = proto.AllowedTileTypes;

        obj.Tile = tile;

        // FIXME: This assumes we are 1x1!
        if (tile.PlaceStructure(obj) == false)
        {
            // For some reason,we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        if (obj.LinksToNeighbor)
        {
            // This type of furniture links itself to its neighbors,
            // so we should inform our neighbors that they have a new
            // buddy. Just trigger their OnChangedCallback.
            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = tile.World.GetTileAt(x, y + 1);
            if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = tile.World.GetTileAt(x + 1, y);
            if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = tile.World.GetTileAt(x, y - 1);
            if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }

            t = tile.World.GetTileAt(x - 1, y);
            if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
            {
                t.Structure.cbOnChanged(t.Structure);
            }
        }

        return obj;
    }

    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    // FIXME: These functions should never be called directly,
    // so they probably shouldn't be public functions
    public bool __IsValidPosition(Tile t)
    {
        // Make sure tile is of allowed types
        // Make sure tile doesn't already have structure
        if ((AllowedTileTypes & t.Type) != t.Type && t.Type != TileType.All)
        {
            return false;
        }

        return true;
    }

    public bool __IsValidPosition_Door(Tile t)
    {
        // Make sure we have a pair of E/W walls or N/S walls

        return true;
    }

    public void RegisterOnChangedCallback(Action<Structure> callbackFunc)
    {
        cbOnChanged += callbackFunc;
    }

    public void UnregisterOnChangedCallback(Action<Structure> callbackFunc)
    {
        cbOnChanged -= callbackFunc;
    }
}
