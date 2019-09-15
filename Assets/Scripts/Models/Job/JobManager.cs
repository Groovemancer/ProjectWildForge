using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectWildForge.Pathfinding;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class JobManager
{
    private Dictionary<JobCategory, HashSet<Job>> jobQueue;

    private ActorJobPriority[] actorPriorityLevels = (ActorJobPriority[])Enum.GetValues(typeof(ActorJobPriority));

    private JobCategory[] categories;

    public JobManager()
    {
        jobQueue = new Dictionary<JobCategory, HashSet<Job>>();
        categories = PrototypeManager.JobCategory.Values.ToArray();
        foreach (JobCategory category in categories)
        {
            jobQueue[category] = new HashSet<Job>();
        }
    }

    public delegate void JobChanged(Job job);

    public event JobChanged JobCreated;

    public event JobChanged JobModified;

    public event JobChanged JobRemoved;

    /// <summary>
    /// Add a job to the JobQueue.
    /// </summary>
    /// <param name="job">The job to be inserted into the Queue.</param>
    public void Enqueue(Job job)
    {
        DebugUtils.LogChannel("JobManager", string.Format("Enqueue({0})", job.Type));

        job.IsBeingWorked = false;

        if (job.Category == null)
        {
            DebugUtils.LogChannel("JobManager", string.Format("Invalid category for job {0}", job));
        }

        jobQueue[job.Category].Add(job);

        if (job.JobCost < 0)
        {
            // Job has a negative job time, so it's not actually
            // supposed to be queued up.  Just insta-complete it.
            job.DoWork(0);
            return;
        }

        if (JobCreated != null)
        {
            JobCreated(job);
        }
    }

    /// <summary>
    /// Search for a job that can be performed by the specified character. Tests that the job can be reached and there is enough inventory to complete it, somewhere.
    /// </summary>
    public Job GetJob(Actor actor)
    {
        DebugUtils.LogChannel("JobManager", string.Format("{0},{1} GetJob() (Queue size: {2})", actor.GetName(), actor.Id, jobQueue.Count));
        if (jobQueue.Count == 0)
        {
            return null;
        }

        foreach (ActorJobPriority actorPriority in actorPriorityLevels)
        {
            List<JobCategory> jobTypes = actor.CategoriesOfPriority(actorPriority);
            foreach (JobCategory category in categories)
            {
                if (jobTypes.Contains(category) == false)
                {
                    continue;
                }

                DebugUtils.LogChannel("JobManager", string.Format("{0} Looking for job of category {1} - {2} options available", actor.Name, category.Type, jobQueue[category].Count));

                Job.JobPriority bestJobPriority = Job.JobPriority.Low;

                // This loop finds the highest priority in the given category
                foreach (Job job in jobQueue[category])
                {
                    if (job.IsActive == false || job.IsBeingWorked == true || job.CanActorReach(actor) == false)
                    {
                        continue;
                    }

                    if (job.CanJobRun(actor.CurrTile.GetNearestRoom(), true) != Job.JobState.Active)
                    {
                        if (JobModified != null)
                        {
                            JobModified(job);
                        }

                        continue;
                    }

                    // Lower numbrs indicate higher priority.
                    if (bestJobPriority > job.Priority)
                    {
                        bestJobPriority = job.Priority;
                    }
                }

                Job bestJob = null;
                float bestJobPathtime = int.MaxValue;

                foreach (Job job in jobQueue[category])
                {
                    if (job.IsActive == false || job.IsBeingWorked == true || job.Priority != bestJobPriority)
                    {
                        continue;
                    }

                    float pathtime = Pathfinder.FindMinPathTime(actor.CurrTile, job.Tile, job.Adjacent, bestJobPathtime);
                    if (pathtime < bestJobPathtime)
                    {
                        bestJob = job;
                        bestJobPathtime = pathtime;
                    }
                }

                if (bestJob != null)
                {
                    DebugUtils.LogChannel("JobManager", string.Format("{0} Job Assigned {1} at {2}", actor.Id, bestJob, bestJob.Tile));
                    if (JobModified != null)
                    {
                        JobModified(bestJob);
                    }

                    return bestJob;
                }
            }
        }

        return null;
    }

    public void Remove(Job job)
    {
        jobQueue[job.Category].Remove(job);

        if (JobRemoved != null)
        {
            JobRemoved(job);
        }
    }

    /// <summary>
    /// Returns an IEnumerable for every job, including jobs that are in the waiting state.
    /// </summary>
    public IEnumerable<Job> PeekAllJobs()
    {
        foreach (IEnumerable<Job> queue in jobQueue.Values)
        {
            foreach (Job job in queue)
            {
                yield return job;
            }
        }
    }
}

