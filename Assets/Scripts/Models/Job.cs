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

    float jobCost;

    // FIXME: Hard-coding a parameter for structure. Do not like.
    public string jobObjectType
    {
        get; protected set;
    }

    Action<Job> cbJobComplete;
    Action<Job> cbJobCancel;

    Dictionary<string, Inventory> inventoryRequirements;

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
    }

    public void RegisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete += cb;
    }

    public void RegisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel += cb;
    }

    public void UnregisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete -= cb;
    }

    public void UnregisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel -= cb;
    }
}