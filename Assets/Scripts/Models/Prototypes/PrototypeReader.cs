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
}