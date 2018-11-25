using UnityEngine;

public enum MouseMode { Hover, BuildFloor, RemoveFloor, BuildObject };

public class MouseState : MonoBehaviour
{
    public MouseMode State;
}