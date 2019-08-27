using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;

//[Flags]
//public enum TileType {
//    Empty = 0, Dirt = 1 << 0, RoughStone = 1 << 2, Marsh = 1 << 3, ShallowWater = 1 << 4,
//    Grass = 1 << 5, Floor = 1 << 6, Road = 1 << 7, All = 1 << 8
//};

public enum Enterability { Yes, Never, Soon };

[Serializable]
public class Tile : IXmlSerializable
{
    TileType _type = TileTypeData.GetByFlagName("Dirt");
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

    public Inventory Inventory;

    public Room Room;

    public Structure Structure { get; protected set; }
    public Job PendingStructureJob;

    public int X { get; protected set; }
    public int Y { get; protected set; }

    public Tile(int x, int y)
    {
        this.X = x;
        this.Y = y;
        this.Type = TileTypeData.Instance.DefaultType;
    }

    public void RegisterTileChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }

    public void UnregisterTileChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public float CalculatedMoveCost()
    {
        float movementCost = 1f;

        if (Type != null)
        {
            movementCost = Type.MoveCost;
        }

        if (Structure != null)
            return movementCost * Structure.MovementCost;
        else
            return movementCost;
    }

    public bool UnplaceStructure()
    {
        // Just uninstalling. FIXME: What if we have a multi-tile structure?
        if (Structure == null)
            return false;

        Structure s = Structure;

        for (int x_off = X; x_off < (X + s.Width); x_off++)
        {
            for (int y_off = Y; y_off < (Y + s.Height); y_off++)
            {
                Tile t = World.current.GetTileAt(x_off, y_off);

                t.Structure = null;
            }
        }

        return true;
    }

    public bool PlaceStructure(Structure objInstance)
    {
        if (objInstance == null)
        {
            return UnplaceStructure();
        }

        if (objInstance.IsValidPosition(this) == false)
        {
            Debug.LogError("Trying to assign a structure to a tile that isn't valid!");
            return false;
        }

        for (int x_off = X; x_off < (X + objInstance.Width); x_off++)
        {
            for (int y_off = Y; y_off < (Y + objInstance.Height); y_off++)
            {
                Tile t = World.current.GetTileAt(x_off, y_off);

                t.Structure = objInstance;
            }
        }

        return true;
    }

    public bool PlaceInventory(Inventory inv)
    {
        if (inv == null)
        {
            Inventory = null;
            return true;
        }

        if (Inventory != null)
        {
            // There's already inventory here. Maybe we can combine a stack?

            if (Inventory.objectType != inv.objectType)
            {
                Debug.LogError("Trying to assign inventory to a tile that already has some of a different type!");
                return false;
            }

            int numToMove = inv.stackSize;
            if (Inventory.stackSize + numToMove > Inventory.maxStackSize)
            {
                numToMove = Inventory.maxStackSize - Inventory.stackSize;
            }

            Inventory.stackSize += numToMove;
            inv.stackSize -= numToMove;

            return true;
        }

        // At this point, we know that our current inventory is actually
        // null. Now we can't just do a direct assignment, because
        // the inventory manager needs to know that the old stack is now
        // empty and has to be removed from the previous lists.
        Inventory = inv.Clone();
        Inventory.tile = this;
        inv.stackSize = 0;
        return true;
    }

    public Enterability IsEnterable()
    {
        // Returns true if you can enter this tile right this moment.
        if (CalculatedMoveCost() == 0)
            return Enterability.Never;

        // Check out structure to see if it has a special block on enterability
        if (Structure != null && Structure.IsEnterable != null)
        {
            return Structure.IsEnterable(Structure);
        }

        return Enterability.Yes;
    }

    public bool IsNeighbor(Tile tile, bool diagOkay = false)
    {
        return (Mathf.Abs(tile.X - this.X) + Mathf.Abs(tile.Y - this.Y) == 1 || // Check hori/vert adjacency
            (diagOkay && (Mathf.Abs(tile.X - this.X) == 1 && Mathf.Abs(tile.Y - this.Y) == 1))); // Check diag adjacency
    }

    public Tile[] GetNeighbors(bool diagOkay = false, bool nullOkay = false)
    {
        Tile[] tiles = new Tile[8];

        tiles[0] = World.current.GetTileAt(X, Y + 1);
        tiles[1] = World.current.GetTileAt(X + 1, Y);
        tiles[2] = World.current.GetTileAt(X, Y - 1);
        tiles[3] = World.current.GetTileAt(X - 1, Y);

        if (diagOkay == true)
        {
            tiles[4] = World.current.GetTileAt(X + 1, Y + 1);
            tiles[5] = World.current.GetTileAt(X + 1, Y - 1);
            tiles[6] = World.current.GetTileAt(X - 1, Y - 1);
            tiles[7] = World.current.GetTileAt(X - 1, Y + 1);
        }

        if (!nullOkay)
        {
            return tiles.Where(tile => tile != null).ToArray();
        }
        else
        {
            return tiles;
        }
    }

    public Tile North()
    {
        return World.current.GetTileAt(X, Y + 1);
    }

    public Tile South()
    {
        return World.current.GetTileAt(X, Y - 1);
    }

    public Tile East()
    {
        return World.current.GetTileAt(X + 1, Y);
    }

    public Tile West()
    {
        return World.current.GetTileAt(X - 1, Y);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                     SAVING & LOADING
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public Tile()
    {
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("RoomID", Room == null ? "-1" : Room.Id.ToString());
        writer.WriteAttributeString("Type", Type.FlagName);
    }

    public void ReadXml(XmlReader reader)
    {
        // X and Y have already been read/processed

        Room = World.current.GetRoomFromId(int.Parse(reader.GetAttribute("RoomID")));

        Type = TileTypeData.GetByFlagName(reader.GetAttribute("Type"));
    }
}

[Serializable]
public struct TileSprite
{
    [SerializeField]
    public string FlagName;

    [SerializeField]
    public Sprite Sprite;
}