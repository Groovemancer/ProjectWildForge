using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverTileBase : MonoBehaviour
{
    // Every frame, this script checks to see which tile
    // is under the mouse and then updates the GetComponent<Text>.text
    // parameter of the object it is attached to.

    protected Text m_text;
    protected MouseController m_mouseController;
    protected Tile m_currentTile;

    // Use this for initialization
    public void Start()
    {
        m_text = GetComponent<Text>();

        if (m_text == null)
        {
            Debug.LogError("MouseOverTileBase: No 'Text' UI component on this object.");
            this.enabled = false;
            return;
        }
        m_mouseController = GameObject.FindObjectOfType<MouseController>();
        if (m_mouseController == null)
        {
            Debug.LogError("MouseOverTileBase: Cannot find MouseController object");
            this.enabled = false;
            return;
        }
    }

    // Update is called once per frame
    public void Update()
    {
        Tile tile = m_mouseController.GetMouseOverTile();

        if (tile != m_currentTile)
        {
            m_currentTile = tile;
            if (m_currentTile != null)
            {
                ValidTile();
            }
            else
            {
                InvalidTile();
            }
        }
    }

    protected virtual void ValidTile()
    {
        // Overriden by child classes
    }

    protected virtual void InvalidTile()
    {
        // Overriden by child classes
    }
}
