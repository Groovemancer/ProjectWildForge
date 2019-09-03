using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

[MoonSharpUserData]
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

    protected float jobCostRequired;

    protected bool jobRepeats = false;

    // FIXME: Hard-coding a parameter for structure. Do not like.
    public string jobObjectType
    {
        get; protected set;
    }

    public Structure structurePrototype;

    public Structure Structure; // The structure that owns this job. Frequently will be null.

    bool acceptsAnyInventoryItem = false;

    public bool canTakeFromStockpile = true;

    Action<Job> cbJobCompleted; // We have finished the work cycle and so things should probably get built or whatever.
    Action<Job> cbJobStopped;   // The job has been stopped, either because it's non-repeateing or was cancelled.
    Action<Job> cbJobWorked;    // Gets called each time some work is performed -- maybe update the UI?

    List<string> cbJobWorkedLua;
    List<string> cbJobCompletedLua;

    public Dictionary<string, Inventory> inventoryRequirements;

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobCost, Inventory[] invReqs, bool jobRepeats = false)
    {
        this.Tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobCompleted += cbJobComplete;
        this.jobCostRequired = this.jobCost = jobCost;
        this.jobRepeats = jobRepeats;

        this.cbJobWorkedLua = new List<string>();
        this.cbJobCompletedLua = new List<string>();

        inventoryRequirements = new Dictionary<string, Inventory>();

        if (invReqs != null)
        {
            foreach (Inventory inv in invReqs)
            {
                inventoryRequirements[inv.Type] = inv.Clone();
            }
        }
    }

    protected Job(Job other)
    {
        this.Tile = other.Tile;
        this.jobObjectType = other.jobObjectType;
        this.cbJobCompleted += other.cbJobCompleted;
        this.jobCost = other.jobCost;

        this.cbJobWorkedLua = new List<string>(other.cbJobWorkedLua);
        this.cbJobCompletedLua = new List<string>(other.cbJobCompletedLua);

        this.inventoryRequirements = new Dictionary<string, Inventory>();

        if (other.inventoryRequirements != null)
        {
            foreach (Inventory inv in other.inventoryRequirements.Values)
            {
                this.inventoryRequirements[inv.Type] = inv.Clone();
            }
        }
    }

    public Inventory[] GetInventoryRequirementValues()
    {
        return inventoryRequirements.Values.ToArray();
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    public void DoWork(float workCost)
    {
        // We don't know if the Job can actually be worked, but still call the callbacks
        // so that animations and whatnot can be updated.
        if (cbJobWorked != null)
        {
            cbJobWorked(this);
        }

        foreach (string luaFunction in cbJobWorkedLua.ToList())
        {
            //StructureActions.CallFunction(luaFunction, this);
            FunctionsManager.Structure.Call(luaFunction, this);
        }

        // Check to make sure we actually have everything we need.
        // If not, don't register the work cost.
        if (HasAllMaterial() == false)
        {
            //Debug.LogError("Tried to do work on a job that doesn't have all the materials.");
            return;
        }

        jobCost -= workCost;

        if (jobCost <= 0)
        {
            // Do whatever is supposed to happen when a job cycle completes.
            if (cbJobCompleted != null)
            {
                cbJobCompleted(this);
            }

            foreach (string luaFunction in cbJobCompletedLua.ToList())
            {
                //StructureActions.CallFunction(luaFunction, this);
                FunctionsManager.Structure.Call(luaFunction, this);
            }

            if (jobRepeats == false)
            {
                // Let everyone know that the job is officially concluded
                if (cbJobStopped != null)
                    cbJobStopped(this);
            }
            else
            {
                // This is a repeating job and must be reset.
                jobCost += jobCostRequired;
            }
        }
    }

    public void CancelJob()
    {
        if (cbJobStopped != null)
            cbJobStopped(this);

        World.Current.jobQueue.Remove(this);
    }

    public void RegisterJobCompletedCallback(Action<Job> cb)
    {
        cbJobCompleted += cb;
    }

    public void RegisterJobStoppedCallback(Action<Job> cb)
    {
        cbJobStopped += cb;
    }

    public void RegisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked += cb;
    }

    public void UnregisterJobCompletedCallback(Action<Job> cb)
    {
        cbJobCompleted -= cb;
    }

    public void UnregisterJobStoppedCallback(Action<Job> cb)
    {
        cbJobStopped -= cb;
    }

    public void UnregisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked -= cb;
    }

    public void RegisterJobWorkedCallback(string cb)
    {
        cbJobWorkedLua.Add(cb);
    }

    public void UnregisterJobWorkedCallback(string cb)
    {
        cbJobWorkedLua.Remove(cb);
    }

    public void RegisterJobCompletedCallback(string cb)
    {
        cbJobCompletedLua.Add(cb);
    }

    public void UnregisterJobCompletedCallback(string cb)
    {
        cbJobCompletedLua.Remove(cb);
    }

    public bool HasAllMaterial()
    {
        foreach(Inventory inv in inventoryRequirements.Values)
        {
            if (inv.MaxStackSize > inv.StackSize)
                return false;
        }

        return true;
    }

    public int DesiresInventoryType(Inventory inv)
    {
        if (acceptsAnyInventoryItem)
        {
            return inv.MaxStackSize;
        }

        if (inventoryRequirements.ContainsKey(inv.Type) == false)
        {
            return 0;
        }

        if (inventoryRequirements[inv.Type].StackSize >= inventoryRequirements[inv.Type].MaxStackSize)
        {
            // We already have all that we need!
            return 0;
        }

        // The inventory is of a type we want, and still need more
        return inventoryRequirements[inv.Type].MaxStackSize - inventoryRequirements[inv.Type].StackSize;
    }

    public Inventory GetFirstDesiredInventory()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.MaxStackSize > inv.StackSize)
            {
                return inv;
            }
        }

        return null;
    }
}