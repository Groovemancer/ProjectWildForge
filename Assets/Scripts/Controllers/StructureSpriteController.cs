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

        foreach (Structure structure in World.structures)
        {
            OnStructureCreated(structure);
        }
    }

    private void LoadSprites()
    {
        structureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Structures");

        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            structureSprites[s.name] = s;
        }
    }

    public void OnStructureCreated(Structure strct)
    {
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject strct_go = new GameObject();
        
        // Add our tile/GO pair to the dictionary.
        structureGameObjectMap.Add(strct, strct_go);

        strct_go.name = strct.ObjectType + "_" + strct.Tile.X + "_" + strct.Tile.Y;
        strct_go.transform.position = new Vector3(strct.Tile.X, strct.Tile.Y, 0);
        strct_go.transform.SetParent(this.transform, true);

        // FIXME: This hardcoding is not ideal!
        if (strct.ObjectType == "Door")
        {
            // By default, the door graphic is meant for walls to the east & west
            // Check to see if we actually have a wall north/south, and if so
            // then rotate this GO by 90 degress

            Tile northTile = World.GetTileAt(strct.Tile.X, strct.Tile.Y + 1);
            Tile southTile = World.GetTileAt(strct.Tile.X, strct.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Structure != null && southTile.Structure != null &&
                northTile.Structure.ObjectType == "Wall" && southTile.Structure.ObjectType == "Wall")
            {
                strct_go.transform.rotation = Quaternion.Euler(0, 0, 90);
                //strct_go.transform.Translate(1f, 0, 0, Space.World);    // UGLY HACK TO COMPENSATE FOR BOTOM_LEFT ANCHOR POINT!
            }
        }

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = strct_go.AddComponent<SpriteRenderer>();
        spr.sprite = GetSpriteForStructure(strct);
        spr.sortingLayerName = "Structures";
        spr.color = strct.tint;

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        strct.RegisterOnChangedCallback(OnStructureChanged);
    }

    void OnStructureChanged(Structure strct)
    {
        // Make sure the structure's graphics are correct.
        if (structureGameObjectMap.ContainsKey(strct) == false)
        {
            Debug.LogError("OnStructureChanged -- trying to change visuals for structure not in our map.");
            return;
        }
        GameObject obj_go = structureGameObjectMap[strct];
        obj_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForStructure(strct);
        obj_go.GetComponent<SpriteRenderer>().color = strct.tint;
    }

    public Sprite GetSpriteForStructure(Structure strct)
    {
        string spriteName = strct.ObjectType;

        if (strct.LinksToNeighbor == false)
        {
            // If this is a DOOR, let's check OPENNESS and update the sprite.
            // FIXME: All this hardcoding needs to be generalized later
            if (strct.ObjectType == "Door")
            {
                if (strct.GetParameter("openness") < 0.1f)
                {
                    // Door is closed
                    spriteName = "Door";
                }
                else if (strct.GetParameter("openness") < 0.5f)
                {
                    // Door is a bit open
                    spriteName = "Door_openness_1";
                }
                else if (strct.GetParameter("openness") < 0.9f)
                {
                    // Door is a lot open
                    spriteName = "Door_openness_2";
                }
                else
                {
                    // Door is fully open
                    spriteName = "Door_openness_3";
                }
            }

            return structureSprites[spriteName];
        }

        // Otherwise, the sprite name is more complicated.

        spriteName = strct.ObjectType + "_";

        // Check for neighbors North, East, South, West
        int x = strct.Tile.X;
        int y = strct.Tile.Y;

        Tile t;

        t = World.GetTileAt(x, y + 1);
        if (t != null && t.Structure != null && t.Structure.ObjectType == strct.ObjectType)
        {
            spriteName += "N";
        }

        t = World.GetTileAt(x + 1, y);
        if (t != null && t.Structure != null && t.Structure.ObjectType == strct.ObjectType)
        {
            spriteName += "E";
        }

        t = World.GetTileAt(x, y - 1);
        if (t != null && t.Structure != null && t.Structure.ObjectType == strct.ObjectType)
        {
            spriteName += "S";
        }

        t = World.GetTileAt(x - 1, y);
        if (t != null && t.Structure != null && t.Structure.ObjectType == strct.ObjectType)
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
