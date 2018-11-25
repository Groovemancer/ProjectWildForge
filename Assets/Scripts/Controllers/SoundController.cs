using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    float soundCooldown = 0;

    // Use this for initialization
    void Start()
    {
        WorldController.Instance.World.RegisterBuildingCreated(OnBuildingCreated);

        WorldController.Instance.World.RegisterTileChanged(OnTileChanged);
    }

    private void Update()
    {
        soundCooldown -= Time.deltaTime;
    }

    public void OnTileChanged(Tile tile_data)
    {
        if (soundCooldown > 0)
            return;

        // FIXME
        AudioClip ac = Resources.Load<AudioClip>("Sounds/Tile_OnCreated");
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }

    public void OnBuildingCreated(Building obj)
    {
        if (soundCooldown > 0)
            return;

        // FIXME
        AudioClip ac = Resources.Load<AudioClip>("Sounds/" + obj.ObjectType + "_OnCreated");
        if (ac == null)
        {
            // If not found, use default -- i.e. Wall_OnCreated
            ac = Resources.Load<AudioClip>("Sounds/Wall_OnCreated");
        }
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }
}
