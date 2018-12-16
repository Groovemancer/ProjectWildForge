using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

[Flags]
public enum TileType {
    Empty = 0, Dirt = 1 << 0, RoughStone = 1 << 2, Marsh = 1 << 3, ShallowWater = 1 << 4,
    Grass = 1 << 5, Floor = 1 << 6, Road = 1 << 7, All = 1 << 8
};

public enum Enterability { Yes, Never, Soon };

[Serializable]
public class Tile : IXmlSerializable
{
    TileType _type = TileType.Dirt;
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

    public float CalculatedMoveCost()
    {
        float movementCost = 1f;
        switch(Type)
        {
            case TileType.Empty:
                movementCost = 0f;
                break;
            case TileType.Road:
                movementCost = 0.5f;
                break;
            case TileType.Floor:
            case TileType.Dirt:
            case TileType.Grass:
                movementCost = 1f;
                break;
            case TileType.RoughStone:
                movementCost = 1.5f;
                break;
            case TileType.Marsh:
            case TileType.ShallowWater:
                movementCost = 2f;
                break;
        }

        if (Structure != null)
            return movementCost * Structure.MovementCost;
        else
            return movementCost;
    }

    public bool PlaceStructure(Structure objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here before.
            Structure = null;
            return true;
        }

        // objInstance isn't null
        if (Structure != null)
        {
            Debug.LogError("Trying to assign a structure to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!
        Structure = objInstance;
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

            if (Inventory.stackSize + inv.stackSize > inv.maxStackSize)
            {
                Debug.LogError("Trying to assign inventory to a tile that would exceed max stack size!");
                return false;
            }

            int numToMove = inv.stackSize;
            if (Inventory.stackSize + inv.stackSize > Inventory.maxStackSize)
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

    public Tile[] GetNeighbors(bool diagOkay = false)
    {
        Tile[] ns;
        if (diagOkay == false)
        {
            ns = new Tile[4]; // Tile order: N E S W
        }
        else
        {
            ns = new Tile[8]; // Tile order: N E S W NE SE SW NW
        }

        Tile n;
        n = North();
        ns[0] = n;
        n = East();
        ns[1] = n;
        n = South();
        ns[2] = n;
        n = West();
        ns[3] = n;

        if (diagOkay == true)
        {
            n = World.GetTileAt(X + 1, Y + 1);
            ns[4] = n;
            n = World.GetTileAt(X + 1, Y - 1);
            ns[5] = n;
            n = World.GetTileAt(X - 1, Y - 1);
            ns[6] = n;
            n = World.GetTileAt(X - 1, Y + 1);
            ns[7] = n;
        }

        return ns;
    }

    public Tile North()
    {
        return World.GetTileAt(X, Y + 1);
    }

    public Tile South()
    {
        return World.GetTileAt(X, Y - 1);
    }

    public Tile East()
    {
        return World.GetTileAt(X + 1, Y);
    }

    public Tile West()
    {
        return World.GetTileAt(X - 1, Y);
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
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
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