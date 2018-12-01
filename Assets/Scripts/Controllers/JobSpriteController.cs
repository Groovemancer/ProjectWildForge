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
        // FIXME: We can only do structure-building jobs.

        // TODO: Sprite
        GameObject job_go = new GameObject();

        if (jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("OnJobCreated for a jobGO that already exists -- most likely a job being RE-QUEUED as opposed to created.");
            return;
        }

        // Add our job/GO pair to the dictionary.
        jobGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.jobObjectType + "_" + job.Tile.X + "_" + job.Tile.Y;
        job_go.transform.position = new Vector3(job.Tile.X, job.Tile.Y, 0);
        job_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = job_go.AddComponent<SpriteRenderer>();
        spr.sprite = ssc.GetSpriteForStructure(job.jobObjectType);
        spr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        spr.sortingLayerName = "Jobs";

        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {
        // This executes whether the job was COMPLETED or CANCELED

        // FIXME: We can only do structure-building jobs.

        GameObject job_go = jobGameObjectMap[job];

        job.UnregisterJobCompleteCallback(OnJobEnded);
        job.UnregisterJobCancelCallback(OnJobEnded);

        Destroy(job_go);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
