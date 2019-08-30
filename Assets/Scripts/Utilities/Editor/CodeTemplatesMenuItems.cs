using UnityEditor;
using UnityEngine;

/// <summary>
/// Definitions for all the Code templates menu items.
/// </summary>
public class CodeTemplatesMenuItems
{
    /// <summary>
    /// The root path for code templates menu items.
    /// </summary>
    private const string MENU_ITEM_PATH = "Assets/Create/Templates/";

    /// <summary>
    /// Menu items priority (so they will be grouped/shown next to existing scripting menu items).
    /// </summary>
    private const int MENU_ITEM_PRIORITY = 70;

    /// <summary>
    /// Creates a new C# class.
    /// </summary>
    [MenuItem(MENU_ITEM_PATH + "C# Class", false, MENU_ITEM_PRIORITY)]
    private static void CreateClass()
    {
        CodeTemplates.CreateFromTemplate(
            "NewClass.cs",
            @"Assets/Editor/Templates/ClassTemplate.txt", "cs Script Icon");
    }

    /// <summary>
    /// Creates a new C# interface.
    /// </summary>
    [MenuItem(MENU_ITEM_PATH + "C# Interface", false, MENU_ITEM_PRIORITY)]
    private static void CreateInterface()
    {
        CodeTemplates.CreateFromTemplate(
            "NewInterface.cs",
            @"Assets/Editor/Templates/InterfaceTemplate.txt", "cs Script Icon");
    }

    /// <summary>
    /// Creates a new XML document.
    /// </summary>
    [MenuItem(MENU_ITEM_PATH + "XML Document", false, MENU_ITEM_PRIORITY)]
    private static void CreateXML()
    {
        CodeTemplates.CreateFromTemplate(
            "NewXml.xml",
            @"Assets/Editor/Templates/XmlTemplate.txt", "TextAsset Icon");
    }

    /// <summary>
    /// Creates a new XML document.
    /// </summary>
    [MenuItem(MENU_ITEM_PATH + "Lua Script", false, MENU_ITEM_PRIORITY)]
    private static void CreateLuaScript()
    {
        CodeTemplates.CreateFromTemplate(
            "NewScript.lua",
            @"Assets/Editor/Templates/LuaTemplate.txt", "TextAsset Icon");
    }
}