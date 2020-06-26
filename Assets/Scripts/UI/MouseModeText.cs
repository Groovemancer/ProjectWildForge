using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseModeText : MonoBehaviour
{
    Text myText;
    MouseController mouseController;
    SelectionInfo mySelection;

    // Start is called before the first frame update
    void Start()
    {
        myText = GetComponent<Text>();
        mouseController = GameObject.FindObjectOfType<MouseController>();

        if (myText == null)
        {
            Debug.LogError("MouseModeText: No 'Text' UI component on this object.");
            this.enabled = false;
            return;
        }

        if (mouseController == null)
        {
            Debug.LogError("MouseModeText: No 'MouseController' object in scene.");
            this.enabled = false;
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (myText != null && mouseController != null)
        {
            string strText = "Mouse Mode: " + mouseController.GetCurrentMode().ToString();

            if (mouseController.mySelection != null && mouseController.mySelection.GetSelectedStuff() != null)
            {
                strText += "\nSelected: " + mouseController.mySelection.GetSelectedStuff().GetName();
            }

            myText.text = strText;
        }
    }
}
