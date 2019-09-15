using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public static class ModUtils
{
    private static string defaultLogChannel = "ModUtility";

    public static float Clamp01(float value)
    {
        return Mathf.Clamp01(value);
    }

    public static int FloorToInt(float value)
    {
        return Mathf.FloorToInt(value);
    }

    public static float Round(float value, int digits)
    {
        return (float)System.Math.Round((double)value, digits);
    }

    public static void Log(object obj)
    {
        DebugUtils.LogChannel(defaultLogChannel, obj);
    }

    public static void LogWarning(object obj)
    {
        DebugUtils.LogWarningChannel(defaultLogChannel, obj);
    }

    public static void LogError(object obj)
    {
        DebugUtils.LogErrorChannel(defaultLogChannel, obj);
    }

    public static void ULogChannel(string channel, string message)
    {
        DebugUtils.LogChannel(channel, message);
    }

    public static void ULogWarningChannel(string channel, string message)
    {
        DebugUtils.LogWarningChannel(channel, message);
    }

    public static void ULogErrorChannel(string channel, string message)
    {
        DebugUtils.LogErrorChannel(channel, message);
    }

    public static void ULog(string message)
    {
        DebugUtils.LogChannel(defaultLogChannel, message);
    }

    public static void ULogWarning(string message)
    {
        DebugUtils.LogWarningChannel(defaultLogChannel, message);
    }

    public static void ULogError(string message)
    {
        DebugUtils.LogErrorChannel(defaultLogChannel, message);
    }

    public static float Clamp(float value, float min, float max)
    {
        return value.Clamp(min, max);
    }

    public static int Min(int a, int b)
    {
        return Mathf.Min(a, b);
    }

    public static int Max(int a, int b)
    {
        return Mathf.Max(a, b);
    }
}