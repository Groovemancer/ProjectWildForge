using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Race : IPrototypable
{
    public Race()
    {
        StatModifiers = new List<Stat>();
    }

    private Race(Race other)
    {
        Type = other.Type;
        Name = other.Name;
        MaleSprites = new List<string>(other.MaleSprites);
        FemaleSprites = new List<string>(other.FemaleSprites);
        ActorMaleNamesFile = other.ActorMaleNamesFile;
        ActorFemaleNamesFile = other.ActorFemaleNamesFile;
        StatModifiers = new List<Stat>(other.StatModifiers);
    }

    public string Type { get; set; }

    public string Name { get; set; }

    public List<string> MaleSprites { get; private set; }

    public List<string> FemaleSprites { get; private set; }

    public string ActorMaleNamesFile { get; set; }
    public string ActorFemaleNamesFile { get; set; }

    public List<Stat> StatModifiers { get; private set; }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["type"].InnerText;
        Name = rootNode.SelectSingleNode("Name").InnerText;

        MaleSprites = ReadSprites(rootNode, "MaleSprites");
        FemaleSprites = ReadSprites(rootNode, "FemaleSprites");

        ActorMaleNamesFile = rootNode.SelectSingleNode("ActorMaleNamesFile").InnerText;
        ActorFemaleNamesFile = rootNode.SelectSingleNode("ActorFemaleNamesFile").InnerText;

        StatModifiers = new List<Stat>();
        XmlNode statModsNode = rootNode.SelectSingleNode("StatMods");
        if (statModsNode != null)
        {
            XmlNodeList statModsNodes = statModsNode.SelectNodes("Stat");
            foreach (XmlNode statModNode in statModsNodes)
            {
                string statType = statModNode.Attributes["type"].InnerText;
                Stat statMod = PrototypeManager.Stat.Get(statType).Clone();
                statMod.Value = int.Parse(statModNode.Attributes["value"].InnerText);
                StatModifiers.Add(statMod);
            }
        }
    }

    private List<string> ReadSprites(XmlNode rootNode, string tag)
    {
        List<string> spriteList = new List<string>();
        XmlNode spritesNode = rootNode.SelectSingleNode(tag);
        if (spritesNode != null)
        {
            XmlNodeList spriteNodes = spritesNode.SelectNodes("Sprite");
            foreach (XmlNode spriteNode in spriteNodes)
            {
                spriteList.Add(spriteNode.InnerText);
            }
        }
        return spriteList;
    }

    public Race Clone()
    {
        return new Race(this);
    }
}