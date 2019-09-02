using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public interface IPrototypable
{
    /// <summary>
    /// Gets the Type of the prototype
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Reads the prototype from the specified xml node.
    /// </summary>
    /// <param name="xmlNode">The xmlNode containing the prototype.</param>
    void ReadXmlPrototype(XmlNode xmlNode);
}