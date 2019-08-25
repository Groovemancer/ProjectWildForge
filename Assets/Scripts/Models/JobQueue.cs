using System;
using System.Collections.Generic;
using UnityEngine;

public class JobQueue
{
    public Queue<Job> jobQueue;

    Action<Job> cbJobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job j)
    {
        if (j.jobCost < 0)
        {
            // Job has a negative job cost, so it's not actually
            // supposed to be queued up. Just insta-complete it.

            j.DoWork(0);
            return;
        }

        jobQueue.Enqueue(j);

        if (cbJobCreated != null)
            cbJobCreated(j);
    }

    public Job Dequeue()
    {
        if (jobQueue.Count == 0)
            return null;

        return jobQueue.Dequeue();
    }

    public void RegisterJobCreationCallback(Action<Job> cb)
    {
        cbJobCreated += cb;
    }

    public void UnregisterJobCreationCallback(Action<Job> cb)
    {
        cbJobCreated -= cb;
    }

    public void Remove(Job j)
    {
        // TODO: Check docs to see if there's a less memory/swappy solution
        List<Job> jobs = new List<Job>(jobQueue);

        if (jobs.Contains(j) == false)
        {
            //Debug.LogError("Trying to remove a job that doesn't exist on the queue.");
            // Most likely, this job wasn't on the queue because a character was working it
            return;
        }

        jobs.Remove(j);
        jobQueue = new Queue<Job>(jobs);
    }
}