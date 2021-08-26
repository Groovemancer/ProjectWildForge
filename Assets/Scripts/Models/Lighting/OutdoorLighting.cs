using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

[ExecuteInEditMode]
public class OutdoorLighting : MonoBehaviour
{
    private Light2D outdoorLight;

    private float lightLevel;
    private float prevLightLevel;

    private int prevHour;
    private int prevMin;
    
    void Awake()
    {
        outdoorLight = GetComponent<Light2D>();
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (outdoorLight == null)
        {
            outdoorLight = GetComponent<Light2D>();
            if (outdoorLight == null)
                return;
        }

        int hour = TimeManager.Instance.WorldTime.Hour;
        int min = TimeManager.Instance.WorldTime.Minute;

        if (hour == prevHour && min == prevMin)
        {
            return;
        }

        if (hour >= 12)
        {
            min = -min;
        }
        if (hour > 12)
        {
            hour = 12 - (hour - 12);
        }
        
        lightLevel = hour + (min / 60.0f);

        lightLevel = Mathf.Clamp(lightLevel, 0, 12);

        if (prevLightLevel != lightLevel)
        {
            prevLightLevel = lightLevel;

            outdoorLight.intensity = 0.3f + (0.2f * (lightLevel / 12f)) + (lightLevel / 12f);
        }

        prevHour = hour;
        prevMin = min;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLighting();
    }
}