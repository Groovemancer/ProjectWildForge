using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ProjectWildForge.Pathfinding;

public class Actor : IXmlSerializable
{
    public Tile CurrTile { get; protected set; }

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

    float workCost = 25f;   // Amount of action points required to do any amount of work
    private float workRate;   // Amount of work completed per DoWork attempt
    private float baseWorkRate = 50f;   // Amount of work completed per DoWork attempt

    public float WorkRate
    {
        get { return workRate; }
    }

    float actionPoints = 0f;

    Action<Actor> cbActorChanged;

    Job myJob;

    // The item we are carrying (not gear/equipment)
    public Inventory inventory;

    /// Stats, for character.
    public Dictionary<string, Stat> Stats { get; protected set; }

    public Actor()
    {
        // Use only for serialization
        InitializeActorValues();
    }

    public Actor(Tile tile)
    {
        CurrTile = destTile = tile;
        InitializeActorValues();
    }

    private void InitializeActorValues()
    {
        LoadStats();
        UseStats();
    }

    private void LoadStats()
    {
        Stats = new Dictionary<string, Stat>(PrototypeManager.Stat.Count);
        for (int i = 0; i < PrototypeManager.Stat.Count; i++)
        {
            Stat prototypeStat = PrototypeManager.Stat.Values[i];
            Stat newStat = prototypeStat.Clone();

            // Gets a random value within the min and max range of the stat.
            // TODO: Should there be any bias or any other algorithm applied here to make stats more interesting?
            newStat.Value = UnityEngine.Random.Range(1, 20);
            Stats.Add(newStat.Type, newStat);

            DebugUtils.LogChannel("Actor", "Stat: " + newStat.ToString());
        }

        DebugUtils.LogChannel("Actor", "Initialized " + Stats.Count + " Stats.");
    }

