using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverTileTypeText : MouseOverTileBase
{
    protected override void ValidTile()
    {
        m_text.text = StringUtils.GetText("$(tid:11)") + StringUtils.GetLocalizedTextFiltered(m_currentTile.Type.NameLocaleId); // Tile Type: 
    }

    protected override void InvalidTile()
    {
        m_text.text = StringUtils.GetText("$(tid:11)"); // Tile Type: 
    }
}
