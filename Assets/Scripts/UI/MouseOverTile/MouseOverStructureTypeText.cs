using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverStructureTypeText : MouseOverTileBase
{
    protected override void ValidTile()
    {
        string strStructureName = StringUtils.ToUpper(StringUtils.GetText("$(filter:comment#none)"), 0, 1);

        if (m_currentTile.Structure != null)
        {
            strStructureName = StringUtils.GetText("$(filter:comment#" + m_currentTile.Structure.Name + ")");
        }

        m_text.text = StringUtils.GetText("$(tid:12)") + strStructureName; // Structure: 
    }

    protected override void InvalidTile()
    {
        m_text.text = StringUtils.GetText("$(tid:12)") + StringUtils.ToUpper(StringUtils.GetText("$(filter:comment#none)"), 0, 1); // Structure: none
    }
}
