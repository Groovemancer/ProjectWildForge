using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AutoSize : MonoBehaviour
{
    public enum AutoSizeAlignment { Horizontal, Vertical }

    public float childSize      = 160f;
    public float topSpacing     = 0f;
    public float bottomSpacing  = 0f;
    public float leftSpacing    = 0f;
    public float rightSpacing   = 0f;

    public AutoSizeAlignment alignment;

    // Use this for initialization
    void Start()
    {
        AdjustSize();
    }

    void Update()
    {
        AdjustSize();
    }

    public void AdjustSize()
    {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        if (alignment == AutoSizeAlignment.Horizontal)
        {
            size.x = this.transform.childCount * childSize + leftSpacing + rightSpacing;
        }
        if (alignment == AutoSizeAlignment.Vertical)
        {
            size.y = this.transform.childCount * childSize + topSpacing + bottomSpacing;
        }
        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
