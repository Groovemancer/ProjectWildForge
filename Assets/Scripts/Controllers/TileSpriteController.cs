using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSpriteController : MonoBehaviour
{
    Dictionary<string, Sprite> tileSprites;

    Dictionary<Tile, GameObject> tileGameObjectMap;
    
    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        tileGameObjectMap = new Dictionary<Tile, GameObject>();

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

                SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();

                sr.sprite = null;
                foreach (Sprite spr in tileSprites.Values)
                {
                    if (tile_data.Type.Sprite == spr.name)
                    {
                        sr.sprite = spr;
                        break;
                    }
                }

                sr.sortingLayerName = "Tiles";
            }
        }

        World.RegisterTileChanged(OnTileChanged);
    }

    private void LoadSprites()
    {
        tileSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Tiles");

        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            tileSprites[s.name] = s;
        }
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
        sprRenderer.sprite = null;
        foreach (Sprite spr in tileSprites.Values)
        {
            if (tile_data.Type.Sprite == spr.name)
            {
                sprRenderer.sprite = spr;
                break;
            }
        }
        sprRenderer.sortingLayerName = "Tiles";
    }
}
