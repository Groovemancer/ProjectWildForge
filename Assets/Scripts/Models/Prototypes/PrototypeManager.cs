using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class PrototypeManager
{
    private static bool isInitialized = false;

    /// <summary>
    /// Gets the inventory prototype map.
    /// </summary>
    /// <value>The inventory prototype map.</value>
    public static PrototypeMap<Inventory> Inventory { get; private set; }

    /// <summary>
    /// Gets the stat prototype map.
    /// </summary>
    /// <value>The stat prototype map.</value>
    public static PrototypeMap<Stat> Stat { get; private set; }

    /// <summary>
    /// Gets the skill prototype map.
    /// </summary>
    /// <value>The skill prototype map.</value>
    public static PrototypeMap<Skill> Skill { get; private set; }

    /// <summary>
    /// Gets the race prototype map.
    /// </summary>
    /// <value>The race prototype map.</value>
    public static PrototypeMap<Race> Race { get; private set; }

    /// <summary>
    /// Gets the structure prototype map.
    /// </summary>
    /// <value>The furniture prototype map.</value>
    public static PrototypeMap<Structure> Structure { get; private set; }

    /// <summary>
    /// Gets the need prototype map.
    /// </summary>
    /// <value>The need prototype map.</value>
    public static PrototypeMap<Need> Need { get; private set; }

    /// <summary>
    /// Gets the Job Category prototype map.
    /// </summary>
    /// <value>The job category prototype map.</value>
    public static PrototypeMap<JobCategory> JobCategory { get; private set; }

    /// <summary>
    /// Initializes the <see cref="PrototypeManager"/> static class files.
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        Inventory = new PrototypeMap<Inventory>();
        Stat = new PrototypeMap<Stat>();
        Skill = new PrototypeMap<Skill>();
        Race = new PrototypeMap<Race>();
        Structure = new PrototypeMap<Structure>();
        Need = new PrototypeMap<Need>();
        JobCategory = new PrototypeMap<JobCategory>();

        isInitialized = true;
    }
}