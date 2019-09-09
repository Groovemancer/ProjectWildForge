using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[Serializable]
public abstract class OrderAction : IXmlSerializable
{
    protected static readonly string OrderActionsLogChannel = "OrderActions";

    private static Dictionary<string, Type> orderActionTypes;

    public OrderAction()
    {
    }

    public OrderAction(OrderAction other)
    {
        JobCostFunction = other.JobCostFunction;
        Inventory = other.Inventory;
        Type = other.Type;
        JobCost = other.JobCost;
        Category = other.Category;
        Priority = other.Priority;
    }

    public string Type { get; set; }

    public float JobCost { get; set; }

    public string JobCostFunction { get; set; }

    public virtual JobCategory Category { get; protected set; }

    public virtual Job.JobPriority Priority { get; protected set; }

    public Dictionary<string, int> Inventory { get; set; }

    public static OrderAction FromXml(XmlNode rootNode)
    {
        if (orderActionTypes == null)
        {
            orderActionTypes = FindOrderActionsInAssembly();
        }

        string orderActionType = rootNode.Attributes["Type"].InnerText;

        Type t;
        if (orderActionTypes.TryGetValue(orderActionType, out t))
        {
            XmlSerializer serial = new XmlSerializer(t, new XmlRootAttribute("OrderAction"));

            using (XmlNodeReader reader = new XmlNodeReader(rootNode))
            {
                OrderAction orderAction = (OrderAction)serial.Deserialize(reader);
                orderAction.Type = orderActionType;
                string prevCategory = string.Empty;
                if (orderAction.Category != null)
                {
                    prevCategory = orderAction.Category.Type;
                }

                string tempCategory = PrototypeReader.ReadXml(prevCategory, rootNode.SelectSingleNode("JobCategory"));
                orderAction.Category = PrototypeManager.JobCategory.Get(tempCategory);
                orderAction.Priority = (Job.JobPriority)PrototypeReader.ReadXml((int)orderAction.Priority, rootNode.SelectSingleNode("JobPriority"));
                return orderAction;
            }
        }
        DebugUtils.LogChannel(OrderActionsLogChannel, string.Format("There is a no deserializer for OrderAction '{0}'", orderActionType));
        return null;
    }

    public virtual void Initialize(string type)
    {
        Type = type;
    }

    public abstract Job CreateJob(Tile tile, string type);

    public override string ToString()
    {
        return Type;
    }

    public abstract OrderAction Clone();

    protected Job CheckJobFromFunction(string functionName, Structure structure)
    {
        Job job = null;
        if (!string.IsNullOrEmpty(functionName))
        {
            job = FunctionsManager.Structure.Call<Job>(functionName, structure, null);
            job.OrderName = Type;
        }

        return job;
    }

    private static Dictionary<string, System.Type> FindOrderActionsInAssembly()
    {
        var orderActionTypes = new Dictionary<string, System.Type>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(asm => !CSharpFunctions.IsDynamic(asm)))
        {
            foreach (Type type in assembly.GetTypes())
            {
                OrderActionNameAttribute[] attribs = (OrderActionNameAttribute[])type.GetCustomAttributes(typeof(OrderActionNameAttribute), false);
                if (attribs != null && attribs.Length > 0)
                {
                    foreach (OrderActionNameAttribute compNameAttr in attribs)
                    {
                        orderActionTypes.Add(compNameAttr.OrderActionName, type);
                        DebugUtils.LogChannel(OrderActionsLogChannel, string.Format("Found OrderAction in assembly: {0}", compNameAttr.OrderActionName));
                    }
                }
            }
        }

        return orderActionTypes;
    }

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        Type = reader.GetAttribute("Type");

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "JobCostFunction":
                    JobCostFunction = reader.ReadInnerXml();
                    break;
                case "JobCost":
                    JobCost = float.Parse(reader.ReadInnerXml());
                    break;
                case "Category":
                    Category = PrototypeManager.JobCategory.Get(reader.ReadInnerXml());
                    break;
                case "Priority":
                    Priority = (Job.JobPriority)int.Parse(reader.ReadInnerXml());
                    break;
                case "Inventory":
                    string invType = reader.GetAttribute("Type");
                    int invAmount = int.Parse(reader.GetAttribute("Amount"));
                    Inventory.Add(invType, invAmount);
                    break;
            }
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new NotImplementedException();
    }
}
