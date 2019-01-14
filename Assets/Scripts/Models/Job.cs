using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Job
{
    // This class holds info for a queued up job, which can include
    // things like placing structures, moving stored loose items
    // working at a desk, and maybe even fighting enemies.

    public Tile Tile;// { get; protected set; }

    public float jobCost
    {
        get;
        protected set;
    }

    // FIXME: Hard-coding a parameter for structure. Do not like.
    public string jobObjectType
    {
        get; protected set;
    }

    bool acceptsAnyInventoryItem = false;

    Action<Job> cbJobComplete;
    Action<Job> cbJobCancel;
    Action<Job> cbJobWorked;

    public Dictionary<string, Inventory> inventoryRequirements;

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobCost, Inventory[] invReqs)
    {
        this.Tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete += cbJobComplete;
        this.jobCost = jobCost;

        inventoryRequirements = new Dictionary<string, Inventory>();

        if (invReqs != null)
        {
            foreach (Inventory inv in invReqs)
            {
                inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    protected Job(Job other)
    {
        this.Tile = other.Tile;
        this.jobObjectType = other.jobObjectType;
        this.cbJobComplete += other.cbJobComplete;
        this.jobCost = other.jobCost;

        this.inventoryRequirements = new Dictionary<string, Inventory>();

        if (other.inventoryRequirements != null)
        {
            foreach (Inventory inv in other.inventoryRequirements.Values)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    public void DoWork(float workCost)
    {
        jobCost -= workCost;
        Debug.Log("Remaining Job Cost: " + jobCost);

        if (cbJobWorked != null)
            cbJobWorked(this);

        if (jobCost <= 0)
        {
            if (cbJobComplete != null)
                cbJobComplete(this);
        }
    }

    public void CancelJob()
    {
        if (cbJobCancel != null)
            cbJobCancel(this);

        Tile.World.jobQueue.Remove(this);
    }

    public void RegisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete += cb;
    }

    public void RegisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel += cb;
    }

    public void RegisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked += cb;
    }

    public void UnregisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete -= cb;
    }

    public void UnregisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel -= cb;
    }

    public void UnregisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked -= cb;
    }

    public bool HasAllMaterial()
    {
        foreach(Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
                return false;
        }

        return true;
    }

    public int DesiresInventoryType(Inventory inv)
    {
        if (acceptsAnyInventoryItem)
        {
            return inv.maxStackSize;
        }

        if (inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            return 0;
        }

        if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize)
        {
            // We already have all that we need!
            return 0;
        }

        // The inventory is of a type we want, and still need more
        return inventoryRequirements[inv.objectType].maxStackSize - inventoryRequirements[inv.objectType].stackSize;
    }

    public Inventory GetFirstDesiredInventory()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return inv;
            }
        }

        return null;
    }
}