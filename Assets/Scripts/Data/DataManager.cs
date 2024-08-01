using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; protected set; }

    private Dictionary<string, List<Action<XmlNode>>> prototypeHandlers = new Dictionary<string, List<Action<XmlNode>>>();

    public int RandomSeed;

    float holdExitButtonDuration = 1.5f;
    float holdExitButtonElapsedTime = 0f;

    void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        LoadSceneFiles();

        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
        DateTime dtNow = DateTime.Now;
        TimeSpan result = dtNow.Subtract(dt);
        RandomSeed = Convert.ToInt32(result.TotalSeconds);

        RandomUtils.Initialize(RandomSeed);
    }

    private void LoadSceneFiles()
    {
        // Load Stuff
        //LocaleData.LoadData();
        LocaleData.LoadDataNew();
        TileTypeData.LoadData();

        //SetCurrentLocale("de-DE");

        //LoadFunctions("Structure.lua", "Structure");

        //PrototypeManager.Initialize();

        //SetupPrototypeHandlers();

        //LoadPrototypes();
    }

    /// <summary>
    /// Loads all the functions using the given file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="functionsName">The functions name.</param>
    private void LoadFunctions(string fileName, string functionsName)
    {
        string ext = Path.GetExtension(fileName);
        string folder = "Scripts";
        Functions.Type scriptType = Functions.Type.Lua;

        if (string.Compare(".cs", ext, true) == 0)
        {
            folder = "CSharp";
            scriptType = Functions.Type.CSharp;
        }

        LoadTextFile(
            folder,
            fileName,
            (filePath) =>
            {
                if (File.Exists(filePath))
                {
                    string text = File.ReadAllText(filePath);
                    FunctionsManager.Get(functionsName).LoadScript(text, functionsName, scriptType);
                }
                else
                {
                    DebugUtils.LogErrorChannel(folder == "CSharp" ? "CSharp" : "LUA", "file " + filePath + " not found");
                }
            });
    }

    /// <summary>
    /// Loads the given file from the given folder in the base and inside the mods and
    /// calls the Action with the file path.
    /// </summary>
    /// <param name="directoryName">Directory name.</param>
    /// <param name="fileName">File name.</param>
    /// <param name="readText">Called to handle the text reading and actual loading.</param>
    private void LoadTextFile(string directoryName, string fileName, Action<string> readText)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, directoryName);
        filePath = Path.Combine(filePath, fileName);
        if (File.Exists(filePath))
        {
            readText(filePath);
        }
        else
        {
            DebugUtils.LogError("File at " + filePath + " not found");
        }

        //foreach (DirectoryInfo mod in mods)
        //{
        //    filePath = Path.Combine(mod.FullName, fileName);
        //    if (File.Exists(filePath))
        //    {
        //        readText(filePath);
        //    }
        //}
    }

    /// <summary>
    /// Sets up all of the prototype managers, useful in doing unit tests and always run as a part of normal code.
    /// </summary>
    public void SetupPrototypeHandlers()
    {
        HandlePrototypes("Inventories", PrototypeManager.Inventory.LoadXmlPrototypes);
        HandlePrototypes("Stats", PrototypeManager.Stat.LoadXmlPrototypes);
    }

    /// <summary>
    /// Subscribes to the prototypeLoader to handle all prototypes with the given key.
    /// </summary>
    /// <param name="prototypeKey">Key for the prototypes to handle.</param>
    /// <param name="prototypesLoader">Called to handle the prototypes loading.</param>
    private void HandlePrototypes(string prototypeKey, Action<XmlNode> prototypesLoader)
    {
        List<Action<XmlNode>> handlers;
        if (prototypeHandlers.TryGetValue(prototypeKey, out handlers) == false)
        {
            handlers = new List<Action<XmlNode>>();
            prototypeHandlers.Add(prototypeKey, handlers);
        }

        // The way these work suggest it should be in a separate class, either a new class (PrototypeLoader?) or in one of the prototype related classes
        handlers.Add(prototypesLoader);
    }

    // Update is called once per frame
    void Update()
    {
        if (DebugUtils.IsDebugBuild())
        {
            if (Input.GetButton("ExitGame"))
            {
                holdExitButtonElapsedTime += Time.deltaTime;
            }
            else
            {
                holdExitButtonElapsedTime = 0;
            }
            if (holdExitButtonElapsedTime >= holdExitButtonDuration)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }

    public void SetCurrentLocale(string locale)
    {
        LocaleData.SetCurrentLocale(locale);
    }

    private void LoadPrototypes()
    {
        string prototypesDirectoryPath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Prototypes");
        DirectoryInfo prototypeDir = new DirectoryInfo(prototypesDirectoryPath);
        FileInfo[] prototypeFiles = prototypeDir.GetFiles("*xml").ToArray();
        Dictionary<string, XmlNode> tagNameToNode = new Dictionary<string, XmlNode>();

        try
        {
            for (int i = 0; i < prototypeFiles.Length; i++)
            {
                FileInfo file = prototypeFiles[i];

                DebugUtils.LogChannel("DataManager", "Loading " + file.FullName);

                XmlDocument doc = new XmlDocument();
                doc.Load(file.FullName);
                string tagName = doc.ChildNodes[1].Name;

                tagNameToNode.Add(tagName, doc.ChildNodes[1]);
            }
        }
        catch (Exception e)
        {
            DebugUtils.DisplayError(e.ToString(), false);
            DebugUtils.LogException(e);
        }

        LoadPrototypesFromXmlNodes(tagNameToNode);
    }

    /// <summary>
    /// Takes in a ditionary of tag names and JTokens, which is then parsed by the prototype managers.
    /// </summary>
    /// <param name="tagNameToProperty"></param>
    public void LoadPrototypesFromXmlNodes(Dictionary<string, XmlNode> tagNameToNode)
    {
        foreach (KeyValuePair<string, List<Action<XmlNode>>> prototypeHandler in prototypeHandlers)
        {
            foreach (Action<XmlNode> handler in prototypeHandler.Value)
            {
                XmlNode rootNode;
                if (tagNameToNode.TryGetValue(prototypeHandler.Key, out rootNode))
                {
                    handler((XmlNode)rootNode);
                }
            }
        }
    }
}
