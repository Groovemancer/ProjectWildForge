using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    Light2D lighting;

    float intensity;
    public float Variance { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        lighting = GetComponent<Light2D>();
        if (lighting == null)
        {
            this.enabled = false;
            return;
        }
        intensity = lighting.intensity;
    }

    // Update is called once per frame
    void Update()
    {
        if (lighting)
        {
            lighting.intensity = intensity + (Variance * Mathf.Sin(Time.time * TimeManager.Instance.TimeScale));
        }
    }
}
