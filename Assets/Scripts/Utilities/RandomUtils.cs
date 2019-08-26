using System;

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

    public static float Range(float min, float max)
    {
        calls++;
        float oldValue = (float)Instance.NextDouble();

        float range = max - min;

        float result = (oldValue * range) + min;

        return result;
    }
}
