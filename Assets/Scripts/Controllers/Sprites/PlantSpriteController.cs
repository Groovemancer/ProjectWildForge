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
        Sprite s = SpriteManager.GetSprite("Plants", proto.GetDefaultSpriteName());

        return s;
    }

    public static SpriteRenderer SetSprite(GameObject inventoryGO, Plant plant, string sortingLayerName = "Inventory")
    {
        SpriteRenderer sr = inventoryGO.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = inventoryGO.AddComponent<SpriteRenderer>();
        }

        sr.sortingLayerName = sortingLayerName;

        sr.sprite = SpriteManager.GetSprite("Plant", plant.Type + "_0"); // TODO - Remove "_0"
        if (sr.sprite == null)
        {
            DebugUtils.LogErrorChannel("PlantSpriteController", "No sprite for: " + plant.Type);
        }

        return sr;
    }

    protected override void OnChanged(Plant plant)
    {
        throw new System.NotImplementedException();
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
            plantGameObject.transform.position = new Vector3(plant.Tile.X, plant.Tile.Y, plant.Tile.Z);
        }

        plantGameObject.transform.SetParent(objectParent.transform, true);

        SetSprite(plantGameObject, plant);
    }

    protected override void OnRemoved(Plant plant)
    {
        GameObject plantGameObject = objectGameObjectMap[plant];
        objectGameObjectMap.Remove(plant);
        GameObject.Destroy(plantGameObject);
    }
}
