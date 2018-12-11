using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameSpeed
{
    Pause, Normal, Fast, VeryFast
}

public class GameSpeedState : MonoBehaviour
{
    public GameSpeed gameSpeed;
    public KeyCode keyCode;
}
