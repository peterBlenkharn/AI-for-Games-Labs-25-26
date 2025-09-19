using UnityEngine;

/// <summary>
/// Minimal finite state machine: keeps a single active State
/// and calls Enter/Exit/Tick as needed. No allocations on transition.
/// </summary>
/// 
public class StateMachine
{
    public State Current { get; private set; }

    public void Initialise(State initial)
    {
        Current = initial;
        Current?.Enter();
    }

    public void ChangeState(State next)
    {
        if (next == null || next == Current) return;
        Current?.Exit();
        Current = next;
        Current.Enter();
    }

    public void Tick()
    {
        Current?.Tick();
    }
}
