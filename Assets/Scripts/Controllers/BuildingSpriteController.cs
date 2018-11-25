using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSpriteController : MonoBehaviour
{
    Dictionary<Building, GameObject> buildingGameObjectMap;

    Dictionary<string, Sprite> buildingSprites;
    
    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        buildingGameObjectMap = new Dictionary<Building, GameObject>();

        World.RegisterBuildingCreated(OnBuildingCreated);
    }

    private void LoadSprites()
    {
        buildingSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Buildings");

        Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            Debug.Log(s);
            buildingSprites[s.name] = s;
        }
    }

    public void OnBuildingCreated(Building obj)
    {
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject obj_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        buildingGameObjectMap.Add(obj, obj_go);

        obj_go.name = obj.ObjectType + "_" + obj.Tile.X + "_" + obj.Tile.Y;
        obj_go.transform.position = new Vector3(obj.Tile.X, obj.Tile.Y, 0);
        obj_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = obj_go.AddComponent<SpriteRenderer>();
        spr.sprite = GetSpriteForBuilding(obj);
        spr.sortingLayerName = "Object";

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        obj.RegisterOnChangedCallback(OnBuildingChanged);
    }

    void OnBuildingChanged(Building obj)
    {
        // Make sure the building's graphics are correct.
        if (buildingGameObjectMap.ContainsKey(obj) == false)
        {
            Debug.LogError("OnBuildingChanged -- trying to change visuals for building not in our map.");
            return;
        }
        GameObject obj_go = buildingGameObjectMap[obj];
        obj_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForBuilding(obj);
    }

    Sprite GetSpriteForBuilding(Building obj)
    {
        if (obj.LinksToNeighbor == false)
        {
            return buildingSprites[obj.ObjectType];
        }

        // Otherwise, the sprite name is more complicated.

        string spriteName = obj.ObjectType + "_";

        // Check for neighbors North, East, South, West
        int x = obj.Tile.X;
        int y = obj.Tile.Y;

        Tile t;

        t = World.GetTileAt(x, y + 1);
        if (t != null && t.Building != null && t.Building.ObjectType == obj.ObjectType)
        {
            spriteName += "N";
        }

        t = World.GetTileAt(x + 1, y);
        if (t != null && t.Building != null && t.Building.ObjectType == obj.ObjectType)
        {
            spriteName += "E";
        }

        t = World.GetTileAt(x, y - 1);
        if (t != null && t.Building != null && t.Building.ObjectType == obj.ObjectType)
        {
            spriteName += "S";
        }

        t = World.GetTileAt(x - 1, y);
        if (t != null && t.Building != null && t.Building.ObjectType == obj.ObjectType)
        {
            spriteName += "W";
        }

        // For example, if this object has all four neighbors of
        // the same type, then the string will look like:
        //      Wall_NESW

        if (buildingSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("GetSpriteForBuilding -- No sprites with name: " + spriteName);
            return null;
        }

        return buildingSprites[spriteName];
    }
}
