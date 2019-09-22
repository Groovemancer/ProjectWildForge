using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[XmlRoot("Component")]
public abstract class BuildableComponent
{
    protected static readonly string ComponentLogChannel = "StructureComponents";

    protected Requirements componentRequirements = Requirements.None;

    private static Dictionary<string, Type> componentTypes;

    private bool initialized = false;

    public BuildableComponent()
    {
        // need to set it, for some reason GetHashCode is called during serialization (when Name is still null)
        Type = string.Empty;
    }

    public BuildableComponent(BuildableComponent other)
    {
        Type = other.Type;
    }

    [Flags]
    public enum Requirements
    {
        None = 0,
        Power = 1,
        Production = 1 << 1,
        Gas = 1 << 2,
        Fluid = 1 << 3
    }

    public enum ConditionType
    {
        [XmlEnum(Name = "IsGreaterThanZero")]
        IsGreaterThanZero,
        [XmlEnum(Name = "IsLessThanOne")]
        IsLessThanOne,
        [XmlEnum(Name = "IsZero")]
        IsZero,
        [XmlEnum(Name = "IsTrue")]
        IsTrue,
        [XmlEnum(Name = "IsFalse")]
        IsFalse
    }

    public string Type { get; set; }

    public Requirements Needs
    {
        get
        {
            return componentRequirements;
        }
    }

    public bool Initialized
    {
        get
        {
            return initialized;
        }
    }

    public virtual bool RequiresSlowUpdate
    {
        get
        {
            return false;
        }
    }

    public virtual bool RequiresFastUpdate
    {
        get
        {
            return false;
        }
    }

    protected Structure ParentStructure { get; set; }

    protected Parameter StructureParams
    {
        get { return ParentStructure.Parameters; }
    }

    public static BuildableComponent FromXml(XmlNode rootNode)
    {
        if (componentTypes == null)
        {
            componentTypes = FindComponentsInAssembly();
        }

        string componentTypeName = rootNode.Attributes["Type"].InnerText;
        Type t;
        if (componentTypes.TryGetValue(componentTypeName, out t))
        {
            XmlSerializer serial = new XmlSerializer(t, new XmlRootAttribute("Component"));

            using (XmlNodeReader reader = new XmlNodeReader(rootNode))
            {
                BuildableComponent component = (BuildableComponent)serial.Deserialize(reader);

                // need to set name explicityly (not part of deserialization as it's passed in)
                component.Type = componentTypeName;

                return component;
            }
        }
        else
        {
            return null;
        }
    }

    //public XmlSchema GetSchema()
    //{
    //    throw new NotImplementedException();
    //}

    //public virtual void ReadXml(XmlReader reader)
    //{
    //    Type = reader.GetAttribute("Type");
    //}

    //public void WriteXml(XmlWriter writer)
    //{
    //    throw new NotImplementedException();
    //}

    /// <summary>
    /// Initializes after loading the prototype.
    /// </summary>
    /// <param name="protoStructure">Reference to prototype of structure.</param>
    public virtual void InitializePrototype(Structure protoStructure)
    {
    }

    /// <summary>
    /// Initializes after placed into world.
    /// </summary>
    /// <param name="parentStructure">Reference to structure placed in world.</param>
    public void Initialize(Structure parentStructure)
    {
        ParentStructure = parentStructure;
        Initialize();
        initialized = true;
    }

    /// <summary>
    /// Determines if the configuration. Checked immediately after parsing the Xml files.
    /// </summary>
    /// <returns>true if valid.</returns>
    public abstract bool IsValid();

    public virtual bool CanFunction()
    {
        return true;
    }

    public virtual void FixedFrequencyUpdate(float deltaTime)
    {
    }

    public virtual void EveryFrameUpdate(float deltaTime)
    {
    }

    public virtual IEnumerable<string> GetDescription()
    {
        return null;
    }

    public override string ToString()
    {
        return Type;
    }

    public abstract BuildableComponent Clone();

    protected abstract void Initialize();

    #region Context Menu
    // TODO: TO BE IMPLEMENTED
    //public virtual List<ContextMenuAction> GetContextMenu()
    //{
    //    return null;
    //}

    // TODO: TO BE IMPLEMENTED
    //protected ContextMenuAction CreateComponentContextMenuItem(ComponentContextMenu componentContextMenuAction)
    //{
    //    return new ContextMenuAction
    //    {
    //        LocalizationKey = componentContextMenuAction.Name,
    //        RequireCharacterSelected = false,
    //        Action = (cma, c) => InvokeContextMenuAction(componentContextMenuAction.Function, componentContextMenuAction.Name)
    //    };
    //}

    // TODO: TO BE IMPLEMENTED
    //protected void InvokeContextMenuAction(Action<Furniture, string> function, string arg)
    //{
    //    function(ParentFurniture, arg);
    //}
    #endregion

