using System;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Linq;
using ProjectWildForge.Pathfinding;

[MoonSharpUserData]
public class InventoryManager
{
    // This is a list of all "live" inventories.
    // Later on this will likely be organized by rooms instead
    // of a single master list. (Or in addition to.)
    public Dictionary<string, List<Inventory>> Inventories { get; private set; }

    private Dictionary<string, List<InventoryOfTypeCreated>> inventoryTypeCreated = new Dictionary<string, List<InventoryOfTypeCreated>>();

    public delegate bool InventoryOfTypeCreated(Inventory inventory);

    public event Action<Inventory> InventoryCreated;

    public InventoryManager()
    {
        Inventories = new Dictionary<string, List<Inventory>>();
    }

    public void RegisterInventoryTypeCreated(InventoryOfTypeCreated func, string type)
    {
        List<InventoryOfTypeCreated> inventories;
        if (inventoryTypeCreated.TryGetValue(type, out inventories) == false)
        {
            inventories = new List<InventoryOfTypeCreated>();
            inventoryTypeCreated[type] = inventories;
        }

        inventories.Add(func);
    }

    public void UnregisterInventoryTypeCreated(InventoryOfTypeCreated func, string type)
    {
        List<InventoryOfTypeCreated> list;
        if (inventoryTypeCreated.TryGetValue(type, out list))
        {
            list.Remove(func);
        }
    }

    void CleanupInventory(Inventory inv)
    {
        if (inv.StackSize == 0)
        {
            if (Inventories.ContainsKey(inv.Type))
            {
                Inventories[inv.Type].Remove(inv);
            }
            if (inv.Tile != null)
            {
                inv.Tile.Inventory = null;
                inv.Tile = null;
            }
            if (inv.actor != null)
            {
                inv.actor.Inventory = null;
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
            if (Inventories.ContainsKey(tile.Inventory.Type) == false)
            {
                Inventories[tile.Inventory.Type] = new List<Inventory>();
            }
            Inventories[tile.Inventory.Type].Add(tile.Inventory);

            InvokeInventoryCreated(tile.Inventory);
        }

        return true;
    }

    private void InvokeInventoryCreated(Inventory inventory)
    {
        Action<Inventory> handler = InventoryCreated;
        if (handler != null)
        {
            handler(inventory);

            InventoryAvailable(inventory);
        }
    }

    public void InventoryAvailable(Inventory inventory)
    {
        List<InventoryOfTypeCreated> inventories;
        if (inventoryTypeCreated.TryGetValue(inventory.Type, out inventories))
        {
            inventories.RemoveAll(func => func(inventory));
        }
    }

    public bool PlaceInventory(Job job, Inventory inv)
    {
        if (job.inventoryRequirements.ContainsKey(inv.Type) == false)
        {
            Debug.LogError("Trying to add inventory to a job that it doesn't want.");
            return false;
        }
        job.inventoryRequirements[inv.Type].StackSize += inv.StackSize;

        if (job.inventoryRequirements[inv.Type].MaxStackSize < job.inventoryRequirements[inv.Type].StackSize)
        {
            inv.StackSize = job.inventoryRequirements[inv.Type].StackSize - job.inventoryRequirements[inv.Type].MaxStackSize;
            job.inventoryRequirements[inv.Type].StackSize = job.inventoryRequirements[inv.Type].MaxStackSize;
        }
        else
        {
            inv.StackSize = 0;
        }

        CleanupInventory(inv);

        return true;
    }

    public bool PlaceInventory(Actor actor, Inventory sourceInventory, int amount = -1)
    {
        if (amount < 0)
        {
            amount = sourceInventory.StackSize;
        }
        else
        {
            amount = Mathf.Min(amount, sourceInventory.StackSize);
        }

        if (actor.Inventory == null)
        {
            actor.Inventory = sourceInventory.Clone();
            actor.Inventory.StackSize = 0;
            Inventories[actor.Inventory.Type].Add(actor.Inventory);
        }
        else if (actor.Inventory.Type != sourceInventory.Type)
        {
            Debug.LogError("Character is trying to pick up a mismatched inventory object type.");
            return false;
        }

        actor.Inventory.StackSize += amount;

        if (actor.Inventory.MaxStackSize < actor.Inventory.StackSize)
        {
            sourceInventory.StackSize = actor.Inventory.StackSize - actor.Inventory.MaxStackSize;
            actor.Inventory.StackSize = actor.Inventory.MaxStackSize;
        }
        else
        {
            sourceInventory.StackSize -= amount;
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
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount, bool canTakeFromStockpile)
    {
        // FIXME:
        //      a) We are LYING about returning the closest item.
        //      b) There's no way to return the closest item in an optimal manner
        //         until our "inventories" database is more sophisticated.
        //         (i.e. separate tile inventory from character inventory and maybe
        //          has room content optimization.)

        if (Inventories.ContainsKey(objectType) == false)
        {
            Debug.LogError("GetClosestInventoryOfType -- no items of desired type.");
            return null;
        }

        foreach (Inventory inv in Inventories[objectType])
        {
            if (inv.Tile != null &&
                (canTakeFromStockpile || inv.Tile.Structure == null || inv.Tile.Structure.IsStockpile() == false))
            {
                return inv;
            }
        }

        return null;
    }

    public List<Tile> GetPathToClosestInventoryOfType(string[] types, Tile tile, bool canTakeFromStockpile)
    {
        if (HasInventoryOfType(types, canTakeFromStockpile) == false)
        {
            return null;
        }

        // We know the objects are out there, now find the closest.
        return Pathfinder.FindPathToInventory(tile, types, canTakeFromStockpile);
    }

    public Inventory GetClosestInventoryOfType(string type, Tile tile, bool canTakeFromStockpile)
    {
        List<Tile> path = GetPathToClosestInventoryOfType(type, tile, canTakeFromStockpile);
        return path != null ? path.Last().Inventory : null;
    }

    public List<Tile> GetPathToClosestInventoryOfType(string type, Tile tile, bool canTakeFromStockpile)
    {
        if (HasInventoryOfType(type, canTakeFromStockpile) == false)
        {
            return null;
        }

        // We know the objects are out there, now find the closest.
        return Pathfinder.FindPathToInventory(tile, type, canTakeFromStockpile);
    }

    public bool HasInventoryOfType(string type, bool canTakeFromStockpile)
    {
        List<Inventory> inventories;
        if (Inventories.TryGetValue(type, out inventories) == false || inventories.Count == 0)
        {
            return false;
        }

        return inventories.Find(inventory => inventory.CanBePickedUp(canTakeFromStockpile)) != null;
    }

    public bool HasInventoryOfType(string[] types, bool canTakeFromStockpile)
    {
        foreach (string type in types)
        {
            if (HasInventoryOfType(type, canTakeFromStockpile))
            {
                return true;
            }
        }

        return false;
    }
}