using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    public GameObject circleCursorPrefab;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 currFramePosition;

    const float MIN_ZOOM = 5;
    const float DEFAULT_ZOOM = 8;
    const float MAX_ZOOM = 25;

    // The world-position start of our left-mouse drag operation
    Vector3 dragStartPosition;

    bool isDragging = false;

    List<GameObject> dragPreviewGameObjects;

    BuildModeController bmc;

    MouseMode currentMode = MouseMode.Select;

    // Use this for initialization
    void Start()
    {
        bmc = GameObject.FindObjectOfType<BuildModeController>();

        dragPreviewGameObjects = new List<GameObject>();

        Camera.main.orthographicSize = DEFAULT_ZOOM;
    }

    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetMousePosition()
    {
        return currFramePosition;
    }

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.GetTileAtWorldCoord(currFramePosition);
    }

    // Update is called once per frame
    void Update()
    {
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (currentMode == MouseMode.Build)
            {
                currentMode = MouseMode.Select;
            }
            else if (currentMode == MouseMode.Select)
            {
                Debug.Log("Show game menu?");
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (currentMode == MouseMode.Build)
            {
                currentMode = MouseMode.Select;
            }
        }

        //UpdateCursor();

        UpdateDragging();
        UpdateCameraMovement();

        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    void UpdateDragging()
    {
        // If we are over a UI element, bail out.
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Clean up old drag previews
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }

        if (currentMode != MouseMode.Build)
            return;

        // Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currFramePosition;
            isDragging = true;
        }
        else if (isDragging == false)
        {
            dragStartPosition = currFramePosition;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Escape))
        {
            // The RIGHT mouse button was released, so we
            // are cancelling any dragging/build mode.
            isDragging = false;
        }

        if (bmc.IsObjectDraggable() == false)
        {
            dragStartPosition = currFramePosition;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int end_x = Mathf.FloorToInt(currFramePosition.x + 0.5f);
        int start_y = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int end_y = Mathf.FloorToInt(currFramePosition.y + 0.5f);

        // We may be dragging in the "wrong" direction, so flip things if needed
        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        //if (isDragging)
        //{
            // Display a preview of the drag area
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.World.GetTileAt(x, y, 0);
                    if (t != null)
                    {
                        if (bmc.buildMode == BuildMode.Structure)
                        {
                            ShowStructureSpriteAtTile(bmc.buildModeObjectType, t);
                        }
                        else
                        {
                            // Display the structure hint on top of this tile position
                            GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                            go.transform.SetParent(this.transform, true);
                            dragPreviewGameObjects.Add(go);
                        }
                    }
                }
            }
        //}

        // End Drag
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.World.GetTileAt(x, y, 0);

                    if (t != null)
                    {
                        // Call BuildModeController DoWork
                        bmc.DoBuild(t);
                    }
                }
            }
        }
    }

    //void OnStructureJobComplete(string objectType, Tile t)
    //{
    //    WorldController.Instance.World.PlaceStructure(objectType, t);
    //}

    void UpdateCameraMovement()
    {
        // Handle screen dragging
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) // Right or Middle Mouse Button
        {
            Vector3 diff = lastFramePosition - currFramePosition;
            Camera.main.transform.Translate(diff);
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, MIN_ZOOM, MAX_ZOOM);
        }
    }


    void ShowStructureSpriteAtTile(string structureType, Tile t)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(this.transform, true);
        dragPreviewGameObjects.Add(go);


        SpriteRenderer spr = go.AddComponent<SpriteRenderer>();
        spr.sortingLayerName = "Jobs";
        spr.sprite = WorldController.StructureSpriteController.GetSpriteForStructure(structureType);

        if (World.Current.StructureManager.IsPlacementValid(structureType, t))
        {
            spr.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        }
        else
        {
            spr.color = new Color(1f, 0.5f, 0.5f, 0.5f);
        }

        Structure proto = PrototypeManager.Structure.Get(structureType);

        go.transform.position = new Vector3(t.X + ((proto.Width - 1) / 2f), t.Y + ((proto.Height - 1) / 2f), 0);
    }

    public void StartBuildMode()
    {
        currentMode = MouseMode.Build;
    }
}


public enum MouseMode
{
    Select, Build
}