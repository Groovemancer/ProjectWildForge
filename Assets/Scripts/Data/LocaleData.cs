using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEditor;

[Serializable]
public class Locale
{
    public string Id;
    public string Comment;
    public string Context;
    public string Options;
    public Dictionary<string, string> Text = new Dictionary<string, string>();

    public Locale(string id, string comment, string context, string options, Dictionary<string, string> text)
    {
        Id = id;
        Comment = comment;
        Context = context;
        Options = options;
        Text = text;
    }

    public string GetText(string locale)
    {
        if (!Text.ContainsKey(locale))
        {
            if (!Text.ContainsKey(LocaleData.DefaultLocale))
            {
                DebugUtils.DisplayError(string.Format("Text ID '{0}' does not contain default locale!", Id));
                return "";
            }

            return GetText(LocaleData.DefaultLocale);
        }

        string result = Text[locale];

        if (string.IsNullOrEmpty(result))
        {
            DebugUtils.DisplayError(string.Format("Text ID '{0}' Locale '{1}' is empty!", Id, locale));
        }

        return result;
    }
}

[Serializable]
public class LocaleData
{
    private static bool loaded = false;
    private static LocaleData instance = null;
    public List<Locale> Data = new List<Locale>();

    public List<string> Locales = new List<string>();

    private int currentLocaleIndex = 0;
    private string defaultLocale;

    public LocaleData()
    {
        //LoadData();
    }

    public static LocaleData Instance
    {
        get
        {
            if (instance == null)
                instance = new LocaleData();
            return instance;
        }
    }

    public static string DefaultLocale
    {
        get { return Instance.defaultLocale; }
        set { Instance.defaultLocale = value; }
    }

    public static int CurrentLocaleIndex
    {
        get { return Instance.currentLocaleIndex; }
        set { Instance.currentLocaleIndex = value; }
    }

    public static string GetText(string locale, string filter)
    {
        Locale data = GetLocale(filter);

        if (data != null)
            return data.GetText(locale);

        DebugUtils.DisplayError(string.Format("string '{0}' not found!", filter));

        return "";
    }

    public static string GetTextById(string locale, string id)
    {
        Locale data = GetById(id);

        if (data != null)
            return data.GetText(locale);

        DebugUtils.DisplayError(string.Format("Text ID '{0}' not found!", id));

        return "";
    }

    public static string CurrentLocale()
    {
        return Instance.Locales[CurrentLocaleIndex];
    }

    public static void SetCurrentLocale(string locale)
    {
        int idx = Instance.Locales.IndexOf(locale);
        if (idx != -1)
            Instance.currentLocaleIndex = idx;
    }

    public static Locale GetLocale(string filter)
    {
        Locale data = null;
        if (filter.Contains("string#"))
        {
            string id = filter.Replace("string#", "");
            data = GetById(id);

            return data;
        }

        string[] subStr = filter.Split('|');
        string comment = "";
        string context = "";
        string options = "";
        foreach (string str in subStr)
        {
            if (str.Contains("comment#"))
            {
                comment = str.Replace("comment#", "");
            }
            if (str.Contains("context#"))
            {
                context = str.Replace("context#", "");
            }
            if (str.Contains("options#"))
            {
                options = str.Replace("options#", "");
            }
        }

        List<Locale> locs = GetLocales(comment, context, options);

        if (locs.Count == 1)
            data = locs[0];

        return data;
    }

    public static Locale GetById(string id)
    {
        Locale data = null;
        if (!string.IsNullOrEmpty(id))
            data = Instance.Data.Find(e => e.Id == id);
        return data;
    }

    public static List<Locale> GetLocales(string comment = "", string context = "", string options = "")
    {
        List<Locale> data = new List<Locale>();

        if (!string.IsNullOrEmpty(comment))
            data = Instance.Data.FindAll(e => e.Comment == comment);
        if (!string.IsNullOrEmpty(context))
            data = data.FindAll(e => e.Context == context);
        if (!string.IsNullOrEmpty(options))
            data = data.FindAll(e => e.Options == options);

        return data;
    }

