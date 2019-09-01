using UnityEngine;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Room : IXmlSerializable
{
    Dictionary<string, float> atmosphericGasses;

    private HashSet<Tile> tiles;

    private HashSet<Tile> boundaryTiles;

    public Room()
    {
        tiles = new HashSet<Tile>();
        boundaryTiles = new HashSet<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

    public int Id
    {
        get
        {
            return World.Current.RoomManager.GetRoomID(this);
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
        if (boundaryTiles.Contains(tile))
        {
            boundaryTiles.Remove(tile);
        }

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
            tile.Room = World.Current.RoomManager.OutsideRoom;
        }

        tiles.Clear();
    }

    public bool IsOutsideRoom()
    {
        return this == World.Current.RoomManager.OutsideRoom;
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

    public HashSet<Tile> GetInnerTiles()
    {
        return tiles;
    }

    public HashSet<Tile> GetBoundaryTiles()
    {
        if (boundaryTiles.Count == 0)
        {
            foreach (Tile tile in tiles)
            {
                Tile[] neighbors = tile.GetNeighbors();
                foreach (Tile tile2 in neighbors)
                {
                    if (tile2 != null && tile.Structure != null)
                    {
                        if (tile2.Structure.RoomEnclosure)
                        {
                            // We have found an enclosing structure, which means it is on our border
                            boundaryTiles.Add(tile2);
                        }
                    }
                }
            }
        }

        return boundaryTiles;
    }

    public List<Tile> FindExits()
    {
        List<Tile> exits = new List<Tile>();
        foreach (Tile tile in tiles)
        {
            Tile[] neighbours = tile.GetNeighbors();
            foreach (Tile tile2 in neighbours)
            {
                if (tile2 != null && tile2.Structure != null)
                {
                    if (tile2.Structure.IsExit())
                    {
                        // We have found an exit
                        exits.Add(tile2);
                    }
                }
            }
        }

        return exits;
    }

    public Tile FindExitBetween(Room room2)
    {
        List<Tile> exits = this.FindExits();

        foreach (Tile exit in exits)
        {
            if (exit.GetNeighbors().Any(tile => tile.Room == room2))
            {
                return exit;
            }
        }

        // In theory this should never be reached, if we are passed two rooms from a roomPath, there should always be an exit between
        // But we should probably add some kind of error checking anyways.
        return null;
    }

    public Dictionary<Tile, Room> GetNeighbors()
    {
        Dictionary<Tile, Room> neighboursRooms = new Dictionary<Tile, Room>();

        List<Tile> exits = this.FindExits();

        foreach (Tile tile in exits)
        {
            // Loop over the exits to find a different room
            Tile[] neighbours = tile.GetNeighbors(true, false, false);
            foreach (Tile neighbor in neighbours)
            {
                if (neighbor == null || neighbor.Room == null)
                {
                    continue;
                }

                // We have found a room
                if (neighbor.Room != this)
                {
                    neighboursRooms[neighbor] = neighbor.Room;
                }
            }
        }

        return neighboursRooms;
    }

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