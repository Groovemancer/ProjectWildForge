using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectWildForge.Pathfinding;

public class JobState : State
{
    private bool jobFinished = false;

    public JobState(Actor actor, Job job, State nextState = null)
        : base("Job", actor, nextState)
    {
        this.Job = job;

        job.OnJobCompleted += OnJobCompleted;
        job.OnJobStopped += OnJobStopped;
        job.IsBeingWorked = true;

        DebugLog("created {0}", job.Type ?? "Unnamed Job");
    }

    public Job Job { get; private set; }

    public override void Update(float deltaAuts)
    {
        if (jobFinished)
        {
            DebugLog(" - Update called on a finished job");
            Finished();
            actor.Acted = false;
            return;
        }

        // If we are lacking material, then go deliver materials
        if (Job.MaterialNeedsMet() == false)
        {
            if (Job.IsRequiredInventoriesAvailable() == false)
            {
                AbandonJob();
                Finished();
                actor.Acted = false;
                return;
            }

            DebugLog(" - Next action: Haul material");
            actor.SetState(new HaulState(actor, Job, this));
            actor.Acted = false;
        }
        else if (Job.IsTileAtJobSite(actor.CurrTile) == false)
        {
            DebugLog(" - Neext action: Go to job");
            List<Tile> path = Pathfinder.FindPathToTile(actor.CurrTile, Job.Tile, Job.Adjacent);
            if (path != null && path.Count > 0)
            {
                actor.SetState(new MoveState(actor, Job.IsTileAtJobSite, path, this));
            }
            else
            {
                Interrupt();
            }
            actor.Acted = false;
        }
        else
        {
            //DebugLog(" - Next action: Work");

            if (Job.Tile != actor.CurrTile)
            {
                // TODO Add tile facing if we want it later
                // We aren't standing on the job spot itself so make sure to face it.
                //actor.FaceTile(Job.Tile);
            }

            if (actor.ActionPoints >= actor.WorkCost)
            {
                Job.DoWork(actor.WorkRate);
                actor.ActionPoints -= actor.WorkCost;
                actor.GainSkillExperience(Job.SkillType, actor.WorkCost);
            }
            actor.Acted = true;
        }
    }

    public override void Interrupt()
    {
        // If we still have a reference to a job, then someone else is stealing the state and we should put it back on the queue.
        if (Job != null)
        {
            AbandonJob();
        }
        base.Interrupt();
    }

    private void AbandonJob()
    {
        DebugLog(" - Job abandoned!");
        DebugUtils.LogChannel("Actor", string.Format("{0}, {1} abandoned their job.", actor.GetName(), actor.Id));

        Job.OnJobCompleted -= OnJobCompleted;
        Job.OnJobStopped -= OnJobStopped;
        Job.IsBeingWorked = false;

        // Tell anyone else who cares that it was cancelled
        // We formerly called Called job.CancelJob() here, but that is incorrect for pausing a job, we may have to do cleanup that is now no longer done
        if (Job.IsNeed)
        {
            return;
        }

        // If the job gets abandoned because of pathing issues or something else, just return it to the queue
        World.Current.JobManager.Enqueue(Job);

        // Tell the player that we need a new task.
        actor.SetState(null);
    }

    private void OnJobStopped(Job stoppedJob)
    {
        DebugLog(" - Job stopped");

        jobFinished = true;

        // Job completed (if non-repeating) or was cancelled.
        stoppedJob.OnJobCompleted -= OnJobCompleted;
        stoppedJob.OnJobStopped -= OnJobStopped;
        Job.IsBeingWorked = false;

        if (Job != stoppedJob)
        {
            DebugUtils.LogErrorChannel("Actor", "Actor being told about job that isn't his. You forgot to unregister something.");
            return;
        }
    }

    private void OnJobCompleted(Job finishedJob)
    {
        // Finish job, unless it repeats, in which case continue as if nothing happened.
        if (finishedJob.IsRepeating == false)
        {
            DebugLog(" - Job finished");

            jobFinished = true;

            finishedJob.OnJobCompleted -= OnJobCompleted;
            finishedJob.OnJobStopped -= OnJobStopped;

            if (Job != finishedJob)
            {
                DebugUtils.LogErrorChannel("Actor", "Actor being told about job that isn't his. You forgot to unregister something.");
                return;
            }
        }
    }
}
