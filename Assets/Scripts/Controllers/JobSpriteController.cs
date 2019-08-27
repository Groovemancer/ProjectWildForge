using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour
{
    // This bare-bones controller is mostly just going to piggyback
    // on StructureSpriteController because we don't yet fully know
    // what our job system is going to look like in the end.

    StructureSpriteController ssc;

    Dictionary<Job, GameObject> jobGameObjectMap;

    // Use this for initialization
    void Start()
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();
        ssc = GameObject.FindObjectOfType<StructureSpriteController>();

        // FIXME: No such thing as a job queue yet.
        WorldController.Instance.World.jobQueue.RegisterJobCreationCallback(OnJobCreated);
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
        spr.sprite = ssc.GetSpriteForStructure(job.jobObjectType);
        spr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        spr.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not ideal!
        if (job.jobObjectType == "Door")
        {
            // By default, the door graphic is meant for walls to the east & west
            // Check to see if we actually have a wall north/south, and if so
            // then rotate this GO by 90 degress

            Tile northTile = World.current.GetTileAt(job.Tile.X, job.Tile.Y + 1);
            Tile southTile = World.current.GetTileAt(job.Tile.X, job.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Structure != null && southTile.Structure != null &&
                northTile.Structure.ObjectType == "Wall" && southTile.Structure.ObjectType == "Wall")
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
