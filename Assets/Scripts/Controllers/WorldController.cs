﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.SceneManagement;

[MoonSharpUserData]
public class WorldController : MonoBehaviour
{
    [SerializeField]
    private GameObject inventoryUI;

    #region Instances
    public static WorldController Instance { get; protected set; }

    public static TileSpriteController TileSpriteController { get; protected set; }

    public ActorSpriteController ActorSpriteController { get; private set; }

    public static StructureSpriteController StructureSpriteController { get; protected set; }

    public static PlantSpriteController PlantSpriteController { get; protected set; }

    public static JobSpriteController JobSpriteController { get; protected set; }

    public static InventorySpriteController InventorySpriteController { get; protected set; }

    public ModsManager ModsManager { get; private set; }

    public World World { get; protected set; }
    #endregion

    static bool loadWorld = false;
    
    // Use this for initialization
    void OnEnable()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("There should never be two world controllers.");
        }

        FunctionsManager.Initialize();
        PrototypeManager.Initialize();
        ActorNameManager.Initialize();
        SpriteManager.Initialize();
        //LoadDirectoryAssets("Sprites", SpriteManager.LoadSpriteFiles);

        ModsManager = new ModsManager();

        if (loadWorld)
        {
            loadWorld = false;
            CreateEmptyWorldFromSaveFile();
        }
        else
        {
            CreateEmptyWorld();
        }
    }

    public void Awake()
    {
        
    }

    public void Start()
    {
        TileSpriteController = new TileSpriteController(World);
        ActorSpriteController = new ActorSpriteController(World);
        StructureSpriteController = new StructureSpriteController(World);
        JobSpriteController = new JobSpriteController(World, StructureSpriteController);
        InventorySpriteController = new InventorySpriteController(World, inventoryUI);
        PlantSpriteController = new PlantSpriteController(World);
    }

    /// <summary>
    /// Loads the all the assets from the given directory.
    /// </summary>
    /// <param name="directoryName">Directory name.</param>
    /// <param name="readDirectory">Called to handle the loading of each file in the given directory.</param>
    private void LoadDirectoryAssets(string directoryName, Action<string> readDirectory)
    {
        string directoryPath = Path.Combine(Application.streamingAssetsPath, directoryName);
        if (Directory.Exists(directoryPath))
        {
            readDirectory(directoryPath);
        }
        else
        {
            DebugUtils.LogWarning("Directory at " + directoryPath + " not found");
        }

        //foreach (DirectoryInfo mod in mods)
        //{
        //    directoryPath = Path.Combine(mod.FullName, directoryName);
        //    if (Directory.Exists(directoryPath))
        //    {
        //        readDirectory(directoryPath);
        //    }
        //}
    }

    public void SetGameSpeed(GameSpeed gameSpeed)
    {
        World.SetGameSpeed(gameSpeed);
    }

    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return World.GetTileAt(x, y, (int)coord.z);
    }

    public void Destroy()
    {
        TimeManager.Instance.Destroy();
    }

    public void NewWorld()
    {
        Debug.Log("NewWorld button was clicked!");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SaveWorld()
    {
        Debug.Log("SaveWorld button was clicked!");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, World);
        writer.Close();

        Debug.Log(writer.ToString());

        PlayerPrefs.SetString("SaveGame00", writer.ToString());

        string myDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string dirPath = Path.Combine(myDocsPath, Application.companyName, Application.productName);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            DebugUtils.LogChannel("WorldController", "Directory Does Not Exist! Create it!");
        }
        else
        {
            DebugUtils.LogChannel("WorldController", "Directory Exists!");
        }

        // Sanity check
        if (Directory.Exists(dirPath))
        {
            string filePath = Path.Combine(dirPath, "SaveGame00.dat");

            byte[] data = Encoding.ASCII.GetBytes(writer.ToString());

            File.WriteAllBytes(filePath, data);
        }
    }

    public void LoadWorld()
    {
        Debug.Log("LoadWorld button was clicked!");

        // Reload the scene to reset all data (and purge old references)
        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CreateEmptyWorld()
    {
        DebugUtils.LogChannel("WorldController", "CreateEmptyWorld");
        // Create a world with Empty tiles
        //World = new World(100, 100, 5);
        World = WorldGenerator.GenerateWorldFromSettings();
        World.ClearSafeSpace();

        // Center the camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);

        World.RandomTreePlacement();
        World.SpawnStoneWalls();
    }

    private void CreateEmptyWorldFromSaveFile()
    {
        DebugUtils.LogChannel("WorldController", "CreateEmptyWorldFromSaveFile");
        // Create a world from our save file data.

        string myDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string savePath = Path.Combine(myDocsPath, Application.companyName, Application.productName, "SaveGame00.dat");
        if (!File.Exists(savePath))
        {
            DebugUtils.LogChannel("WorldController", "CreateEmptyWorldFromSaveFile Save File Does Not Exist!");

            return;
        }

        XmlSerializer serializer = new XmlSerializer(typeof(World));

        using (StreamReader reader = new StreamReader(savePath))
        {
            World = (World)serializer.Deserialize(reader);
        }

        // Center the camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);

        //World.RandomizeTiles();
    }

    private void OnDrawGizmos()
    {
        if (World == null || World.ActorManager == null)
            return;
        
        foreach (Actor actor in World.ActorManager.Actors)
        {
            if (actor.MyJob != null)
            {
                Debug.DrawLine(actor.CurrTile.Vector3, actor.MyJob.Tile.Vector3, Color.green);
            }
        }
    }
}
