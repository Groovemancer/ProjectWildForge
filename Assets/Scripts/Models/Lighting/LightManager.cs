using Mono.CSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;


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
        int id = AddLight("OutdoorLighting", Vector3.zero, Light2D.LightType.Global);
        outdoorLight = Lights[id];

        if (!outdoorLight.GetComponent<OutdoorLighting>())
        {
            outdoorLight.AddComponent<OutdoorLighting>();
        }
    }

    public int AddLight(string name, Vector3 position, Light2D.LightType lightType)
    {
        GameObject lightObject;
        int id = GetAvailableId();

        lightObject = new GameObject(name);
        lightObject.transform.SetParent(objectParent.transform);
        lightObject.transform.position = position;

        Light2D lighting = lightObject.AddComponent<Light2D>();
        lighting.lightType = lightType;

        int[] layers = layers = SortingLayer.layers.Select(x => x.id).ToArray();

        for (int i = 0; i < layers.Length; i++)
        {
            Debug.Log("AddLight layer id=" + layers[i] + ", Name=" + SortingLayer.IDToName(layers[i]));
        }

        lighting.sortingLayers = layers;

        Lights.Add(id, lightObject);

        return id;
    }


    public int AddPointLight(string name, Vector3 position, float innerRadius, float outterRadius)
    {
        int id = AddLight(name, position, Light2D.LightType.Point);

        Light2D lighting = Lights[id].GetComponent<Light2D>();

        lighting.pointLightInnerRadius = innerRadius;
        lighting.pointLightOuterRadius = outterRadius;

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
