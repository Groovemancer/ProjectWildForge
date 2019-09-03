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

        isInitialized = true;
    }
}