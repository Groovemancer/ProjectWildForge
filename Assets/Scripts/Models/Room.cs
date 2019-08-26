using UnityEngine;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;

public class Room : IXmlSerializable
{
    Dictionary<string, float> atmosphericGasses;

    List<Tile> tiles;

    World world;

    public Room(World world)
    {
        this.world = world;
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t))
            return;

        if (t.Room != null)
        {
            // Belongs to some other room
            t.Room.tiles.Remove(t);
        }

        Debug.Log("AssignTile");

        t.Room = this;
        tiles.Add(t);
    }

    public void ReturnTilesToOutsideRoom()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].Room = tiles[i].World.GetOutsideRoom();    // Assign to outside
        }

        tiles = new List<Tile>();
    }

    public bool IsOutsideRoom()
    {
        return this == world.GetOutsideRoom();
    }

    public void ChangeGas(string name, float amount)
    {
        if (IsOutsideRoom())
            return;

        if (atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
        }
        else
        {
            atmosphericGasses[name] = amount;
        }

        if (atmosphericGasses[name] < 0)
            atmosphericGasses[name] = 0;
    }

    public float GetGasAmount(string name)
    {
        if (atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name];
        }
        return 0;
    }

    public float GetGasPercentage(string name)
    {
        if (atmosphericGasses.ContainsKey(name) == false)
            return 0;

        float t = 0;
        foreach (string n in atmosphericGasses.Keys)
        {
            t += atmosphericGasses[n];
        }

        if (t > 0)
            return atmosphericGasses[name] / t;
        else
            return 0;
    }

    public string[] GetGasNames()
    {
        return atmosphericGasses.Keys.ToArray();
    }

    public static void DoRoomFloodFill(Tile sourceTile, bool onlyIfOutside = false)
    {
        // TODO REMOVE when we figure out some optimizations
        return;

        // sourceStruct is the structure that may be splitting
        // two existing rooms, or may be the final enclosing piece
        // to form a new room.
        // Check the NESW neighbors of the structure's tile
        // and do flood fill from them

        World world = sourceTile.World;

        Room oldRoom = sourceTile.Room;

        
        if (oldRoom != null)
        {
            // The source tile had a room, so this must be a new structure
            // that is potentiallly dividing this old room into as many four new rooms.

            // Try building a new rooms for each of our NESW directions
            foreach (Tile t in sourceTile.GetNeighbors())
            {
                if (t.Room != null && (onlyIfOutside == false || t.Room.IsOutsideRoom()))
                {
                    FloodFill(t, oldRoom);
                }
            }

            sourceTile.Room = null;

            oldRoom.tiles.Remove(sourceTile);

            // If this structure was added to an existing room
            // (which should always be true assuming with consider "outside"
            // to be a big room)
            // delete that room and assign all tiles within to be "outside" for now

            // We know all tiles now point to another room so we can just force the old
            // rooms tiles list to be blank.

            if (oldRoom.IsOutsideRoom() == false)
            {
                // At this point, oldRoom shouldn't have any more tiles left in it,
                // so in practice this "DeleteRoom" should mostly only need
                // to remove the room from the world's list.

                if (oldRoom.tiles.Count > 0)
                {
                    Debug.LogError("'oldRoom' still has tiles assigned to it. This is clearly wrong.");
                }

                world.DeleteRoom(oldRoom);
            }
        }
        else
        {
            // oldRoom is null, which means the source tile was probably a wall,
            // though this MAY not be the case any longer (i.e. the wall was
            // probably deconstructed. So the only thing we have to try is
            // to spawn ONE new room starting from the tile in question.
            FloodFill(sourceTile, null);
        }
    }

    protected static void FloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return;
        }

        if (tile.Room != oldRoom)
        {
            // This tile was already assigned to another "new" room, which means
            // that the direction picked isn't isolated. So we can just return
            // without creating a new room.
            return;
        }
        if (tile.Structure != null && tile.Structure.RoomEnclosure)
        {
            // This tile has a wall/door/whatever in it, so clearly
            // we can't do a room here.
            return;
        }

        if (tile.Type == TileTypeData.Instance.EmptyType)
        {
            // This tile is empty space and must remain part of the outside.
            return;
        }

        // If we get to this point, then we know that we need to create a new room.
        Room newRoom = new Room(tile.World);
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        bool isConnectedToSpace = false;
        int processedTiles = 0;

        while (tilesToCheck.Count > 0)
        {
            processedTiles++;
            Tile t = tilesToCheck.Dequeue();

            if (t.Room != newRoom)
            {
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbors();
                foreach (Tile t2 in ns)
                {
                    if (t2 == null || t2.Type.Flag == TileTypeData.Instance.EmptyFlag)
                    {
                        // we have hit open space (either by being the edge of the map or being an empty tile)
                        // so this "room" we're building is actually part of the Outside.
                        // Therefore, we can immediately end the flood fill (which otherwise would take ages)
                        // and more importantly, we need to delete this "newRoom" and re-assign
                        // all the tiles to Outside.

                        isConnectedToSpace = true;

                        //if (oldRoom != null)
                        //{
                        //    newRoom.ReturnTilesToOutsideRoom();
                        //    return;
                        //}
                    }
                    else
                    {
                        if (t2.Room != newRoom && (t2.Structure == null || t2.Structure.RoomEnclosure == false))
                        {
                            tilesToCheck.Enqueue(t2);
                        }
                    }

                }
            }
        }

        Debug.Log("FloodFill -- Processed Tiles: " + processedTiles);

        if (isConnectedToSpace)
        {
            // All tiles that were found by this flood fill should
            // actually be "assigned" to outside.
            newRoom.ReturnTilesToOutsideRoom();
            return;
        }

        // TODO: Copy data from old room to new room
        // newRoom.data = oldRoom.data;
        if (oldRoom != null)
        {
            // In this case we are splitting one room into two or more,
            // so we can just copy the old gas ratios.
            newRoom.CopyGas(oldRoom);
        }
        else
        {
            // In THIS case, we are merging one or more rooms together,
            // so we need to actually figure out the total volume of gas
            // in the old room vs the new room and correctly adjust
            // atmospheric quantities.

            // TODO
        }

        // Tell the world that a new room has been formed.
        tile.World.AddRoom(newRoom);
    }

    #region DEPRECATED -- DO NOT USE
    /// <summary>
    /// DEPRECATED -- USE FloodFill
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="oldRoom"></param>
    private static void NewFloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return;
        }

        if (tile.Room != oldRoom)
        {
            // This tile was already assigned to another "new" room, which means
            // that the direction picked isn't isolated. So we can just return
            // without creating a new room.
            return;
        }
        if (tile.Structure != null && tile.Structure.RoomEnclosure)
        {
            // This tile has a wall/door/whatever in it, so clearly
            // we can't do a room here.
            return;
        }
        Room newRoom = new Room(oldRoom.world);
        int x = tile.X;
        int y = tile.Y;

        int maxX = tile.World.Tiles.GetLength(0) - 1;
        int maxY = tile.World.Tiles.GetLength(1) - 1;
        Debug.Log("MaxX: " + maxX + ", MaxY: " + maxY);
        int[,] stack = new int[(maxX + 1) * (maxY + 1), 2];
        int index = 0;
        stack[0, 0] = x;
        stack[0, 1] = y;
        tile.World.Tiles[x, y].Room = newRoom;

        while (index >= 0)
        {
            x = stack[index, 0];
            y = stack[index, 1];
            index--;

            if ((x > 0) && (tile.World.Tiles[x - 1, y].Room == oldRoom))
            {
                tile.World.Tiles[x - 1, y].Room = newRoom;
                index++;
                stack[index, 0] = x - 1;
                stack[index, 1] = y;
            }

            if ((x < maxX) && (tile.World.Tiles[x + 1, y].Room == oldRoom))
            {
                tile.World.Tiles[x + 1, y].Room = newRoom;
                index++;
                stack[index, 0] = x + 1;
                stack[index, 1] = y;
            }

            if ((y > 0) && (tile.World.Tiles[x, y - 1].Room == oldRoom))
            {
                tile.World.Tiles[x, y - 1].Room = newRoom;
                index++;
                stack[index, 0] = x;
                stack[index, 1] = y - 1;
            }

            if ((y < maxY) && (tile.World.Tiles[x, y + 1].Room == oldRoom))
            {
                tile.World.Tiles[x, y + 1].Room = newRoom;
                index++;
                stack[index, 0] = x;
                stack[index, 1] = y + 1;
            }
        }

        newRoom.CopyGas(oldRoom);
        tile.World.AddRoom(newRoom);
    }
    #endregion

    private void CopyGas(Room other)
    {
        foreach (string n in other.atmosphericGasses.Keys)
        {
            this.atmosphericGasses[n] = other.atmosphericGasses[n];
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                     SAVING & LOADING
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public Room()
    {
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        //writer.WriteAttributeString("X", X.ToString());
        //writer.WriteAttributeString("Y", Y.ToString());
        //writer.WriteAttributeString("Type", Type.FlagName);
    }

    public void ReadXml(XmlReader reader)
    {
        //Type = TileTypeData.GetByFlagName(reader.GetAttribute("Type"));
    }
}