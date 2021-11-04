using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : Text
{
    private bool m_invalidate = false;

    protected override void OnValidate()
    {
        if (Application.isPlaying)
        {
            if (LocaleData.IsLoaded() && m_invalidate)
            {
                text = StringUtils.GetText(text);
                m_invalidate = false;
            }
            else
            {
                m_invalidate = true;
            }
        }
        
        base.OnValidate();
    }

    private void Update()
    {
        if (LocaleData.IsLoaded() && m_invalidate)
        {
            OnValidate();
        }
    }
}