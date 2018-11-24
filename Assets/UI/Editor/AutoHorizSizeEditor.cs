using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AutoHorizSize))]
public class AutoHorizSizeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Recalc Size"))
        {
            ((AutoHorizSize)target).AdjustSize();
        }
    }
}
