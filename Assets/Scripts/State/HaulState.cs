using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectWildForge.Pathfinding;
using UnityEngine;

public enum HaulAction
{
    DumpMaterial,
    FindMaterial,
    PickupMaterial,
    DeliverMaterial,
    DropOffMaterial
}

public class HaulState : State
{
    private bool noMoreMaterialFound = false;

    private int totalDistance = 0;

    private const float EXP_PER_DISTANCE = 1.5f;

    public HaulState(Actor actor, Job job, State nextState = null)
        : base("Haul", actor, nextState)
    {
        Job = job;
    }

    private Job Job { get; set; }

    public override void Update(float deltaAuts)
    {
        List<Tile> path = null;
        HaulAction nextAction = NextAction();

        //DebugLog(" - next action: {0}", nextAction);

        switch (nextAction)
        {
            case HaulAction.DumpMaterial:
                actor.SetState(new DumpState(actor, this));
                break;

            case HaulAction.FindMaterial:
                // Find material somewhere
                string[] inventoryTypes = actor.Inventory != null ?
                    new string[] { actor.Inventory.Type } :
                    Job.RequestedItems.Keys.ToArray();
                path = World.Current.InventoryManager.GetPathToClosestInventoryOfType(inventoryTypes, actor.CurrTile, Job.canTakeFromStockpile);
                if (path != null && path.Count > 0)
                {
                    Inventory inv = path.Last().Inventory;
                    inv.Claim(actor, (inv.AvailableInventory < Job.RequestedItems[inv.Type].AmountDesired()) ? inv.AvailableInventory : Job.RequestedItems[inv.Type].AmountDesired());
                    actor.SetState(new MoveState(actor, Pathfinder.GoalTileEvaluator(path.Last(), false), path, this));
                }
                else if (actor.Inventory == null)
                {
                    // The actor has no inventory and can't find anything to haul.
                    Interrupt();
                    DebugLog(" - Nothing to haul");
                    Finished();
                }
                else
                {
                    noMoreMaterialFound = true;
                }

                break;

            case HaulAction.PickupMaterial:
                Inventory tileInventory = actor.CurrTile.Inventory;
                int amountCarried = actor.Inventory != null ? actor.Inventory.StackSize : 0;
                int amount = Mathf.Min(Job.AmountDesiredOfInventoryType(tileInventory.Type) - amountCarried, tileInventory.StackSize);
                //DebugLog(" - Picked up {0} {1}", amount, tileInventory.Type);
                World.Current.InventoryManager.PlaceInventory(actor, tileInventory, amount);
                break;

            case HaulAction.DeliverMaterial:
                path = Pathfinder.FindPath(actor.CurrTile, Job.IsTileAtJobSite, Pathfinder.DefaultDistanceHeuristic(Job.Tile));
                totalDistance = path.Count;
                if (path != null && path.Count > 0)
                {
                    actor.SetState(new MoveState(actor, Pathfinder.GoalTileEvaluator(path.Last(), false), path, this));
                }
                else
                {
                    Job.AddActorCantReach(actor);
                    actor.InterruptState();
                }

                break;

            case HaulAction.DropOffMaterial:
                //DebugLog(" - Delivering {0} {1}", actor.Inventory.StackSize, actor.Inventory.Type);
                World.Current.InventoryManager.PlaceInventory(Job, actor);
                actor.GainSkillExperience("Hauling", totalDistance * EXP_PER_DISTANCE);

                // Ping the job system
                Job.DoWork(0);
                Finished();
                break;
        }
    }

    private HaulAction NextAction()
    {
        Inventory tileInventory = actor.CurrTile.Inventory;
        bool jobWantsTileInventory = InventoryManager.CanBePickedUp(tileInventory, Job.canTakeFromStockpile) &&
                                        Job.AmountDesiredOfInventoryType(tileInventory.Type) > 0;

        if (noMoreMaterialFound && actor.Inventory != null)
        {
            return Job.IsTileAtJobSite(actor.CurrTile) ? HaulAction.DropOffMaterial : HaulAction.DeliverMaterial;
        }
        else if (actor.Inventory != null && Job.AmountDesiredOfInventoryType(actor.Inventory.Type) == 0)
        {
            return HaulAction.DumpMaterial;
        }
        else if (actor.Inventory == null)
        {
            return jobWantsTileInventory ? HaulAction.PickupMaterial : HaulAction.FindMaterial;
        }
        else
        {
            int amountWanted = Job.AmountDesiredOfInventoryType(actor.Inventory.Type);
            int currentlyCarrying = actor.Inventory.StackSize;
            int spaceAvailable = actor.Inventory.MaxStackSize - currentlyCarrying;

            // Already carrying it
            if (amountWanted <= currentlyCarrying || spaceAvailable == 0)
            {
                return Job.IsTileAtJobSite(actor.CurrTile) ? HaulAction.DropOffMaterial : HaulAction.DeliverMaterial;
            }
            else
            {
                // Can carry more and want more
                return jobWantsTileInventory ? HaulAction.PickupMaterial : HaulAction.FindMaterial;
            }
        }
    }
}