using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; protected set; }

    public List<TileSprite> Sprites; // FIXME

    Dictionary<Tile, GameObject> tileGameObjectMap;
    Dictionary<InstalledObject, GameObject> installedObjectGameObjectMap;

    Dictionary<string, Sprite> installedObjectSprites;

    public World World { get; protected set; }

    // Use this for initialization
    void OnEnable()
    {
        LoadSprites();

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        // Create a world with Empty tiles
        World = new World();

        World.RegisterInstalledObjectCreated(OnInstalledObjectCreated);

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        tileGameObjectMap = new Dictionary<Tile, GameObject>();
        installedObjectGameObjectMap = new Dictionary<InstalledObject, GameObject>();

        // Create a GameObject for each of our tiles, so they show visually.
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                Tile tile_data = World.GetTileAt(x, y);

                GameObject tile_go = new GameObject();

                // Add our tile/GO pair to the dictionary.
                tileGameObjectMap.Add(tile_data, tile_go);

                tile_go.name = "Tile_" + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                tile_go.AddComponent<SpriteRenderer>();
            }
        }

        World.RegisterTileChanged(OnTileChanged);

        // Center the camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);

        World.RandomizeTiles();
    }

    private void LoadSprites()
    {
        installedObjectSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/InstalledObjects");

        Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            Debug.Log(s);
            installedObjectSprites[s.name] = s;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    // EXAMPLE
    void DestroyAllTileGameObjects()
    {
        while (tileGameObjectMap.Count > 0)
        {
            Tile tile_data = tileGameObjectMap.Keys.First();
            GameObject tile_go = tileGameObjectMap[tile_data];

            tileGameObjectMap.Remove(tile_data);

            tile_data.UnregisterTileChangedCallback(OnTileChanged);

            Destroy(tile_go);
        }
    }

    void OnTileChanged(Tile tile_data)
    {
        if (!tileGameObjectMap.ContainsKey(tile_data))
        {
            Debug.LogError("tileGameObjectMap doesn't contain the tile_data.");
            return;
        }

        GameObject tile_go = tileGameObjectMap[tile_data];

        if (tile_go == null)
        {
            Debug.LogError("tileGameObjectMap's returned GameObject is null");
            return;
        }

        SpriteRenderer sprRenderer = tile_go.GetComponent<SpriteRenderer>();
        sprRenderer.sprite = Sprites.Find(spr => spr.Type == tile_data.Type).Sprite;
        sprRenderer.sortingLayerName = "Tile";
    }

    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return World.GetTileAt(x, y);
    }

    public void OnInstalledObjectCreated(InstalledObject obj)
    {
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject obj_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        installedObjectGameObjectMap.Add(obj, obj_go);

        obj_go.name = obj.ObjectType + "_" + obj.Tile.X + "_" + obj.Tile.Y;
        obj_go.transform.position = new Vector3(obj.Tile.X, obj.Tile.Y, 0);
        obj_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = obj_go.AddComponent<SpriteRenderer>();
        spr.sprite = GetSpriteForInstalledObject(obj);
        spr.sortingLayerName = "Object";

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        obj.RegisterOnChangedCallback(OnInstalledObjectChanged);
    }

    void OnInstalledObjectChanged(InstalledObject obj)
    {
        // Make sure the installed object's graphics are correct.
        if (installedObjectGameObjectMap.ContainsKey(obj) == false)
        {
            Debug.LogError("OnInstalledObjectChanged -- trying to change visuals for installed object not in our map.");
            return;
        }
        GameObject obj_go = installedObjectGameObjectMap[obj];
        obj_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForInstalledObject(obj);
    }

    Sprite GetSpriteForInstalledObject(InstalledObject obj)
    {
        if (obj.LinksToNeighbor == false)
        {
            return installedObjectSprites[obj.ObjectType];
        }

        // Otherwise, the sprite name is more complicated.

        string spriteName = obj.ObjectType + "_";

        // Check for neighbors North, East, South, West
        int x = obj.Tile.X;
        int y = obj.Tile.Y;

        Tile t;

        t = World.GetTileAt(x, y + 1);
        if (t != null && t.InstalledObject != null && t.InstalledObject.ObjectType == obj.ObjectType)
        {
            spriteName += "N";
        }

        t = World.GetTileAt(x + 1, y);
        if (t != null && t.InstalledObject != null && t.InstalledObject.ObjectType == obj.ObjectType)
        {
            spriteName += "E";
        }

        t = World.GetTileAt(x, y - 1);
        if (t != null && t.InstalledObject != null && t.InstalledObject.ObjectType == obj.ObjectType)
        {
            spriteName += "S";
        }

        t = World.GetTileAt(x - 1, y);
        if (t != null && t.InstalledObject != null && t.InstalledObject.ObjectType == obj.ObjectType)
        {
            spriteName += "W";
        }

        // For example, if this object has all four neighbors of
        // the same type, then the string will look like:
        //      Wall_NESW

        if (installedObjectSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("GetSpriteForInstalledObject -- No sprites with name: " + spriteName);
            return null;
        }

        return installedObjectSprites[spriteName];
    }
}
