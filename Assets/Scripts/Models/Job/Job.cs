using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using ProjectWildForge.Pathfinding;

[MoonSharpUserData]
public class Job : ISelectable
{
    // This class holds info for a queued up job, which can include
    // things like placing structures, moving stored loose items
    // working at a desk, and maybe even fighting enemies.

    public Tile Tile;// { get; protected set; }

    public IBuildable buildablePrototype;

    // The structure that owns this job. Frequently will be null.
    public IBuildable Buildable;

    public float JobCost
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

    public string SkillType
    {
        get; protected set;
    }

    public bool canTakeFromStockpile = true;

    // We have finished the work cycle and so things should probably get built or whatever.
    public event Action<Job> OnJobCompleted;
    public event Action<Job> OnJobStopped;   // The job has been stopped, either because it's non-repeateing or was cancelled.
    public event Action<Job> OnJobWorked;    // Gets called each time some work is performed -- maybe update the UI?

    public enum JobPriority
    {
        High,
        Medium,
        Low
    }

    public enum JobState
    {
        Active,
        CantReach,
        MissingInventory,
        Suspended
    }

    /// <summary>
    /// If true the job is workable if ANY of the inventory requirements are met.
    /// Otherwise ALL requirements must be met before work can start.
    /// This is useful for stockpile/storage jobs which can accept many types of items.
    /// Defaults to false.
    /// </summary>
    public bool AcceptsAny { get; set; }

    /// <summary>
    /// If true, the work will be carried out on any adjacent tile of the target tile rather than on it.
    /// </summary>
    public bool Adjacent { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; protected set; }

    /// <summary>
    /// Name of order that created this job. This should prevent multiple same orders on same things if not allowed.
    /// </summary>
    public string OrderName { get; set; }

    public string Type
    {
        get;
        protected set;
    }

    public bool IsNeed
    {
        get;
        protected set;
    }

    public bool Critical
    {
        get;
        protected set;
    }

    public bool IsBeingWorked { get; set; }

    public TileType JobTileType
    {
        get;
        protected set;
    }

    public JobPriority Priority
    {
        get;
        set;
    }

    public JobCategory Category
    {
        get;
        set;
    }

    public bool IsRepeating
    {
        get
        {
            return jobRepeats;
        }
    }

    public int ActorsCantReachCount
    {
        get
        {
            return actorsCantReach.Count;
        }
    }

    public Pathfinder.GoalEvaluator IsTileAtJobSite
    {
        get
        {
            if (Tile == null)
            {
                return null;
            }

            // TODO: This doesn't handle multi-tile structure
            return Pathfinder.GoalTileEvaluator(Tile, Adjacent);
        }
    }

    List<string> cbJobWorkedLua;
    List<string> cbJobCompletedLua;

    // The items needed to do this job.
    public Dictionary<string, RequestedItem> RequestedItems { get; set; }

    // The items that have been delivered to the jobsite.
    public Dictionary<string, Inventory> DeliveredItems { get; set; }

    public bool IsSelected
    {
        get; set;
    }

    private List<Actor> actorsCantReach = new List<Actor>();

    public Job(Tile tile, string type, Action<Job> jobComplete, float jobCost, RequestedItem[] requestedItems, Job.JobPriority jobPriority, string category, string jobSkillType, bool jobRepeats = false, bool need = false, bool critical = false, bool adjacent = false) :
        this(tile, type, jobComplete, jobCost, requestedItems, jobPriority, PrototypeManager.JobCategory.Get(category), jobSkillType, jobRepeats, need, critical, adjacent)
    {
        // This is identical to the next structure, except the category is a string. Intended primarily for Lua
    }

    public Job(Tile tile, string type, Action<Job> jobComplete, float jobCost, RequestedItem[] requestedItems, Job.JobPriority jobPriority, JobCategory category, string jobSkillType, bool jobRepeats = false, bool need = false, bool critical = false, bool adjacent = false)
    {
        this.Tile = tile;
        this.Type = type;
        this.OnJobCompleted += jobComplete;
        this.jobCostRequired = this.JobCost = jobCost;
        this.jobRepeats = jobRepeats;
        this.IsNeed = need;
        this.Critical = critical;
        this.Priority = jobPriority;
        this.Category = category;
        this.Adjacent = adjacent;
        this.IsActive = true;
        this.Description = "job_error_missing_desc";
        this.SkillType = jobSkillType;

        this.cbJobWorkedLua = new List<string>();
        this.cbJobCompletedLua = new List<string>();

        this.DeliveredItems = new Dictionary<string, Inventory>();
        this.RequestedItems = new Dictionary<string, RequestedItem>();

        if (requestedItems != null)
        {
            foreach (RequestedItem item in requestedItems)
            {
                this.RequestedItems[item.Type] = item.Clone();
            }
        }

        if (this.Category == null)
        {
            DebugUtils.LogError("Invalid category detected.");
        }
    }

