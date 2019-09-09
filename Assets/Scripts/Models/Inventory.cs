using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Xml;
using System.Linq;

// Inventory are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

[MoonSharpUserData]
public class Inventory : IPrototypable
{
    private const float ClaimDuration = 120; // in Seconds

    private int stackSize = 1;
    private List<InventoryClaim> claims;

    public int StackSize
    {
        get { return stackSize; }
        set
        {
            if (stackSize == value)
            {
                return;
            }

            stackSize = value;
            InvokeStackSizeChanged(this);
        }
    }

    public int AvailableInventory
    {
        get
        {
            float requestTime = TimeManager.Instance.GameTime;
            return this.stackSize - claims.Where(claim => (requestTime - claim.time) < ClaimDuration).Sum(claim => claim.amount);
        }
    }

    public event Action<Inventory> StackSizeChanged;

    public string Type { get; set; }

    public int MaxStackSize { get; set; }

    public float BasePrice { get; set; }

    public string Category { get; set; }

    public string Name { get; set; }

    public Tile Tile { get; set; }

    // Should this inventory be allowed to be picked up for completing a job?
    public bool Locked { get; set; }

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

    public bool CanClaim()
    {
        float requestTime = TimeManager.Instance.GameTime;
        List<InventoryClaim> validClaims = claims.Where(claim => (requestTime - claim.time) < ClaimDuration).ToList();
        int availableInventory = this.stackSize - validClaims.Sum(claim => claim.amount);

        // Set claims to validClaims to keep claims from filling up with old claims
        claims = validClaims;

        return availableInventory > 0;
    }

    public void ReleaseClaim(Actor actor)
    {
        bool noneAvailable = AvailableInventory == 0;
        claims.RemoveAll(claim => claim.actor == actor);
        if (noneAvailable && AvailableInventory > 0)
        {
            World.Current.InventoryManager.InventoryAvailable(this);
        }
    }

    public void Claim(Actor actor, int amount)
    {
        float requestTime = TimeManager.Instance.GameTime;
        List<InventoryClaim> validClaims = claims.Where(claim => (requestTime - claim.time) < ClaimDuration).ToList();
        int availableInventory = this.stackSize - validClaims.Sum(claim => claim.amount);
        if (availableInventory >= amount)
        {
            validClaims.Add(new InventoryClaim(requestTime, actor, amount));
        }

        // Set claims to validClaims to keep claims from filling up with old claims
        claims = validClaims;
    }

    public bool CanBePickedUp(bool canTakeFromStockpile)
    {
        // You can't pick up stuff that isn't on a tile or if it's locked
        if (Tile == null || Locked || !CanClaim())
        {
            return false;
        }

        return Tile.Structure == null || canTakeFromStockpile == true || Tile.Structure.HasTypeTag("Storage") == false;
    }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.SelectSingleNode("Name").InnerText;
        MaxStackSize = int.Parse(rootNode.SelectSingleNode("MaxStackSize").InnerText);
        BasePrice = float.Parse(rootNode.SelectSingleNode("BasePrice").InnerText);
        Category = rootNode.SelectSingleNode("Category").InnerText;
    }

    public struct InventoryClaim
    {
        public float time;
        public Actor actor;
        public int amount;

        public InventoryClaim(float time, Actor actor, int amount)
        {
            this.time = time;
            this.actor = actor;
            this.amount = amount;
        }
    }
}
