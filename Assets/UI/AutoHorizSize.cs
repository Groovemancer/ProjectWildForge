using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoHorizSize : MonoBehaviour
{
    public float childWidth = 160f;

    // Use this for initialization
    void Start()
    {
        AdjustSize();
    }

    public void AdjustSize()
    {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        size.x = this.transform.childCount * childWidth;
        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
