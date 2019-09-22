using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectWildForge.Pathfinding;
using UnityEngine;

public class MoveState : State
{
    private Pathfinder.GoalEvaluator hasReachedDestination;

    private float movementPercentage;
    private float distToTravel;

    private Tile nextTile;

    public MoveState(Actor actor, Pathfinder.GoalEvaluator goalEvaluator, List<Tile> path, State nextState = null)
        : base("Move", actor, nextState)
    {
        hasReachedDestination = goalEvaluator;
        actor.Path = path;

        DebugLog("created with path length: {0}", path.Count);
    }

    public override void Update(float deltaAuts)
    {
        if (nextTile.IsEnterable() == Enterability.Soon)
        {
            // We can't enter the NOW, but we should be able to in the
            // future. This is likely a DOOR.
            // So we DON'T bail on our movement/path, but we do return
            // now and don't actually process the movement.
            actor.Acted = false;
            return;
        }

        if (nextTile.IsEnterable() == Enterability.Never)
        {
            Finished();
            actor.Acted = false;
            return;
        }

        float moveCost = CalculatedMoveCost();

        Vector2 heading = new Vector2(nextTile.X - actor.CurrTile.X, nextTile.Y - actor.CurrTile.Y);
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

        if (actor.CurrTile != nextTile)
        {
            if (actor.ActionPoints >= moveCost)
            {
                if (dirX != 0)
                {
                    actor.CurrTile = World.Current.GetTileAt(actor.CurrTile.X + dirX, actor.CurrTile.Y, actor.CurrTile.Z);
                }
                else if (dirY != 0)
                {
                    actor.CurrTile = World.Current.GetTileAt(actor.CurrTile.X, actor.CurrTile.Y + dirY, actor.CurrTile.Z);
                }

                //actor.CurrTile.OnEnter();

                if (hasReachedDestination(nextTile) || actor.Path.Count == 0)
                {
                    Finished();
                }
                else
                {
                    if (actor.CurrTile == nextTile)
                    {
                        AdvanceNextTile();
                    }
                }

                if (nextTile.IsEnterable() == Enterability.Never)
                {
                    Finished();
                }
                actor.ActionPoints -= moveCost;
            }
            actor.Acted = true;
        }
    }

    private float CalculatedMoveCost()
    {
        float tileCost = nextTile.CalculatedMoveCost();
        if (tileCost == 0)
            tileCost = 1f;
        return actor.MovementCost * tileCost;
    }

    public override void Enter()
    {
        base.Enter();

        if (actor.Path == null || actor.Path.Count == 0)
        {
            Finished();
            return;
        }

        // The starting tile might be included, so we need to get rid of it
        while (actor.Path[0].Equals(actor.CurrTile))
        {
            actor.Path.RemoveAt(0);

            if (actor.Path.Count == 0)
            {
                DebugLog(" - Ran out of path to walk");

                // We've either arrived, or we need to find a new path to the target
                Finished();
                return;
            }
        }

        AdvanceNextTile();
    }

    public override void Interrupt()
    {
        if (actor.Path != null)
        {
            Tile goal = actor.Path.Last();
            if (goal.Inventory != null)
            {
                goal.Inventory.ReleaseClaim(actor);
            }
        }

        base.Interrupt();
    }

    private void AdvanceNextTile()
    {
        nextTile = actor.Path[0];
        actor.Path.RemoveAt(0);

        // TODO Add if we decide to have player facing
        //actor.FaceTile(nextTile);
    }
}
