using UnityEngine;

public class IdleState : State
{
    public override void Enter()
    {
        agent.SetColour(Color.grey);
    }

    public override void Tick()
    {
        if (HandleDemoHotkeys()) return;
        // Does nothing.
    }
}