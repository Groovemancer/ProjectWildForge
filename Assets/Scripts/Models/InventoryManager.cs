using System.Collections.Generic;
using UnityEngine;

public class InventoryManager
{
    // This is a list of all "live" inventories.
    // Later on this will likely be organized by rooms instead
    // of a single master list. (Or in addition to.)
    public Dictionary<string, List<Inventory>> inventories;

    public InventoryManager()
    {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    void CleanupInventory(Inventory inv)
    {
        if (inv.stackSize == 0)
        {
            if (inventories.ContainsKey(inv.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }
            if (inv.tile != null)
            {
                inv.tile.Inventory = null;
                inv.tile = null;
            }
            if (inv.actor != null)
            {
                inv.actor.inventory = null;
                inv.actor = null;
            }
        }
    }

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        bool tileWasEmpty = tile.Inventory == null;

        if (tile.PlaceInventory(inv) == false)
        {
            // The tile did not accept the inventory for whatever reason, therefor stop.
            return false;
        }

        CleanupInventory(inv);

        // We may have also created a new stack on the tile, if the tile was previously empty.
        if (tileWasEmpty)
        {
            if (inventories.ContainsKey(tile.Inventory.objectType) == false)
            {
                inventories[tile.Inventory.objectType] = new List<Inventory>();
            }
            inventories[tile.Inventory.objectType].Add(tile.Inventory);
        }

        return true;
    }

    public bool PlaceInventory(Job job, Inventory inv)
    {
        if (job.inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            Debug.LogError("Trying to add inventory to a job that it doesn't want.");
            return false;
        }
        job.inventoryRequirements[inv.objectType].stackSize += inv.stackSize;

        if (job.inventoryRequirements[inv.objectType].stackSize > job.inventoryRequirements[inv.objectType].maxStackSize)
        {
            inv.stackSize = job.inventoryRequirements[inv.objectType].stackSize - job.inventoryRequirements[inv.objectType].maxStackSize;
            job.inventoryRequirements[inv.objectType].stackSize = job.inventoryRequirements[inv.objectType].maxStackSize;
        }
        else
        {
            inv.stackSize = 0;
        }

        CleanupInventory(inv);

        return true;
    }

    public bool PlaceInventory(Actor actor, Inventory sourceInventory, int amount = -1)
    {
        if (amount < 0)
            amount = sourceInventory.stackSize;

        if (actor.inventory == null)
        {
            actor.inventory = sourceInventory.Clone();
            actor.inventory.stackSize = 0;
            inventories[actor.inventory.objectType].Add(actor.inventory);
        }
        else if (actor.inventory.objectType != sourceInventory.objectType)
        {
            Debug.LogError("Character is trying to pick up a mismatched inventory object type.");
            return false;
        }

        actor.inventory.stackSize += amount;

        if (actor.inventory.stackSize > actor.inventory.maxStackSize)
        {
            sourceInventory.stackSize = actor.inventory.stackSize - actor.inventory.maxStackSize;
            actor.inventory.stackSize = actor.inventory.maxStackSize;
        }
        else
        {
            sourceInventory.stackSize -= amount;
        }

        CleanupInventory(sourceInventory);

        return true;
    }

    /// <summary>
    /// Gets the type of the closest
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="t"></param>
    /// <param name="desiredAmount">Desired amount. If no stack has enough, it instead returns the largest</param>
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount)
    {
        // FIXME:
        //      a) We are LYING about returning the closest item.
        //      b) There's no way to return the closest item in an optimal manner
        //         until our "inventories" database is more sophisticated.
        //         (i.e. separate tile inventory from character inventory and maybe
        //          has room content optimization.)

        if (inventories.ContainsKey(objectType) == false)
        {
            Debug.LogError("GetClosestInventoryOfType -- no items of desired type.");
            return null;
        }

        foreach (Inventory inv in inventories[objectType])
        {
            if (inv.tile != null)
            {
                return inv;
            }
        }

        return null;
    }
}