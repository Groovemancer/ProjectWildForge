using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class StringUtils
{
    public static string GetLocalizedText(string textId)
    {
        return LocaleData.GetTextById(LocaleData.CurrentLocale(), textId);
    }

    public static string GetLocalizedTextFiltered(string filter)
    {
        return LocaleData.GetText(LocaleData.CurrentLocale(), filter);
    }

    /// <summary>
    /// Get localized text from either a tID (text id) or from a filter
    /// </summary>
    /// <param name="text">$(tid:3125) or $(filter:comment#text|context#text|options#text)"</param>
    /// <returns></returns>
    public static string GetText(string text)
    {
        string result = "";

        string tidTag = "$(tid:";
        string filterTag = "$(filter:";

        result = text;

        if (result.Contains(tidTag))
        {
            while (result.Contains(tidTag))
            {
                int startIdx = result.IndexOf(tidTag);
                int endIdx = result.IndexOf(")");

                if (startIdx == -1 || endIdx == -1)
                    break;

                string strId = result.Substring(startIdx, endIdx - startIdx);
                strId = strId.Remove(0, tidTag.Length);

                result = result.Remove(startIdx, endIdx - startIdx + 1);
                string locText = GetLocalizedText(strId);
                result = result.Insert(startIdx, locText);
            }
        }
        else if (result.Contains(filterTag))
        {

            while (result.Contains(filterTag))
            {
                int startIdx = result.IndexOf(filterTag);
                int endIdx = result.IndexOf(")");

                if (startIdx == -1 || endIdx == -1)
                    break;

                string strFiltered = result.Substring(startIdx, endIdx - startIdx);
                strFiltered = strFiltered.Remove(0, filterTag.Length);

                result = result.Remove(startIdx, endIdx - startIdx + 1);
                string locText = GetLocalizedTextFiltered(strFiltered);
                result = result.Insert(startIdx, locText);
            }
        }

        return result;
    }

    public static List<int> AllIndexesOf(string str, string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("The string to find may not be empty", "value");
        List<int> indexes = new List<int>();
        for (int index = 0; ; index += value.Length)
        {
            index = str.IndexOf(value, index);
            if (index == -1)
                return indexes;
            indexes.Add(index);
        }
    }

    public static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int place = source.LastIndexOf(find);

        if (place == -1)
            return source;

        string result = source.Remove(place, find.Length).Insert(place, replace);
        return result;
    }
}
