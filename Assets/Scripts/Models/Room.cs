using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Room
{
    List<Tile> tiles;

    static int floodIterations = 0;

    public Room()
    {
        tiles = new List<Tile>();
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

    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].Room = tiles[i].World.GetOutsideRoom();    // Assign to outside
        }

        tiles = new List<Tile>();
    }

    public static void DoRoomFloodFill(Structure sourceStruct)
    {
        // sourceStruct is the structure that may be splitting
        // two existing rooms, or may be the final enclosing piece
        // to form a new room.
        // Check the NESW neighbors of the structure's tile
        // and do flood fill from them

        World world = sourceStruct.Tile.World;

        Room oldRoom = sourceStruct.Tile.Room;

        // Try building a new rooms for each of our NESW directions
        foreach (Tile t in sourceStruct.Tile.GetNeighbors())
        {
            FloodFill(t, oldRoom);
            //NewFloodFill(t, oldRoom);
        }

        sourceStruct.Tile.Room = null;
        oldRoom.tiles.Remove(sourceStruct.Tile);

        Debug.Log("Flood Fill iterations: " + floodIterations);

        // If this structure was added to an existing room
        // (which should always be true assuming with consider "outside"
        // to be a big room)
        // delete that room and assign all tiles within to be "outside" for now

        // We know all tiles now point to another room so we can just force the old
        // rooms tiles list to be blank.

        if (oldRoom != world.GetOutsideRoom())
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

        // If we get to this point, then we know that we need to create a new room.
        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            if (t.Room == oldRoom)
            {
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbors();
                foreach (Tile t2 in ns)
                {
                    if (t2 == null || t2.Type == TileType.Empty)
                    {
                        // we have hit open space (either by being the edge of the map or being an empty tile)
                        // so this "room" we're building is actually part of the Outside.
                        // Therefore, we can immediately end the flood fill (which otherwise would take ages)
                        // and more importantly, we need to delete this "newRoom" and re-assign
                        // all the tiles to Outside.
                        newRoom.UnAssignAllTiles();
                        return;
                    }

                    if (t2.Room == oldRoom && (t2.Structure == null || t2.Structure.RoomEnclosure == false))
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                    floodIterations++;
                }
            }
        }

        // TODO: Copy data from old room to new room
        // newRoom.data = oldRoom.data;

        // Tell the world that a new room has been formed.
        tile.World.AddRoom(newRoom);
    }

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

        tile.World.AddRoom(newRoom);
    }
}