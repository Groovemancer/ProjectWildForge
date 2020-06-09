#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MoonSharp.Interpreter;
using MoonSharp.VsCodeDebugger;

public class LuaFunctions : IFunctions
{
    protected Script script;
    private string scriptName;

    public LuaFunctions()
    {
        // Tell the LUA interpreter system to load all the classes
        // that we have marked as [MoonSharpUserData]
        UserData.RegisterAssembly();

        this.script = new Script();

        // Registering types
        UserData.RegisterType<UnityEngine.Vector3>();
        UserData.RegisterType<UnityEngine.Vector2>();
        UserData.RegisterType<UnityEngine.Vector4>();
        UserData.RegisterType<UnityEngine.UI.Text>();

        // If we want to be able to instantiate a new object of a class
        //   i.e. by doing    SomeClass.__new()
        // We need to make the base type visible.
        RegisterGlobal(typeof(Inventory));
        RegisterGlobal(typeof(Job));
        RegisterGlobal(typeof(JobManager));
        RegisterGlobal(typeof(ModUtils));
        RegisterGlobal(typeof(World));
        RegisterGlobal(typeof(WorldController));
        //RegisterGlobal(typeof(Connection));
        //RegisterGlobal(typeof(Scheduler.Scheduler));
        //RegisterGlobal(typeof(Scheduler.ScheduledEvent));
        RegisterGlobal(typeof(RequestedItem));
        //RegisterGlobal(typeof(DeveloperConsole.DevConsole));
        //RegisterGlobal(typeof(Settings));
    }

    ~LuaFunctions()
    {
        server.Dispose();
    }

    static MoonSharpVsCodeDebugServer server;

    public bool HasFunction(string name)
    {
        return name != null && script.Globals[name] != null;
    }

    public bool HasConstructor(string className)
    {
        return className != null && script.Globals[className] != null;
    }

    /// <summary>
    /// Loads the script from the specified text.
    /// </summary>
    /// <param name="text">The code text.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadScript(string text, string scriptName)
    {
        this.scriptName = scriptName;
        try
        {
            // TODO: Disabling debugging server temporarily, it seems to have stopped working. Will investigate later.
            // TEst
            /*
            if (server == null)
            {
                server = new MoonSharpVsCodeDebugServer();

                // Start the debugger server
                int retries = 0;
                int maxRetries = 10;
                while (retries <= maxRetries)
                {
                    try
                    {
                        server.Start();
                        retries = maxRetries + 1;
                        DebugUtils.LogChannel("Lua", "VsCodeServer Debugger Started!");
                    }
                    catch (SocketException se)
                    {
                        DebugUtils.LogErrorChannel("Lua", "SocketException! Try: " + retries.ToString());
                        if (se.ErrorCode == 10048)
                        {
                            DebugUtils.LogErrorChannel("Lua", "Failed to start Debugger! " + se.ErrorCode.ToString() + " - " + se.Message);
                            if (retries != maxRetries)
                            {
                                DebugUtils.LogErrorChannel("Lua", "Waiting for current socket to close. Retrying in 1 second!");
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        else
                        {
                            DebugUtils.LogErrorChannel("Lua", "Cannot start debugger! Unhandled Error: " + se.ErrorCode.ToString() + " - " + se.Message);
                        }

                        if (retries == maxRetries)
                        {
                            DebugUtils.LogErrorChannel("Lua", "Max retries exceeded!");
                            throw new Exception("Failed to start Debugger!");
                        }

                        retries++;
                    }
                }

            }

            server.AttachToScript(script, "StructureActions", s => text);
            */
            script.DoString(text, script.Globals);
        }
        catch (SyntaxErrorException e)
        {
            DebugUtils.LogErrorChannel("Lua", "[" + scriptName + "] LUA Parse error: " + e.DecoratedMessage);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads the script from the given file.
    /// </summary>
    /// <param name="file">The file to run.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadFile(string file, string scriptName)
    {
        // can't use LoadFile without a custom ILoader (since it defaults to Resources/Scripts)
        // so basically it would just be more work than doing this
        return LoadScript(System.IO.File.ReadAllText(file), scriptName);
    }

    public DynValue CallWithError(string functionName, params object[] args)
    {
        return Call(functionName, true, args);
    }

    public DynValue Call(string functionName, params object[] args)
    {
        return Call(functionName, false, args);
    }

    public T Call<T>(string functionName, params object[] args)
    {
        return Call(functionName, args).ToObject<T>();
    }

    public DynValue CreateInstance(object fromObject)
    {
        return DynValue.FromObject(script, fromObject);
    }

    public T CreateInstance<T>(string className, params object[] arguments)
    {
        return Call<T>(className, arguments);
    }

    public void RegisterType(Type type)
    {
        RegisterGlobal(type);
    }

    /// <summary>
    /// Call the specified lua function with the specified args.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="args">Arguments.</param>
    private DynValue Call(string functionName, bool throwError, params object[] args)
    {
        try
        {
            return ((Closure)script.Globals[functionName]).Call(args);
        }
        catch (ScriptRuntimeException e)
        {
            DebugUtils.LogErrorChannel("Lua", "[" + scriptName + "," + functionName + "] LUA RunTime error: " + e.DecoratedMessage);
            return null;
        }
        catch (Exception e)
        {
            DebugUtils.LogErrorChannel("Lua", "[" + scriptName + "," + functionName + "] Something else went wrong: " + e.Message);
            DebugUtils.LogErrorChannel("Lua", e);
            return null;
        }
    }

    /// <summary>
    /// Registers a class as a global entity to use it inside of lua.
    /// </summary>
    /// <param name="type">Class typeof.</param>
    private void RegisterGlobal(Type type)
    {
        script.Globals[type.Name] = type;
    }
}
