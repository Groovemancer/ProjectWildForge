using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MoonSharp.Interpreter;

// Inventory are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

[MoonSharpUserData]
public class Inventory
{
    public string objectType = "inv_RawStone";
    public int MaxStackSize { get; set; }

    protected int _stackSize = 1;
    public int StackSize
    {
        get { return _stackSize; }
        set
        {
            if (_stackSize != value)
            {
                _stackSize = value;
                if (Tile != null && cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            }
        }
    }

    Action<Inventory> cbInventoryChanged;

    public Tile Tile { get; set; }
    public Actor actor;

    public Inventory()
    {

    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.MaxStackSize = maxStackSize;
        this.StackSize = stackSize;
    }

    protected Inventory(Inventory other)
    {
        objectType      = other.objectType;
        MaxStackSize    = other.MaxStackSize;
        StackSize       = other.StackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    public void RegisterOnChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged += callback;
    }

    public void UnregisterInventoryOnChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged -= callback;
    }
}
