using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BuildMode { Tile, Structure, Deconstruct };

public class BuildModeController : MonoBehaviour
{
    public BuildMode buildMode = BuildMode.Tile;
    TileType buildModeTile = null;
    public string buildModeObjectType;

    MouseController mouseController;

    private void Start()
    {
        
    }

    public bool IsObjectDraggable()
    {
        if (buildMode == BuildMode.Tile || buildMode == BuildMode.Deconstruct)
        {
            // floors are draggable
            return true;
        }

        Structure proto = WorldController.Instance.World.GetStructurePrototype(buildModeObjectType);

        return proto.Width == 1 && proto.Height == 1;
    }


    void OnStructureJobComplete(string objectType, Tile t)
    {
        WorldController.Instance.World.PlaceStructure(objectType, t);
    }

    public BuildMode GetBuildMode()
    {
        return buildMode;
    }

    public void SetMode_Tile()
    {
        buildMode = BuildMode.Tile;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetMode_Structure()
    {
        buildMode = BuildMode.Structure;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetMode_Deconstruct()
    {
        buildMode = BuildMode.Deconstruct;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetMode(BuildMode _buildMode)
    {
        this.buildMode = _buildMode;
        switch (buildMode)
        {
            case BuildMode.Tile:
                SetMode_Tile();
                break;
            case BuildMode.Structure:
                SetMode_Structure();
                break;
            case BuildMode.Deconstruct:
                SetMode_Deconstruct();
                break;
        }
    }

    public void DoPathfindingTest()
    {
        WorldController.Instance.World.SetupPathfindingExample();
    }

    public void SetBuildTileType(string tileType)
    {
        buildModeTile = TileTypeData.GetByFlagName(tileType);
    }

    public void SetBuildStructure(string objStructure)
    {
        buildModeObjectType = objStructure;
    }

    public void DoBuild(Tile t)
    {
        if (buildMode == BuildMode.Structure)
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

                j.structurePrototype = WorldController.Instance.World.GetStructurePrototype(structureType);

                // FIXME: I don't like having to manually and explicitly set
                // flags to prevent conflicts. It's too easy to forget to set/clear them!
                t.PendingStructureJob = j;

                j.RegisterJobCancelCallback((theJob) => { theJob.Tile.PendingStructureJob = null; });

                // Add job to queue later
                WorldController.Instance.World.jobQueue.Enqueue(j);
            }
        }
        else if (buildMode == BuildMode.Tile)
        {
            // We are in tile-changing mode.
            t.Type = buildModeTile;
        }
        else if (buildMode == BuildMode.Deconstruct)
        {
            // TODO
            if (t.Structure != null)
            {
                t.Structure.Deconstruct();
            }
        }
        else
        {
            Debug.LogError("UNIMPLEMENTED BUILD MODE");
        }
    }
}
