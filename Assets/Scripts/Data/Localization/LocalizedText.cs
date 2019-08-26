using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    public string text = null;

    // Use this for initialization
    void Start()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        Text tc = GetComponent<Text>();

        if (tc != null)
            tc.text = StringUtils.GetText(text);
        else
            tc.text = StringUtils.GetText(tc.text);
    }
}
