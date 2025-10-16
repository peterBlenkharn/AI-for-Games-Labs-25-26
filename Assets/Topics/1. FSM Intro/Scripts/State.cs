using System;
using UnityEngine;

/// <summary>
/// Generic base State. States get references to the agent and machine
/// (set once after construction) and implement Enter/Exit/Tick.
/// Transitions live inside states for clarity.
/// </summary>
[Serializable]
public abstract class State
{
    // Set once by the agent after the state is constructed.
    public SimpleAgent agent;
    public StateMachine fsm;

    public virtual string Name => GetType().Name;

    public virtual void Enter() { /* optional */ }
    public virtual void Exit() { /* optional */ }
    public abstract void Tick();

    // Shared demo hotkeys so students can force transitions clearly.
    protected bool HandleDemoHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { fsm.ChangeState(agent.Idle); return true; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { fsm.ChangeState(agent.Wander); return true; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { fsm.ChangeState(agent.Jump); return true; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { fsm.ChangeState(agent.Circle); return true; }
        return false;
    }
}