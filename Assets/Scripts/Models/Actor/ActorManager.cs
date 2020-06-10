using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class ActorManager : IEnumerable<Actor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActorManager"/> class.
    /// </summary>
    public ActorManager()
    {
        Actors = new List<Actor>();
    }

    /// <summary>
    /// Occurs when a actor is created.
    /// </summary>
    public event Action<Actor> Created;

    public List<Actor> Actors { get; private set; }

    /// <summary>
    /// Create a Actor in the specified tile
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="name"></param>
    /// <param name="race"></param>
    /// <param name="isFemale"></param>
    /// <returns></returns>
    public Actor Create(Tile tile, string race, string name = null, bool isFemale = false, string spriteName = null)
    {
        name = name != null ? name : ActorNameManager.GetNewName(race, isFemale);
        Actor actor = new Actor(tile, name, race, isFemale, spriteName);
        Actors.Add(actor);

        DebugUtils.LogChannel("ActorManager", string.Format("Created Actor {0} ID: {1} a {2} {3} {4}", actor.Name, actor.Id, isFemale ? "female" : "male", actor.Race.Name, spriteName));

        TimeManager.Instance.RegisterFastUpdate(actor);

        if (Created != null)
        {
            Created(actor);
        }

        return actor;
    }

    /// <summary>
    /// A function to return all actor that match the given name.
    /// </summary>
    /// <param name="name">The name of the actor.</param>
    /// <returns>The actor with that name.</returns>
    public IEnumerable<Actor> GetAllFromName(string name)
    {
        return Actors.Where(x => x.Name == name);
    }

    /// <summary>
    /// Returns the actor with the ID wanted.
    /// </summary>
    /// <param name="id"> ID of the actor. </param>
    /// <returns> The actor or null if no actor has Id supplied. </returns>
    public Actor GetFromID(int id)
    {
        return Actors.FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// Gets the actors enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator GetEnumerator()
    {
        return Actors.GetEnumerator();
    }

    /// <summary>
    /// Gets each actor.
    /// </summary>
    /// <returns>Each actor.</returns>
    IEnumerator<Actor> IEnumerable<Actor>.GetEnumerator()
    {
        foreach (Actor actor in Actors)
        {
            yield return actor;
        }
    }
}