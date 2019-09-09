using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
public class JobSpriteController : MonoBehaviour
{
    // This bare-bones controller is mostly just going to piggyback
    // on StructureSpriteController because we don't yet fully know
    // what our job system is going to look like in the end.

    Dictionary<Job, GameObject> jobGameObjectMap;

    // Use this for initialization
    void Start()
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();

        // FIXME: No such thing as a job queue yet.
        WorldController.Instance.World.JobManager.JobCreated += OnJobCreated;
    }

    void OnJobCreated(Job job)
    {
        if (job.jobObjectType == null)
        {
            // This job doesn't really have an associated sprite with it, so no need to render.
            return;
        }

        // FIXME: We can only do structure-building jobs.

        if (jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("OnJobCreated for a jobGO that already exists -- most likely a job being RE-QUEUED as opposed to created.");
            //DebugUtils.DisplayError("OnJobCreated for a jobGO that already exists -- most likely a job being RE-QUEUED as opposed to created.");
            return;
        }

        GameObject job_go = new GameObject();
        // Add our job/GO pair to the dictionary.
        jobGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.jobObjectType + "_" + job.Tile.X + "_" + job.Tile.Y;
        job_go.transform.position = new Vector3(job.Tile.X + ((job.structurePrototype.Width - 1) / 2f), job.Tile.Y + ((job.structurePrototype.Height - 1) / 2f), 0);
        job_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = job_go.AddComponent<SpriteRenderer>();
        spr.sprite = WorldController.StructureSpriteController.GetSpriteForStructure(job.jobObjectType);
        spr.color = new Color32(128, 255, 128, 64);
        spr.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not ideal!
        if (job.jobObjectType == "Door")
        {
            // By default, the door graphic is meant for walls to the east & west
            // Check to see if we actually have a wall north/south, and if so
            // then rotate this GO by 90 degress

            Tile northTile = World.Current.GetTileAt(job.Tile.X, job.Tile.Y + 1, job.Tile.Z);
            Tile southTile = World.Current.GetTileAt(job.Tile.X, job.Tile.Y - 1, job.Tile.Z);

            if (northTile != null && southTile != null && northTile.Structure != null && southTile.Structure != null &&
                northTile.Structure.Type == "Wall" && southTile.Structure.Type == "Wall")
            {
                job_go.transform.rotation = Quaternion.Euler(0, 0, 90);
                //job_go.transform.Translate(1f, 0, 0, Space.World);    // UGLY HACK TO COMPENSATE FOR BOTOM_LEFT ANCHOR POINT!
            }
        }

        job.RegisterJobCompletedCallback(OnJobEnded);
        job.RegisterJobStoppedCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {
        // This executes whether the job was COMPLETED or CANCELED

        // FIXME: We can only do structure-building jobs.

        GameObject job_go = jobGameObjectMap[job];

        job.UnregisterJobCompletedCallback(OnJobEnded);
        job.UnregisterJobStoppedCallback(OnJobEnded);

        Destroy(job_go);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
*/

public class JobSpriteController : BaseSpriteController<Job>
{
    // This bare-bones controller is mostly just going to piggyback
    // on StructureSpriteController because we don't yet fully know
    // what our job system is going to look like in the end.
    private StructureSpriteController ssc;

    // Use this for initialization
    public JobSpriteController(World world, StructureSpriteController structureSpriteController)
        : base(world, "Jobs", 200)
    {
        ssc = structureSpriteController;
        world.JobManager.JobCreated += OnCreated;

        foreach (Job job in world.JobManager.PeekAllJobs())
        {
            OnCreated(job);
        }

        foreach (Actor actor in world.ActorManager)
        {
            if (actor.MyJob != null)
            {
                OnCreated(actor.MyJob);
            }
        }
    }

    public override void RemoveAll()
    {
        world.JobManager.JobCreated -= OnCreated; ;

        foreach (Job job in world.JobManager.PeekAllJobs())
        {
            job.OnJobCompleted -= OnRemoved;
            job.OnJobStopped -= OnRemoved;
        }

        foreach (Actor actor in world.ActorManager)
        {
            if (actor.MyJob != null)
            {
                actor.MyJob.OnJobCompleted -= OnRemoved;
                actor.MyJob.OnJobStopped -= OnRemoved;
            }
        }

        base.RemoveAll();
    }

    protected override void OnCreated(Job job)
    {
        if (job.JobTileType == null && job.Type == null)
        {
            // This job doesn't really have an associated sprite with it, so no need to render.
            return;
        }

        // This is weird why do we have this here?
        // OnCreated should not be called twice for a given job?
        // This seems like there is a bug hiding somewhere
        if (objectGameObjectMap.ContainsKey(job))
        {
            return;
        }

        GameObject job_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.Type + "_" + job.Tile.X + "_" + job.Tile.Y + "_" + job.Tile.Z;
        job_go.transform.SetParent(objectParent.transform, true);

        SpriteRenderer sr = job_go.AddComponent<SpriteRenderer>();
        if (job.JobTileType != null)
        {
            // This job is for building a tile.
            // For now, the only tile that could be is the floor, so just show a floor sprite
            // until the graphics system for tiles is fleshed out further.
            job_go.transform.position = job.Tile.Vector3;
            sr.sprite = SpriteManager.GetSprite("Tile", "solid");
            sr.color = new Color32(128, 255, 128, 192);
        }
        else if (job.Description.Contains("deconstruct"))
        {
            sr.sprite = SpriteManager.GetSprite("UI", "CursorCircle");
            sr.color = Color.red;
            job_go.transform.position = job.Tile.Vector3;
        }
        else if (job.Description.Contains("mine"))
        {
            sr.sprite = SpriteManager.GetSprite("UI", "MiningIcon");
            sr.color = new Color(1, 1, 1, 0.25f);
            job_go.transform.position = job.Tile.Vector3;
        }
        else
        {
            // If we get this far we need a buildable prototype, bail if we don't have one
            if (job.buildablePrototype == null)
            {
                return;
            }

            // This is a normal structure job.
            if (job.buildablePrototype.GetType().ToString() == "Structure")
            {
                Structure structureToBuild = (Structure)job.buildablePrototype;
                sr.sprite = ssc.GetSpriteForStructure(job.Type);
                job_go.transform.position = new Vector3(job.Tile.X + ((structureToBuild.Width - 1) / 2f), job.Tile.Y + ((structureToBuild.Height - 1) / 2f), 0);

                // FIXME: This hardcoding is not ideal!     <== Understatement
                if (structureToBuild.HasTypeTag("Door"))
                {
                    // Check to see if we actually have a wall north/south, and if so
                    // set the structure verticalDoor flag to true.
                    Tile northTile = world.GetTileAt(job.Tile.X, job.Tile.Y + 1, job.Tile.Z);
                    Tile southTile = world.GetTileAt(job.Tile.X, job.Tile.Y - 1, job.Tile.Z);

                    if (northTile != null && southTile != null && northTile.Structure != null && southTile.Structure != null &&
                        northTile.Structure.HasTypeTag("Wall") && southTile.Structure.HasTypeTag("Wall"))
                    {
                        structureToBuild.VerticalDoor = true;
                    }
                }
            }

            sr.color = new Color32(128, 255, 128, 64);
        }

        sr.sortingLayerName = "Jobs";

        job.OnJobCompleted += OnRemoved;
        job.OnJobStopped += OnRemoved;
    }

    protected override void OnChanged(Job job)
    {
        
    }

    protected override void OnRemoved(Job job)
    {
        // This executes whether a job was COMPLETED or CANCELLED
        job.OnJobCompleted -= OnRemoved;
        job.OnJobStopped -= OnRemoved;

        GameObject job_go = objectGameObjectMap[job];
        objectGameObjectMap.Remove(job);
        GameObject.Destroy(job_go);
    }
}