    public static bool Contains(string id)
    {
        Locale data = null;
        if (!string.IsNullOrEmpty(id))
            data = Instance.Data.Find(e => e.Id == id);
        return data != null;
    }

    public static void LoadData()
    {
        if (!loaded)
        {
            DebugUtils.Log("Loading Locale...");
            try
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(Path.Combine(Application.streamingAssetsPath, "Data/LocaleData.xml"));

                DefaultLocale = doc.SelectSingleNode("LocaleData/Locales/Default").InnerText;

                XmlNode locales = doc.SelectSingleNode("LocaleData/Locales");

                Instance.Locales.Clear();

                foreach (XmlNode locNode in locales.ChildNodes)
                {
                    string loc = locNode.InnerText;

                    if (!Instance.Locales.Contains(loc))
                        Instance.Locales.Add(loc);
                }

                XmlNodeList entryNodes = doc.SelectNodes("LocaleData/Entry");

                foreach (XmlNode entryNode in entryNodes)
                {
                    string id = entryNode.Attributes["Id"].InnerText;

                    string comment = entryNode.SelectSingleNode("Comment").InnerText;
                    string context = entryNode.SelectSingleNode("Context").InnerText;
                    string options = entryNode.SelectSingleNode("Options").InnerText;

                    XmlNode textNode = entryNode.SelectSingleNode("Text");

                    Dictionary<string, string> localeText = new Dictionary<string, string>();
                    foreach (XmlNode locTextNode in textNode.ChildNodes)
                    {
                        string loc = locTextNode.Name;
                        if (!Instance.Locales.Contains(loc))
                            continue;

                        if (!localeText.ContainsKey(loc))
                            localeText.Add(loc, locTextNode.InnerText);
                    }

                    if (Contains(id))
                        continue;

                    Instance.Data.Add(new Locale(id, comment, context, options, localeText));
                }
                DebugUtils.Log("Locale Entries Loaded: " + Instance.Data.Count);
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

//[CustomEditor(typeof(LocaleData))]
//public class LocaleDrawer : PropertyDrawer
//{
//    int localeRegionSelected = 0;

//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        EditorGUI.BeginProperty(position, label, property);

//        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

//        // Don't make child fields be indented
//        var indent = EditorGUI.indentLevel;
//        EditorGUI.indentLevel = 0;

//        // Calculate rects
//        Rect localeRect = new Rect(position.x, position.y, 30, position.height);

//        // Draw fields - pass GUIContent.none to each so they are drawn without labels
//        //EditorGUI.PropertyField(localeRect, property.FindPropertyRelative("Id"), GUIContent.none);

//        localeRegionSelected = EditorGUI.Popup(localeRect, localeRegionSelected, LocaleData.Locales.ToArray());

//        // Set indent back to what it was
//        EditorGUI.indentLevel = indent;

//        EditorGUI.EndProperty();
//    }
//}

[CustomEditor(typeof(Locale))]
public class LocaleEditor : Editor
{
    int localeRegionSelected = 0;

    private string[] list;

    private void OnEnable()
    {
        ReloadList();
        LocaleData.LoadData();
    }

    private void ReloadList()
    {
        list = null;
        list = LocaleData.Instance.Locales.ToArray();
    }

    public override void OnInspectorGUI()
    {
        DebugUtils.Log("Test...");
        if (list == null)
        {
            list = LocaleData.Instance.Locales.ToArray();
            LocaleData.LoadData();
        }
        DrawDefaultInspector();

        localeRegionSelected = EditorGUILayout.Popup("Locales", localeRegionSelected, list, EditorStyles.popup);
    }
}

[CustomPropertyDrawer(typeof(Locale))]
public class LocaleDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var localeRect = new Rect(position.x, position.y, 130, position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        //EditorGUI.PropertyField(localeRect, property.FindPropertyRelative("Id"), GUIContent.none);
        GUIContent content = new GUIContent("Locale");

        EditorGUI.DropdownButton(localeRect, content, FocusType.Passive);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}