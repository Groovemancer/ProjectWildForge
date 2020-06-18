using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSpriteController : BaseSpriteController<Plant>
{
    // Use this for initialization
    public PlantSpriteController(World world) : base(world, "Plant", 500)
    {
        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.PlantManager.Created += OnCreated;

        // Go through any EXISTING structure (i.e. from a save that was loaded OnEnable) and call the OnCreated event manually.
        foreach (Plant plant in world.PlantManager)
        {
            OnCreated(plant);
        }
    }

    public Sprite GetSpriteForPlant(string type)
    {
        Plant proto = PrototypeManager.Plant.Get(type);
        Sprite s = SpriteManager.GetSprite("Plant", proto.GetCurrentSprite());

        return s;
    }

    public static SpriteRenderer SetSprite(GameObject plantGO, Plant plant, string sortingLayerName = "Objects")
    {
        SpriteRenderer sr = plantGO.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = plantGO.AddComponent<SpriteRenderer>();
        }

        sr.sortingLayerName = sortingLayerName;

        sr.sprite = SpriteManager.GetSprite("Plant", plant.GetCurrentSprite());
        sr.sortingOrder = Mathf.RoundToInt(plant.Tile.Y) * -1;
        if (sr.sprite == null)
        {
            DebugUtils.LogErrorChannel("PlantSpriteController", "No sprite for: " + plant.Type);
        }

        return sr;
    }

    protected override void OnChanged(Plant plant)
    {
        // Make sure the structure's graphics are correct.
        GameObject plantGameObject;
        if (objectGameObjectMap.TryGetValue(plant, out plantGameObject) == false)
        {
            DebugUtils.LogErrorChannel("PlantSpriteController", "OnChanged -- trying to change visuals for plant not in our map.");
            return;
        }

        SetSprite(plantGameObject, plant);
    }

    protected override void OnCreated(Plant plant)
    {
        // This creates a new GameObject and adds it to our scene.
        GameObject plantGameObject = new GameObject();

        // Add our plant/GO pair to the dictionary.
        objectGameObjectMap.Add(plant, plantGameObject);

        plantGameObject.name = plant.Type;

        // Only create a Game Object if inventory was created on tile, anything else will handle its own game object
        if (plant.Tile != null)
        {
            plantGameObject.name += string.Format("_{0}_{1}_{2}", plant.Tile.X, plant.Tile.Y, plant.Tile.Z);
            plantGameObject.transform.position = new Vector3(plant.Tile.X, plant.Tile.Y, plant.Tile.Z);
        }

        plantGameObject.transform.SetParent(objectParent.transform, true);

        SetSprite(plantGameObject, plant);

        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        plant.Changed += OnChanged;
        plant.Removed += OnRemoved;
    }

    protected override void OnRemoved(Plant plant)
    {
        // Retrieve gameobject from mapping
        GameObject plantGameObject;
        if (objectGameObjectMap.TryGetValue(plant, out plantGameObject) == false)
        {
            DebugUtils.LogErrorChannel("PlantSpriteController", "OnRemoved -- trying to remove visuals for plant not in our map.");
            return;
        }

        // Unregister our callbacks
        plant.Changed -= OnChanged;
        plant.Removed -= OnRemoved;

        // Remove from gameobject map
        objectGameObjectMap.Remove(plant);

        // Delete game object
        GameObject.Destroy(plantGameObject);
    }

    public override void RemoveAll()
    {
        world.PlantManager.Created -= OnCreated;

        foreach (Plant plant in world.PlantManager)
        {
            plant.Changed -= OnChanged;
            plant.Removed -= OnRemoved;
        }
        base.RemoveAll();
    }
}
