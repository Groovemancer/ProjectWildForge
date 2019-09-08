using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Linq;
using MoonSharp.Interpreter;
using ProjectWildForge.Pathfinding;
using UnityEngine;

//[Flags]
//public enum TileType {
//    Empty = 0, Dirt = 1 << 0, RoughStone = 1 << 2, Marsh = 1 << 3, ShallowWater = 1 << 4,
//    Grass = 1 << 5, Floor = 1 << 6, Road = 1 << 7, All = 1 << 8
//};

public enum Enterability { Yes, Never, Soon };

[MoonSharpUserData]
public class Tile : IXmlSerializable
{
    TileType _type = TileTypeData.GetByFlagName("Dirt");
    public TileType Type
    {
        get { return _type; }
    }

    // The function we callback any time our tile data changes
    Action<Tile> cbTileChanged;

    public event Action<Tile> TileChanged;
    public event Action<Tile> TileTypeChanged;

    public Inventory Inventory;

    public Room Room;

    public Structure Structure { get; protected set; }
    public Job PendingStructureJob;

    /// <summary>
    /// The total pathfinding cost of entering this tile.
    /// The final cost is equal to the Tile's BaseMovementCost * Tile's PathfindingWeight * Structure's PathfindingWeight * Structure's MovementCost +
    /// Tile's PathfindingModifier + Structure's PathfindingModifier.
    /// </summary>
    public float PathfindingCost { get; protected set; }

    public int X { get; protected set; }
    public int Y { get; protected set; }
    public int Z { get; protected set; }

    public bool CanSee { get; set; }

    public Tile(int x, int y, int z = 0)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
        CanSee = false;
        _type = TileTypeData.Instance.DefaultType;

