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

    GameObject structurePreview;

    StructureSpriteController ssc;

    MouseController mouseController;

    private void Start()
    {
        ssc = GameObject.FindObjectOfType<StructureSpriteController>();
        mouseController = GameObject.FindObjectOfType<MouseController>();

        structurePreview = new GameObject();
        structurePreview.transform.SetParent(this.transform);
        structurePreview.AddComponent<SpriteRenderer>().sortingLayerName = "Jobs";
        structurePreview.SetActive(false);
    }

    private void Update()
    {
        if (buildModeIsObjects == true && !string.IsNullOrEmpty(buildModeObjectType))
        {
            // Show a transparent preview of the object that is color-coded based
            // on whether or not you can actually build the object here.
            ShowStructureSpriteAtTile(buildModeObjectType, mouseController.GetMouseOverTile());
        }
    }

    public bool IsObjectDraggable()
    {
        if (buildModeIsObjects == false)
        {
            // floors are draggable
            return true;
        }

        Structure proto = WorldController.Instance.World.GetStructurePrototype(buildModeObjectType);

        return proto.Width == 1 && proto.Height == 1;
    }

    void ShowStructureSpriteAtTile(string structureType, Tile t)
    {
        structurePreview.SetActive(true);

        SpriteRenderer spr = structurePreview.GetComponent<SpriteRenderer>();
        spr.sprite = ssc.GetSpriteForStructure(structureType);

        if (WorldController.Instance.World.IsStructurePlacementValid(structureType, t))
        {
            spr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            spr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        Structure proto = t.World.GetStructurePrototype(structureType);

        structurePreview.transform.position = new Vector3(t.X + ((proto.Width - 1) / 2f), t.Y + ((proto.Height - 1) / 2f), 0);
    }

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

                j.structurePrototype = WorldController.Instance.World.GetStructurePrototype(structureType);

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
