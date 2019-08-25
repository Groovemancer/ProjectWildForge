using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    BuildMode buildMode = BuildMode.Nothing;
    bool buildModeIsObjects = false;
    TileType buildModeTile = null;
    string buildModeObjectType;

    void OnStructureJobComplete(string objectType, Tile t)
    {
        WorldController.Instance.World.PlaceStructure(objectType, t);
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
            case BuildMode.BuildRoad:
                buildModeIsObjects = false;
                buildModeTile = TileTypeData.GetByFlagName("Road");
                break;
            case BuildMode.BuildTile:
                buildModeIsObjects = false;
                buildModeTile = TileTypeData.GetByFlagName("Floor");
                break;
            case BuildMode.RemoveTile:
                buildModeIsObjects = false;
                buildModeTile = TileTypeData.GetByFlagName("Dirt");
                break;
            case BuildMode.BuildObject:
                buildModeIsObjects = true;
                break;
        }
    }

    public void DoPathfindingTest()
    {
        WorldController.Instance.World.SetupPathfindingExample();
    }

    public void SetBuildStructure(string objStructure)
    {
        buildModeObjectType = objStructure;
    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects)
        {
            // Create the Structure and assign it to the tile

            // FIXME: This instantly builds the object
            //WorldController.Instance.World.PlaceStructure(buildModeObjectType, t);

            // Can we build the object in the selected tile?
            // Run the ValidPlacement function
            string structureType = buildModeObjectType;

            if (WorldController.Instance.World.IsStructurePlacementValid(structureType, t) &&
                t.PendingStructureJob == null)
            {
                // This tile position is valid for this object
                // Create a job for it to be build
                Job j;

                if (WorldController.Instance.World.structureJobPrototypes.ContainsKey(structureType))
                {
                    // Make a clone of the job prototype
                    j = WorldController.Instance.World.structureJobPrototypes[structureType].Clone();
                    // Assign the correct tile.
                    j.Tile = t;
                }
                else
                {
                    j = new Job(t, structureType, StructureActions.JobComplete_StructureBuilding,
                        100, // AUTs needed to complete
                        null
                    );
                }

                // FIXME: I don't like having to manually and explicitly set
                // flags to prevent conflicts. It's too easy to forget to set/clear them!
                t.PendingStructureJob = j;

                j.RegisterJobCancelCallback((theJob) => { theJob.Tile.PendingStructureJob = null; });

                // Add job to queue later
                WorldController.Instance.World.jobQueue.Enqueue(j);
            }
        }
        else
        {
            // We are in tile-changing mode.
            t.Type = buildModeTile;
        }
    }
}
