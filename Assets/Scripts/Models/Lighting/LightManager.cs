using Mono.CSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightManager
{
    public Dictionary<int, GameObject> Lights { get; private set; }

    private GameObject objectParent;
    private GameObject outdoorLight;

    public LightManager()
    {
        Lights = new Dictionary<int, GameObject>();
        objectParent = new GameObject("Lights");

        CreateOutdoorLighting();
    }

    public GameObject OutdoorLight
    {
        get
        {
            if (outdoorLight == null)
                CreateOutdoorLighting();
            return outdoorLight;
        }
        private set { outdoorLight = value; }
    }

    public void CreateOutdoorLighting()
    {
        int id = AddLight("OutdoorLighting", Vector3.zero, Light2D.LightType.Global, MathUtils.HexToRGB(0xE0E0E0));
        outdoorLight = Lights[id];

        if (!outdoorLight.GetComponent<OutdoorLighting>())
        {
            outdoorLight.AddComponent<OutdoorLighting>();
        }
    }

    public int AddLight(string name, Vector3 position, Light2D.LightType lightType, Color lightColor)
    {
        GameObject lightObject;
        int id = GetAvailableId();

        lightObject = new GameObject(name);
        lightObject.transform.SetParent(objectParent.transform);
        lightObject.transform.position = position;

        Light2D lighting = lightObject.AddComponent<Light2D>();
        lighting.lightType = lightType;
        lighting.color = lightColor;

        int[] layers = layers = SortingLayer.layers.Select(x => x.id).ToArray();

        for (int i = 0; i < layers.Length; i++)
        {
            Debug.Log("AddLight layer id=" + layers[i] + ", Name=" + SortingLayer.IDToName(layers[i]));
        }

        //lighting.sortingLayers = layers;

        FieldInfo fieldInfo = lighting.GetType().GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        fieldInfo.SetValue(lighting, layers);

        Lights.Add(id, lightObject);

        return id;
    }


    public int AddPointLight(string name, Vector3 position, Color lightColor, float innerRadius, float outterRadius, float intensity = 1f, bool flicker = false, float variance = 0.1f)
    {
        int id = AddLight(name, position, Light2D.LightType.Point, lightColor);

        Light2D lighting = Lights[id].GetComponent<Light2D>();

        lighting.pointLightInnerRadius = innerRadius;
        lighting.pointLightOuterRadius = outterRadius;
        lighting.intensity = intensity;

        if (flicker)
        {
            LightFlicker flickering = Lights[id].AddComponent<LightFlicker>();
            flickering.Variance = variance;
        }

        return id;
    }

    private int GetAvailableId()
    {
        int id = -1;
        for (int i = 0; i < Lights.Values.Count; i++)
        {
            if (Lights.ContainsKey(i) && Lights[i] == null)
            {
                id = i;
                break;
            }
        }

        if (id == -1)
        {
            id = Lights.Values.Count;
        }

        return id;
    }
}
