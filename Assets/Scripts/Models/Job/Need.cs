using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Need : IPrototypable
{
    private bool highToLow = true;
    private float amount = 0;

    // Use this for initialization
    public Need()
    {
        Amount = 0;
        RestoreNeedAmount = 100;
        EventActions = new EventActions();
    }

    private Need(Need other)
    {
        Amount = 0;
        Type = other.Type;
        Name = other.Name;
        GrowthRate = other.GrowthRate;
        highToLow = other.highToLow;
        RestoreNeedStruct = other.RestoreNeedStruct;
        RestoreNeedTime = other.RestoreNeedTime;
        RestoreNeedAmount = other.RestoreNeedAmount;
        Damage = other.Damage;

        if (other.EventActions != null)
        {
            EventActions = other.EventActions.Clone();
        }
    }

    public Actor Actor { get; set; }

    public string Type { get; private set; }
    
    public string Name { get; private set; }

    public float Amount
    {
        get
        {
            return amount;
        }
        
        set
        {
            amount = value.Clamp(0.0f, 100.0f);
        }
    }

    public float RestoreNeedAmount { get; private set; }

    public float GrowthRate { get; private set; }

    public float Damage { get; private set; }

    public bool CompleteOnFail { get; private set; }

    public Structure RestoreNeedStruct { get; private set; }

    public float RestoreNeedTime { get; private set; }

    public string DisplayAmount
    {
        get
        {
            if (highToLow)
            {
                return (100 - (int)Amount) + "%";
            }

            return ((int)Amount) + "%";
        }
    }

    public EventActions EventActions { get; private set; }

    public void Update(float deltaTime)
    {
        if (EventActions != null && EventActions.HasEvent("OnUpdate"))
        {
            EventActions.Trigger("OnUpdate", this, deltaTime);
        }
        else
        {
            DefaultNeedDecay(deltaTime);
        }

        if (Amount.AreEqual(100))
        {
            if (EventActions != null && EventActions.HasEvent("OnEmptyNeed"))
            {
                EventActions.Trigger("OnEmptyNeed", this, deltaTime);
            }
            else
            {
                DefaultEmptyNeed();
            }
        }
        else if (Amount > 90f)
        {
            if (EventActions != null)
            {
                EventActions.Trigger("OnSevereNeed", this, deltaTime);
            }
        }
        else if (Amount > 75f)
        {
            if (EventActions != null)
            {
                EventActions.Trigger("OnCriticalNeed", this, deltaTime);
            }
        }
        else if (Amount > 50f)
        {
            if (EventActions != null)
            {
                EventActions.Trigger("OnModerateNeed", this, deltaTime);
            }
        }
    }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["Type"].InnerText;
        
        string structure = PrototypeReader.ReadXml(string.Empty, rootNode.SelectSingleNode("RestoreNeedStructureType"));
        if (structure != string.Empty)
        {
            RestoreNeedStruct = PrototypeManager.Structure.Get(structure);
        }

        RestoreNeedTime = PrototypeReader.ReadXml(RestoreNeedTime, rootNode.SelectSingleNode("RestoreNeedTime"));
        Damage = PrototypeReader.ReadXml(Damage, rootNode.SelectSingleNode("Damage"));
        CompleteOnFail = PrototypeReader.ReadXml(CompleteOnFail, rootNode.SelectSingleNode("CompleteOnFail"));
        highToLow = PrototypeReader.ReadXml(highToLow, rootNode.SelectSingleNode("HighToLow"));
        GrowthRate = PrototypeReader.ReadXml(GrowthRate, rootNode.SelectSingleNode("GrowthRate"));
        RestoreNeedAmount = PrototypeReader.ReadXml(RestoreNeedAmount, rootNode.SelectSingleNode("RestoreNeedAmount"));
        Name = PrototypeReader.ReadXml(Name, rootNode.SelectSingleNode("Name"));
        EventActions.ReadXml(rootNode.SelectNodes("EventAction"));
    }

    public void CompleteJobNorm(Job job)
    {
        Amount -= RestoreNeedAmount;
    }

    public void CompleteJobCrit(Job job)
    {
        Amount -= RestoreNeedAmount / 4;
    }

    public Need Clone()
    {
        return new Need(this);
    }

    public void DefaultNeedDecay(float deltaTime)
    {
        Amount += this.GrowthRate * deltaTime;
    }

    public void DefaultEmptyNeed()
    {
        // TODO: Default for empty need should probably be taking damage, but shouldn't be implemented until characters are 
        //       better able to handle getting their oxygen and maybe have real space suits.
    }
}
