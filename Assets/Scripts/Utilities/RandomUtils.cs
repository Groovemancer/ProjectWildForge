using System;
using System.Collections.Generic;

public class RandomUtils
{
    private static ulong calls = 0;
    private static int seed;
    private static Random instance;

    public static int Seed
    {
        get { return seed; }
        set { Initialize(value); }
    }

    public static ulong Calls
    {
        get { return calls; }
    }

    public static float value
    {
        get { return Range(0.0f, 1.0f); }
    }

    public static Random Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Random(Seed);
            }
            return instance;
        }
    }

    public static void Initialize(int _seed)
    {
        calls = 0;
        seed = _seed;
        instance = new Random(Seed);
    }

    public static ulong GenerateId()
    {
        calls++;
        return (ulong)Instance.Next();
    }

    public static int GenerateInt()
    {
        calls++;
        return Instance.Next();
    }

    public static int Range(int min, int max)
    {
        calls++;
        return Instance.Next(min, max);
    }

    public static int GenerateRandomSeed()
    {
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
        DateTime dtNow = DateTime.Now;
        TimeSpan result = dtNow.Subtract(dt);
        int randomSeed = Convert.ToInt32(result.TotalSeconds);
        return randomSeed;
    }

    public static float Range(float min, float max)
    {
        calls++;
        float oldValue = (float)Instance.NextDouble();

        float range = max - min;

        float result = (oldValue * range) + min;

        return result;
    }

    public static int DiceRoll(int count, int sides)
    {
        return DiceRoll(count, sides, 0);
    }

    public static int DiceRoll(int count, int sides, int modifier)
    {
        return DiceRoll(count, sides, modifier, 1);
    }

    public static int DiceRoll(int count, int sides, int modifier, int min)
    {
        int val = modifier;

        for (int i = 0; i < count; i++)
        {
            val += Range(min, sides + 1);
        }
        return val;
    }

    public static bool Boolean()
    {
        return Convert.ToBoolean(Instance.Next(0, 2));
    }

    /// <summary>
    /// Returns a random value from a list
    /// </summary>
    /// <param name="list"></param>
    /// <param name="nullValue">if list is empty or null, return this value</param>
    /// <returns></returns>
    public static T ObjectFromList<T>(List<T> list, T nullValue)
    {
        calls++;

        if (list == null || list.Count == 0)
            return nullValue;

        return list[Range(0, list.Count)];
    }
}
