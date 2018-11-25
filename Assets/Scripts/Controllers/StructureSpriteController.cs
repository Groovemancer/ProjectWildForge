using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureSpriteController : MonoBehaviour
{
    Dictionary<Structure, GameObject> structureGameObjectMap;

    Dictionary<string, Sprite> structureSprites;
    
    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        structureGameObjectMap = new Dictionary<Structure, GameObject>();

        World.RegisterStructureCreated(OnStructureCreated);
    }

    private void LoadSprites()
    {
        structureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Structures");

        Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            Debug.Log(s);
            structureSprites[s.name] = s;
        }
    }

    public void OnStructureCreated(Structure obj)
    {
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject obj_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        structureGameObjectMap.Add(obj, obj_go);

        obj_go.name = obj.ObjectType + "_" + obj.Tile.X + "_" + obj.Tile.Y;
        obj_go.transform.position = new Vector3(obj.Tile.X, obj.Tile.Y, 0);
        obj_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = obj_go.AddComponent<SpriteRenderer>();
        spr.sprite = GetSpriteForStructure(obj);
        spr.sortingLayerName = "Object";

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        obj.RegisterOnChangedCallback(OnStructureChanged);
    }

    void OnStructureChanged(Structure obj)
    {
        // Make sure the structure's graphics are correct.
        if (structureGameObjectMap.ContainsKey(obj) == false)
        {
            Debug.LogError("OnStructureChanged -- trying to change visuals for structure not in our map.");
            return;
        }
        GameObject obj_go = structureGameObjectMap[obj];
        obj_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForStructure(obj);
    }

    public Sprite GetSpriteForStructure(Structure obj)
    {
        if (obj.LinksToNeighbor == false)
        {
            return structureSprites[obj.ObjectType];
        }

        // Otherwise, the sprite name is more complicated.

        string spriteName = obj.ObjectType + "_";

        // Check for neighbors North, East, South, West
        int x = obj.Tile.X;
        int y = obj.Tile.Y;

        Tile t;

        t = World.GetTileAt(x, y + 1);
        if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
        {
            spriteName += "N";
        }

        t = World.GetTileAt(x + 1, y);
        if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
        {
            spriteName += "E";
        }

        t = World.GetTileAt(x, y - 1);
        if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
        {
            spriteName += "S";
        }

        t = World.GetTileAt(x - 1, y);
        if (t != null && t.Structure != null && t.Structure.ObjectType == obj.ObjectType)
        {
            spriteName += "W";
        }

        // For example, if this object has all four neighbors of
        // the same type, then the string will look like:
        //      Wall_NESW

        if (structureSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("GetSpriteForStructure -- No sprites with name: " + spriteName);
            return null;
        }

        return structureSprites[spriteName];
    }

    public Sprite GetSpriteForStructure(string objectType)
    {
        if (structureSprites.ContainsKey(objectType))
        {
            return structureSprites[objectType];
        }

        if (structureSprites.ContainsKey(objectType+"_"))
        {
            return structureSprites[objectType+"_"];
        }

        Debug.LogError("GetSpriteForStructure -- No sprites with name: " + objectType);
        return null;
    }
}