    protected bool AreParameterConditionsFulfilled(List<ParameterCondition> conditions)
    {
        bool conditionsFulFilled = true;
        //// here evaluate all parameter conditions
        if (conditions != null)
        {
            foreach (ParameterCondition condition in conditions)
            {
                bool partialEval = true;
                switch (condition.Condition)
                {
                    case ConditionType.IsZero:
                        partialEval = StructureParams[condition.ParameterName].ToFloat().Equals(0);
                        break;
                    case ConditionType.IsGreaterThanZero:
                        partialEval = StructureParams[condition.ParameterName].ToFloat() > 0f;
                        break;
                    case ConditionType.IsLessThanOne:
                        partialEval = StructureParams[condition.ParameterName].ToFloat() < 1f;
                        break;
                    case ConditionType.IsTrue:
                        partialEval = StructureParams[condition.ParameterName].ToBool() == true;
                        break;
                    case ConditionType.IsFalse:
                        partialEval = StructureParams[condition.ParameterName].ToBool() == false;
                        break;
                }

                conditionsFulFilled &= partialEval;
            }
        }

        return conditionsFulFilled;
    }

    protected string RetrieveStringFor(SourceDataInfo sourceDataInfo, Structure structure)
    {
        string retString = null;
        if (sourceDataInfo != null)
        {
            if (!string.IsNullOrEmpty(sourceDataInfo.Value))
            {
                retString = sourceDataInfo.Value;
            }
            else if (!string.IsNullOrEmpty(sourceDataInfo.FromFunction))
            {
                DynValue ret = FunctionsManager.Structure.Call(sourceDataInfo.FromFunction, structure);
                retString = ret.String;
            }
            else if (!string.IsNullOrEmpty(sourceDataInfo.FromParameter))
            {
                retString = structure.Parameters[sourceDataInfo.FromParameter].ToString();
            }
        }

        return retString;
    }

    protected float RetrieveFloatFor(SourceDataInfo sourceDataInfo, Structure structure)
    {
        float retFloat = 0f;
        if (sourceDataInfo != null)
        {
            if (!string.IsNullOrEmpty(sourceDataInfo.Value))
            {
                retFloat = float.Parse(sourceDataInfo.Value);
            }
            else if (!string.IsNullOrEmpty(sourceDataInfo.FromFunction))
            {
                DynValue ret = FunctionsManager.Structure.Call(sourceDataInfo.FromFunction, structure);
                retFloat = (float)ret.Number;
            }
            else if (!string.IsNullOrEmpty(sourceDataInfo.FromParameter))
            {
                retFloat = structure.Parameters[sourceDataInfo.FromParameter].ToFloat();
            }
        }

        return retFloat;
    }

    private static Dictionary<string, System.Type> FindComponentsInAssembly()
    {
        componentTypes = new Dictionary<string, System.Type>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(asm => !CSharpFunctions.IsDynamic(asm)))
        {
            foreach (Type type in assembly.GetTypes())
            {
                BuildableComponentNameAttribute[] attribs = (BuildableComponentNameAttribute[])type.GetCustomAttributes(typeof(BuildableComponentNameAttribute), false);
                if (attribs != null && attribs.Length > 0)
                {
                    foreach (BuildableComponentNameAttribute compNameAttr in attribs)
                    {
                        componentTypes.Add(compNameAttr.ComponentName, type);
                        DebugUtils.LogChannel(ComponentLogChannel, string.Format("Found component in assembly: {0}", compNameAttr.ComponentName));
                    }
                }
            }
        }

        return componentTypes;
    }

    [Serializable]
    public class UseAnimation
    {
        public string Name { get; set; }

        public string ValueBasedParameterName { get; set; }

        public Conditions RunConditions { get; set; }
    }

    [Serializable]
    public class ParameterCondition
    {
        public string ParameterName { get; set; }

        [XmlElement("Condition")]
        public ConditionType Condition { get; set; }
    }

    [Serializable]
    public class ParameterDefinition
    {
        public ParameterDefinition()
        {
        }

        public ParameterDefinition(string paramName)
        {
            this.ParameterName = paramName;
        }

        [XmlAttribute("Name")]
        public string ParameterName { get; set; }

        [XmlAttribute("Type")]
        public string Type { get; set; }
    }

    [Serializable]
    public class SourceDataInfo
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlAttribute("FromParameter")]
        public string FromParameter { get; set; }

        [XmlAttribute("FromFunction")]
        public string FromFunction { get; set; }
    }

    [Serializable]
    public class Info
    {
        public float Rate { get; set; }

        public float Capacity { get; set; }

        public int CapacityThresholds { get; set; }

        public bool CanUseVariableEfficiency { get; set; }
    }

    [Serializable]
    public class Conditions
    {
        [XmlArray("ParamConditions")]
        public List<ParameterCondition> ParamConditions { get; set; }
    }
}
