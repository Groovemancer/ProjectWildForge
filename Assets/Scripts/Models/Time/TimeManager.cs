﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeManager
{
    private const int FramesInSlowUpdateCycle = 10;

    private const float GameTickPerSecond = 5;

    private const float AutsPerSecond = 100f;

    private const float RealTimeToWorldTimeFactor = 90;

    private static TimeManager instance;

    private List<Action> nextFrameActions = new List<Action>();

    // An array of possible time multipliers.
    private float[] possibleTimeScales = new float[10] { 0.5f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f };

    private List<IUpdatable> fastUpdatables = new List<IUpdatable>();

    private List<IUpdatable>[] slowUpdatablesLists = new List<IUpdatable>[FramesInSlowUpdateCycle];

    private float[] accumulatedTime = new float[FramesInSlowUpdateCycle];
    private int timePos = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeManager"/> class.
    /// </summary>
    public TimeManager()
    {
        instance = this;
        TimeScale = 1f;
        TotalDeltaTime = 0f;
        TimeScalePosition = 1;
        IsPaused = true;
        WorldTime = new WorldTime().SetHour(8);

        for (int i = 0; i < slowUpdatablesLists.Length; i++)
        {
            slowUpdatablesLists[i] = new List<IUpdatable>();
        }

        KeyboardManager.Instance.RegisterInputAction("SetSpeed1", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(1));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed2", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(2));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed3", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(3));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed4", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(4));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed5", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(5));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed6", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(6));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed7", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(7));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed8", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(8));
        KeyboardManager.Instance.RegisterInputAction("SetSpeed9", KeyboardMappedInputType.KeyUp, () => SetTimeScalePosition(9));
        KeyboardManager.Instance.RegisterInputAction("DecreaseSpeed", KeyboardMappedInputType.KeyUp, DecreaseTimeScale);
        KeyboardManager.Instance.RegisterInputAction("IncreaseSpeed", KeyboardMappedInputType.KeyUp, IncreaseTimeScale);
    }

    /// <summary>
    /// Systems that update every frame.
    /// </summary>
    public event Action<float> EveryFrame;

    /// <summary>
    /// Systems that update every frame not in Modal.
    /// </summary>
    public event Action<float> EveryFrameNotModal;

    /// <summary>
    /// Systems that update every frame while unpaused.
    /// </summary>
    public event Action<float> EveryFrameUnpaused;

    /// <summary>
    /// Systems that update at fixed frequency.
    /// </summary>
    public event Action<float> FixedFrequency;

    /// <summary>
    /// Systems that update at fixed frequency while unpaused.
    /// </summary>
    public event Action<float> FixedFrequencyUnpaused;

    /// <summary>
    /// Gets the TimeManager instance.
    /// </summary>
    /// <value>The TimeManager instance.</value>
    public static TimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                new TimeManager();
            }

            return instance;
        }
    }

    /// <summary>
    /// Gets the game time.
    /// </summary>
    /// <value>The game time.</value>
    public float GameTime { get; private set; } // TODO: Implement saving and loading game time, so time is persistent across loads.

    /// <summary>
    /// <para>Gets or sets the world time.</para>
    /// <para>Assigning to WorldTime will adjust GameTime appropriately, however, using the Set and Add methods directly
    /// on WorldTime will not work. To adjust WorldTime assign a new WorldTime, or assign immediately after adjusting.</para>
    /// <para>Example: <code>WorldTime = WorldTime.SetHour(8).SetMinute(0)</code> will properly change the time to 8:00, leaving everything else the same.</para>
    /// </summary>
    /// <value>The world time.</value>
    public WorldTime WorldTime
    {
        get
        {
            return new WorldTime(GameTime * RealTimeToWorldTimeFactor);
        }

        set
        {
            GameTime = value.Seconds / RealTimeToWorldTimeFactor;
        }
    }

    // Current position in that array.
    // Public so TimeScaleUpdater can easily get a position appropriate to an image.
    public int TimeScalePosition { get; private set; }

    /// <summary>
    /// Gets the game time tick delay.
    /// </summary>
    /// <value>The game time tick delay.</value>
    public float GameTickDelay
    {
        get { return 1f / GameTickPerSecond; }
    }

    /// <summary>
    /// Gets the total delta time.
    /// </summary>
    /// <value>The total delta time.</value>
    public float TotalDeltaTime { get; private set; }

    /// <summary>
    /// Multiplier of Time.deltaTime.
    /// </summary>
    /// <value>The time scale.</value>
    public float TimeScale { get; private set; }

    /// <summary>
    /// Returns true if the game is paused.
    /// </summary>
    /// <value><c>true</c> if this game is paused; otherwise, <c>false</c>.</value>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Returns a copy of the time scale array.
    /// </summary>
    /// <returns> A non reference copy of the time scale array. </returns>
    public float[] GetTimeScaleArrayCopy()
    {
        return possibleTimeScales;
    }

    private float avgDeltaAuts = 0;
    private int count = 0;
    private int maxCount = 600;

    /// <summary>
    /// Update the total time and invoke the required events.
    /// </summary>
    /// <param name="time">Time since last frame.</param>
    public void Update(float time)
    {
        float deltaTime = time * TimeScale;

        float deltaAuts = AutsPerSecond * time * TimeScale;

        //DebugUtils.LogChannel("TimeManager", "Update deltaAuts: " + deltaAuts);

        count++;
        avgDeltaAuts += deltaAuts / count;
        if (count > maxCount)
        {
            //DebugUtils.LogChannel("TimeManager", "Update Avg Delta Auts: " + avgDeltaAuts);
            count = 0;
            avgDeltaAuts = 0;
        }

        // Systems that update every frame.
        InvokeEvent(EveryFrame, time);

        if (nextFrameActions.Count > 0)
        {
            for (int i = 0; i < nextFrameActions.Count; i++)
            {
                nextFrameActions[i].Invoke();
            }
            
            nextFrameActions.Clear();
        }

        // Systems that update every frame not in Modal.
        if (GameController.Instance.IsModal == false)
        {
            InvokeEvent(EveryFrameNotModal, time);
        }

        // Systems that update every frame while unpaused.
        if (GameController.Instance.IsPaused == false)
        {
            GameTime += deltaTime;
            InvokeEvent(EveryFrameUnpaused, deltaAuts);
            ProcessUpdatables(deltaAuts);
        }

        // Systems that update at fixed frequency.
        if (TotalDeltaTime >= GameTickDelay)
        {
            InvokeEvent(FixedFrequency, TotalDeltaTime);

            // Systems that update at fixed frequency when not paused.
            if (GameController.Instance.IsPaused == false)
            {
                InvokeEvent(FixedFrequencyUnpaused, TotalDeltaTime);
            }

            TotalDeltaTime = 0;
        }

        TotalDeltaTime += deltaAuts;
    }

    public void ProcessUpdatables(float deltaAuts)
    {
        IUpdatable[] updatablesCopy = new IUpdatable[fastUpdatables.Count];
        fastUpdatables.CopyTo(updatablesCopy, 0);
        for (int i = 0; i < updatablesCopy.Length; i++)
        {
            updatablesCopy[i].EveryFrameUpdate(deltaAuts);
        }

        accumulatedTime[timePos] = deltaAuts;
        float accumulatedDeltaTime = accumulatedTime.Sum();

        updatablesCopy = new IUpdatable[slowUpdatablesLists[timePos].Count];
        slowUpdatablesLists[timePos].CopyTo(updatablesCopy, 0);

        for (int i = 0; i < updatablesCopy.Length; i++)
        {
            updatablesCopy[i].FixedFrequencyUpdate(accumulatedDeltaTime);
        }

        timePos++;
        if (timePos >= accumulatedTime.Length)
        {
            timePos = 0;
        }

        updatablesCopy = null;
    }

    /// <summary>
    /// Sets the speed of the game. Greater time scale position equals greater speed.
    /// </summary>
    /// <param name="newTimeScalePosition">New time scale position.</param>
    public void SetTimeScalePosition(int newTimeScalePosition)
    {
        if (newTimeScalePosition < possibleTimeScales.Length && newTimeScalePosition >= 0 && newTimeScalePosition != TimeScalePosition)
        {
            TimeScalePosition = newTimeScalePosition;
            TimeScale = possibleTimeScales[newTimeScalePosition];
            DebugUtils.LogChannel("Game speed", "Game speed set to " + TimeScale + "x");
            IsPaused = false;
        }
    }

    /// <summary>
    /// Increases the game speed by increasing the time scale by 1.
    /// </summary>
    public void IncreaseTimeScale()
    {
        SetTimeScalePosition(TimeScalePosition + 1);
    }

    /// <summary>
    /// Decreases the game speed by decreasing the time scale by 1.
    /// </summary>
    public void DecreaseTimeScale()
    {
        SetTimeScalePosition(TimeScalePosition - 1);
    }

    /// <summary>
    /// Destroy this instance.
    /// </summary>
    public void Destroy()
    {
        instance = null;
    }

    public void RegisterFastUpdate(IUpdatable updatable)
    {
        if (!fastUpdatables.Contains(updatable))
        {
            fastUpdatables.Add(updatable);
        }
    }

    public void UnregisterFastUpdate(IUpdatable updatable)
    {
        if (fastUpdatables.Contains(updatable))
        {
            fastUpdatables.Remove(updatable);
        }
    }

    public void RegisterSlowUpdate(IUpdatable updatable)
    {
        if (!slowUpdatablesLists.Any(list => list.Contains(updatable)))
        {
            slowUpdatablesLists.OrderBy(list => list.Count).First().Add(updatable);
        }
    }

    public void UnregisterSlowUpdate(IUpdatable updatable)
    {
        if (slowUpdatablesLists.Any(list => list.Contains(updatable)))
        {
            // This should only ever return one list, as if statement guarantees it has at least one, and register method ensures no more than one
            slowUpdatablesLists.Single(list => list.Contains(updatable)).Remove(updatable);
        }
    }

    /// <summary>
    /// Runs the action in the next frame before any updates are ran.
    /// </summary>
    /// <param name="action">Action with no parameters.</param>
    public void RunNextFrame(Action action)
    {
        nextFrameActions.Add(action);
    }

    /// <summary>
    /// Invokes the given event action.
    /// </summary>
    /// <param name="eventAction"></param>
    /// <param name="time"></param>
    private void InvokeEvent(Action<float> eventAction, float time)
    {
        if (eventAction != null)
        {
            eventAction(time);
        }
    }
}