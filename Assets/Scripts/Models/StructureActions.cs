using UnityEngine;
using System.Collections;

public static class StructureActions
{
    public static void Door_UpdateAction(Structure structure, float deltaAuts)
    {
        if (structure.GetParameter("isOpening") >= 1)
        {
            //Debug.Log("Door_UpdateAction: " + structure.GetParameter("openness") + ", deltaAuts: " + deltaAuts);
            structure.ChangeParameter("openness", deltaAuts);
            if (structure.GetParameter("openness") >= structure.GetParameter("doorOpenTime"))
            {
                structure.SetParameter("isOpening", 0);
            }
        }
        else
        {
            structure.ChangeParameter("openness", -deltaAuts);
        }

        structure.SetParameter("openness", Mathf.Clamp(structure.GetParameter("openness"), 0,
            structure.GetParameter("doorOpenTime")));

        structure.cbOnChanged(structure);
    }

    public static Enterability Door_IsEnterable(Structure structure)
    {
        //Debug.Log("Door_IsEnterable");
        structure.SetParameter("isOpening", 1);

        if (structure.GetParameter("openness") >= structure.GetParameter("doorOpenTime"))
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }

    public static void JobComplete_StructureBuilding(Job theJob)
    {
        WorldController.Instance.World.PlaceStructure(theJob.jobObjectType, theJob.Tile);
        theJob.Tile.PendingStructureJob = null;
    }

    public static void Stockpile_UpdateAction(Structure structure, float deltaAuts)
    {
        Debug.Log("Stockpile_UpdateAction");
        // We need to ensure that we have a job on the queue
        // asking for either:
        //  (if we are empty): That ANY loose inventory be brought to us.
        //  (if we have something: Then IF we are still below the max stack size,
        //                          that more of the same should be brought to us.

        if (structure.Tile.Inventory == null)
        {
            // We are empty -- just ask for ANYTHING to be brought here.

            // Do we already have a job?
            if (structure.JobCount() > 0)
            {
                return;
            }

            Job j = new Job(
                structure.Tile,
                null,
                null,
                0,
                new Inventory[1] { new Inventory("RawStone", 50, 0) }   // FIXME: Need to be able to indicate all/any type is okay
            );
            j.RegisterJobWorkedCallback(Stockpile_JobWorked);

            structure.AddJob(j);
        }
        else if (structure.Tile.Inventory.stackSize < structure.Tile.Inventory.maxStackSize)
        {
            // We have a stack of something started but we're not full yet.

            // Do we already have a job?
            if (structure.JobCount() > 0)
            {
                return;
            }

            Inventory desInv = structure.Tile.Inventory.Clone();
            desInv.maxStackSize -= desInv.stackSize;
            desInv.stackSize = 0;

            Job j = new Job(
                structure.Tile,
                null,
                null,
                0,
                new Inventory[1] { desInv }   // FIXME: Need to be able to indicate all/any type is okay
            );
            j.RegisterJobWorkedCallback(Stockpile_JobWorked);

            structure.AddJob(j);
        }
    }

    static void Stockpile_JobWorked(Job j)
    {
        j.Tile.Structure.RemoveJob(j);

        // TODO: Change this when we figure out what we're doing for the all/any pickup job.
        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                j.Tile.World.inventoryManager.PlaceInventory(j.Tile, inv);
                return;
            }
        }
    }
}