using UnityEngine;

public enum BuildMode { Nothing, BuildTile, RemoveTile, BuildObject, BuildRoad };

public class BuildState : MonoBehaviour
{
    public BuildMode State;
}