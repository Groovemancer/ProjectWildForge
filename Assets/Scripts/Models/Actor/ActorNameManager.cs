using System.Collections.Generic;
using System.Linq;


/// <summary>
/// actor name manager that holds all the possible names and tries to give a random and unused name everytime is requested.
/// </summary>
public static class ActorNameManager
{
    private static Dictionary<string, string[]> actorNames;
    private static int ptr;
    private static bool isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorNameManager"/> class.
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        actorNames = new Dictionary<string, string[]>();

        isInitialized = true;
    }

    /// <summary>
    /// Load new names to use for Actors.
    /// </summary>
    /// <param name="nameStrings">An array of name strings.</param>
    public static void LoadNames(string raceType, bool isFemale, string[] nameStrings)
    {
        string key = Key(raceType, isFemale);

        // Randomize the given strings
        // Add all the names to the unused queue in the random order

        if (actorNames.ContainsKey(key) == false)
        {
            actorNames.Add(key, new string[0]);
        }

        actorNames[key] = actorNames[key].Concat(nameStrings.OrderBy(c => RandomUtils.value)).ToArray();
    }

    /// <summary>
    /// Returns a randomly chosen name, prioritizing names which have not been used yet.
    /// </summary>
    /// <returns>A randomly chosen name.</returns>
    public static string GetNewName(string raceType, bool isFemale)
    {
        string key = Key(raceType, isFemale);
        // If character names doesn't exist then just return null
        if (actorNames == null || actorNames.ContainsKey(key) == false || actorNames[key].Length == 0)
        {
            return null;
        }

        // Assign name then iterate pointer, the modulo section bounds it to be within the range of the array
        return actorNames[key][ptr++ % actorNames[key].Length];
    }

    private static string Key(string raceType, bool isFemale)
    {
        return raceType + GenderSuffix(isFemale);
    }

    private static string GenderSuffix(bool isFemale)
    {
        return isFemale ? "_F" : "_M";
    }
}
