using UnityEngine;

public enum BuildMode { Nothing, BuildTile, RemoveTile, BuildObject };

public class BuildState : MonoBehaviour
{
    public BuildMode State;
}