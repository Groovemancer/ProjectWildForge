using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; protected set; }

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
        // Load Stuff
        LocaleData.LoadData();
        TileTypeData.LoadData();

        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
        DateTime dtNow = DateTime.Now;
        TimeSpan result = dtNow.Subtract(dt);
        RandomSeed = Convert.ToInt32(result.TotalSeconds);

        RandomUtils.Initialize(RandomSeed);
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


}
