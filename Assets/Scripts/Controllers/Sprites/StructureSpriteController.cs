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
//using ProjectPorcupine.Buildable.Components;
using UnityEngine;

public class StructureSpriteController : BaseSpriteController<Structure>
{
    private const float IndicatorOffset = 0.25f;
    private const float IndicatorScale = 0.5f;

    private Dictionary<Structure, GameObject> childObjectMap;

    //private Dictionary<BuildableComponent.Requirements, Vector3> statusIndicatorOffsets;

    // Use this for initialization
    public StructureSpriteController(World world) : base(world, "Structure", world.Volume / 2)
    {
        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        childObjectMap = new Dictionary<Structure, GameObject>();// = new Dictionary<Structure, StructureChildObjects>();

        //statusIndicatorOffsets = new Dictionary<BuildableComponent.Requirements, Vector3>();
        //statusIndicatorOffsets[BuildableComponent.Requirements.Power] = new Vector3(IndicatorOffset, -IndicatorOffset, 0);
        //statusIndicatorOffsets[BuildableComponent.Requirements.Fluid] = new Vector3(-IndicatorOffset, -IndicatorOffset, 0);
        //statusIndicatorOffsets[BuildableComponent.Requirements.Gas] = new Vector3(IndicatorOffset, IndicatorOffset, 0);
        //statusIndicatorOffsets[BuildableComponent.Requirements.Production] = new Vector3(-IndicatorOffset, IndicatorOffset, 0);

        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.StructureManager.Created += OnCreated;

        // Go through any EXISTING structure (i.e. from a save that was loaded OnEnable) and call the OnCreated event manually.
        foreach (Structure structure in world.StructureManager)
        {
            OnCreated(structure);
        }
    }

    public override void RemoveAll()
    {
        world.StructureManager.Created -= OnCreated;

        foreach (Structure structure in world.StructureManager)
        {
            structure.Changed -= OnChanged;
            structure.Removed -= OnRemoved;
            //structure.IsOperatingChanged -= OnIsOperatingChanged;
        }
        /*
        foreach (StructureChildObjects childObjects in childObjectMap.Values)
        {
            childObjects.Destroy(childObjects);
        }

        childObjectMap.Clear();
        */
        base.RemoveAll();
    }

    public Sprite GetSpriteForStructure(string type)
    {
        Structure proto = PrototypeManager.Structure.Get(type);
        Sprite s = SpriteManager.GetSprite("Structure", proto.GetDefaultSpriteName());

        return s;
    }

    public Sprite GetSpriteForStructure(Structure structure)
    {
        bool explicitSpriteUsed;
        string spriteName = structure.GetSpriteName(out explicitSpriteUsed);

        if (explicitSpriteUsed || string.IsNullOrEmpty(structure.LinksToNeighbors))
        {
            return SpriteManager.GetSprite("Structure", spriteName);
        }

        // Otherwise, the sprite name is more complicated.
        spriteName += "_";

        // Check for neighbours North, East, South, West, Northeast, Southeast, Southwest, Northwest
        int x = structure.Tile.X;
        int y = structure.Tile.Y;
        string suffix = string.Empty;

        suffix += GetSuffixForNeighbour(structure, x, y + 1, structure.Tile.Z, "N");
        suffix += GetSuffixForNeighbour(structure, x + 1, y, structure.Tile.Z, "E");
        suffix += GetSuffixForNeighbour(structure, x, y - 1, structure.Tile.Z, "S");
        suffix += GetSuffixForNeighbour(structure, x - 1, y, structure.Tile.Z, "W");

        // Now we check if we have the neighbours in the cardinal directions next to the respective diagonals
        // because pure diagonal checking would leave us with diagonal walls and stockpiles, which make no sense.
        suffix += GetSuffixForDiagonalNeighbour(suffix, "N", "E", structure, x + 1, y + 1, structure.Tile.Z);
        suffix += GetSuffixForDiagonalNeighbour(suffix, "S", "E", structure, x + 1, y - 1, structure.Tile.Z);
        suffix += GetSuffixForDiagonalNeighbour(suffix, "S", "W", structure, x - 1, y - 1, structure.Tile.Z);
        suffix += GetSuffixForDiagonalNeighbour(suffix, "N", "W", structure, x - 1, y + 1, structure.Tile.Z);

        // For example, if this object has all eight neighbours of
        // the same type, then the string will look like:
        //       Wall_NESWneseswnw
        return SpriteManager.GetSprite("Structure", spriteName + suffix);
    }

