using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ProjectWildForge.Pathfinding;
using System.Linq;

public class Actor : IXmlSerializable, IUpdatable
{
    /// Unique ID of the actor
    public readonly int Id;

    /// What Id we currently are sitting at
    private static int currentId = 0;

    public Bounds Bounds
    {
        get
        {
            return new Bounds(
                new Vector3(X - 1, Y - 1, 0),
                new Vector3(1, 1));
        }
    }

    private Tile currTile;
    public Tile CurrTile
    {
        get
        {
            return currTile;
        }
        set
        {
            if (currTile != null)
            {
                //currTile.Actors.Remove(this);
            }
            currTile = value;
            //currTile.Actors.Add(this);

            TileOffset = Vector3.zero;
        }
    }

    // If we aren't moving, then destTile = CurrTile
    Tile _destTile;
    Tile destTile
    {
        get { return _destTile; }
        set
        {
            if (_destTile != value)
            {
                _destTile = value;
            }
        }
    }
    Tile nextTile;

    /// Tile offset for animation
    public Vector3 TileOffset { get; set; }

    /// <summary>
    /// Returns a float representing the Character's X position, which can
    /// be part-way between two tiles during movement.
    /// </summary>
    public float X
    {
        get
        {
            return CurrTile.X + TileOffset.x;
        }
    }

    /// <summary>
    /// Returns a float representing the Character's Y position, which can
    /// be part-way between two tiles during movement.
    /// </summary>
    public float Y
    {
        get
        {
            return CurrTile.Y + TileOffset.y;
        }
    }

    /// <summary>
    /// Returns a float representing the Character's Z position, which can
    /// be part-way between two tiles during movement.
    /// </summary>
    public float Z
    {
        get
        {
            return CurrTile.Z + TileOffset.z;
        }
    }

    public List<Tile> Path { get; set; }

    // Amount of action points required to move 1 tile.
    private float movementCost = 25f; 
    private float baseMovementCost = 25f;

    /// Tiles per second.
    public float MovementCost
    {
        get
        {
            return movementCost;
        }
    }

    // The current state
    private State state;

    // List of global states that always run
    private List<State> globalStates;

    // Queue of states that aren't important enough to interrupt, but should run soon
    private Queue<State> stateQueue;

    float workCost = 25f;   // Amount of action points required to do any amount of work
    private float workRate;   // Amount of work completed per DoWork attempt
    private float baseWorkRate = 50f;   // Amount of work completed per DoWork attempt

    public float WorkRate
    {
        get { return workRate; }
    }

    public float ActionPoints { get; set; }
    public bool Acted { get; set; }

    /// A callback to trigger when actor information changes (notably, the position).
    public event Action<Actor> OnActorChanged;

    Job myJob;

    public string Name { get; protected set; }

    public Race Race { get; protected set; }

    public string SpriteName { get; set; }

    public bool IsFemale { get; set; }

    // The item we are carrying (not gear/equipment)
    public Inventory Inventory { get; set; }

    /// Stats, for character.
    public Dictionary<string, Stat> Stats { get; protected set; }

    public Actor()
    {
        // Use only for serialization
        InitializeActorValues();
        Id = currentId++;
        IsFemale = false;
    }

    public Actor(Tile tile, string name, string race, bool isFemale)
    {
        CurrTile = destTile = tile;
        Name = name;
        Race = PrototypeManager.Race.Get(race);
        IsFemale = isFemale;
        InitializeActorValues();

        if (IsFemale)
        {
            SpriteName = RandomUtils.ObjectFromList(Race.FemaleSprites, string.Empty);
        }
        else
        {
            SpriteName = RandomUtils.ObjectFromList(Race.MaleSprites, string.Empty);
        }

        stateQueue = new Queue<State>();
        globalStates = new List<State>
        {
            // TODO Add NeedState
            //new NeedState(this);
        };
        Id = currentId++;
    }

    private void InitializeActorValues()
    {
        LoadStats();
        UseStats();
    }

    public string GetName()
    {
        return Name;
    }

    private void LoadStats()
    {
        Stats = new Dictionary<string, Stat>(PrototypeManager.Stat.Count);
        for (int i = 0; i < PrototypeManager.Stat.Count; i++)
        {
            Stat prototypeStat = PrototypeManager.Stat.Values[i];
            Stat newStat = prototypeStat.Clone();

            Stat raceStat = Race.StatModifiers.Find(stat => stat.Type == newStat.Type);
            int raceValue = (raceStat != null) ? raceStat.Value : 0;

            // Gets a random value within the min and max range of the stat.
            // TODO: Should there be any bias or any other algorithm applied here to make stats more interesting?
            newStat.Value = Math.Max(StatRange(raceValue), 1);
            Stats.Add(newStat.Type, newStat);

            DebugUtils.LogChannel("Actor", "Stat: " + newStat.ToString());
        }

        DebugUtils.LogChannel("Actor", "Initialized " + Stats.Count + " Stats.");
    }

    private int StatRange(int raceMod)
    {
        int val = raceMod;

        for (int i = 0; i < 3; i++)
        {
            val += RandomUtils.Range(1, 9); // 8
        }
        return val;
    }

