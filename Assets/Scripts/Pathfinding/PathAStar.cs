using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;

public class PathAStar
{
    Queue<Tile> path;

    public PathAStar(World world, Tile tileStart, Tile tileEnd)
    {
        // Check to see if we have a valid tile graph
        if (world.tileGraph == null)
        {
            world.tileGraph = new PathTileGraph(world);
        }

        // A dictionary of all valid, walkable nodes
        Dictionary<Tile, PathNode<Tile>> nodes = world.tileGraph.nodes;

        // Make sure our start/end tiles are in the list of nodes
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("PathAStar: The starting tile isn't in the list of nodes.");

            // FIXME: Right now, we're going to manually add the start tile into the list
            // of valid nodes.

            return;
        }
        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("PathAStar: The ending tile isn't in the list of nodes.");
            return;
        }

        PathNode<Tile> startNode = nodes[tileStart];
        PathNode<Tile> goalNode = nodes[tileEnd];

        List<PathNode<Tile>> closedSet = new List<PathNode<Tile>>();

        //List<PathNode<Tile>> OpenSet = new List<PathNode<Tile>>();
        //OpenSet.Add(nodes[tileStart]);

        SimplePriorityQueue<PathNode<Tile>> openSet = new SimplePriorityQueue<PathNode<Tile>>();
        openSet.Enqueue(startNode, 0);

        Dictionary<PathNode<Tile>, PathNode<Tile>> cameFrom = new Dictionary<PathNode<Tile>, PathNode<Tile>>();

        Dictionary<PathNode<Tile>, float> gScore = new Dictionary<PathNode<Tile>, float>();
        foreach (PathNode<Tile> n in nodes.Values)
        {
            gScore[n] = Mathf.Infinity;
        }

        gScore[startNode] = 0;

        Dictionary<PathNode<Tile>, float> fScore = new Dictionary<PathNode<Tile>, float>();
        foreach (PathNode<Tile> n in nodes.Values)
        {
            fScore[n] = Mathf.Infinity;
        }
        fScore[startNode] = HeuristicCostEstimate(startNode, goalNode);

        while (openSet.Count > 0)
        {
            PathNode<Tile> current = openSet.Dequeue();
            if (current == goalNode)
            {
                // TODO: return reconstruct path
                ReconstructPath(cameFrom, current);
                return;
            }
            closedSet.Add(current);

            foreach(PathEdge<Tile> edgeNeighbor in current.edges)
            {
                PathNode<Tile> neighbor = edgeNeighbor.node;
                if (closedSet.Contains(neighbor) == true)
                {
                    continue; // ignore this already completed neighbor
                }

                float movementCostToNeighbor = neighbor.data.CalculatedMoveCost() * distBetween(current, neighbor);

                float tentative_g_score = gScore[current] +  movementCostToNeighbor;

                if (openSet.Contains(neighbor) && tentative_g_score >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentative_g_score;
                fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, goalNode);

                if (openSet.Contains(neighbor) == false)
                {
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
                else
                {
                    openSet.UpdatePriority(neighbor, fScore[neighbor]);
                }
            } // foreach neighbor
        } // while

        // If we reached here, it means that we've burned through the entire
        // OpenSet without ever reaching a point where current == goal.
        // This happens when there is no path from start to goal.
    }

    float HeuristicCostEstimate(PathNode<Tile> a, PathNode<Tile> b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );
    }

    float distBetween(PathNode<Tile> a, PathNode<Tile> b)
    {
        if ((Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y)) == 1)
        {
            return 1f;
        }

        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );
    }

    void ReconstructPath(Dictionary<PathNode<Tile>, PathNode<Tile>> cameFrom,
        PathNode<Tile> current)
    {
        Queue<Tile> totalPath = new Queue<Tile>();
        totalPath.Enqueue(current.data);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Enqueue(current.data);
        }

        path = new Queue<Tile>(totalPath.Reverse());
    }

    public Tile Dequeue()
    {
        return path.Dequeue();
    }

    public int Length()
    {
        if (path == null)
            return 0;

        return path.Count;
    }
}