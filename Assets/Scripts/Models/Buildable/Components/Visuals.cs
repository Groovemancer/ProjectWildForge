using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

[Serializable]
[BuildableComponentName("Visuals")]
public class Visuals : BuildableComponent//, IXmlSerializable
{
    public Visuals()
    {
    }

    private Visuals(Visuals other) : base(other)
    {
        SpriteName = other.SpriteName;
        DefaultSpriteName = other.SpriteName;
        OverlaySpriteName = other.OverlaySpriteName;
        UsedAnimations = other.UsedAnimations;
    }

    [XmlElement("DefaultSpriteName")]
    public SourceDataInfo DefaultSpriteName { get; set; }

    [XmlElement("SpriteName")]
    public SourceDataInfo SpriteName { get; set; }

    [XmlElement("OverlaySpriteName")]
    public SourceDataInfo OverlaySpriteName { get; set; }

    [XmlElement("UseAnimation")]
    public List<UseAnimation> UsedAnimations { get; set; }

    [XmlIgnore]
    public string CurrentAnimationName { get; private set; }

    [XmlIgnore]
    public override bool RequiresSlowUpdate
    {
        get
        {
            return Initialized && (UsedAnimations != null && ParentStructure.Animations != null && UsedAnimations.Count > 0);
        }
    }

    private string DefaultAnimationName { get; set; }

    public override BuildableComponent Clone()
    {
        return new Visuals(this);
    }

    public override void FixedFrequencyUpdate(float deltaTime)
    {
        if (UsedAnimations != null && ParentStructure.Animations != null && UsedAnimations.Count > 0)
        {
            foreach (UseAnimation anim in UsedAnimations)
            {
                if (!string.IsNullOrEmpty(anim.ValueBasedParameterName))
                {
                    // is value based animation
                    if (ParentStructure.Animations != null)
                    {
                        int frmIdx = StructureParams[anim.ValueBasedParameterName].ToInt();
                        ParentStructure.Animations.SetFrameIndex(frmIdx);
                    }
                }
                else if (anim.RunConditions.ParamConditions != null)
                {
                    if (AreParameterConditionsFulfilled(anim.RunConditions.ParamConditions))
                    {
                        ChangeAnimation(anim.Name);
                        break;
                    }
                }
            }
        }
    }

    public override void InitializePrototype(Structure protoStructure)
    {
        // default sprite (used for showing sprite in menu)
        protoStructure.DefaultSpriteName = RetrieveStringFor(DefaultSpriteName, protoStructure);
    }

    public override bool IsValid()
    {
        if (UsedAnimations != null)
        {
            foreach (UseAnimation anim in UsedAnimations)
            {
                if (anim.RunConditions == null && string.IsNullOrEmpty(anim.ValueBasedParameterName))
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected override void Initialize()
    {
        if (UsedAnimations != null && ParentStructure.Animations != null && UsedAnimations.Count > 0)
        {
            ParentStructure.Animations.SetState(UsedAnimations[0].Name);
            DefaultAnimationName = CurrentAnimationName = UsedAnimations[0].Name;
        }

        ParentStructure.Changed += StructureChanged;
        //ParentStructure.IsOperatingChanged += (furniture) => SetDefaultAnimation(furniture.IsOperating);
    }

    private void StructureChanged(Structure obj)
    {
        // regular sprite
        ParentStructure.SpriteName = RetrieveStringFor(SpriteName, ParentStructure);

        // overlay sprite, if any
        //ParentStructure.OverlaySpriteName = RetrieveStringFor(OverlaySpriteName, ParentStructure);
    }

    private void ChangeAnimation(string newAnimation)
    {
        if (newAnimation != CurrentAnimationName && ParentStructure.Animations != null)
        {
            ParentStructure.Animations.SetState(newAnimation);
            CurrentAnimationName = newAnimation;
        }
    }

    private void SetDefaultAnimation(bool setDefault)
    {
        if (setDefault)
        {
            ChangeAnimation(DefaultAnimationName);
        }
    }

    //public override void ReadXml(XmlReader reader)
    //{
    //    base.ReadXml(reader);

    //    while (reader.Read())
    //    {
    //        switch (reader.Name)
    //        {
    //            case "DefaultSpriteName":
    //                // Do stuff
    //                int a = 0;
    //                break;
    //        }
    //    }
    //}
}

