using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverTileTypeDescText : MouseOverTileBase
{
    protected override void ValidTile()
    {
        m_text.text = StringUtils.GetLocalizedTextFiltered(m_currentTile.Type.DescriptionLocaleId);
    }

    protected override void InvalidTile()
    {
        m_text.text = "";
    }
}
