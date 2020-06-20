using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public /*sealed*/ class InventorySpriteController : BaseSpriteController<Inventory>
{
    private GameObject inventoryUIPrefab;
    private GameObject selectionObject;

    // Use this for initialization
    public InventorySpriteController(World world, GameObject inventoryUI) : base(world, "Inventory", 500)
    {
        inventoryUIPrefab = inventoryUI;

        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.InventoryManager.InventoryCreated += OnCreated;
        world.InventoryManager.InventorySelectionChanged += SelectInventory;

        // Check for pre-existing inventory, which won't do the callback.
        foreach (Inventory inventory in world.InventoryManager.Inventories.SelectMany(pair => pair.Value))
        {
            OnCreated(inventory);
        }
    }

    public static SpriteRenderer SetSprite(GameObject inventoryGO, Inventory inventory, string sortingLayerName = "Inventory")
    {
        SpriteRenderer sr = inventoryGO.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = inventoryGO.AddComponent<SpriteRenderer>();
        }

        sr.sortingLayerName = sortingLayerName;

        //if (inventory.Category != "crated_structure")
        //{
            sr.sprite = SpriteManager.GetSprite("Inventory", inventory.Type);
            if (sr.sprite == null)
            {
                DebugUtils.LogErrorChannel("InventorySpriteController", "No sprite for: " + inventory.Type);
            }
        //}
        //else
        //{
            // Do stuff for crated objects
        //}

        return sr;
    }

    public override void RemoveAll()
    {
        world.InventoryManager.InventoryCreated -= OnCreated;
        world.InventoryManager.InventorySelectionChanged -= SelectInventory;
        foreach (Inventory inventory in world.InventoryManager.Inventories.SelectMany(pair => pair.Value))
        {
            inventory.StackSizeChanged -= OnChanged;
        }
        base.RemoveAll();
    }

    protected override void OnCreated(Inventory inventory)
    {
        // This creates a new GameObject and adds it to our scene.
        GameObject inventoryGameObject = new GameObject();

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(inventory, inventoryGameObject);

        inventoryGameObject.name = inventory.Type;

        // Only create a Game Object if inventory was created on tile, anything else will handle its own game object
        if (inventory.Tile != null)
        {
            inventoryGameObject.transform.position = new Vector3(inventory.Tile.X, inventory.Tile.Y, inventory.Tile.Z);
        }

        inventoryGameObject.transform.SetParent(objectParent.transform, true);

        SetSprite(inventoryGameObject, inventory);

        if (inventory.MaxStackSize > 1)
        {
            // This is a stackabe object, so let's add a InventoryUI component
            // (Which is text that shows the current stackSize.)
            GameObject uiGameObject = GameObject.Instantiate(inventoryUIPrefab);
            uiGameObject.transform.SetParent(inventoryGameObject.transform);
            uiGameObject.transform.localPosition = new Vector3(0.5f, 0.5f, 0);// Vector3.zero; // If we change the sprite anchor, this may need to be mondified.
            uiGameObject.GetComponentInChildren<Text>().text = inventory.StackSize.ToString();
        }

        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        // FIXME: Add on changed callbacks
        inventory.StackSizeChanged += OnChanged;
    }

    protected override void OnChanged(Inventory inventory)
    {
        // Make sure the structure's graphics are correct.
        GameObject inventoryGameObject;
        if (objectGameObjectMap.TryGetValue(inventory, out inventoryGameObject) == false)
        {
            DebugUtils.LogErrorChannel("InventorySpriteController", "OnChanged -- trying to change visuals for inventory not in our map.");
            return;
        }

        //SelectInventory(inventory);

        if (inventory.StackSize > 0)
        {
            Text text = inventoryGameObject.GetComponentInChildren<Text>();

            // FIXME: If maxStackSize changed to/from 1, then we either need to create or destroy the text
            if (text != null)
            {
                text.text = inventory.StackSize.ToString();
            }
        }
        else
        {
            // This stack has gone to zero, so remove the sprite!
            OnRemoved(inventory);
        }
    }

    protected void SelectInventory(Inventory inventory)
    {
        if (inventory.IsSelected)
        {
            SpriteRenderer sr;
            if (selectionObject == null)
            {
                selectionObject = new GameObject("Selection");
                sr = selectionObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                sr = selectionObject.GetComponent<SpriteRenderer>();
            }
            selectionObject.SetActive(true);

            selectionObject.transform.position = new Vector3(inventory.Tile.X, inventory.Tile.Y, 0);
            selectionObject.transform.SetParent(objectParent.transform, true);

            if (sr != null)
            {
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = -Mathf.RoundToInt(inventory.Tile.Y) - 1;
                sr.sprite = SpriteManager.GetSelectionSprite();
            }
        }
        else
        {
            if (selectionObject != null)
            {
                selectionObject.SetActive(false);
            }
        }
    }

    protected override void OnRemoved(Inventory inventory)
    {
        if (inventory.IsSelected)
        {
            if (selectionObject != null)
            {
                selectionObject.SetActive(false);
            }
        }

        inventory.StackSizeChanged -= OnChanged;
        GameObject inventoryGameObject = objectGameObjectMap[inventory];
        objectGameObjectMap.Remove(inventory);
        GameObject.Destroy(inventoryGameObject);
    }
}