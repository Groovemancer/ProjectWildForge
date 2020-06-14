using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameSpeed
{
    Pause, Normal, Fast, VeryFast, SuperFast, SuperDuperFast, MegaFast, UberFast, UltraFast, OmegaFast
}

public class GameSpeedState : MonoBehaviour
{
    public GameSpeed gameSpeed;
}