    private void UseStats()
    {
        try
        {
            movementCost = baseMovementCost - (0.3f * baseMovementCost * ((float)Stats["Agility"].Value - 10) / 10); // +/- 30%
            workRate = baseWorkRate + (0.5f * baseWorkRate * ((float)Stats["Dexterity"].Value - 10) / 10); // +/- 50%

            DebugUtils.LogChannel("Actor", string.Format("Actor {0} movementCost: {1}", Id, movementCost));
            DebugUtils.LogChannel("Actor", string.Format("Actor {0} workRate: {1}", Id, workRate));
        }
        catch (KeyNotFoundException)
        {
            DebugUtils.LogError("Stat keys not found. If not testing, this is really bad!");
        }
    }

    void GetNewJob()
    {
        myJob = World.Current.jobQueue.Dequeue();
        if (myJob == null)
            return;

        destTile = myJob.Tile;
        myJob.RegisterJobStoppedCallback(OnJobStopped);

        // Immediately check to see if the job tile is reachable.
        // NOTE: We might not be pathing to it right away (due to
        // requiring materials), but we still need to verify that
        // the final location can be reached.

        //pathAStar = new Path_AStar(World.Current, CurrTile, destTile); // This will calculate a path from curr to dest.
        Path = Pathfinder.FindPathToTile(CurrTile, destTile);
        //if (pathAStar.Length() == 0)
        if (Path.Count == 0)
        {
            Debug.LogError("PathAStar returned no path to target job tile!");
            AbandonJob();
            destTile = CurrTile;
        }
    }

    bool UpdateDoJob(float deltaAuts)
    {
        // Do I have a job?
        if (myJob == null)
        {
            GetNewJob();

            if (myJob == null)
            {
                // There was no job on the queue for us, so just return.
                destTile = CurrTile;
                return false;
            }
        }

        // We have a job! (And the job tile is reachable)

        // STEP 1: Does the job have all the materials it needs?
        if (myJob.HasAllMaterial() == false)
        {
            // No, we are missing something!

            // STEP 2: Are we CARRYING anything that the job location wants?
            if (Inventory != null)
            {
                if (myJob.DesiresInventoryType(Inventory) > 0)
                {
                    // If so, deliver the goods.
                    //  Walk to the job tile, then drop off the stack into the job.

                    if (CurrTile == myJob.Tile)
                    {
                        // We are at the job's site, so drop the inventory
                        World.Current.InventoryManager.PlaceInventory(myJob, Inventory);
                        myJob.DoWork(0); // This will call all cbJobWorked callbacks, because even though
                                        // we aren't progressing, it might want to do something with the fact
                                        // that the requirements are being met.

                        // Are we still carrying things?
                        if (Inventory.StackSize == 0)
                        {
                            Inventory = null;
                        }
                        else
                        {
                            Debug.LogError("Character is still carrying inventory, which shouldn't be. Just setting to null for now, but this means we are LEAKING inventory.");
                            Inventory = null;
                        }
                    }
                    else
                    {
                        // We still need to walk to the job site.
                        destTile = myJob.Tile;
                        return false;
                    }
                }
                else
                {
                    // We are carrying something, but the job doesn't want it!
                    // Dump the inventory at our feet
                    // TODO: Actually, walk to the nearest empty tile and dump it there.
                    if (World.Current.InventoryManager.PlaceInventory(CurrTile, Inventory) == false)
                    {
                        Debug.LogError("Character tried to dump inventory into an invalid tile (maybe there's already something here.");
                        // FIXME: For the sake of continuing on, we are still going to dump any
                        // reference to the current inventory, but this means we are "leaking"
                        // inventory. This is permanently lost now.
                        Inventory = null;
                    }
                }
            }
            else
            {
                // At this point, the job still requires inventory, but we aren't carrying it!

                // Are we standing on a tile with goods that are desired by the job?

                if (CurrTile.Inventory != null &&
                    (myJob.canTakeFromStockpile || CurrTile.Structure == null || CurrTile.Structure.IsStockpile() == false) &&
                    myJob.DesiresInventoryType(CurrTile.Inventory) > 0)
                {
                    // Pick up the stuff!
                    World.Current.InventoryManager.PlaceInventory(
                        this,
                        CurrTile.Inventory,
                        myJob.DesiresInventoryType(CurrTile.Inventory)
                    );
                }
                else
                {
                    // Walk towards a tile containing the required goods.

                    // Find the first thing in the job that isn't satisfied.
                    Inventory desired = myJob.GetFirstDesiredInventory();

                    Inventory supplier = World.Current.InventoryManager.GetClosestInventoryOfType(
                        desired.Type,
                        CurrTile,
                        desired.MaxStackSize - desired.StackSize,
                        myJob.canTakeFromStockpile
                    );

                    if (supplier == null)
                    {
                        Debug.Log("No tile contains objects of type'" + desired.Type + "' to satisfy job requirements.");
                        AbandonJob();
                        return false;
                    }

                    destTile = supplier.Tile;
                    return false;
                }
            }
            return false; // We can't continue until all materials are satisfied.
        }

        // If we get here, then the job has all the material that it needs.
        // Lets make sure that our destination tile is the job site tile.
        destTile = myJob.Tile;

        // We are at our destination
        if (CurrTile == destTile)
        {
            // We are at the correct tile for our job, so
            // execute the job's "DoWork", which is mostly
            // going to countdown jobTime and potentially
            // call its "Job Complete" callback.
            if (myJob != null)
            {
                if (ActionPoints >= workCost)
                {
                    myJob.DoWork(workRate);
                    ActionPoints -= workCost;
                }
                return true;
            }
        }

        // Nothing left for us to do here, we mostly just need Update_DoMovement to
        // get us where we want to go.

        return false;
    }

