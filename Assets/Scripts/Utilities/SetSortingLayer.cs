using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSortingLayer : MonoBehaviour
{
    public string sortingLayerName = "Default";
    
    void Start()
    {
        GetComponent<Renderer>().sortingLayerName = sortingLayerName;
    }
}
