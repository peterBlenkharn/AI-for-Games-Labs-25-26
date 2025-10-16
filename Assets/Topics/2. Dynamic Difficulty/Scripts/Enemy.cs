using UnityEngine;

namespace DDA.Core
{
    /// <summary>
    /// Minimal target dummy.
    /// Extended: slowly moves toward the player at a fixed speed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/Gameplay/Enemy")]
    public class Enemy : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("How fast the enemy moves toward the player (m/s).")]
        public float moveSpeed = 1.5f;

        private Transform _player;

        private void Start()
        {
            // Find the player once at spawn (assumes Player tagged as "Player")
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) _player = playerObj.transform;
        }

        private void Update()
        {
            if (_player == null) return;

            // Look at player on the horizontal plane (ignore y difference for MVP).
            Vector3 target = new Vector3(_player.position.x, transform.position.y, _player.position.z);
            transform.LookAt(target);

            // Step forward.
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        }

        public void TakeHit()
        {
            Destroy(gameObject);
        }

        // EXTENSION POINTS (for students)
        // • Scale moveSpeed by difficulty (via DifficultyManager).
        // • Add attack behaviour if they reach the player.
        // • Add simple avoidance so they don’t overlap too much.

    }
}