    // AUTs are "Arbitrary Unit of Time", e.g. 100 AUT/s means every 1 second 100 AUTs pass
    public void EveryFrameUpdate(float deltaAuts)
    {
        Acted = false;
        ActionPoints += deltaAuts;
        
        // Run all the global states first so that they can interrupt or queue up new states
        foreach (State globalState in globalStates)
        {
            globalState.Update(deltaAuts);
        }

        // We finished the last state
        if (state == null)
        {
            if (stateQueue.Count > 0)
            {
                SetState(stateQueue.Dequeue());
            }
            else
            {
                Job job = null;// = World.Current.jobManager.GetJob(this);
                if (job != null)
                {
                    //SetState(new JobState(this, job));
                }
                else
                {
                    // TODO: Lack of job states should be more interesting. Maybe go to the pub and have a pint?
                    SetState(new IdleState(this));
                }
            }
        }

        state.Update(deltaAuts);

        //Animation.Update(deltaAuts);

        if (Acted == false)
            ActionPoints -= deltaAuts;

        if (OnActorChanged != null)
        {
            OnActorChanged(this);
        }

        //DebugUtils.LogChannel("Actor", "EveryFrameUpdate called deltaAuts: " + deltaAuts);
        /*
        ActionPoints += deltaAuts;

        if (UpdateDoJob(deltaAuts))
            Acted = true;

        if (UpdateHandleMovement())
            Acted = true;

        if (Acted == false)
            ActionPoints -= deltaAuts;

        if (cbActorChanged != null)
            cbActorChanged(this);
        */
    }

    public void FixedFrequencyUpdate(float deltaAuts)
    {
        throw new InvalidOperationException("Not supported by this class");
    }

    // AUTs are "Arbitrary Unit of Time", e.g. 100 AUT/s means every 1 second 100 AUTs pass
    public void Update(float deltaAuts)
    {
        return;
        DebugUtils.LogChannel("Actor", "Update called deltaAuts: " + deltaAuts);
        bool didSomething = false;
        ActionPoints += deltaAuts;

        if (UpdateDoJob(deltaAuts))
            didSomething = true;

        if (didSomething == false)
            ActionPoints -= deltaAuts;

        if (OnActorChanged != null)
            OnActorChanged(this);
    }

    public void AbandonJob()
    {
        Debug.Log("Abandon Job");
        nextTile = destTile = CurrTile;
        myJob.UnregisterJobStoppedCallback(OnJobStopped);
        myJob.UnregisterJobCompletedCallback(OnJobStopped);
        World.Current.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    #region State

    public void PrioritizeJob(Job job)
    {
        if (state != null)
        {
            state.Interrupt();
        }

        //SetState(new JobState(this, job));
    }

    /// <summary>
    /// Stops the current state. Makes the character halt what is going on and start looking for something new to do, might be the same thing.
    /// </summary>
    public void InterruptState()
    {
        if (state != null)
        {
            state.Interrupt();

            // We can't use SetState(null), because it runs Exit on the state and we don't want to run both Interrupt and Exit.
            state = null;
        }
    }

    /// <summary>
    /// Removes all the queued up states.
    /// </summary>
    public void ClearStateQueue()
    {
        // If we interrupt, we get rid of the queue as well.
        while (stateQueue.Count > 0)
        {
            State queuedState = stateQueue.Dequeue();
            queuedState.Interrupt();
        }
    }

    public void QueueState(State newState)
    {
        stateQueue.Enqueue(newState);
    }

    public void SetState(State newState)
    {
        if (state != null)
        {
            state.Exit();
        }

        state = newState;

        if (state != null)
        {
            state.Enter();
        }
    }

    #endregion

    public void RegisterOnChangedCallback(Action<Actor> cb)
    {
        OnActorChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Actor> cb)
    {
        OnActorChanged -= cb;
    }

    void OnJobStopped(Job j)
    {
        // Job completed (if non-repating) or was cancelled.

        j.UnregisterJobStoppedCallback(OnJobStopped);

        if (j != myJob)
        {
            Debug.LogError("Actor being told about job that isn't there's. You forgot to unregister something.");
            return;
        }

        myJob = null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                     SAVING & LOADING
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", CurrTile.X.ToString());
        writer.WriteAttributeString("Y", CurrTile.Y.ToString());
        writer.WriteAttributeString("Z", CurrTile.Z.ToString());

    }

    public void ReadXml(XmlReader reader)
    {

    }
}