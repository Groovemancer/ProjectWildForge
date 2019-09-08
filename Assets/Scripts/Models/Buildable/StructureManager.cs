using System;
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
        if (PrototypeManager.Structure.Has(type) == false)
        {
            Debug.LogError("structurePrototypes doesn't contain a proto for key: " + type);
            return null;
        }

        Structure structure = PrototypeManager.Structure.Get(type).Clone();

        return PlaceStructure(structure, tile, doRoomFloodFill);
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
        Structure structure = job.structurePrototype;


        // TODO Add Reserve tile workspot
        // Let our workspot tile know it is no longer reserved for us
        //World.Current.UnreserveTileAsWorkSpot(structure, job.Tile);

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
