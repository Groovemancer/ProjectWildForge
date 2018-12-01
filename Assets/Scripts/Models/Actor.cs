using System;
using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Actor : IXmlSerializable
{
    int x;
    int y;

    public Tile CurrTile { get; protected set; }
    Tile destTile;
    Tile nextTile;

    PathAStar pathAStar;

    float movementCost = 25f; // Amount of action points required to move 1 tile.

    float workCost = 25f;   // Amount of action points required to do any amount of work
    float workRate = 50f;   // Amount of work completed per DoWork attempt

    float actionPoints = 0f;

    Action<Actor> cbActorChanged;

    Job myJob;

    public Actor()
    {
        // Use only for serialization
    }

    public Actor(Tile tile)
    {
        CurrTile = destTile = tile;
    }

    bool UpdateDoJob(float deltaAuts)
    {
        // Do I have a job?
        if (myJob == null)
        {
            // Grab a new jew job.
            myJob = CurrTile.World.jobQueue.Dequeue();

            if (myJob != null)
            {
                // We have a job!
                destTile = myJob.Tile;
                myJob.RegisterJobCompleteCallback(OnJobEnded);
                myJob.RegisterJobCancelCallback(OnJobEnded);
            }
        }

        // We are at our destination
        if (CurrTile == destTile)
        {
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
        pathAStar = null;
        myJob.UnregisterJobCancelCallback(OnJobEnded);
        myJob.UnregisterJobCompleteCallback(OnJobEnded);
        CurrTile.World.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    bool UpdateHandleMovement()
    {
        if (CurrTile == destTile)
        {
            pathAStar = null;
            return false;
        }

        if (nextTile == null || nextTile == CurrTile)
        {
            // Get the next tile from the pathfinder
            if (pathAStar == null || pathAStar.Length() == 0)
            {
                // Generate a path to our destination
                pathAStar = new PathAStar(CurrTile.World, CurrTile, destTile); // This will calculate a path from curr to dest.
                if (pathAStar.Length() == 0)
                {
                    Debug.LogError("PathAStar returned no path to destination!");
                    // FIXME: Job should maybe be re-enqued instead?
                    AbandonJob();
                    pathAStar = null;
                    return false;
                }
            }

            // Grab the next waypoint from the pathing system!
            nextTile = pathAStar.Dequeue();

            if (nextTile == CurrTile)
            {
                //Debug.LogError("UpdateHandleMovement - nextTile is currtile?");
            }
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
                    CurrTile = CurrTile.World.GetTileAt(CurrTile.X + dirX, CurrTile.Y);
                }
                else if (dirY != 0)
                {
                    CurrTile = CurrTile.World.GetTileAt(CurrTile.X, CurrTile.Y + dirY);
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

    public float CalculatedMoveCost()
    {
        return movementCost;
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

    void OnJobEnded(Job j)
    {
        // Job completed or was cancelled.
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

    }

    public void ReadXml(XmlReader reader)
    {
        
    }
}