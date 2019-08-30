using UnityEngine;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;

using System;

public class Room : IXmlSerializable
{
    Dictionary<string, float> atmosphericGasses;

    HashSet<Tile> tiles;

    public Room()
    {
        tiles = new HashSet<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

    public int Id
    {
        get
        {
            return World.current.GetRoomId(this);
        }
    }

    public int TileCount
    {
        get
        {
            return tiles.Count;
        }
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

        t.Room = this;
        tiles.Add(t);
    }

    public void UnassignTile(Tile tile)
    {
        if (tiles.Contains(tile) == false)
        {
            // This tile is not in this room.
            return;
        }

        tile.Room = null;
        tiles.Remove(tile);
    }

    public void ReturnTilesToOutsideRoom()
    {
        foreach (Tile tile in tiles)
        {
            // Assign to outside
            tile.Room = World.current.OutsideRoom;
        }

        tiles.Clear();
    }

    public bool IsOutsideRoom()
    {
        return this == World.current.OutsideRoom;
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

    public static void DoRoomFloodFill(Tile sourceTile, bool splitting, bool floodFillingOnTileChange = false)
    {
        // TODO REMOVE when we figure out some optimizations
        //return;

        // sourceStruct is the structure that may be splitting
        // two existing rooms, or may be the final enclosing piece
        // to form a new room.
        // Check the NESW neighbors of the structure's tile
        // and do flood fill from them

        Room oldRoom = sourceTile.Room;

        if (floodFillingOnTileChange)
        {
            if (splitting)
            {
                // The source tile had a room, so this must be a new piece of furniture
                // that is potentially dividing this old room into as many as four new rooms.

                // Save the size of old room before we start removing tiles.
                // Needed for gas calculations.
                int sizeOfOldRoom = oldRoom.TileCount;

                HashSet<Room> oldRooms = new HashSet<Room>();

                // You need to delete the surrounding rooms so a new room can be created
                oldRooms.Add(oldRoom);
                World.current.rooms.Remove(oldRoom);

                if (sourceTile != null && sourceTile.Room != null)
                {
                    Room newRoom = FloodFill(sourceTile, oldRoom);

                    //if (newRoom != null && Split != null)
                    //{
                    //    Split(oldRoom, newRoom);
                    //}
                }

                // If this piece of furniture was added to an existing room
                // (which should always be true assuming with consider "outside" to be a big room)
                // delete that room and assign all tiles within to be "outside" for now.
                if (oldRoom.IsOutsideRoom() == false)
                {
                    // At this point, oldRoom shouldn't have any more tiles left in it,
                    // so in practice this "DeleteRoom" should mostly only need
                    // to remove the room from the world's list.
                    if (oldRoom.TileCount > 0)
                    {
                        Debug.LogError("'oldRoom' still has tiles assigned to it. This is clearly wrong.");
                    }

                    World.current.rooms.Remove(oldRoom);
                }

                oldRoom.UnassignTile(sourceTile);
            }
            else
            {
                // The source tile had a room, so this must be a new structure
                // that is potentially dividing this old room into as many as four new rooms.

                // Save the size of old room before we start removing tiles.
                // Needed for gas calculations.
                int sizeOfOldRoom = oldRoom.TileCount;

                // oldRoom is null, which means the source tile was probably a wall,
                // though this MAY not be the case any longer (i.e. the wall was 
                // probably deconstructed. So the only thing we have to try is
                // to spawn ONE new room starting from the tile in question.

                // Save a list of all the rooms to be removed for later calls
                // TODO: find a way of not doing this, because at the time of the
                // later calls, this is stale data.
                HashSet<Room> oldRooms = new HashSet<Room>();

                if (sourceTile != null && sourceTile.Room != null && !sourceTile.Room.IsOutsideRoom())
                {
                    oldRooms.Add(sourceTile.Room);

                    World.current.rooms.Remove(sourceTile.Room);
                }
            }
        }
        else if (oldRoom != null && splitting)
        {
            // The source tile had a room, so this must be a new structure
            // that is potentially dividing this old room into as many as four new rooms.

            // Save the size of old room before we start removing tiles.
            // Needed for gas calculations.
            int sizeOfOldRoom = oldRoom.TileCount;

            // Try building new rooms for each of our NESW directions.
            foreach (Tile t in sourceTile.GetNeighbors())
            {
                if (t != null && t.Room != null)
                {
                    Room newRoom = FloodFill(t, oldRoom);

                    //if (newRoom != null && Split != null)
                    //{
                    //    Split(oldRoom, newRoom);
                    //}
                }
            }

            sourceTile.Room = null;

            oldRoom.UnassignTile(sourceTile);

            // If this piece of furniture was added to an existing room
            // (which should always be true assuming with consider "outside" to be a big room)
            // delete that room and assign all tiles within to be "outside" for now.
            if (oldRoom.IsOutsideRoom() == false)
            {
                // At this point, oldRoom shouldn't have any more tiles left in it,
                // so in practice this "DeleteRoom" should mostly only need
                // to remove the room from the world's list.
                if (oldRoom.TileCount > 0)
                {
                    Debug.LogError("'oldRoom' still has tiles assigned to it. This is clearly wrong.");
                }

                World.current.rooms.Remove(oldRoom);
            }
        }
        else if (oldRoom == null && splitting == false)
        {
            // oldRoom is null, which means the source tile was probably a wall,
            // though this MAY not be the case any longer (i.e. the wall was 
            // probably deconstructed. So the only thing we have to try is
            // to spawn ONE new room starting from the tile in question.

            // Save a list of all the rooms to be removed for later calls
            // TODO: find a way of not doing this, because at the time of the
            // later calls, this is stale data.
            HashSet<Room> oldRooms = new HashSet<Room>();

            // You need to delete the surrounding rooms so a new room can be created
            foreach (Tile t in sourceTile.GetNeighbors())
            {
                if (t != null && t.Room != null && !t.Room.IsOutsideRoom())
                {
                    oldRooms.Add(t.Room);

                    World.current.rooms.Remove(t.Room);
                }
            }

            // FIXME: find a better way to do this since right now it 
            // requires using stale data.
            Room newRoom = FloodFill(sourceTile, null);

            //if (newRoom != null && oldRooms.Count > 0 && Joined != null)
            //{
            //    foreach (Room r in oldRooms)
            //    {
            //        Joined(r, newRoom);
            //    }
            //}
        }

    }

    protected static Room FloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // We are trying to flood fill off the map, so just return
            // without doing anything.
            return null;
        }

        if (tile.Room != oldRoom)
        {
            // This tile was already assigned to another "new" room, which means
            // that the direction picked isn't isolated. So we can just return
            // without creating a new room.
            return null;
        }
        if (tile.Structure != null && tile.Structure.RoomEnclosure)
        {
            // This tile has a wall/door/whatever in it, so clearly
            // we can't do a room here.
            return null;
        }

        if (tile.Type == TileTypeData.Instance.EmptyType)
        {
            // This tile is empty space and must remain part of the outside.
            return null;
        }

        // If we get to this point, then we know that we need to create a new room.
        HashSet<Room> listOfOldRooms = new HashSet<Room>();

        // If we get to this point, then we know that we need to create a new room.
        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        bool isConnectedToSpace = false;
        int processedTiles = 0;

        DateTime startTime = DateTime.Now;

        while (tilesToCheck.Count > 0)
        {
            Tile currentTile = tilesToCheck.Dequeue();
            processedTiles++;

            if (currentTile.Room != newRoom)
            {
                if (currentTile.Room != null && listOfOldRooms.Contains(currentTile.Room) == false)
                {
                    listOfOldRooms.Add(currentTile.Room);
                }

                newRoom.AssignTile(currentTile);

                Tile[] neighbors = currentTile.GetNeighbors();
                foreach (Tile neighborTile in neighbors)
                {
                    if (neighborTile == null || neighborTile.Type.Flag == TileTypeData.Instance.EmptyFlag)
                    {
                        // we have hit open space (either by being the edge of the map or being an empty tile)
                        // so this "room" we're building is actually part of the Outside.
                        // Therefore, we can immediately end the flood fill (which otherwise would take ages)
                        // and more importantly, we need to delete this "newRoom" and re-assign
                        // all the tiles to Outside.

                        isConnectedToSpace = true;
                    }
                    else
                    {
                        if (neighborTile.Room != newRoom && (neighborTile.Structure == null || neighborTile.Structure.RoomEnclosure == false))
                        {
                            tilesToCheck.Enqueue(neighborTile);
                        }
                    }

                }
            }

            //if (processedTiles > 10000)
            //    break;
        }

        TimeSpan ts = DateTime.Now.Subtract(startTime);

        Debug.Log("FloodFill -- Processed Tiles: " + processedTiles + ", time: " + ts.TotalSeconds.ToString());

        if (isConnectedToSpace)
        {
            // All tiles that were found by this flood fill should
            // actually be "assigned" to outside.
            newRoom.ReturnTilesToOutsideRoom();
            return null;
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
        World.current.AddRoom(newRoom);

        return newRoom;
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
        Room newRoom = new Room();
        int x = tile.X;
        int y = tile.Y;

        int maxX = World.current.Tiles.GetLength(0) - 1;
        int maxY = World.current.Tiles.GetLength(1) - 1;
        Debug.Log("MaxX: " + maxX + ", MaxY: " + maxY);
        int[,] stack = new int[(maxX + 1) * (maxY + 1), 2];
        int index = 0;
        stack[0, 0] = x;
        stack[0, 1] = y;
        World.current.Tiles[x, y].Room = newRoom;

        while (index >= 0)
        {
            x = stack[index, 0];
            y = stack[index, 1];
            index--;

            if ((x > 0) && (World.current.Tiles[x - 1, y].Room == oldRoom))
            {
                World.current.Tiles[x - 1, y].Room = newRoom;
                index++;
                stack[index, 0] = x - 1;
                stack[index, 1] = y;
            }

            if ((x < maxX) && (World.current.Tiles[x + 1, y].Room == oldRoom))
            {
                World.current.Tiles[x + 1, y].Room = newRoom;
                index++;
                stack[index, 0] = x + 1;
                stack[index, 1] = y;
            }

            if ((y > 0) && (World.current.Tiles[x, y - 1].Room == oldRoom))
            {
                World.current.Tiles[x, y - 1].Room = newRoom;
                index++;
                stack[index, 0] = x;
                stack[index, 1] = y - 1;
            }

            if ((y < maxY) && (World.current.Tiles[x, y + 1].Room == oldRoom))
            {
                World.current.Tiles[x, y + 1].Room = newRoom;
                index++;
                stack[index, 0] = x;
                stack[index, 1] = y + 1;
            }
        }

        newRoom.CopyGas(oldRoom);
        World.current.AddRoom(newRoom);
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

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        // Write out gas info
        foreach (string k in atmosphericGasses.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", atmosphericGasses[k].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        // Read gas info
        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                atmosphericGasses[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }
}