    public Job(Tile tile, TileType jobTileType, Action<Job> jobCompleted, float jobCost, RequestedItem[] requestedItems, Job.JobPriority jobPriority, string category, string jobSkillType, bool jobRepeats = false, bool adjacent = false)
    {
        this.Tile = tile;
        this.JobTileType = jobTileType;
        this.Type = jobTileType.NameLocaleId;
        this.OnJobCompleted += jobCompleted;
        this.jobCostRequired = this.JobCost = jobCost;
        this.jobRepeats = jobRepeats;
        this.Priority = jobPriority;
        this.Category = PrototypeManager.JobCategory.Get(category);
        this.Adjacent = adjacent;
        this.Description = "job_error_missing_desc";
        this.IsActive = true;
        this.SkillType = jobSkillType;

        this.cbJobWorkedLua = new List<string>();
        this.cbJobCompletedLua = new List<string>();

        this.DeliveredItems = new Dictionary<string, Inventory>();
        this.RequestedItems = new Dictionary<string, RequestedItem>();

        if (requestedItems != null)
        {
            foreach (RequestedItem item in requestedItems)
            {
                this.RequestedItems[item.Type] = item.Clone();
            }
        }

        if (this.Category == null)
        {
            DebugUtils.LogError("Invalid category detected.");
        }
    }

    protected Job(Job other)
    {
        this.Tile = other.Tile;
        this.jobObjectType = other.jobObjectType;
        this.OnJobCompleted += other.OnJobCompleted;
        this.JobCost = other.JobCost;
        this.Priority = other.Priority;
        this.Category = other.Category;
        this.Adjacent = other.Adjacent;
        this.AcceptsAny = other.AcceptsAny;
        this.Description = other.Description;
        this.OrderName = other.OrderName;
        this.IsActive = true; // A copied job should always start out as active.
        this.SkillType = other.SkillType;

        this.cbJobWorkedLua = new List<string>(other.cbJobWorkedLua);
        this.cbJobCompletedLua = new List<string>(other.cbJobCompletedLua);

        this.DeliveredItems = new Dictionary<string, Inventory>();
        this.RequestedItems = new Dictionary<string, RequestedItem>();
        if (other.RequestedItems != null)
        {
            foreach (RequestedItem item in other.RequestedItems.Values)
            {
                this.RequestedItems[item.Type] = item.Clone();
            }
        }
    }

    public RequestedItem[] GetInventoryRequirementValues()
    {
        return RequestedItems.Values.ToArray();
    }

