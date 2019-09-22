using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public class Skill : IPrototypable
{
    public Skill()
    {
    }

    private Skill(Skill other)
    {
        Type = other.Type;
        Name = other.Name;
    }

    const int MAX_LEVEL = 100;
    const int EXP_PER_LEVEL = 1000;

    public string Type { get; set; }

    public string Name { get; set; }

    public SkillCategory Category { get; set; }

    public int Value { get; set; }

    public float Experience { get; set; }

    public void ReadXmlPrototype(XmlNode rootNode)
    {
        Type = rootNode.Attributes["Type"].InnerText;
        Name = rootNode.Attributes["Name"].InnerText;
        Category = (SkillCategory)Enum.Parse(typeof(SkillCategory), rootNode.Attributes["Category"].InnerText);
    }

    public Skill Clone()
    {
        return new Skill(this);
    }

    public override string ToString()
    {
        return string.Format("{0}: {1}", Type, Value);
    }

    public enum SkillCategory
    {
        Profession,
        Combat
    }

    public void GainExperience(float experience)
    {
        if (Value < MAX_LEVEL)
        {
            Experience += Math.Max(experience, 0);

            if (Experience >= ((Value + 1) * EXP_PER_LEVEL))
            {
                Experience -= ((Value + 1) * EXP_PER_LEVEL);
                Value++;
                DebugUtils.LogChannel("Skill", string.Format("{0} increased to: {1}", Type, Value));
            }
        }
    }
}
