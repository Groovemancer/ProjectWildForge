using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEditor;

[Serializable]
public class TileType
{
    public uint Id { get; set; }
    public uint Flag { get; set; }
    public string FlagName { get; set; }
    public string NameLocaleId { get; set; }
    public float MoveCost { get; set; }
    public string Sprite { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAll { get; set; }
}

[Serializable]
class TileTypeData
{
    private static bool loaded = false;
    private static TileTypeData instance = null;

    public List<TileType> Data = new List<TileType>();
    private TileType defaultType = null;
    private TileType allType = null;
    private uint? allFlag = null;

    public TileTypeData()
    {
    }

    public static TileTypeData Instance
    {
        get
        {
            if (instance == null)
                instance = new TileTypeData();
            return instance;
        }
    }

    public TileType DefaultType
    {
        get
        {
            if (defaultType == null)
            {
                foreach (TileType type in Data)
                {
                    if (type.IsDefault)
                    {
                        defaultType = type;
                        break;
                    }
                }
            }
            return defaultType;
        }
    }

    public TileType AllType
    {
        get
        {
            if (allType == null)
            {
                foreach (TileType type in Data)
                {
                    if (type.IsAll)
                    {
                        allType = type;
                        break;
                    }
                }
            }
            return allType;
        }
    }

    public uint? AllFlag
    {
        get
        {
            if (allFlag == null)
            {
                foreach (TileType type in Data)
                {
                    if (type.IsAll)
                    {
                        allFlag = type.Flag;
                        break;
                    }
                }
            }
            return allFlag.Value;
        }
    }

    public static TileType GetById(uint id)
    {
        TileType data = null;
        data = Instance.Data.Find(e => e.Id == id);
        return data;
    }

    public static TileType GetByFlagName(string flagName)
    {
        TileType data = null;
        data = Instance.Data.Find(e => e.FlagName == flagName);
        return data;
    }

    public static uint GetFlagByFlagName(string flagName)
    {
        TileType data = null;
        data = Instance.Data.Find(e => e.FlagName == flagName);
        return data.Flag;
    }

    public static uint Flag(string flagName)
    {
        return GetFlagByFlagName(flagName);
    }

    public uint this[string flagName]
    {
        get { return GetFlagByFlagName(flagName); }
    }

    public static bool Contains(uint id)
    {
        TileType data = null;
        data = Instance.Data.Find(e => e.Id == id);
        return data != null;
    }

    public static void LoadData()
    {
        if (!loaded)
        {
            DebugUtils.Log("Loading TileTypes...");
            try
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(Path.Combine(Application.streamingAssetsPath, "Data/TileTypes.xml"));

                XmlNode tileTypes = doc.SelectSingleNode("TileTypes");

                Instance.Data.Clear();

                XmlNodeList typeNodes = doc.SelectNodes("TileTypes/TileType");

                foreach (XmlNode typeNode in typeNodes)
                {
                    int id = int.Parse(typeNode.Attributes["id"].InnerText);

                    string flagName = typeNode.SelectSingleNode("FlagName").InnerText;

                    string nameLocaleId = typeNode.SelectSingleNode("NameLocaleId").InnerText;
                    float moveCost = float.Parse(typeNode.SelectSingleNode("MoveCost").InnerText);
                    string sprite = "";
                    if (typeNode.SelectSingleNode("Sprite") != null)
                        sprite = typeNode.SelectSingleNode("Sprite").InnerText;

                    bool isDefault = typeNode.SelectSingleNode("Default") != null;
                    if (isDefault && Instance.defaultType != null)
                    {
                        DebugUtils.Log("TileTypeData: Warning - Multiple \"default\" types!");
                    }

                    bool isAll = typeNode.SelectSingleNode("All") != null;
                    if (isDefault && Instance.defaultType != null)
                    {
                        DebugUtils.Log("TileTypeData: Warning - Multiple \"all\" types!");
                    }

                    uint flag = (uint)1 << id;

                    if (Contains((uint)id))
                        continue;

                    TileType type = new TileType();
                    type.Id = (uint)id;
                    type.Flag = flag;
                    type.FlagName = flagName;
                    type.NameLocaleId = nameLocaleId;
                    type.MoveCost = moveCost;
                    type.Sprite = sprite;
                    type.IsDefault = isDefault;
                    type.IsAll = isAll;

                    Instance.Data.Add(type);

                    if (isDefault)
                    {
                        Instance.defaultType = type;
                    }

                    if (isAll)
                    {
                        Instance.allType = type;
                        Instance.allFlag = type.Flag;
                    }
                }
                DebugUtils.Log("TileTypeData Loaded: " + Instance.Data.Count);
                loaded = true;
            }
            catch (Exception e)
            {
                DebugUtils.DisplayError(e.ToString(), false);
                DebugUtils.LogException(e);
            }
        }
    }
}