    protected override void OnCreated(Structure structure)
    {
        GameObject structure_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(structure, structure_go);

        // FIXME: This hardcoding is not ideal!
        if (structure.HasTag("Door"))
        {
            // Check to see if we actually have a wall north/south, and if so
            // set the structure verticalDoor flag to true.
            Tile northTile = world.GetTileAt(structure.Tile.X, structure.Tile.Y + 1, structure.Tile.Z);
            Tile southTile = world.GetTileAt(structure.Tile.X, structure.Tile.Y - 1, structure.Tile.Z);

            if (northTile != null && southTile != null && northTile.Structure != null && southTile.Structure != null &&
                northTile.Structure.HasTag("Wall") && southTile.Structure.HasTag("Wall"))
            {
                structure.VerticalDoor = true;
            }
        }

        SpriteRenderer sr = structure_go.AddComponent<SpriteRenderer>();
        if (structure.Tile.CanSee)
        {
            sr.sprite = GetSpriteForStructure(structure);
            sr.color = structure.Tint;
        }
        else
        {
            sr.sprite = SpriteManager.CreateBlankSprite();
            sr.color = Color.gray;
        }

        sr.sortingLayerName = "Structures";

        structure_go.name = structure.Type + "_" + structure.Tile.X + "_" + structure.Tile.Y;
        //structure_go.transform.position = structure.Tile.Vector3;// + ImageUtils.SpritePivotOffset(sr.sprite, structure.Rotation);
        structure_go.transform.position = new Vector3(structure.Tile.X + ((structure.Width - 1) / 2f), structure.Tile.Y + ((structure.Height - 1) / 2f), 0);
        //structure_go.transform.Rotate(0, 0, structure.Rotation);
        structure_go.transform.SetParent(objectParent.transform, true);

        sr.sortingOrder = Mathf.RoundToInt(structure_go.transform.position.y * -1);

        /*
        StructureChildObjects childObjects = new StructureChildObjects();
        childObjectMap.Add(structure, childObjects);

        childObjects.Overlay = new GameObject();
        childObjects.Overlay.transform.parent = structure_go.transform;
        childObjects.Overlay.transform.position = structure_go.transform.position;
        SpriteRenderer spriteRendererOverlay = childObjects.Overlay.AddComponent<SpriteRenderer>();
        Sprite overlaySprite = GetOverlaySpriteForStructure(structure);
        if (overlaySprite != null)
        {
            spriteRendererOverlay.sprite = overlaySprite;
            spriteRendererOverlay.sortingLayerName = "Structure";
            spriteRendererOverlay.sortingOrder = Mathf.RoundToInt(structure_go.transform.position.y * -1) + 1;
        }

        // indicators (power, fluid, ...)
        BuildableComponent.Requirements structureReq = structure.GetPossibleRequirements();
        foreach (BuildableComponent.Requirements req in Enum.GetValues(typeof(BuildableComponent.Requirements)))
        {
            if (req != BuildableComponent.Requirements.None && (structureReq & req) == req)
            {
                GameObject indicator = new GameObject();
                indicator.transform.parent = structure_go.transform;
                indicator.transform.localScale = new Vector3(IndicatorScale, IndicatorScale, IndicatorScale);
                indicator.transform.position = structure_go.transform.position + statusIndicatorOffsets[req];

                SpriteRenderer powerSpriteRenderer = indicator.AddComponent<SpriteRenderer>();
                powerSpriteRenderer.sprite = GetStatusIndicatorSprite(req);
                powerSpriteRenderer.sortingLayerName = "Power";
                powerSpriteRenderer.color = Color.red;

                childObjects.AddStatus(req, indicator);
            }
        }

        UpdateIconObjectsVisibility(structure, childObjects);
        */

        if (structure.Animations != null)
        {
            structure.Animations.Renderer = sr;
        }
        
        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        structure.Changed += OnChanged;
        structure.Removed += OnRemoved;

        //structure.IsOperatingChanged += OnIsOperatingChanged;
    }

