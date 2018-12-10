using System.Collections.Generic;

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

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        bool tileWasEmpty = tile.Inventory == null;

        if (tile.PlaceInventory(inv) == false)
        {
            // The tile did not accept the inventory for whatever reason, therefor stop.
            return false;
        }

        // At this point, "inv" might be an empty stack if it was merged to another stack.
        if (inv.stackSize == 0)
        {
            if (inventories.ContainsKey(tile.Inventory.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }
        }

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
}