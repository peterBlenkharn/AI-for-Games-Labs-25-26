using UnityEngine;

public class JumpState : State
{
    Vector3 anchorXZ;
    float t;

    public override void Enter()
    {
        agent.SetColour(Color.yellow);
        var p = agent.transform.position;
        anchorXZ = new Vector3(p.x, 0f, p.z);
        t = 0f;
    }

    public override void Tick()
    {
        if (HandleDemoHotkeys()) return;

        t += Time.deltaTime * agent.jumpSpeed * Mathf.PI * 2f; // radians
        float y = agent.GroundY + Mathf.Abs(Mathf.Sin(t)) * Mathf.Max(0f, agent.jumpHeight);
        agent.transform.position = new Vector3(anchorXZ.x, y, anchorXZ.z); // bounce in place
    }

    public override void Exit()
    {
        agent.SetY(agent.GroundY);
    }
}

