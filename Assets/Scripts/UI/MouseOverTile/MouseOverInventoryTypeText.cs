using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverInventoryTypeText : MouseOverTileBase
{
    protected override void ValidTile()
    {
        if (m_currentTile.Inventory != null)
        {
            DebugUtils.LogChannel("MouseOverInventoryTypeText", "ValidTile invName=" + m_currentTile.Inventory.GetName());

            m_text.text = StringUtils.GetLocalizedText("39") + StringUtils.GetLocalizedTextFiltered("comment#" + m_currentTile.Inventory.GetName()); ; // Item Type: 
        }
        else
        {
            m_text.text = "";
        }
    }

    protected override void InvalidTile()
    {
        m_text.text = "";
    }
}
