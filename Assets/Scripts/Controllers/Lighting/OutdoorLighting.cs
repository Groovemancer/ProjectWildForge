using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class OutdoorLighting : MonoBehaviour
{
    public Material EffectMaterial;

    //[Range(0, 12)]
    private float lightLevel;
    private float prevLightLevel;

    private int prevHour;
    private int prevMin;
    
    void Awake()
    {
        UpdateLighting();
    }

    private void UpdateLighting()
    {
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

        if (prevLightLevel != lightLevel)
        {
            prevLightLevel = lightLevel;

            if (EffectMaterial != null)
            {
                EffectMaterial.SetFloat("_LightLevel", lightLevel);
            }
        }

        prevHour = hour;
        prevMin = min;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLighting();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (EffectMaterial != null)
            Graphics.Blit(source, destination, EffectMaterial);
    }
}