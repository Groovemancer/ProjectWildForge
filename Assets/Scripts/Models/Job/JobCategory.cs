using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public class JobCategory : IPrototypable
{
    private string type;

    public JobCategory()
    {
    }

    public JobCategory(JobCategory other)
    {
        Name = other.Name;
        IsHidden = other.IsHidden;
    }

    public string Name { get; private set; }

    /// <summary>
    /// Is this a hidden job type? Hidden job types will not show up on the list of jobs.
    /// </summary>
    /// <value>Should this be hidden.</value>
    public bool IsHidden { get; private set; }

    public string Type
    {
        get
        {
            return type;
        }
    }

    public JobCategory Clone()
    {
        return new JobCategory(this);
    }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        type = rootNode.Attributes["Type"].InnerText;
        IsHidden = PrototypeReader.ReadXml(false, rootNode.SelectSingleNode("Hidden"));
        Name = rootNode.SelectSingleNode("Name").InnerText;
    }
}
