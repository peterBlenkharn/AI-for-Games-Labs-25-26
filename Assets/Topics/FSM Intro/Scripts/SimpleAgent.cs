using UnityEngine;

/// <summary>
/// Attach to a Capsule. Uses transform-based motion for clarity.
/// Press 1=Idle, 2=Wander, 3=Jump, 4=Circle at runtime to switch states.
/// </summary>
public class SimpleAgent : MonoBehaviour
{
    [Header("Debug")]
    public string currentState;

    [Header("Wander")]
    public float wanderSpeed = 2.0f;
    public float wanderRadius = 6.0f;
    public float wanderRetargetDistance = 0.25f;

    [Header("Jump")]
    public float jumpHeight = 1.0f;
    public float jumpSpeed = 3.0f; // cycles per second

    [Header("Circle")]
    public float circleRadius = 3.0f;
    public float circleAngularSpeed = 90f; // degrees/sec
    public bool faceAlongPath = true;

    [Header("General")]
    public float turnLerp = 10f; // how snappy the rotation is while moving

    // FSM + pre-made states (reused; no per-transition allocations)
    public StateMachine FSM { get; private set; }
    public IdleState Idle = new IdleState();
    public WanderState Wander = new WanderState();
    public JumpState Jump = new JumpState();
    public CircleState Circle = new CircleState();

  
    // cached
    public float GroundY { get; private set; }
    Renderer rend;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        GroundY = transform.position.y;

        FSM = new StateMachine();

        // Bind context once so states can reference the agent + FSM.
        Bind(Idle); Bind(Wander); Bind(Jump); Bind(Circle);
    }

    void Start()
    {
        FSM.Initialise(Idle);
    }

    void Update()
    {
        FSM.Tick();
        currentState = FSM.Current != null ? FSM.Current.Name : "(none)";
    }

    void Bind(State s)
    {
        s.agent = this;
        s.fsm = FSM;
    }

    // ---------- tiny helpers to keep states clean ----------

    public void MoveTowardsXZ(Vector3 target, float speed)
    {
        Vector3 pos = transform.position;
        Vector3 to = new Vector3(target.x, pos.y, target.z);
        Vector3 next = Vector3.MoveTowards(pos, to, speed * Time.deltaTime);

        Vector3 delta = next - pos;
        if (delta.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnLerp * Time.deltaTime);
        }

        transform.position = next;
    }

    public void SetY(float y)
    {
        Vector3 p = transform.position; p.y = y; transform.position = p;
    }

    public void SetColour(Color c)
    {
        if (rend != null) rend.material.color = c; 
    }

    public static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x; float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

}
