using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySpriteController : MonoBehaviour
{
    public GameObject inventoryUIPrefab;

    Dictionary<Inventory, GameObject> inventoryGameObjectMap;

    Dictionary<string, Sprite> inventorySprites;

    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();

        World.RegisterInventoryCreated(OnInventoryCreated);

        // Check for pre-existing inventorys, which won't do the callback.
        foreach (string objectType in World.inventoryManager.inventories.Keys)
        {
            foreach (Inventory inv in World.inventoryManager.inventories[objectType])
            {
                OnInventoryCreated(inv);
            }
        }
    }

    private void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Inventory");

        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            inventorySprites[s.name] = s;
        }
    }

    public void OnInventoryCreated(Inventory inv)
    {
        Debug.Log("Created item: " + inv.objectType);
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject inv_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        inventoryGameObjectMap.Add(inv, inv_go);

        inv_go.name = inv.objectType;
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
        inv_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = inv_go.AddComponent<SpriteRenderer>();
        spr.sprite = inventorySprites[inv.objectType];
        spr.sortingLayerName = "Inventory";

        if (inv.maxStackSize > 1)
        {
            // This is a stackable object, so let's add a InventoryUI component
            // (which is text that shows the current stackSize.)

            GameObject ui_go = Instantiate(inventoryUIPrefab);
            ui_go.transform.SetParent(inv_go.transform);
            ui_go.transform.localPosition = new Vector3(0.5f, 0.5f, 0);     // If we change the sprite anchor, this may need to be modified.
            ui_go.GetComponentInChildren<Text>().text = inv.stackSize.ToString();
        }

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        // FIXME: Add on changed callbacks
        inv.RegisterOnChangedCallback(OnInventoryChanged);
    }

    void OnInventoryChanged(Inventory inv)
    {
        // Make sure the inventory's graphics are correct.
        if (inventoryGameObjectMap.ContainsKey(inv) == false)
        {
            Debug.LogError("OnInventoryChanged -- trying to change visuals for inventory not in our map.");
            return;
        }
        GameObject inv_go = inventoryGameObjectMap[inv];

        if (inv.stackSize > 0)
        {
            Text text = inv_go.GetComponentInChildren<Text>();

            // FIXME: If maxStackSize changed to/from 1, then we either need to create or destroy the text
            if (text != null)
            {
                text.text = inv.stackSize.ToString();
            }
        }
        else
        {
            // This stack has gone to zero, so remove the sprite!
            Destroy(inv_go);
            inventoryGameObjectMap.Remove(inv);
            inv.UnregisterInventoryOnChangedCallback(OnInventoryChanged);
        }
    }
}
