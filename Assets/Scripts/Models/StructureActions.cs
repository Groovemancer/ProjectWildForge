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

    public static Inventory[] Stockpile_GetItemsFromFilter()
    {
        // TODO: This should be reading from some kind of UI for this
        // particular stockpile

        // Since jobs copy arrays automatically, we could already have
        // an Inventory[] prepared and just return that (as a sort of example filter)

        return new Inventory[1] { new Inventory("RawStone", 50, 0) };
    }

    public static void Stockpile_UpdateAction(Structure structure, float deltaAuts)
    {
        DebugUtils.Log("Stockpile_UpdateAction");
        // We need to ensure that we have a job on the queue
        // asking for either:
        //  (if we are empty): That ANY loose inventory be brought to us.
        //  (if we have something: Then IF we are still below the max stack size,
        //                          that more of the same should be brought to us.

        // TODO: This function doesn't need to run each update. Once we get a lot
        // of structures in a running game, this will run a LOT more than required.
        // Instead, it only really needs to run whenever:
        //      -- It gets created
        //      -- A good gets delivered (at which point we reset the job)
        //      -- A good gets picked up (at which point we reset the job)
        //      -- The UI's filter of allowed items gets changed

        if (structure.Tile.Inventory != null && structure.Tile.Inventory.stackSize >= structure.Tile.Inventory.maxStackSize)
        {
            // We are full
            structure.CancelJobs();
            return;
        }

        // Maybe we already hae a job queued up?
        if (structure.JobCount() > 0)
        {
            // Cool, all done.
            return;
        }

        // We currently are NOT full, but we don't have a job either.
        // Two possibilities: Either we have SOME inventory, or we have NO inventory

        // Third possibility: Something is WHACK
        if (structure.Tile.Inventory != null && structure.Tile.Inventory.stackSize == 0)
        {
            DebugUtils.LogError("Stockpile has a zero-size stack. This is clearly WRONG!");
            structure.CancelJobs();
            return;
        }

        // TODO: In the future, stockpiles -- rather than being a bunch of individual
        // 1x1 tiles -- should manifest themselves as single, large objects (this
        // would represent our first and probably only VARIABLE sized structure --
        // at what happens if there's a "hole" in our stockpile because we have an
        // actual structure (like a cooking station) installed in the middle of our stockpile?
        // In any case, once we implement "mega stockpiles", then the job-creation system
        // could be a lot smarter, in that even if the stockpile has some stuff in it, it
        // can also still be requesting different object types in its job creation.

        Inventory[] itemsDesired;

        if (structure.Tile.Inventory == null)
        {
            Debug.Log("Creating job for new stack.");
            itemsDesired = Stockpile_GetItemsFromFilter();
        }
        else
        {
            Debug.Log("Creating job for existing stack.");
            Inventory desInv = structure.Tile.Inventory.Clone();
            desInv.maxStackSize -= desInv.stackSize;
            desInv.stackSize = 0;

            itemsDesired = new Inventory[] { desInv };
        }

        Job j = new Job(
                structure.Tile,
                null,
                null,
                0,
                itemsDesired
            );
        // TODO: Later on, add stockpile priorities, so that we can take from a lower
        // priority stockpile for a higher priority one.
        j.canTakeFromStockpile = false;

        j.RegisterJobWorkedCallback(Stockpile_JobWorked);
        structure.AddJob(j);
    }

    static void Stockpile_JobWorked(Job j)
    {
        Debug.Log("Stockpile_JobWorked");
        j.CancelJob();

        // TODO: Change this when we figure out what we're doing for the all/any pickup job.
        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                World.current.inventoryManager.PlaceInventory(j.Tile, inv);
                return;
            }
        }
    }

    public static void OxygenGenerator_UpdateAction(Structure structure, float deltaAuts)
    {
        if (structure.Tile.Room.GetGasAmount("O2") < 0.20f)
        {
            //TODO: Change the gas contribution based on the volume of the room
            structure.Tile.Room.ChangeGas("O2", 0.01f / deltaAuts); // TODO: Replace hardcoded value!
        }
    }

    public static void WorkStation_UpdateAction(Structure structure, float deltaAuts)
    {
        Tile spawnSpot = structure.GetSpawnSpotTile();

        if (structure.JobCount() > 0)
        {
            // Check to see if the Raw Stone destination tile is full.
            if (spawnSpot.Inventory != null && spawnSpot.Inventory.stackSize >= spawnSpot.Inventory.maxStackSize)
            {
                // We should stop this job, because it's impossible to make any more items.
                structure.CancelJobs();
            }

            return;
        }

        // If we get here, then we have no current job. Check to see if our destination is full.
        if (spawnSpot.Inventory != null && spawnSpot.Inventory.stackSize >= spawnSpot.Inventory.maxStackSize)
        {
            // We are full! Don't make a job.
            return;
        }

        // If we get here, we need to CREATE a new job.

        Tile jobSpot = structure.GetJobSpotTile();

        if (jobSpot.Inventory != null && (jobSpot.Inventory.stackSize >= jobSpot.Inventory.maxStackSize))
        {
            // Our drop spot is already full, so don't create a job.
            return;
        }

        Job j = new Job(
            jobSpot,
            null,
            WorkStation_JobComplete,
            600,
            null,
            true    // This job repeats until the destination tile is full.
        );

        structure.AddJob(j);
    }

    public static void WorkStation_JobComplete(Job j)
    {
        Debug.Log("WorkStation_JobComplete");

        World.current.inventoryManager.PlaceInventory(j.structure.GetSpawnSpotTile(), new Inventory("RawStone", 50, 10));
    }
}