using UnityEngine;

public class WanderState : State
{
    Vector3 centre;
    Vector3 target;

    public override void Enter()
    {
        agent.SetColour(Color.green);
        centre = agent.transform.position; // wander around where we entered
        PickNewTarget();
    }

    public override void Tick()
    {
        if (HandleDemoHotkeys()) return;

        agent.MoveTowardsXZ(target, agent.wanderSpeed);

        if (SimpleAgent.DistanceXZ(agent.transform.position, target) <= agent.wanderRetargetDistance)
            PickNewTarget();
    }

    void PickNewTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * Mathf.Max(0.01f, agent.wanderRadius);
        target = new Vector3(centre.x + rnd.x, agent.GroundY, centre.z + rnd.y);
    }

    public override void Exit() { /* nothing needed */ }
}