    public void SetTileFromNeedStructure(Tile currentTile, string needStructure)
    {
        Tile = Pathfinder.FindPathToStructure(currentTile, needStructure).Last();
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    public void DoWork(float workCost)
    {
        // We don't know if the Job can actually be worked, but still call the callbacks
        // so that animations and whatnot can be updated.
        if (OnJobWorked != null)
        {
            OnJobWorked(this);
        }

        foreach (string luaFunction in cbJobWorkedLua.ToList())
        {
            FunctionsManager.Structure.Call(luaFunction, this);
        }

        // Check to make sure we actually have everything we need.
        // If not, don't register the work cost.
        if (HasAllMaterial() == false)
        {
            //Debug.LogError("Tried to do work on a job that doesn't have all the materials.");
            return;
        }

        JobCost -= workCost;

        if (JobCost <= 0)
        {
            foreach (string luaFunction in cbJobCompletedLua.ToList())
            {
                //StructureActions.CallFunction(luaFunction, this);
                FunctionsManager.Structure.Call(luaFunction, this);
            }

            // Do whatever is supposed to happen when a job cycle completes.
            if (OnJobCompleted != null)
            {
                OnJobCompleted(this);
            }

            World.Current.JobManager.Remove(this);

            if (jobRepeats != true)
            {
                // Let everyone know that the job is officially concluded
                if (OnJobStopped != null)
                {
                    OnJobStopped(this);
                }
            }
            else
            {
                // This is a repeating job and must be reset.
                JobCost += jobCostRequired;
            }
        }
    }

    public void SuspendCantReach()
    {
        World.Current.RoomManager.Removed += (room) => ClearActorsCantReach();
        Tile.TileChanged += (tile) => ClearActorsCantReach();
        Suspend();
    }

    public void SuspendWaitingForInventory(string missing)
    {
        if (missing == "*")
        {
            World.Current.InventoryManager.InventoryCreated += InventoryAvailable;
        }
        else
        {
            World.Current.InventoryManager.RegisterInventoryTypeCreated(CheckIfInventorySufficient, missing);
        }

        Suspend();
    }

    public void InventoryAvailable(Inventory inventory)
    {
        IsActive = true;
        World.Current.InventoryManager.InventoryCreated -= InventoryAvailable;
        World.Current.InventoryManager.UnregisterInventoryTypeCreated(CheckIfInventorySufficient, inventory.Type);
    }

    public bool CheckIfInventorySufficient(Inventory inventory)
    {
        RequestedItem item = GetFirstDesiredItem();
        if (item.Type == inventory.Type)
        {
            IsActive = true;
            return true;
        }

        return false;
    }

    public void CancelJob()
    {
        if (OnJobStopped != null)
            OnJobStopped(this);

        // If we are a building job let our tile know we are no longer pending
        if (buildablePrototype != null)
        {
            // If we are a structure building job, Let our workspot tile know it is no longer reserved for us.
            if (buildablePrototype.GetType() == typeof(Structure))
            {
                World.Current.UnreserveTileAsWorkSpot((Structure)buildablePrototype, Tile);
            }
        }

        World.Current.JobManager.Remove(this);
    }

    public void RegisterJobCompletedCallback(Action<Job> cb)
    {
        OnJobCompleted += cb;
    }

    public void RegisterJobStoppedCallback(Action<Job> cb)
    {
        OnJobStopped += cb;
    }

    public void RegisterJobWorkedCallback(Action<Job> cb)
    {
        OnJobWorked += cb;
    }

    public void UnregisterJobCompletedCallback(Action<Job> cb)
    {
        OnJobCompleted -= cb;
    }

    public void UnregisterJobStoppedCallback(Action<Job> cb)
    {
        OnJobStopped -= cb;
    }

    public void UnregisterJobWorkedCallback(Action<Job> cb)
    {
        OnJobWorked -= cb;
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

    public void AddActorCantReach(Actor actor)
    {
        if (!actorsCantReach.Contains(actor))
        {
            actorsCantReach.Add(actor);
        }
    }

    public bool CanActorReach(Actor actor)
    {
        return actorsCantReach.Contains(actor) == false;
    }

    public bool CanGetToInventory(Actor actor)
    {
        List<Tile> path = null;
        path = World.Current.InventoryManager.GetPathToClosestInventoryOfType(RequestedItems.Keys.ToArray(), actor.CurrTile, canTakeFromStockpile);
        if (path != null && path.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ClearActorsCantReach()
    {
        World.Current.RoomManager.Removed -= (room) => ClearActorsCantReach();
        if (Tile != null)
        {
            Tile.TileChanged -= (tile) => ClearActorsCantReach();
        }

        actorsCantReach.Clear();
        IsActive = true;
    }

    /// <summary>
    /// Checks to see if the job has met the material requirements needed to do this job.
    /// </summary>
    /// <returns> Returns True if the job can do work based on material requirements.</returns>
    public bool MaterialNeedsMet()
    {
        if (AcceptsAny && HasAnyMaterial())
        {
            return true;
        }

        if ((AcceptsAny == false) && HasAllMaterial())
        {
            return true;
        }

        return false;
    }

    public bool HasAllMaterial()
    {
        if (RequestedItems == null)
        {
            return true;
        }

        foreach (RequestedItem item in RequestedItems.Values)
        {
            Inventory inventory;
            if (DeliveredItems.TryGetValue(item.Type, out inventory) == false || item.AmountNeeded(inventory) > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks to see if a job can run, and suspends if it can't.
    /// </summary>
    /// <param name="actorRoom">Room actor is in.</param>
    /// <param name="changeState">If true allows changing the state.</param>
    /// <returns>true if the job can be run.</returns>
    public JobState CanJobRun(Room actorRoom, bool changeState)
    {
        // If the job requires material but there is nothing available, store it in jobsWaitingForInventory
        if (RequestedItems.Count > 0 && GetFirstFulfillableInventoryRequirement() == null)
        {
            if (changeState)
            {
                string missing = AcceptsAny ? "*" : GetFirstDesiredItem().Type;
                SuspendWaitingForInventory(missing);
            }

            return JobState.MissingInventory;
        }
        else if (Tile != null)
        {
            if (((Adjacent == false && Tile.IsEnterable() != Enterability.Never) ||
                (Adjacent && Tile.IsReachableFromAnyNeighbor(false))) &&
                Tile.CanSee)
            {
                HashSet<Room> roomsToCheck = new HashSet<Room>();
                if (Tile.Room != null)
                {
                    roomsToCheck.Add(Tile.Room);
                }

                foreach (Tile neighbor in Tile.GetNeighbors(false))
                {
                    if (neighbor.Room != null && roomsToCheck.Contains(neighbor.Room) == false)
                    {
                        roomsToCheck.Add(neighbor.Room);
                    }
                }

                if (CanReachRoom(roomsToCheck, actorRoom))
                {
                    return JobState.Active;
                }
            }

            if (changeState)
            {
                // No one can reach the job.
                SuspendCantReach();
            }

            return JobState.CantReach;
        }

        return JobState.Active;
    }

    private bool CanReachRoom(HashSet<Room> roomsToCheck, Room actorRoom)
    {
        if (Pathfinder.IsRoomReachable(actorRoom, roomsToCheck) == false)
        {
            return false;
        }

        return true;
    }

    public bool HasAnyMaterial()
    {
        return DeliveredItems.Count > 0 && DeliveredItems.First().Value.StackSize > 0;
    }

    public int AmountDesiredOfInventoryType(string type)
    {
        RequestedItem requestedItem;
        if (RequestedItems.TryGetValue(type, out requestedItem) == false)
        {
            return 0;
        }

        Inventory inventory;
        if (DeliveredItems.TryGetValue(type, out inventory) == false)
        {
            inventory = null;
        }

        return requestedItem.AmountDesired(inventory);
    }

    public bool IsRequiredInventoriesAvailable()
    {
        return FulfillableInventoryRequirements() != null;
    }

    /// <summary>
    /// Returns the first fulfillable requirement of this job. Especially useful for jobs that has a long list of materials and can use any of them.
    /// </summary>
    public RequestedItem GetFirstFulfillableInventoryRequirement()
    {
        foreach (RequestedItem item in GetInventoryRequirementValues())
        {
            if (World.Current.InventoryManager.HasInventoryOfType(item.Type, canTakeFromStockpile))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Fulfillable inventory requirements for job.
    /// </summary>
    /// <returns>A list of (string) Type for job inventory requirements that can be met. Returns null if the job requires materials which do not exist on the map.</returns>
    public List<string> FulfillableInventoryRequirements()
    {
        List<string> fulfillableInventoryRequirements = new List<string>();

        foreach (RequestedItem item in this.GetInventoryRequirementValues())
        {
            if (this.AcceptsAny == false)
            {
                if (World.Current.InventoryManager.HasInventoryOfType(item.Type, canTakeFromStockpile) == false)
                {
                    // the job requires ALL inventory requirements to be met, and there is no source of a desired Type
                    return null;
                }
                else
                {
                    fulfillableInventoryRequirements.Add(item.Type);
                }
            }
            else if (World.Current.InventoryManager.HasInventoryOfType(item.Type, canTakeFromStockpile))
            {
                // there is a source for a desired Type that the job will accept
                fulfillableInventoryRequirements.Add(item.Type);
            }
        }

        return fulfillableInventoryRequirements;
    }

    public RequestedItem GetFirstDesiredItem()
    {
        foreach (RequestedItem item in RequestedItems.Values)
        {
            Inventory inventory;
            if (DeliveredItems.TryGetValue(item.Type, out inventory) == false)
            {
                inventory = null;
            }

            if (item.DesiresMore(inventory))
            {
                return item;
            }
        }

        return null;
    }

    private void Suspend()
    {
        DebugUtils.LogChannel("Job", "Job suspended!");
        IsActive = false;
    }

    public string GetName()
    {
        try
        {
            return PrototypeManager.Structure.Get(Type).GetName();
        }
        catch
        {
            return StringUtils.GetLocalizedTextFiltered("comment#" + Type);
        }
    }

    public string GetDescription()
    {
        string description = StringUtils.GetLocalizedTextFiltered("#comments:job_requirements");
        foreach (RequestedItem item in RequestedItems.Values)
        {
            string itemDesc = string.Format("\t{0} {1}..{2}\n", PrototypeManager.Inventory.Get(item.Type).GetName(),
                item.MinAmountRequested, item.MaxAmountRequested);

            // TODO: Not sure if this works or not.
            description += StringUtils.GetLocalizedTextFiltered(itemDesc);
        }

        return description;
    }

    public string GetJobDescription()
    {
        return GetDescription();
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        yield break;
    }
}