using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class PlantManager : IEnumerable<Plant>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlantManager"/> class.
    /// </summary>
    public PlantManager()
    {
        Plants = new List<Plant>();
    }

    /// <summary>
    /// Occurs when a plant is created.
    /// </summary>
    public event Action<Plant> Created;

    public List<Plant> Plants { get; private set; }


    /// <summary>
    /// Creates a plant with the given type and places it at the given tile.
    /// </summary>
    /// <returns>The plant.</returns>
    /// <param name="type">The type of the structure.</param>
    /// <param name="tile">The tile to place the structure at.</param>
    public Plant PlacePlant(string type, Tile tile)
    {
        DebugUtils.LogChannel("PlantManager", string.Format("PlantStructure type={0}, tile x={1}, y={2}", type, tile.X, tile.Y));
        Plant plant;
        if (PrototypeManager.Plant.TryGet(type, out plant) == false)
        {
            DebugUtils.LogErrorChannel("PlantManager", "plantPrototypes doesn't contain a proto for key: " + type);
            return null;
        }

        return PlacePlant(plant.Clone(), tile);
    }

    /// <summary>
    /// Places the given plant prototype at the given tile.
    /// </summary>
    /// <returns>The plant.</returns>
    /// <param name="prototype">The plant prototype.</param>
    /// <param name="tile">The tile to place the plant at.</param>
    public Plant PlacePlant(Plant prototype, Tile tile)
    {
        Plant plant = Plant.PlaceInstance(prototype, tile);

        if (plant == null)
        {
            // Failed to place object -- most likely there was already something there.
            return null;
        }

        plant.Removed += OnRemoved;

        Plants.Add(plant);

        TimeManager.Instance.RegisterSlowUpdate(plant);

        if (Created != null)
        {
            Created(plant);
        }

        return plant;
    }




    /// <summary>
    /// Gets the plants enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator GetEnumerator()
    {
        return Plants.GetEnumerator();
    }

    /// <summary>
    /// Gets each plant.
    /// </summary>
    /// <returns>Each plant.</returns>
    IEnumerator<Plant> IEnumerable<Plant>.GetEnumerator()
    {
        foreach (Plant plant in Plants)
        {
            yield return plant;
        }
    }

    /// <summary>
    /// Called when a plant is removed so that it can be deleted from the list.
    /// </summary>
    /// <param name="plant">The structure being removed.</param>
    private void OnRemoved(Plant plant)
    {
        Plants.Remove(plant);
        TimeManager.Instance.UnregisterSlowUpdate(plant);
    }
}