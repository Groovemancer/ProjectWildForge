﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class RequestedItem
{
    public RequestedItem(string type, int minAmountRequested, int maxAmountRequested)
    {
        Type = type;
        MinAmountRequested = minAmountRequested;
        MaxAmountRequested = maxAmountRequested;
    }

    public RequestedItem(string type, int maxAmountRequested)
        : this(type, maxAmountRequested, maxAmountRequested)
    {
    }

    public RequestedItem(RequestedItem item)
        : this(item.Type, item.MinAmountRequested, item.MaxAmountRequested)
    {
    }

    public RequestedItem(Inventory inventory)
        : this(inventory.Type, inventory.MaxStackSize - inventory.StackSize)
    {
    }

    public string Type { get; set; }

    public int MinAmountRequested { get; set; }

    public int MaxAmountRequested { get; set; }

    public RequestedItem Clone()
    {
        return new RequestedItem(this);
    }

    public bool NeedsMore(Inventory inventory)
    {
        return AmountNeeded(inventory) > 0;
    }

    public bool DesiresMore(Inventory inventory)
    {
        return AmountDesired(inventory) > 0;
    }

    public int AmountDesired(Inventory inventory = null)
    {
        if (inventory == null || inventory.Type != Type)
        {
            return MaxAmountRequested;
        }

        return MaxAmountRequested - inventory.StackSize;
    }

    public int AmountNeeded(Inventory inventory = null)
    {
        if (inventory == null || inventory.Type != Type)
        {
            return MinAmountRequested;
        }

        return Mathf.Max(MinAmountRequested - inventory.StackSize, 0);
    }
}
