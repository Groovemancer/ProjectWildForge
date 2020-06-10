using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverRoomDetails : MouseOverTileBase
{
    protected override void ValidTile()
    {
        string strRoomDetails = "";

        if (m_currentTile.Room != null)
        {
            foreach (string strGas in m_currentTile.Room.GetGasNames())
            {
                strRoomDetails += strGas + ": " + m_currentTile.Room.GetGasAmount(strGas) + " (" + (m_currentTile.Room.GetGasPercentage(strGas) * 100) + "%) ";
            }
        }

        m_text.text = strRoomDetails;
    }

    protected override void InvalidTile()
    {
        m_text.text = "";
    }
}
