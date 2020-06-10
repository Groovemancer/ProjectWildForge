using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverStructureTypeText : MouseOverTileBase
{
    protected override void ValidTile()
    {
        string strStructureName = StringUtils.ToUpper(StringUtils.GetLocalizedTextFiltered("comment#none"), 0, 1);

        if (m_currentTile.Structure != null)
        {
            strStructureName = StringUtils.GetLocalizedTextFiltered("comment#" + m_currentTile.Structure.Name);
        }

        m_text.text = StringUtils.GetLocalizedText("12") + strStructureName; // Structure: 
    }

    protected override void InvalidTile()
    {
        m_text.text = StringUtils.GetLocalizedText("12") + StringUtils.ToUpper(StringUtils.GetLocalizedTextFiltered("comment#none"), 0, 1); // Structure: none
    }
}
