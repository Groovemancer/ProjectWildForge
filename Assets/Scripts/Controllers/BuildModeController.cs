using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public void DoReplantTrees()
    {
        WorldController.Instance.World.DebugReplantTrees();
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
                World.Current.StructureManager.IsWorkSpotClear(structureType, tile) &&
                DoesStructureBuildJobOverlapExistingBuildJob(tile, structureType) == false)
            {
                // This tile position is valid for this object
                // Create a job for it to be build
                Job job;

                Structure toBuildProto = PrototypeManager.Structure.Get(structureType);
                OrderAction orderAction = toBuildProto.GetOrderAction<Build>();
                if (orderAction != null)
                {
                    // Make a clone of the job prototype
                    job = orderAction.CreateJob(tile, structureType);
                    job.OnJobCompleted += (theJob) => World.Current.StructureManager.ConstructJobCompleted(theJob);
                }
                else
                {
                    job = new Job(tile, structureType, null, 100, null, Job.JobPriority.High, "construct", "Building")
                    {
                        Adjacent = true,
                        Description = "job_build_" + structureType + "_desc"
                    };

                    job.OnJobCompleted += (theJob) => World.Current.StructureManager.ConstructJobCompleted(theJob);
                }

                job.buildablePrototype = PrototypeManager.Structure.Get(structureType).Clone();

                for (int x_off = tile.X; x_off < (tile.X + job.buildablePrototype.Width); x_off++)
                {
                    for (int y_off = tile.Y; y_off < (tile.Y + job.buildablePrototype.Height); y_off++)
                    {
                        // FIXME: I don't like having to manually and explicitly set
                        // flags that prevent conflicts. It's too easy to forget to set/clear them!
                        Tile offsetTile = World.Current.GetTileAt(x_off, y_off, tile.Z);
                        HashSet<Job> pendingBuildJobs = WorldController.Instance.World.GetTileAt(x_off, y_off, tile.Z).PendingBuildJobs;
                        if (pendingBuildJobs != null)
                        {
                            // if the existing buildJobs structure is replaceable by the current structureType,
                            // we can pretend it does not overlap with the new build

                            // We should only have 1 structure building job per tile, so this should return that job and only that job
                            IEnumerable<Job> pendingStructureJob = pendingBuildJobs.Where(pendingJob => pendingJob.buildablePrototype.GetType() == typeof(Structure));
                            if (pendingStructureJob.Count() == 1)
                            {
                                pendingStructureJob.Single().CancelJob();
                            }
                        }

                        offsetTile.PendingBuildJobs.Add(job);
                        job.OnJobStopped += (theJob) => offsetTile.PendingBuildJobs.Remove(job);
                    }
                }

                // Add job to queue later
                World.Current.JobManager.Enqueue(job);

                // Let our workspot tile know it is reserved for us
                World.Current.ReserveTileAsWorkSpot((Structure)job.buildablePrototype, job.Tile);
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
            if (tile.Plant != null)
            {
                tile.Plant.Deconstruct();
            }
        }
        else
        {
            Debug.LogError("UNIMPLEMENTED BUILD MODE");
        }
    }

    public bool DoesStructureBuildJobOverlapExistingBuildJob(Tile t, string structureType, float rotation = 0)
    {
        Structure structureToBuild = PrototypeManager.Structure.Get(structureType).Clone();

        for (int x_off = t.X; x_off < (t.X + structureToBuild.Width); x_off++)
        {
            for (int y_off = t.Y; y_off < (t.Y + structureToBuild.Height); y_off++)
            {
                HashSet<Job> pendingBuildJobs = WorldController.Instance.World.GetTileAt(x_off, y_off, t.Z).PendingBuildJobs;
                if (pendingBuildJobs != null)
                {
                    // if the existing buildJobs furniture is replaceable by the current furnitureType,
                    // we can pretend it does not overlap with the new build

                    // We should only have 1 furniture building job per tile, so this should return that job and only that job
                    IEnumerable<Job> pendingFurnitureJob = pendingBuildJobs.Where(job => job.buildablePrototype.GetType() == typeof(Structure));
                    if (pendingFurnitureJob.Count() == 1)
                    {
                        return !structureToBuild.ReplaceableStructure.Any(pendingFurnitureJob.Single().buildablePrototype.HasTypeTag);
                    }
                }
            }
        }

        return false;
    }
}
