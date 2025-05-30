﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class StructureManager : IEnumerable<Structure>
{
    private List<Structure> structures;

    public StructureManager()
    {
        structures = new List<Structure>();
    }

    public delegate void RoomsUpdated();

    public event Action<Structure> Created;

    /// <summary>
    /// Creates a structure with the given type and places it at the given tile.
    /// </summary>
    /// <returns>The structure.</returns>
    /// <param name="type">The type of the structure.</param>
    /// <param name="tile">The tile to place the structure at.</param>
    /// <param name="doRoomFloodFill">If set to <c>true</c> do room flood fill.</param>
    public Structure PlaceStructure(string type, Tile tile, bool doRoomFloodFill = true)
    {
        DebugUtils.LogChannel("StructureManager", string.Format("PlaceStructure type={0}, tile x={1}, y={2}", type, tile.X, tile.Y));
        Structure structure;
        if (PrototypeManager.Structure.TryGet(type, out structure) == false)
        {
            DebugUtils.LogErrorChannel("StructureManager", "structurePrototypes doesn't contain a proto for key: " + type);
            return null;
        }

        return PlaceStructure(structure.Clone(), tile, doRoomFloodFill);
    }

    /// <summary>
    /// Places the given structure prototype at the given tile.
    /// </summary>
    /// <returns>The structure.</returns>
    /// <param name="prototype">The structure prototype.</param>
    /// <param name="tile">The tile to place the furniture at.</param>
    /// <param name="doRoomFloodFill">If set to <c>true</c> do room flood fill.</param>
    public Structure PlaceStructure(Structure prototype, Tile tile, bool doRoomFloodFill = true)
    {
        Structure structure = Structure.PlaceInstance(prototype, tile);

        if (structure == null)
        {
            // Failed to place object -- most likely there was already something there.
            return null;
        }

        structure.Removed += OnRemoved;

        structures.Add(structure);
        if (structure.RequiresFastUpdate)
        {
            TimeManager.Instance.RegisterFastUpdate(structure);
        }

        if (structure.RequiresSlowUpdate)
        {
            TimeManager.Instance.RegisterSlowUpdate(structure);
        }

        // Do we need to recalculate our rooms/reachability for other jobs?
        if (doRoomFloodFill && structure.RoomEnclosure)
        {
            World.Current.RoomManager.DoRoomFloodFill(structure.Tile, true);
        }

        if (Created != null)
        {
            Created(structure);
        }

        return structure;
    }

    /// <summary>
    /// When a construction job is completed, place the structure.
    /// </summary>
    /// <param name="job">The completed job.</param>
    public void ConstructJobCompleted(Job job)
    {
        Structure structure = (Structure)job.buildablePrototype;

        // Let our workspot tile know it is no longer reserved for us
        World.Current.UnreserveTileAsWorkSpot(structure, job.Tile);

        PlaceStructure(structure, job.Tile);
    }

    /// <summary>
    /// Determines whether the placement of a structure with the given type at the given tile is valid.
    /// </summary>
    /// <returns><c>true</c> if the placement is valid; otherwise, <c>false</c>.</returns>
    /// <param name="type">The structure type.</param>
    /// <param name="tile">The tile where the structure will be placed.</param>
    public bool IsPlacementValid(string type, Tile tile)
    {
        Structure structure = PrototypeManager.Structure.Get(type).Clone();
        return structure.IsValidPosition(tile);
    }

    /// <summary>
    /// Determines whether the work spot of the structure with the given type at the given tile is clear.
    /// </summary>
    /// <returns><c>true</c> if the work spot at the give tile is clear; otherwise, <c>false</c>.</returns>
    /// <param name="structureType">Structure type.</param>
    /// <param name="tile">The tile we want to check.</param>
    public bool IsWorkSpotClear(string type, Tile tile)
    {
        Structure proto = PrototypeManager.Structure.Get(type);

        // If the workspot is internal, we don't care about structure blocking it, this will be stopped or allowed
        //      elsewhere depending on if the structure being placed can replace the structure already in this tile.
        if (proto.Jobs.WorkSpotIsInternal())
        {
            return true;
        }

        if (proto.Jobs != null && World.Current.GetTileAt((int)(tile.X + proto.Jobs.WorkSpotOffset.x), (int)(tile.Y + proto.Jobs.WorkSpotOffset.y), (int)tile.Z).Structure != null)
        {
            return false;
        }

        return true;
    }


    public IEnumerator<Structure> GetEnumerator()
    {
        return structures.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return structures.GetEnumerator();
    }

    /// <summary>
    /// Called when a structure is removed so that it can be deleted from the list.
    /// </summary>
    /// <param name="structures">The structure being removed.</param>
    private void OnRemoved(Structure structure)
    {
        structures.Remove(structure);
        TimeManager.Instance.UnregisterFastUpdate(structure);
        TimeManager.Instance.UnregisterSlowUpdate(structure);
    }
}
