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
        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.Attributes["Name"].InnerText;
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
