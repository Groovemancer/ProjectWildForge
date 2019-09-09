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

        Structure proto = PrototypeManager.Structure.Get(buildModeObjectType);

        return proto.Width == 1 && proto.Height == 1;
    }


    void OnStructureJobComplete(string objectType, Tile tile)
    {
        World.Current.StructureManager.PlaceStructure(objectType, tile);
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

    public void DoBuild(Tile tile)
    {
        if (buildMode == BuildMode.Structure)
        {
            // Create the Structure and assign it to the tile

            // FIXME: This instantly builds the object
            //WorldController.Instance.World.PlaceStructure(buildModeObjectType, t);

            // Can we build the object in the selected tile?
            // Run the ValidPlacement function
            string structureType = buildModeObjectType;

            if (World.Current.StructureManager.IsPlacementValid(structureType, tile) &&
                tile.PendingStructureJob == null)
            {
                // This tile position is valid for this object
                // Create a job for it to be build
                Job job;

                if (WorldController.Instance.World.structureJobPrototypes.ContainsKey(structureType))
                {
                    // Make a clone of the job prototype
                    job = WorldController.Instance.World.structureJobPrototypes[structureType].Clone();

                    // Assign the correct tile.
                    job.Tile = tile;
                    job.OnJobCompleted += (theJob) => World.Current.StructureManager.ConstructJobCompleted(theJob);
                }
                else
                {
                    job = new Job(tile, structureType, World.Current.StructureManager.ConstructJobCompleted, 100, null, Job.JobPriority.High, "construct")
                    {
                        Adjacent = true,
                        Description = "job_build_" + structureType + "_desc"
                    };

                    job.OnJobCompleted += (theJob) => World.Current.StructureManager.ConstructJobCompleted(theJob);
                }

                job.buildablePrototype = PrototypeManager.Structure.Get(structureType).Clone();

                // FIXME: I don't like having to manually and explicitly set
                // flags to prevent conflicts. It's too easy to forget to set/clear them!
                tile.PendingStructureJob = job;

                job.RegisterJobStoppedCallback((theJob) => { theJob.Tile.PendingStructureJob = null; });

                // Add job to queue later
                World.Current.JobManager.Enqueue(job);
            }
        }
        else if (buildMode == BuildMode.Tile)
        {
            // We are in tile-changing mode.
            tile.SetTileType(buildModeTile);
        }
        else if (buildMode == BuildMode.Deconstruct)
        {
            // TODO
            if (tile.Structure != null)
            {
                tile.Structure.Deconstruct();
            }
        }
        else
        {
            Debug.LogError("UNIMPLEMENTED BUILD MODE");
        }
    }
}
