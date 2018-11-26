using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorSpriteController : MonoBehaviour
{

    Dictionary<Actor, GameObject> actorGameObjectMap;

    Dictionary<string, Sprite> actorSprites;

    World World
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        actorGameObjectMap = new Dictionary<Actor, GameObject>();

        World.RegisterActorCreated(OnActorCreated);

        // DEBUG
        Actor a = World.CreateActor(World.GetTileAt(World.Width / 2, World.Height / 2));
        a.SetDestination(World.GetTileAt(World.Width / 2 + 5, World.Height / 2 + 2));
    }

    private void LoadSprites()
    {
        actorSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Actors");

        Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            Debug.Log(s);
            actorSprites[s.name] = s;
        }
    }

    public void OnActorCreated(Actor actor)
    {
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        GameObject actor_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        actorGameObjectMap.Add(actor, actor_go);

        actor_go.name = "Actor";
        actor_go.transform.position = new Vector3(actor.CurrTile.X, actor.CurrTile.Y, 0);
        actor_go.transform.SetParent(this.transform, true);

        // FIXME: We assume that the object must be a wall, so use
        // the hardcoded reference to the wall sprite
        SpriteRenderer spr = actor_go.AddComponent<SpriteRenderer>();
        spr.sprite = actorSprites["Elf_M_Front"];
        spr.sortingLayerName = "Actors";

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        actor.RegisterOnChangedCallback(OnActorChanged);
    }

    void OnActorChanged(Actor actor)
    {
        // Make sure the actor's graphics are correct.
        if (actorGameObjectMap.ContainsKey(actor) == false)
        {
            Debug.LogError("OnActorChanged -- trying to change visuals for actor not in our map.");
            return;
        }
        GameObject actor_go = actorGameObjectMap[actor];
        //actor_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForStructure(obj);

        actor_go.transform.position = new Vector3(actor.CurrTile.X, actor.CurrTile.Y, 0);
    }
}
