/*
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
*/

#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileSpriteController : BaseSpriteController<Tile>
{
    public Tilemap[] tilemaps;
    public TilemapRenderer[] tilemapRenderers;
    public UnityEngine.Tilemaps.Tile errorTile;
    public UnityEngine.Tilemaps.Tile fogTile;

    public Dictionary<string, TileBase> TileLookup;

    // Use this for initialization
    public TileSpriteController(World world) : base(world, "Tiles", world.Volume)
    {
        world.OnTileChanged += OnChanged;
        world.OnTileTypeChanged += OnChanged;

        TileLookup = new Dictionary<string, TileBase>();
        foreach (var tiletype in TileTypeData.Instance.Data)
        {
            UnityEngine.Tilemaps.Tile tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            Sprite sprite = SpriteManager.GetSprite("Tile", tiletype.FlagName);
            tile.sprite = sprite;
            tile.name = tiletype.FlagName;
            TileLookup[tiletype.FlagName] = tile;
        }

        TileLookup[TileTypeData.Instance.EmptyType.FlagName] = null;

        objectParent.AddComponent<Grid>();

        errorTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        errorTile.sprite = SpriteManager.CreateErrorSprite();
        errorTile.name = "ErrorTile";
        errorTile.color = Color.white;

        fogTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        fogTile.sprite = SpriteManager.CreateBlankSprite();
        fogTile.name = "FogTile";
        fogTile.color = Color.gray;

        tilemaps = new Tilemap[world.Depth];
        tilemapRenderers = new TilemapRenderer[world.Depth];
        for (int z = 0; z < world.Depth; z++)
        {
            GameObject go = new GameObject("Tile layer " + (z + 1));
            go.transform.SetParent(objectParent.transform);
            go.transform.position -= new Vector3(.5f, .5f, -z);

            tilemaps[z] = go.AddComponent<Tilemap>();
            tilemaps[z].orientation = Tilemap.Orientation.XY;
            tilemapRenderers[z] = go.AddComponent<TilemapRenderer>();
            tilemapRenderers[z].sortingLayerID = SortingLayer.NameToID("Tiles");
            tilemapRenderers[z].sortingOrder = -z;

            TileBase[] tiles = new TileBase[world.Width * world.Height];
            BoundsInt bounds = new BoundsInt(0, 0, 0, world.Width, world.Height, 1);

            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    Tile worldTile = world.GetTileAt(x, y, z);

                    tiles[x + (y * world.Width)] = DetermineTileBaseToUse(worldTile);
                }
            }

            tilemaps[z].SetTilesBlock(bounds, tiles);
        }
    }

    public override void RemoveAll()
    {
        world.UnregisterTileChanged(OnChanged);
        world.OnTileChanged -= OnChanged;

        base.RemoveAll();
    }

    protected override void OnCreated(Tile tile)
    {
        OnChanged(tile);
    }

    // This function should be called automatically whenever a tile's data gets changed.
    protected override void OnChanged(Tile tile)
    {
        tilemaps[tile.Z].SetTile(new Vector3Int(tile.X, tile.Y, 0), DetermineTileBaseToUse(tile));
    }

    protected override void OnRemoved(Tile tile)
    {
    }

    private TileBase DetermineTileBaseToUse(Tile tile)
    {
        if (tile.Type == TileTypeData.Instance.EmptyType)
        {
            return null;
        }

        TileBase tilemapTile;
        if (tile.CanSee == false)
        {
            return fogTile;
        }
        else if (TileLookup.TryGetValue(tile.Type.FlagName, out tilemapTile) == false)
        {
            tilemapTile = errorTile;
            DebugUtils.LogWarningChannel("TileSpriteController", string.Format("Could not find graphics tile for type {0}", tile.Type.FlagName));
        }

        return tilemapTile;
    }
}