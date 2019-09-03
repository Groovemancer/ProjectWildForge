using System.Collections.Generic;
using System.Xml;
using MoonSharp.Interpreter;

/// <summary>
/// This class handles LUA actions take in response to events triggered within C# or LUA. For each event name (e.g. OnUpdate, ...) there
/// is a list of LUA function that are registered and will be called once the event with that name is fired.
/// </summary>
[MoonSharpUserData]
public class EventActions
{
    /// <summary>
    /// Stores a list of Lua functions for each type of event (eventName). All will be called at once.
    /// </summary>
    protected Dictionary<string, List<string>> actionList = new Dictionary<string, List<string>>();

    /// <summary>
    /// Used to transfer register actions to new object.
    /// </summary>
    /// <returns>A new copy of this.</returns>
    public EventActions Clone()
    {
        EventActions evt = new EventActions();

        evt.actionList = new Dictionary<string, List<string>>(actionList);

        return evt;
    }

    /// <summary>
    /// Fill the values of this from Xml
    /// </summary>
    /// <param name="rootNode">Xml node pointing to an EventAction tag.</param>
    public void ReadXml(XmlNode rootNode)
    {
        if (rootNode == null)
        {
            return;
        }

        string name = rootNode.Attributes["event"].InnerText;
        string functionName = rootNode.Attributes["FunctionName"].InnerText;

        Register(name, functionName);
    }

    /// <summary>
    /// Register a function named luaFunc, that gets fired in response to an action named actionName.
    /// </summary>
    /// <param name="actionName">Name of event triggering action.</param>
    /// <param name="luaFunc">Lua function to add to list of actions.</param>
    public void Register(string actionName, string luaFunc)
    {
        List<string> actions;
        if (actionList.TryGetValue(actionName, out actions) == false || actions == null)
        {
            actions = new List<string>();
            actionList[actionName] = actions;
        }

        actions.Add(luaFunc);
    }

    /// <summary>
    /// Deregister a function named luaFunc, from the action.
    /// </summary>
    /// <param name="actionName">name of event triggering action.</param>
    /// <param name="luaFunc">Lua function to add to list of actions.</param>
    public void Deregister(string actionName, string luaFunc)
    {
        List<string> actions;
        if (actionList.TryGetValue(actionName, out actions) == false || actions == null)
        {
            return;
        }

        actions.Remove(luaFunc);
    }

    /// <summary>
    /// Fire the event named actionName, resulting in all lua functions being called.
    /// This one reduces GC bloat.
    /// </summary>
    /// <param name="actionName">Name of the action being triggered.</param>
    /// <param name="parameters">Parameters in question. First one must be target instance.</param>
    public void Trigger(string actionName, params object[] parameters)
    {
        List<string> actions;
        if (actionList.TryGetValue(actionName, out actions) && actions != null)
        {
            FunctionsManager.Get(parameters[0].GetType().Name).Call(actions, parameters);
        }
    }

    /// <summary>
    /// Determines whether this instance has any events named actionName.
    /// </summary>
    /// <returns><c>true</c> if this instance has any events named actionName; otherwise, <c>false</c>.</returns>
    /// <param name="actionName">Action name.</param>
    public bool HasEvent(string actionName)
    {
        // FIXME: 'Has' methods are generally a bad idea, should be a 'TryGet' instead
        return actionList.ContainsKey(actionName);
    }

    /// <summary>
    /// Determines whether this instance has any events.
    /// </summary>
    public bool HasEvents()
    {
        return actionList.Count > 0;
    }
}