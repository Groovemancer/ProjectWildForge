using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; protected set; }

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
        // Load locale
        LocaleData.LoadData();
        TileTypeData.LoadData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
