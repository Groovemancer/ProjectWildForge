using UnityEngine;
using System.Collections.Generic;

public class PathAStar
{
    Queue<Tile> path;

    public PathAStar(World world, Tile start, Tile end)
    {

    }

    public Tile GetNextTile()
    {
        return path.Dequeue();
    }
}