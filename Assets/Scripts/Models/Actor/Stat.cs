using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public class Stat : IPrototypable
{
    public Stat()
    {

    }

    private Stat(Stat other)
    {
        Type = other.Type;
        Name = other.Name;
    }

    public string Type { get; set; }

    public string Name { get; set; }

    public int Value { get; set; }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["type"].InnerText;
        Name = rootNode.Attributes["name"].InnerText;
    }

    public Stat Clone()
    {
        return new Stat(this);
    }

    public override string ToString()
    {
        return string.Format("{0}: {1}", Type, Value);
    }
}