    protected override void OnChanged(Structure structure)
    {
        // Make sure the structure's graphics are correct.
        GameObject structure_go;
        if (objectGameObjectMap.TryGetValue(structure, out structure_go) == false)
        {
            DebugUtils.LogErrorChannel("StructureSpriteController", "OnStructureChanged -- trying to change visuals for structure not in our map.");
            return;
        }

        if (structure.HasTag("Door"))
        {
            // Check to see if we actually have a wall north/south, and if so
            // set the structure verticalDoor flag to true.
            Tile northTile = world.GetTileAt(structure.Tile.X, structure.Tile.Y + 1, structure.Tile.Z);
            Tile southTile = world.GetTileAt(structure.Tile.X, structure.Tile.Y - 1, structure.Tile.Z);
            Tile eastTile = world.GetTileAt(structure.Tile.X + 1, structure.Tile.Y, structure.Tile.Z);
            Tile westTile = world.GetTileAt(structure.Tile.X - 1, structure.Tile.Y, structure.Tile.Z);

            if (northTile != null && southTile != null && northTile.Structure != null && southTile.Structure != null &&
                northTile.Structure.HasTag("Wall") && southTile.Structure.HasTag("Wall"))
            {
                structure.VerticalDoor = true;
            }
            else if (eastTile != null && westTile != null && eastTile.Structure != null && westTile.Structure != null &&
                eastTile.Structure.HasTag("Wall") && westTile.Structure.HasTag("Wall"))
            {
                structure.VerticalDoor = false;
            }
        }

        // don't change sprites on structure with animations
        if (structure.Animations != null)
        {
            structure.Animations.OnStructureChanged();
            return;
        }

        structure_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForStructure(structure);
        structure_go.GetComponent<SpriteRenderer>().color = structure.Tint;

        /*
        Sprite overlaySprite = GetOverlaySpriteForStructure(structure);
        if (overlaySprite != null)
        {
            childObjectMap[structure].Overlay.GetComponent<SpriteRenderer>().sprite = overlaySprite;
        }
        */
    }

    protected override void OnRemoved(Structure structure)
    {
        GameObject structure_go;
        if (objectGameObjectMap.TryGetValue(structure, out structure_go) == false)
        {
            DebugUtils.LogErrorChannel("StructureSpriteController", "OnStructureRemoved -- trying to change visuals for structure not in our map.");
            return;
        }

        structure.UnregisterOnChangedCallback(OnChanged);
        structure.UnregisterOnRemovedCallback(OnRemoved);
        //structure.IsOperatingChanged -= OnIsOperatingChanged;
        objectGameObjectMap.Remove(structure);
        GameObject.Destroy(structure_go);

        childObjectMap.Remove(structure);
    }
    /*
    private void OnIsOperatingChanged(Structure structure)
    {
        if (structure == null)
        {
            return;
        }

        StructureChildObjects childObjects;
        if (childObjectMap.TryGetValue(structure, out childObjects) == false)
        {
            return;
        }

        UpdateIconObjectsVisibility(structure, childObjects);
    }
    
    private void UpdateIconObjectsVisibility(Structure structure, StructureChildObjects statuses)
    {
        if (statuses.StatusIndicators != null && statuses.StatusIndicators.Count > 0)
        {
            // TODO: Cache the Enum.GetValues call?
            foreach (BuildableComponent.Requirements req in Enum.GetValues(typeof(BuildableComponent.Requirements)).Cast<BuildableComponent.Requirements>())
            {
                if (req == BuildableComponent.Requirements.None)
                {
                    continue;
                }

                GameObject go;
                if (statuses.StatusIndicators.TryGetValue(req, out go) == false)
                {
                    continue;
                }

                if ((structure.Requirements & req) == 0)
                {
                    go.SetActive(false);
                }
                else
                {
                    go.SetActive(true);
                }
            }
        }
    }
    */

    private string GetSuffixForNeighbour(Structure structure, int x, int y, int z, string suffix)
    {
        Tile t = world.GetTileAt(x, y, z);
        if (t != null && t.Structure != null && t.Structure.LinksToNeighbors == structure.LinksToNeighbors)
        {
            return suffix;
        }

        return string.Empty;
    }

    private string GetSuffixForDiagonalNeighbour(string suffix, string coord1, string coord2, Structure structure, int x, int y, int z)
    {
        if (suffix.Contains(coord1) && suffix.Contains(coord2))
        {
            // FIXME: Doing ToLower here sucks!
            return GetSuffixForNeighbour(structure, x, y, z, coord1.ToLower() + coord2.ToLower());
        }

        return string.Empty;
    }
    /*
    private Sprite GetStatusIndicatorSprite(BuildableComponent.Requirements oneIdicator)
    {
        return SpriteManager.GetSprite("Icon", string.Format("{0}Indicator", oneIdicator.ToString()));
    }

    public class StructureChildObjects
    {
        public GameObject Overlay { get; set; }

        public Dictionary<BuildableComponent.Requirements, GameObject> StatusIndicators { get; set; }

        public void AddStatus(BuildableComponent.Requirements requirements, GameObject gameObj)
        {
            if (StatusIndicators == null)
            {
                StatusIndicators = new Dictionary<BuildableComponent.Requirements, GameObject>();
            }

            StatusIndicators[requirements] = gameObj;
        }

        public void Destroy()
        {
            GameObject.Destroy(Overlay);
            if (StatusIndicators != null)
            {
                foreach (GameObject status in StatusIndicators.Values)
                {
                    GameObject.Destroy(status);
                }
            }
        }
    }
    */
}
