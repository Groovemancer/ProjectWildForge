using System;
using UnityEngine;
using System.Collections;

public class Actor
{
    int x;
    int y;

    public Tile CurrTile { get; protected set; }
    Tile destTile;

    float movementCost = 25f; // Amount of action points required to move 1 tile.

    float actionPoints = 0f;

    Action<Actor> cbActorChanged;

    Job myJob;

    public Actor(Tile tile)
    {
        CurrTile = destTile = tile;
    }

    // AUTs are "Arbitrary Unit of Time", e.g. 100 AUT/s means every 1 second 100 AUTs pass
    public void Update(float deltaAuts)
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
                myJob.DoWork(deltaAuts);

            return;
        }

        actionPoints += deltaAuts;

        float distToTravel = Mathf.Sqrt(Mathf.Pow(CurrTile.X - destTile.X, 2) + Mathf.Pow(CurrTile.Y - destTile.Y, 2));

        Vector2 heading = new Vector2(destTile.X - CurrTile.X, destTile.Y - CurrTile.Y);
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

        float moveCost = CalculatedMoveCost();

        if (CurrTile != destTile)
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
                
                if (CurrTile == destTile)
                {
                    CurrTile = destTile;
                    Debug.Log("Arrived!");
                }   
                actionPoints -= moveCost;
            }
        }

        if (cbActorChanged != null)
            cbActorChanged(this);
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
}