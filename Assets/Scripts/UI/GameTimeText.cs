using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameTimeText : MonoBehaviour
{
    Text myText;

    // Start is called before the first frame update
    void Start()
    {
        myText = GetComponent<Text>();

        if (myText == null)
        {
            Debug.LogError("GameTimeText: No 'Text' UI component on this object.");
            this.enabled = false;
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (myText != null)
        {
            myText.text = TimeManager.Instance.WorldTime.TimeToString() + " " + TimeManager.Instance.WorldTime.DateToString();
        }
    }
}
