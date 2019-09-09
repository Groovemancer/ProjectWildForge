using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public class PrototypeReader
{
    public static T ReadXml<T>(T value, XmlNode xmlNode)
    {
        if (xmlNode != null)
        {
            return (T)Convert.ChangeType(xmlNode.InnerText, typeof(T));
        }
        else
        {
            return value;
        }
    }

    public static Dictionary<string, OrderAction> ReadOrderActions(XmlNode orderActionsNode)
    {
        Dictionary<string, OrderAction> orderActions = new Dictionary<string, OrderAction>();
        if (orderActionsNode != null)
        {
            foreach (XmlNode orderActionNode in orderActionsNode.SelectNodes("OrderAction"))
            {
                orderActions.Add(orderActionNode.Attributes["Type"].InnerText, OrderAction.FromXml(orderActionNode));
            }
        }

        return orderActions;
    }
}