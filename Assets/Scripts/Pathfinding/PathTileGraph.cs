using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTileGraph
{
    // This class constructs a simple path-finding compatible graph
    // of our world. Each tile is a node. Each WALKABLE neighbor
    // from a tile is linked via an edge connection.

    Dictionary<Tile, PathNode<Tile>> nodes;

    public PathTileGraph(World world)
    {
        Debug.Log("PathTileGraph");

        // Loop through all tiles of the world
        // For each tile, create a node

        nodes = new Dictionary<Tile, PathNode<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile t = world.GetTileAt(x, y);

                if (t.CalculatedMoveCost() > 0)     // Tiles with a move cost of 0 are unwalkable
                {
                    PathNode<Tile> n = new PathNode<Tile>();
                    n.data = t;
                    nodes.Add(t, n);
                }
            }
        }

        Debug.Log("PathTileGraph: Created " + nodes.Count + " nodes.");

        int edgeCount = 0;
        // Now loop through all tiles again
        // Create edges for neighbors

        foreach (Tile t in nodes.Keys)
        {
            PathNode<Tile> n = nodes[t];

            List<PathEdge<Tile>> edges = new List<PathEdge<Tile>>();

            // Get a list of neighbors for the tile
            Tile[] neighbors = t.GetNeighbors();

            // If neighbor is walkable, create an edge to the relevant node.
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null)
                {
                    float cost = neighbors[i].CalculatedMoveCost();

                    if (cost > 0)
                    {
                        // This neighbor exists and is walkable, so create an edge
                        PathEdge<Tile> e = new PathEdge<Tile>();
                        e.cost = cost;
                        e.node = nodes[neighbors[i]];

                        // Add the edge to our temporary (and growable!) list
                        edges.Add(e);
                        edgeCount++;
                    }
                }
            }
            n.edges = edges.ToArray();
        }

        Debug.Log("PathTileGraph: Created " + edgeCount + " edges.");
    }
}
