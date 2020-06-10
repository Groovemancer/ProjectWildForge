using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
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

        // Check for pre-existing actors, which won't do the callback.
        foreach (Actor a in World.actors)
        {
            OnActorCreated(a);
        }
    }

    private void LoadSprites()
    {
        actorSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Actors");

        //Debug.Log("LOADED RESOURCES:");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
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
*/

public class ActorSpriteController : BaseSpriteController<Actor>
{
    // Use this for initialization
    public ActorSpriteController(World world) : base(world, "Actor", 100)
    {
        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.ActorManager.Created += OnCreated;

        // Check for pre-existing actors, which won't do the callback.
        foreach (Actor actor in world.ActorManager)
        {
            OnCreated(actor);
        }
    }

    public override void RemoveAll()
    {
        world.ActorManager.Created -= OnCreated;

        foreach (Actor actor in world.ActorManager)
        {
            actor.OnActorChanged -= OnChanged;
        }

        base.RemoveAll();
    }

    protected override void OnCreated(Actor actor)
    {
        // This creates a new GameObject and adds it to our scene.
        GameObject actor_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(actor, actor_go);

        actor_go.name = "Actor";
        actor_go.transform.position = new Vector3(actor.CurrTile.X, actor.CurrTile.Y, 0);
        actor_go.transform.SetParent(objectParent.transform, true);

        SpriteRenderer sr = actor_go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Actors";
        sr.sortingOrder = Mathf.RoundToInt(actor.Y * 100f) * -1;
        sr.sprite = SpriteManager.GetSprite("Actor", actor.SpriteName);

        // Register our callback so that our GameObject gets updated whenever
        // the object's info changes.
        actor.OnActorChanged += OnChanged;

        DebugUtils.LogChannel("ActorSpriteController", "OnCreated actorID: " + actor.Id);
    }

    protected override void OnChanged(Actor actor)
    {
        GameObject actor_go;
        if (objectGameObjectMap.TryGetValue(actor, out actor_go) == false)
        {
            DebugUtils.LogErrorChannel("ActorSpriteController", "OnActorChanged -- trying to change visuals for actor not in our map.");
            return;
        }

        if (actor_go != null)
        {
            actor_go.transform.position = new Vector3(actor.CurrTile.X, actor.CurrTile.Y, 0);
            actor_go.GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(actor.Y * 100f) * -1;
        }
    }

    protected override void OnRemoved(Actor actor)
    {
        actor.OnActorChanged -= OnChanged;
        GameObject actor_go = objectGameObjectMap[actor];
        objectGameObjectMap.Remove(actor);
        GameObject.Destroy(actor_go);
    }
}