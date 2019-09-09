using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectWildForge.Pathfinding;

public class DumpState : State
{
    public DumpState(Actor actor, State nextState = null)
        : base("Dump", actor, nextState)
    {
    }

    public override void Update(float deltaAuts)
    {
        Inventory tileInventory = actor.CurrTile.Inventory;

        // Current tile is empty
        if (tileInventory == null)
        {
            DebugLog(" - Dumping");
            World.Current.InventoryManager.PlaceInventory(actor.CurrTile, actor.Inventory);
            Finished();
            return;
        }

        // Current tile contains the same type and there is room
        if (tileInventory.Type == actor.Inventory.Type && (tileInventory.StackSize + actor.Inventory.StackSize) <= tileInventory.MaxStackSize)
        {
            DebugLog(" - Dumping");
            World.Current.InventoryManager.PlaceInventory(actor.CurrTile, actor.Inventory);
            Finished();
            return;
        }

        List<Tile> path = Pathfinder.FindPathToDumpInventory(actor.CurrTile, actor.Inventory.Type, actor.Inventory.StackSize);
        if (path != null && path.Count > 0)
        {
            actor.SetState(new MoveState(actor, Pathfinder.GoalTileEvaluator(path.Last(), false), path, this));
        }
        else
        {
            DebugLog(" - Can't find any place to dump inventory!");
            Finished();
        }
    }
}
