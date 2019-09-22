using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
[OrderActionName("Build")]
public class Build : OrderAction
{
    public Build()
    {
        Category = PrototypeManager.JobCategory.Get("construct");
        Priority = Job.JobPriority.Medium;
    }

    private Build(Build other) : base(other)
    {
    }

    public override OrderAction Clone()
    {
        return new Build(this);
    }

    public override Job CreateJob(Tile tile, string type)
    {
        Job job = null;
        if (tile != null)
        {
            job = CheckJobFromFunction(JobCostFunction, tile.Structure);
        }
        else
        {
            DebugUtils.LogErrorChannel("Build", "Invalid tile detected. If this wasn't a test, you have an issue.");
        }

        if (job == null)
        {
            job = new Job(
            tile,
            type,
            null,
            JobCost,
            Inventory.Select(it => new RequestedItem(it.Key, it.Value)).ToArray(),
            Priority,
            Category,
            "Building");
            job.Adjacent = true;
            job.Description = "job_build_" + type + "_desc";
            job.OrderName = Type;
        }

        return job;
    }
}
