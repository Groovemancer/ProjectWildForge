using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    BuildMode buildMode = BuildMode.Nothing;
    bool buildModeIsObjects = false;
    TileType buildModeTile = TileType.Empty;
    string buildModeObjectType;
    
    void OnBuildingJobComplete(string objectType, Tile t)
    {
        WorldController.Instance.World.PlaceBuilding(objectType, t);
    }

    public BuildMode GetBuildMode()
    {
        return buildMode;
    }

    public void SetMode(BuildState buildState)
    {
        buildMode = buildState.State;
        switch (buildState.State)
        {
            case BuildMode.BuildTile:
                buildModeIsObjects = false;
                buildModeTile = TileType.Floor;
                break;
            case BuildMode.RemoveTile:
                buildModeIsObjects = false;
                buildModeTile = TileType.Dirt;
                break;
            case BuildMode.BuildObject:
                buildModeIsObjects = true;
                break;
        }
    }

    public void SetBuildBuilding(string objBuilding)
    {
        buildModeObjectType = ""; // Clear this, will get set later
        buildModeObjectType = objBuilding;
    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects)
        {
            // Create the Building and assign it to the tile

            // FIXME: This instantly builds the object
            //WorldController.Instance.World.PlaceBuilding(buildModeObjectType, t);

            // Can we build the object in the selected tile?
            // Run the ValidPlacement function
            string buildingType = buildModeObjectType;

            if (WorldController.Instance.World.IsBuildingPlacementValid(buildingType, t) &&
                t.PendingBuildingJob == null)
            {
                // This tile position is valid for this object
                // Create a job for it to be build
                Job j = new Job(t, (theJob) =>
                {
                    WorldController.Instance.World.PlaceBuilding(buildingType, theJob.Tile);
                    t.PendingBuildingJob = null;
                });

                // FIXME: I don't like having to manually and explicitly set
                // flags to prevent conflicts. It's too easy to forget to set/clear them!
                t.PendingBuildingJob = j;

                j.RegisterJobCancelCallback((theJob) => { theJob.Tile.PendingBuildingJob = null; });

                // Add job to queue later
                WorldController.Instance.World.jobQueue.Enqueue(j);
                Debug.Log("Job Queue Size: " + WorldController.Instance.World.jobQueue.Count);
            }
        }
        else
        {
            // We are in tile-changing mode.
            t.Type = buildModeTile;
        }
    }
}
