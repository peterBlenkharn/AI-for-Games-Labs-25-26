using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    /// <summary>
    /// Minimal character controller + shooting. 
    /// Uses CharacterController for simple, dependable movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/Gameplay/Player Controller")]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Meters per second.")]
        public float moveSpeed = 6f;

        [Tooltip("Mouse horizontal sensitivity for turning the player.")]
        public float mouseSensitivity = 120f;

        [Header("Shooting")]
        [Tooltip("Projectile prefab with Projectile.cs + kinematic Rigidbody + trigger collider.")]
        public GameObject projectilePrefab;

        [Tooltip("Where the projectile spawns from (child transform at barrel/camera).")]
        public Transform muzzle;

        [Tooltip("Meters per second the projectile travels.")]
        public float projectileSpeed = 30f;

        [Header("References")]
        [Tooltip("Assigned by you in the scene (usually via GameManager reference).")]
        public PerformanceTracker performance;

        private CharacterController _cc;
        private Camera _cam; // optional if you parent the camera to the player

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cam = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            Look();
            Move();
            Shoot();
        }

        private void Look()
        {
            // Horizontal look (yaw) on player body (no vertical pitch to keep MVP simple)
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            transform.Rotate(0f, mouseX, 0f);
        }

        private void Move()
        {
            // WASD in local space (Y is gravityless for MVP)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 input = new Vector3(h, 0f, v).normalized;
            Vector3 world = transform.TransformDirection(input) * moveSpeed;
            _cc.SimpleMove(world); // applies deltaTime and basic ground stick internally
        }

        private void Shoot()
        {
            // Left mouse to fire; ensure prefab & muzzle exist.
            if (Input.GetMouseButtonDown(0) && projectilePrefab && muzzle)
            {
                var go = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
                var proj = go.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Init(performance, projectileSpeed, muzzle.forward);
                }
                performance?.RegisterShotFired();
            }
        }

        // EXTENSION IDEAS
        // • Add jump + gravity, or mouse pitch (clamped), or sprint modifier.
        // • Add simple crosshair and recoil to influence accuracy.
    }
}
