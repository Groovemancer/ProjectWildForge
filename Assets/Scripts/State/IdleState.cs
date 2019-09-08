using ProjectWildForge.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Random = UnityEngine.Random;

[System.Diagnostics.DebuggerDisplay("Idle: ")]
public class IdleState : State
{
    private float totatlIdleAuts;
    private float autsSpentIdle;
    
    public IdleState(Actor actor, State nextState = null)
        : base("Idle", actor, nextState)
    {
        autsSpentIdle = 0f;
        totatlIdleAuts = RandomUtils.Range(20, 200);
    }

    public override void Update(float deltaAuts)
    {
        // Moves character in a random direction while idle.
        autsSpentIdle += deltaAuts;
        if (autsSpentIdle >= totatlIdleAuts)
        {
            Tile[] neighbors = actor.CurrTile.GetNeighbors();
            Tile endTile = neighbors[RandomUtils.Range(0, neighbors.Length)];
            List<Tile> path = new List<Tile>() { actor.CurrTile, endTile };

            if (endTile.CalculatedMoveCost() != 0)
            {
                // See if the desired tile is walkable, then go there if we can.
                actor.SetState(new MoveState(actor, Pathfinder.GoalTileEvaluator(endTile, true), path));
            }
            else
            {
                // If the tile is unwalkable, just get a new state.
                actor.SetState(null);
            }
        }
    }
}
