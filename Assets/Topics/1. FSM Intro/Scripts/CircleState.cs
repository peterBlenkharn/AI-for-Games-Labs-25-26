using UnityEngine;

public class CircleState : State
{
    Vector3 centre;
    float angleDeg;

    public override void Enter()
    {
        agent.SetColour(Color.red);

        // Radius & current position on the XZ plane
        float r = Mathf.Max(0.01f, agent.circleRadius);
        Vector3 pos = agent.transform.position;
        pos.y = agent.GroundY;

        // Use the agent's facing to choose circle direction (tangent = forward at start)
        Vector3 fwd = agent.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward; // fallback
        fwd.Normalize();

        // Radial vector = forward rotated 90° clockwise in XZ (so tangent aligns with forward)
        Vector3 radial = new Vector3(fwd.z, 0f, -fwd.x) * r;

        // Choose centre so that current position lies on the circle at angleDeg
        centre = pos - radial;

        // Set angleDeg so that centre + (cos,sin)*r == current position
        angleDeg = Mathf.Atan2(radial.z, radial.x) * Mathf.Rad2Deg;
    }

    public override void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) fsm.ChangeState(agent.Idle);
        if (Input.GetKeyDown(KeyCode.Alpha2)) fsm.ChangeState(agent.Wander);
        if (Input.GetKeyDown(KeyCode.Alpha3)) fsm.ChangeState(agent.Jump);

        angleDeg += agent.circleAngularSpeed * Time.deltaTime;
        float r = Mathf.Max(0.01f, agent.circleRadius);
        float rad = angleDeg * Mathf.Deg2Rad;

        Vector3 pos = new Vector3(
            centre.x + Mathf.Cos(rad) * r,
            agent.GroundY,
            centre.z + Mathf.Sin(rad) * r
        );

        if (agent.faceAlongPath)
        {
            float nextRad = (angleDeg + 2f) * Mathf.Deg2Rad;
            Vector3 nextPos = new Vector3(
                centre.x + Mathf.Cos(nextRad) * r,
                agent.GroundY,
                centre.z + Mathf.Sin(nextRad) * r
            );
            Vector3 dir = (nextPos - pos).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, look, agent.turnLerp * Time.deltaTime);
            }
        }

        agent.transform.position = pos;
    }
}
