using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Xml;

// Inventory are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

[MoonSharpUserData]
public class Inventory : IPrototypable
{
    protected int _stackSize = 1;
    public int StackSize
    {
        get { return _stackSize; }
        set
        {
            if (_stackSize == value)
            {
                return;
            }

            _stackSize = value;
            InvokeStackSizeChanged(this);
        }
    }

    public event Action<Inventory> StackSizeChanged;

    public string Type { get; set; }

    public int MaxStackSize { get; set; }

    public float BasePrice { get; set; }

    public string Category { get; set; }

    public string Name { get; set; }

    public Tile Tile { get; set; }
    public Actor actor;

    public Inventory()
    {

    }

    public Inventory(string type, int stackSize, int maxStackSize = 50)
    {
        this.Type = type;
        ImportPrototypeSettings(maxStackSize, 1f, "inv_cat_none");
        this.StackSize = stackSize;
    }

    protected Inventory(Inventory other)
    {
        Type = other.Type;
        MaxStackSize = other.MaxStackSize;
        BasePrice = other.BasePrice;
        Category = other.Category;
        StackSize = other.StackSize;
        Name = other.Name;
    }

    private Inventory(string type, int maxStackSize, float basePrice, string category, string name)
    {
        Type = type;
        MaxStackSize = maxStackSize;
        BasePrice = basePrice;
        Category = category;
        Name = name;
    }

    public static Inventory CreatePrototype(string type, int maxStackSize, float basePrice, string category, string name)
    {
        return new Inventory(type, maxStackSize, basePrice, category, name);
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    private void InvokeStackSizeChanged(Inventory inventory)
    {
        Action<Inventory> handler = StackSizeChanged;
        if (handler != null)
        {
            handler(inventory);
        }
    }

    private void ImportPrototypeSettings(int defaultMaxStackSize, float defaultBasePrice, string defaultCategory)
    {
        Inventory prototype;
        if (PrototypeManager.Inventory.TryGet(Type, out prototype))
        {
            MaxStackSize = prototype.MaxStackSize;
            BasePrice = prototype.BasePrice;
            Category = prototype.Category;
            Name = prototype.Name;
        }
        else
        {
            MaxStackSize = defaultMaxStackSize;
            BasePrice = defaultBasePrice;
            Category = defaultCategory;
        }
    }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.SelectSingleNode("Name").InnerText;
        MaxStackSize = int.Parse(rootNode.SelectSingleNode("MaxStackSize").InnerText);
        BasePrice = float.Parse(rootNode.SelectSingleNode("BasePrice").InnerText);
        Category = rootNode.SelectSingleNode("Category").InnerText;
    }
}
