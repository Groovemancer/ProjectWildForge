using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverRoomIndexText : MouseOverTileBase
{
    protected override void ValidTile()
    {
        string strRoomId = "N/A";

        if (m_currentTile.Room != null && m_currentTile.Room.Id != -1)
        {
            strRoomId = m_currentTile.Room.Id.ToString();
        }

        m_text.text = StringUtils.GetText("$(tid:13)") + strRoomId; // Room Index: 
    }

    protected override void InvalidTile()
    {
        m_text.text = StringUtils.GetText("$(tid:13)"); // Room Index: 
    }
}