        UpdatePathfindingCost();
    }

    public Vector3 Vector3
    {
        get { return new Vector3(X, Y, Z); }
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
                Tile t = World.Current.GetTileAt(x_off, y_off, Z);
                t.Structure = null;
                UpdatePathfindingCost();

                SetCanSee();
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
                Tile t = World.Current.GetTileAt(x_off, y_off, Z);
                t.Structure = objInstance;
                UpdatePathfindingCost();
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

            if (Inventory.Type != inv.Type)
            {
                Debug.LogError("Trying to assign inventory to a tile that already has some of a different type!");
                return false;
            }

            int numToMove = inv.StackSize;
            if (Inventory.StackSize + numToMove > Inventory.MaxStackSize)
            {
                numToMove = Inventory.MaxStackSize - Inventory.StackSize;
            }

            Inventory.StackSize += numToMove;
            inv.StackSize -= numToMove;

            return true;
        }

        // At this point, we know that our current inventory is actually
        // null. Now we can't just do a direct assignment, because
        // the inventory manager needs to know that the old stack is now
        // empty and has to be removed from the previous lists.
        Inventory = inv.Clone();
        Inventory.Tile = this;
        inv.StackSize = 0;
        return true;
    }

    public Enterability IsEnterable()
    {
        // Returns true if you can enter this tile right this moment.
        if (PathfindingCost == 0)
            return Enterability.Never;

        // Check out structure to see if it has a special block on enterability
        if (Structure != null)
        {
            return Structure.IsEnterable();
        }

        return Enterability.Yes;
    }

    public void SetTileType(TileType newTileType, bool doRoomFloodFill = true)
    {
        if (_type == newTileType)
            return;

        _type = newTileType;
        //ForceTileUpdate = true;

        bool splitting = true;
        if (newTileType == TileTypeData.Instance.EmptyType)
        {
            splitting = false;
        }

        if (doRoomFloodFill)
        {
            World.Current.RoomManager.DoRoomFloodFill(this, splitting, true);
        }

        if (TileTypeChanged != null)
        {
            TileTypeChanged(this);
        }

        UpdatePathfindingCost();
    }

    public void UpdatePathfindingCost()
    {
        float newCost;

        // If Tile's BaseMovementCost, PathFindingWeight or Structure's MovementCost, PathFindingWeight = 0 (i.e. impassable)
        // we should always return 0 (stay impassable)
        if (Type.MoveCost.AreEqual(0) || Structure != null && Structure.MovementCost.AreEqual(0))
        {
            newCost = 0f;
        }

        if (Structure != null)
        {
            newCost = Type.MoveCost * Structure.MovementCost;
        }
        else
        {
            newCost = Type.MoveCost;
        }

        PathfindingCost = newCost;

        // TODO Add Extra pathfinding stuff
        /*
        if (Type.BaseMovementCost.AreEqual(0) || Type.PathfindingWeight.AreEqual(0) || (Furniture != null && (Furniture.MovementCost.AreEqual(0) || Furniture.PathfindingWeight.AreEqual(0))))
        {
            newCost = 0f;
        }

        if (Furniture != null)
        {
            newCost = (Furniture.PathfindingWeight * Furniture.MovementCost * Type.PathfindingWeight * Type.BaseMovementCost) +
            Furniture.PathfindingModifier + Type.PathfindingModifier;
        }
        else
        {
            newCost = (Type.PathfindingWeight * Type.BaseMovementCost) + Type.PathfindingModifier;
        }
        */
    }

    private void SetCanSee()
    {
        CanSee = true;
        ReportTileChanged();

        if (IsEnterable() == Enterability.Never)
        {
            return;
        }

        foreach (Tile neighbor in GetNeighbors(false, true, false))
        {
            if (neighbor.CanSee == false)
            {
                neighbor.SetCanSee();
            }
        }
    }

    private void ReportTileChanged()
    {
        // Call the callback and let things know we've changed.
        if (TileChanged != null)
        {
            TileChanged(this);
        }

        //ForceTileUpdate = false;
    }

    public bool IsNeighbor(Tile tile, bool diagOkay = false)
    {
        return (Mathf.Abs(tile.X - this.X) + Mathf.Abs(tile.Y - this.Y) == 1 || // Check hori/vert adjacency
            (diagOkay && (Mathf.Abs(tile.X - this.X) == 1 && Mathf.Abs(tile.Y - this.Y) == 1))); // Check diag adjacency
    }

    public Tile[] GetNeighbors(bool diagOkay = false, bool vertOkay = false, bool nullOkay = false)
    {
        Tile[] tiles = new Tile[10];

        tiles[0] = World.Current.GetTileAt(X, Y + 1, Z);
        tiles[1] = World.Current.GetTileAt(X + 1, Y, Z);
        tiles[2] = World.Current.GetTileAt(X, Y - 1, Z);
        tiles[3] = World.Current.GetTileAt(X - 1, Y, Z);

        if (diagOkay == true)
        {
            tiles[4] = World.Current.GetTileAt(X + 1, Y + 1, Z);
            tiles[5] = World.Current.GetTileAt(X + 1, Y - 1, Z);
            tiles[6] = World.Current.GetTileAt(X - 1, Y - 1, Z);
            tiles[7] = World.Current.GetTileAt(X - 1, Y + 1, Z);
        }

        if (vertOkay)
        {
            Tile[] vertTiles = GetVerticalNeighbors(true);
            tiles[8] = vertTiles[0];
            tiles[9] = vertTiles[1];
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

    public Tile[] GetVerticalNeighbors(bool nullOkay = false)
    {
        Tile[] tiles = new Tile[2];
        Tile tileup = World.Current.GetTileAt(X, Y, Z - 1);
        if (tileup != null && tileup.Type == TileTypeData.Instance.EmptyType)
        {
            tiles[0] = World.Current.GetTileAt(X, Y, Z - 1);
        }

        if (Type == TileTypeData.Instance.EmptyType)
        {
            tiles[1] = World.Current.GetTileAt(X, Y, Z + 1);
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

    public bool HasClearLineToBottom()
    {
        if (_type != TileTypeData.Instance.EmptyType)
        {
            return false;
        }

        if (Down() == null)
        {
            return true;
        }
        else
        {
            return Down().HasClearLineToBottom();
        }
    }

    public Tile North()
    {
        return World.Current.GetTileAt(X, Y + 1, Z);
    }

    public Tile South()
    {
        return World.Current.GetTileAt(X, Y - 1, Z);
    }

    public Tile East()
    {
        return World.Current.GetTileAt(X + 1, Y, Z);
    }

    public Tile West()
    {
        return World.Current.GetTileAt(X - 1, Y, Z);
    }

    public Tile Up()
    {
        return World.Current.GetTileAt(X, Y, Z - 1);
    }

    public Tile Down()
    {
        return World.Current.GetTileAt(X, Y, Z + 1);
    }

    public Room GetNearestRoom()
    {
        if (Room != null)
        {
            return this.Room;
        }

        foreach (Tile neighbor in GetNeighbors(true, true))
        {
            if (neighbor.Room != null)
            {
                return neighbor.Room;
            }
        }

        return Pathfinder.FindNearestRoom(this);
    }

    public bool IsClippingCorner(Tile neighborTile)
    {
        // If the movement from curr to neigh is diagonal (e.g. N-E)
        // Then check to make sure we aren't clipping (e.g. N and E are both walkable)
        int dX = this.X - neighborTile.X;
        int dY = this.Y - neighborTile.Y;

        if (Mathf.Abs(dX) + Mathf.Abs(dY) == 2)
        {
            // We are diagonal
            if (World.Current.GetTileAt(X - dX, Y, Z).PathfindingCost.AreEqual(0f))
            {
                // East or West is unwalkable, therefore this would be a clipped movement.
                return true;
            }

            if (World.Current.GetTileAt(X, Y - dY, Z).PathfindingCost.AreEqual(0f))
            {
                // North or South is unwalkable, therefore this would be a clipped movement.
                return true;
            }

            // If we reach here, we are diagonal, but not clipping
        }

        // If we are here, we are either not clipping, or not diagonal
        return false;
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
        writer.WriteAttributeString("Z", Z.ToString());
        writer.WriteAttributeString("RoomID", Room == null ? "-1" : Room.Id.ToString());
        writer.WriteAttributeString("Type", Type.FlagName);
        writer.WriteAttributeString("CanSee", CanSee.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        Room = World.Current.RoomManager[int.Parse(reader.GetAttribute("RoomID"))];
        if (Room != null)
        {
            Room.AssignTile(this);
        }

        SetTileType(TileTypeData.GetByFlagName(reader.GetAttribute("Type")), false);

        CanSee = bool.Parse(reader.GetAttribute("CanSee"));
        
        ReportTileChanged();
    }
}

public struct TileSprite
{
    public string FlagName;

    public Sprite Sprite;
}