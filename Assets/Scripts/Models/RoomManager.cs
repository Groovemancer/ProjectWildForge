using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectWildForge.Rooms
{

    public class RoomManager : IEnumerable<Room>
    {
        private List<Room> rooms;

        public RoomManager()
        {
            rooms = new List<Room>();

            OutsideRoom = new Room();

            rooms.Add(OutsideRoom);
        }

        /// <summary>
        /// Occurs when adding a new room to the manager, 
        /// before the room is actually added.
        /// </summary>
        public event Action<Room> Adding;

        /// <summary>
        /// Occurs after the room has been added to the manager.
        /// </summary>
        public event Action<Room> Added;

        /// <summary>
        /// Occurs when a room is split.
        /// The first room is the old one, 
        /// the second is the new one.
        /// </summary>
        public event Action<Room, Room> Split;

        /// <summary>
        /// Occurs when two rooms are joined.
        /// The first room is the old one, 
        /// the second is the new one.
        /// 
        /// Warning, the old room has already been removed from
        /// the manager, and most of its data can be considered
        /// stale. This is a possible FIXME.
        /// </summary>
        public event Action<Room, Room> Joined;

        /// <summary>
        /// Occurs when removing a room from the manager, 
        /// before the room is actually removed.
        /// </summary>
        public event Action<Room> Removing;

        /// <summary>
        /// Occurs after the room has been removed 
        /// from the manager.
        /// </summary>
        public event Action<Room> Removed;

        /// <summary>
        /// Occurs before the floodfill or 'as it begins'.
        /// </summary>
        public event Action FloodFillBegan;

        /// <summary>
        /// Occurs after the floodfill.
        /// </summary>
        public event Action FloodFillEnded;

        public int Count
        {
            get
            {
                return rooms.Count;
            }
        }

        public Room OutsideRoom { get; private set; }

        public Room this[int index]
        {
            get
            {
                if (index < 0 || index > Count - 1)
                {
                    return null;
                }

                return rooms[index];
            }
        }

        public int GetRoomID(Room room)
        {
            return rooms.IndexOf(room);
        }

        public void Add(Room room)
        {
            if (Adding != null)
            {
                Adding(room);
            }

            rooms.Add(room);

            if (Added != null)
            {
                Added(room);
            }
        }

        public void Remove(Room room)
        {
            if (room.IsOutsideRoom())
            {
                return;
            }

            if (Removing != null)
            {
                Removing(room);
            }

            rooms.Remove(room);

            if (Removed != null)
            {
                Removed(room);
            }
        }

        /// <summary>
        /// Does a room flood fill.
        /// </summary>
        /// <param name="sourceTile">Source tile.</param>
        /// <param name="splitting">If set to <c>true</c> it will perform a split action.
        /// This is for when a room could be logically subdivided into more rooms.
        /// If set to <c>false</c> then it will try to join rooms.</param>
        public void DoRoomFloodFill(Tile sourceTile, bool splitting, bool floodFillingOnTileChange = false)
        {
            // SourceStructure is the piece of structure that may be
            // splitting two existing rooms, or may be the final 
            // enclosing piece to form a new room.
            // Check the NESW neighbours of the furniture's tile
            // and do flood fill from them.
            Room oldRoom = sourceTile.Room;
            if (floodFillingOnTileChange)
            {
                if (FloodFillBegan != null)
                {
                    FloodFillBegan();
                }

                if (splitting)
                {
                    // The source tile had a room, so this must be a new structure
                    // that is potentially dividing this old room into as many as four new rooms.

                    // Save the size of old room before we start removing tiles.
                    // Needed for gas calculations.
                    int sizeOfOldRoom = oldRoom.TileCount;

                    // Save a list of all the rooms to be removed for later calls
                    // TODO: find a way of not doing this, because at the time of the
                    // later calls, this is stale data.
                    HashSet<Room> oldRooms = new HashSet<Room>();

                    // You need to delete the surrounding rooms so a new room can be created
                    oldRooms.Add(oldRoom);
                    Remove(oldRoom);

                    if (sourceTile != null && sourceTile.Room != null)
                    {
                        Room newRoom = ActualFloodFill(sourceTile, oldRoom, sizeOfOldRoom);

                        if (newRoom != null && Split != null)
                        {
                            Split(oldRoom, newRoom);
                        }
                    }

                    Tile sourceTileDown = sourceTile.Down();

                    if (sourceTileDown != null && sourceTileDown.Room != null)
                    {
                        Room newRoom = ActualFloodFill(sourceTileDown, oldRoom, sizeOfOldRoom);

                        if (newRoom != null && Split != null)
                        {
                            Split(oldRoom, newRoom);
                        }
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
                            DebugUtils.LogErrorChannel("Room", "'oldRoom' still has tiles assigned to it. This is clearly wrong.");
                        }

                        Remove(oldRoom);
                    }

                    oldRoom.UnassignTile(sourceTile);
                }
                else
                {
                    // The source tile had a room, so this must be a new piece of furniture
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

                        Remove(sourceTile.Room);
                    }

                    Tile sourceTileDown = sourceTile.Down();

                    if (sourceTileDown != null && sourceTileDown.Room != null && !sourceTileDown.Room.IsOutsideRoom())
                    {
                        oldRooms.Add(sourceTileDown.Room);

                        Remove(sourceTileDown.Room);
                    }

                    if (sourceTileDown != null && sourceTileDown.Room.IsOutsideRoom())
                    {
                        // We're punching a hole to the outside, just skip flood filling and put everything outside
                        sourceTile.Room.ReturnTilesToOutsideRoom();
                    }
                    else
                    {
                        // FIXME: find a better way to do this since right now it 
                        // requires using stale data.
                        Room newRoom = ActualFloodFill(sourceTile, oldRoom, sizeOfOldRoom);
                        if (newRoom != null && oldRooms.Count > 0 && Joined != null)
                        {
                            foreach (Room r in oldRooms)
                            {
                                Joined(r, newRoom);
                            }
                        }
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
                        Room newRoom = ActualFloodFill(t, oldRoom, sizeOfOldRoom);

                        if (newRoom != null && Split != null)
                        {
                            Split(oldRoom, newRoom);
                        }
                    }
                }

                sourceTile.Room = null;

                oldRoom.UnassignTile(sourceTile);

                // If this structure was added to an existing room
                // (which should always be true assuming with consider "outside" to be a big room)
                // delete that room and assign all tiles within to be "outside" for now.
                if (oldRoom.IsOutsideRoom() == false)
                {
                    // At this point, oldRoom shouldn't have any more tiles left in it,
                    // so in practice this "DeleteRoom" should mostly only need
                    // to remove the room from the world's list.
                    if (oldRoom.TileCount > 0)
                    {
                        DebugUtils.LogErrorChannel("Room", "'oldRoom' still has tiles assigned to it. This is clearly wrong.");
                    }

                    Remove(oldRoom);
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

                        Remove(t.Room);
                    }
                }

                // FIXME: find a better way to do this since right now it 
                // requires using stale data.
                Room newRoom = ActualFloodFill(sourceTile, null, 0);
                if (newRoom != null && oldRooms.Count > 0 && Joined != null)
                {
                    foreach (Room r in oldRooms)
                    {
                        Joined(r, newRoom);
                    }
                }
            }

            if (FloodFillEnded != null)
            {
                FloodFillEnded();
            }
        }

        protected Room ActualFloodFill(Tile sourceTile, Room oldRoom, int sizeOfOldRoom)
        {
            if (sourceTile == null)
            {
                // We are trying to flood fill off the map, so just return
                // without doing anything.
                return null;
            }

            if (sourceTile.Room != oldRoom)
            {
                // This tile was already assigned to another "new" room, which means
                // that the direction picked isn't isolated. So we can just return
                // without creating a new room.
                return null;
            }

            if (sourceTile.Structure != null && sourceTile.Structure.RoomEnclosure)
            {
                // This tile has a wall/door/whatever in it, so clearly
                // we can't do a room here.
                return null;
            }

            // If we get to this point, then we know that we need to create a new room.
            HashSet<Room> listOfOldRooms = new HashSet<Room>();

            Room newRoom = new Room();
            Queue<Tile> tilesToCheck = new Queue<Tile>();
            tilesToCheck.Enqueue(sourceTile);

            bool connectedToSpace = false;
            int processedTiles = 0;

            while (tilesToCheck.Count > 0)
            {
                Tile currentTile = tilesToCheck.Dequeue();

                processedTiles++;

                if (currentTile.Room != newRoom)
                {
                    if (currentTile.Room != null && listOfOldRooms.Contains(currentTile.Room) == false)
                    {
                        listOfOldRooms.Add(currentTile.Room);
                        //AtmosphereUtils.MovePercentageOfAtmosphere(currentTile.Room.Atmosphere, newRoom.Atmosphere, 1.0f);
                    }

                    newRoom.AssignTile(currentTile);

                    Tile[] neighbors = currentTile.GetNeighbors(false, true);
                    foreach (Tile neighborTile in neighbors)
                    //for (int i = 0; i < neighbors.Length; i++)
                    {
                        if (neighborTile == null || neighborTile.HasClearLineToBottom())
                        {
                            // We have hit open space (either by being the edge of the map or being an empty tile)
                            // so this "room" we're building is actually part of the Outside.
                            // Therefore, we can immediately end the flood fill (which otherwise would take ages)
                            // and more importantly, we need to delete this "newRoom" and re-assign
                            // all the tiles to Outside.
                            connectedToSpace = true;
                        }
                        else
                        {
                            // We know t2 is not null nor is it an empty tile, so just make sure it
                            // hasn't already been processed and isn't a "wall" type tile.
                            if (
                                neighborTile.Room != newRoom && (neighborTile.Structure == null || neighborTile.Structure.RoomEnclosure == false))
                            {
                                tilesToCheck.Enqueue(neighborTile);
                            }
                        }
                    }
                }
            }

            if (connectedToSpace)
            {
                // All tiles that were found by this flood fill should
                // actually be "assigned" to outside.
                newRoom.ReturnTilesToOutsideRoom();
                return null;
            }

            // Copy data from the old room into the new room.
            if (oldRoom != null)
            {
                // In this case we are splitting one room into two or more,
                // so we can just copy the old gas ratios.
                // 1 is subtracted from size of old room to account for tile being filled by furniture,
                // this prevents gas from being lost
                float ratio = oldRoom.IsOutsideRoom() ? 0.0f : (float)newRoom.TileCount / (sizeOfOldRoom - 1);
                ////UnityDebugger.Debugger.Log("Splitting atmo between " + oldRoom.ID + " and " + newRoom.ID + ". " + newRoom.TileCount + " / " + (sizeOfOldRoom - 1) + " = " + ratio);
                //AtmosphereUtils.MovePercentageOfAtmosphere(oldRoom.Atmosphere, newRoom.Atmosphere, ratio);
            }

            // Tell the world that a new room has been formed.
            Add(newRoom);

            return newRoom;
        }


        #region IEnumerator Implementation
        public IEnumerator<Room> GetEnumerator()
        {
            return rooms.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (Room room in rooms)
            {
                yield return room;
            }
        }
        #endregion
    }
}