    private void UseStats()
    {
        try
        {
            movementCost = baseMovementCost - (0.3f * baseMovementCost * ((float)Stats["Agility"].Value - 10) / 10); // +/- 30%
            workRate = baseWorkRate + (0.5f * baseWorkRate * ((float)Stats["Dexterity"].Value - 10) / 10); // +/- 50%

            DebugUtils.LogChannel("Actor", string.Format("Actor {0} movementCost: {1}", World.Current.actors.IndexOf(this), movementCost));
            DebugUtils.LogChannel("Actor", string.Format("Actor {0} workRate: {1}", World.Current.actors.IndexOf(this), workRate));
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
            if (inventory != null)
            {
                if (myJob.DesiresInventoryType(inventory) > 0)
                {
                    // If so, deliver the goods.
                    //  Walk to the job tile, then drop off the stack into the job.

                    if (CurrTile == myJob.Tile)
                    {
                        // We are at the job's site, so drop the inventory
                        World.Current.InventoryManager.PlaceInventory(myJob, inventory);
                        myJob.DoWork(0); // This will call all cbJobWorked callbacks, because even though
                                        // we aren't progressing, it might want to do something with the fact
                                        // that the requirements are being met.

                        // Are we still carrying things?
                        if (inventory.StackSize == 0)
                        {
                            inventory = null;
                        }
                        else
                        {
                            Debug.LogError("Character is still carrying inventory, which shouldn't be. Just setting to null for now, but this means we are LEAKING inventory.");
                            inventory = null;
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
                    if (World.Current.InventoryManager.PlaceInventory(CurrTile, inventory) == false)
                    {
                        Debug.LogError("Character tried to dump inventory into an invalid tile (maybe there's already something here.");
                        // FIXME: For the sake of continuing on, we are still going to dump any
                        // reference to the current inventory, but this means we are "leaking"
                        // inventory. This is permanently lost now.
                        inventory = null;
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
                        desired.objectType,
                        CurrTile,
                        desired.MaxStackSize - desired.StackSize,
                        myJob.canTakeFromStockpile
                    );

                    if (supplier == null)
                    {
                        Debug.Log("No tile contains objects of type'" + desired.objectType + "' to satisfy job requirements.");
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
                if (actionPoints >= workCost)
                {
                    myJob.DoWork(workRate);
                    actionPoints -= workCost;
                }
                return true;
            }
        }

        // Nothing left for us to do here, we mostly just need Update_DoMovement to
        // get us where we want to go.

        return false;
    }

    // AUTs are "Arbitrary Unit of Time", e.g. 100 AUT/s means every 1 second 100 AUTs pass
    public void Update(float deltaAuts)
    {
        bool didSomething = false;
        actionPoints += deltaAuts;

        if (UpdateDoJob(deltaAuts))
            didSomething = true;

        if (UpdateHandleMovement())
            didSomething = true;

        if (didSomething == false)
            actionPoints -= deltaAuts;

        if (cbActorChanged != null)
            cbActorChanged(this);
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

    bool UpdateHandleMovement()
    {
        if (CurrTile == destTile)
        {
            //pathAStar = null;
            Path = null;
            return false;
        }

        if (nextTile == null || nextTile == CurrTile)
        {
            // Get the next tile from the pathfinder
            //if (pathAStar == null || pathAStar.Length() == 0)
            if (Path == null || Path.Count == 0)
            {
                // Generate a path to our destination
                //pathAStar = new Path_AStar(World.Current, CurrTile, destTile); // This will calculate a path from curr to dest.
                Path = Pathfinder.FindPathToTile(CurrTile, destTile);

                if (Path.Count == 0)
                {
                    Debug.LogError("PathAStar returned no path to destination!");
                    AbandonJob();
                    return false;
                }
                // Let's ignore the first tile, because that's the tile we're currently in.
                //nextTile = pathAStar.Dequeue();
                AdvanceNextTile();
            }

            // Grab the next waypoint from the pathing system!
            //nextTile = pathAStar.Dequeue();
            AdvanceNextTile();

            if (nextTile == CurrTile)
            {
                //Debug.LogError("UpdateHandleMovement - nextTile is currtile?");
            }
        }

        if (nextTile.IsEnterable() == Enterability.Never)
        {
            //FIXME: Ideally, when a wall gets spawned, we should invalidate our path immediately,
            //      so that we don't waste a bunch of time walking towards a dead end.
            //      To save CPU, maybe we can only check every so often?
            //      Or maybe we should register a callback to the OnTileChanged event?
            Debug.LogError("FIXME: A character was trying to enter an unwalkable tile.");
            nextTile = null;    // our next tile is a no-go
            //pathAStar = null;   // clearly our pathfinding is out of date.
            Path = null;
            return false;
        }
        else if (nextTile.IsEnterable() == Enterability.Soon)
        {
            // We can't enter NOW, but we should be able to in the
            // future. This is likely a DOOR.
            // So we DON'T bail on our movement/path, but we do return
            // now and don't actually process the movement.
            return false;
        }

        // At this point we should have a valid nextTile to move to.

        float moveCost = CalculatedMoveCost();

        Vector2 heading = new Vector2(nextTile.X - CurrTile.X, nextTile.Y - CurrTile.Y);
        float distance = heading.magnitude;
        Vector2 direction = heading / distance;

        int dirX;
        int dirY;

        if (direction.x > 0)
            dirX = Mathf.CeilToInt(direction.x);
        else if (direction.x < 0)
            dirX = Mathf.FloorToInt(direction.x);
        else
            dirX = 0;

        if (direction.y > 0)
            dirY = Mathf.CeilToInt(direction.y);
        else if (direction.y < 0)
            dirY = Mathf.FloorToInt(direction.y);
        else
            dirY = 0;


        if (CurrTile != nextTile)
        {
            if (actionPoints >= moveCost)
            {
                Debug.Log("Moving heading: " + heading);
                Debug.Log("Moving distance: " + distance);
                Debug.Log("Moving Dir: " + dirX + ", " + dirY);

                if (dirX != 0)
                {
                    CurrTile = World.Current.GetTileAt(CurrTile.X + dirX, CurrTile.Y, CurrTile.Z);
                }
                else if (dirY != 0)
                {
                    CurrTile = World.Current.GetTileAt(CurrTile.X, CurrTile.Y + dirY, CurrTile.Z);
                }

                if (CurrTile == nextTile)
                {
                    // TODO: Get the next tile from the pathfinding system.
                    //       If there are no more tiles, then we have TRULY
                    //       reached our destination.

                    CurrTile = nextTile;
                    Debug.Log("Arrived!");
                }
                actionPoints -= moveCost;
            }
            return true;
        }

        return false;
    }

    private void AdvanceNextTile()
    {
        nextTile = Path[0];
        Path.RemoveAt(0);
    }

    public float CalculatedMoveCost()
    {
        float tileCost = nextTile.CalculatedMoveCost();
        if (tileCost == 0)
            tileCost = 1f;
        return MovementCost * tileCost;
    }

    public void SetDestination(Tile tile)
    {
        if (CurrTile.IsNeighbor(tile) == false)
        {
            Debug.Log("Actor::SetDestination -- Our destination tile isn't actually our neighbor");
        }

        destTile = tile;
    }

    public void RegisterOnChangedCallback(Action<Actor> cb)
    {
        cbActorChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Actor> cb)
    {
        cbActorChanged -= cb;
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