using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inventory are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

public class Inventory
{
    public string objectType = "RawStone";
    public int maxStackSize = 64;
    public int stackSize = 1;

    public Tile tile;
    public Actor actor;

    public Inventory()
    {

    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

    protected Inventory(Inventory other)
    {
        objectType      = other.objectType;
        maxStackSize    = other.maxStackSize;
        stackSize       = other.stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }
}
