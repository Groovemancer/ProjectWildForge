using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class State
{
    protected Actor actor;

    public State(string name, Actor actor, State nextState)
    {
        Name = name;
        this.actor = actor;
        NextState = nextState;
    }

    public string Name { get; protected set; }

    public State NextState { get; protected set; }

    public virtual void Enter()
    {
        DebugLog(" - Enter");
    }

    public abstract void Update(float deltaTime);

    public virtual void Exit()
    {
        DebugLog(" - Exit");
    }

    public virtual void Interrupt()
    {
        DebugLog(" - Interrupt");

        if (NextState != null)
        {
            NextState.Interrupt();
            NextState = null;
        }
    }

    public override string ToString()
    {
        return string.Format("[{0}State]", Name);
    }

    protected void Finished()
    {
        DebugLog(" - Finished");
        actor.SetState(NextState);
    }

    #region Debug

    protected void DebugLog(string message, params object[] par)
    {
        string prefixedMessage = string.Format("{0}, {1} {2}: {3}", actor.GetName(), actor.Id, StateStack(), message);
        DebugUtils.LogChannel("Actor", string.Format(prefixedMessage, par));
    }

    private string StateStack()
    {
        List<string> names = new List<string> { Name };
        State state = this;
        while (state.NextState != null)
        {
            state = state.NextState;
            names.Insert(0, state.Name);
        }

        return string.Join(".", names.ToArray());
    }

    #endregion
}
