﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ModsManager
{
    private DirectoryInfo[] mods;
    private Dictionary<string, List<Action<XmlNode>>> prototypeHandlers = new Dictionary<string, List<Action<XmlNode>>>();

    public ModsManager()
    {
        //if (SceneController.IsAtIntroScene())
        //{
        //    SetUp(Type.Intro);
        //}
        //else if (SceneController.IsAtMainScene())
        //{
            SetUp(Type.MainScene);
        //}
    }

    public ModsManager(Type type)
    {
        SetUp(type);
    }

    public enum Type
    {
        Intro, MainScene
    }

    /// <summary>
    /// Return directory info of the mod folder.
    /// </summary>
    public static DirectoryInfo[] GetModsFiles()
    {
        DirectoryInfo modsDir = new DirectoryInfo(GetPathToModsFolder());
        return modsDir.GetDirectories();
    }

    /// <summary>
    /// Loads the script file in the given location.
    /// </summary>
    /// <param name="file">The file name.</param>
    /// <param name="functionsName">The functions name.</param>
    public void LoadFunctionsInFile(FileInfo file, string functionsName)
    {
        LoadTextFile(
            file.DirectoryName,
            file.Name,
            (filePath) =>
            {
                StreamReader reader = new StreamReader(file.OpenRead());
                string text = reader.ReadToEnd();
                FunctionsManager.Get(functionsName).LoadScript(text, functionsName, file.Extension == ".lua" ? Functions.Type.Lua : Functions.Type.CSharp);
            });
    }

    /// <summary>
    /// Sets up all of the prototype managers, useful in doing unit tests and always run as a part of normal code.
    /// </summary>
    public void SetupPrototypeHandlers()
    {
        //HandlePrototypes("Tile", PrototypeManager.TileType.LoadXmlPrototypes);
        HandlePrototypes("JobCategories", PrototypeManager.JobCategory.LoadXmlPrototypes);
        HandlePrototypes("Structures", PrototypeManager.Structure.LoadXmlPrototypes);
        //HandlePrototypes("Utility", PrototypeManager.Utility.LoadXmlPrototypes);
        //HandlePrototypes("RoomBehavior", PrototypeManager.RoomBehavior.LoadXmlPrototypes);
        HandlePrototypes("Inventories", PrototypeManager.Inventory.LoadXmlPrototypes);
        HandlePrototypes("Plants", PrototypeManager.Plant.LoadXmlPrototypes);
        HandlePrototypes("Need", PrototypeManager.Need.LoadXmlPrototypes);
        //HandlePrototypes("Trader", PrototypeManager.Trader.LoadXmlPrototypes);
        //HandlePrototypes("Currency", PrototypeManager.Currency.LoadXmlPrototypes);
        //HandlePrototypes("GameEvent", PrototypeManager.GameEvent.LoadXmlPrototypes);
        //HandlePrototypes("ScheduledEvent", PrototypeManager.ScheduledEvent.LoadXmlPrototypes);
        HandlePrototypes("Stats", PrototypeManager.Stat.LoadXmlPrototypes);
        HandlePrototypes("Skills", PrototypeManager.Skill.LoadXmlPrototypes);
        HandlePrototypes("Races", PrototypeManager.Race.LoadXmlPrototypes);
        //HandlePrototypes("Quest", PrototypeManager.Quest.LoadXmlPrototypes);
        //HandlePrototypes("Headline", PrototypeManager.Headline.LoadXmlPrototypes);
        //HandlePrototypes("Overlay", PrototypeManager.Overlay.LoadXmlPrototypes);
        //HandlePrototypes("Ship", PrototypeManager.Ship.LoadXmlPrototypes);
    }

    /// <summary>
    /// Takes in a dictionary of tag names and JTokens, which is then parsed by the prototype managers.
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

    /// <summary>
    /// Return the path to the mod folder.
    /// </summary>
    private static string GetPathToModsFolder()
    {
        return Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Mods");
    }

    /// <summary>
    /// Common initialization to make testing easier.
    /// </summary>
    private void SetUp(Type type)
    {
        mods = GetModsFiles();

        LoadSharedFiles();

        if (type == Type.Intro)
        {
            LoadIntroFiles();
        }
        else if (type == Type.MainScene)
        {
            LoadMainSceneFiles();
        }

        LoadPrototypes();

        foreach (Race race in PrototypeManager.Race.Values)
        {
            LoadActorNames(race.Type, false, race.ActorMaleNamesFile + ".txt");
            LoadActorNames(race.Type, true, race.ActorFemaleNamesFile + ".txt");
        }
    }

    private void LoadMainSceneFiles()
    {
        LoadFunctions("Structure.lua", "Structure");
        //LoadFunctions("Utility.lua", "Utility");
        //LoadFunctions("RoomBehavior.lua", "RoomBehavior");
        //LoadFunctions("Need.lua", "Need");
        //LoadFunctions("GameEvent.lua", "GameEvent");
        //LoadFunctions("Tiles.lua", "TileType");
        //LoadFunctions("Quest.lua", "Quest");
        //LoadFunctions("ScheduledEvent.lua", "ScheduledEvent");
        //LoadFunctions("Overlay.lua", "Overlay");

        //LoadFunctions("FurnitureFunctions.cs", "Furniture");
        //LoadFunctions("OverlayFunctions.cs", "Overlay");

        SetupPrototypeHandlers();

        

        LoadDirectoryAssets("Sprites", SpriteManager.LoadSpriteFiles);
        //LoadDirectoryAssets("Audio", AudioManager.LoadAudioFiles);
    }

    private void LoadIntroFiles()
    {
        //LoadDirectoryAssets("Audio", AudioManager.LoadAudioFiles);
        //LoadDirectoryAssets("MainMenu/Images", SpriteManager.LoadSpriteFiles);
        //LoadDirectoryAssets("MainMenu/Audio", AudioManager.LoadAudioFiles);
    }

    private void LoadSharedFiles()
    {
        // Not currently used
        // LoadDirectoryAssets("Shared/Images", SpriteManager.LoadSpriteFiles);
        // LoadDirectoryAssets("Shared/Audio", AudioManager.LoadAudioFiles);
        //LoadFunctions("CommandFunctions.cs", "DevConsole");
        //LoadFunctions("ConsoleCommands.lua", "DevConsole");

        //HandlePrototypes("ConsoleCommand", PrototypeManager.DevConsole.LoadJsonPrototypes);
        //HandlePrototypes("Category", PrototypeManager.SettingsCategories.LoadJsonPrototypes);
        //HandlePrototypes("ComponentGroup", PrototypeManager.PerformanceHUD.LoadJsonPrototypes);
        //HandlePrototypes("JobCategory", PrototypeManager.JobCategory.LoadJsonPrototypes);

        //LoadFunctions("SettingsMenuFunctions.cs", "SettingsMenu");
        //LoadFunctions("SettingsMenuCommands.lua", "SettingsMenu");

        //LoadFunctions("PerformanceHUDFunctions.cs", "PerformanceHUD");
        //LoadFunctions("PerformanceHUDCommands.lua", "PerformanceHUD");
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
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;

                using (XmlReader reader = XmlReader.Create(file.FullName, readerSettings))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(reader);
                    string tagName = doc.ChildNodes[1].Name;

                    tagNameToNode.Add(tagName, doc.ChildNodes[1]);
                }
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
    /// Loads all the protoypes using the given file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="prototypesLoader">Called to handle the prototypes loading.</param>
    private void LoadPrototypes(string fileName, Action<string> prototypesLoader)
    {
        LoadTextFile(
            "Data",
            fileName,
            (filePath) =>
            {
                string text = File.ReadAllText(filePath);
                prototypesLoader(text);
            });
    }

    /// <summary>
    /// Loads all the actor names from the given file.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    private void LoadActorNames(string raceType, bool isFemale, string fileName)
    {
        LoadTextFile(
            Path.Combine("Data", "ActorNames"),
            fileName,
            (filePath) =>
            {
                string[] lines = File.ReadAllLines(filePath);
                ActorNameManager.LoadNames(raceType, isFemale, lines);
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

        foreach (DirectoryInfo mod in mods)
        {
            filePath = Path.Combine(mod.FullName, fileName);
            if (File.Exists(filePath))
            {
                readText(filePath);
            }
        }
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

        foreach (DirectoryInfo mod in mods)
        {
            directoryPath = Path.Combine(mod.FullName, directoryName);
            if (Directory.Exists(directoryPath))
            {
                readDirectory(directoryPath);
            }
        }
    }
}
