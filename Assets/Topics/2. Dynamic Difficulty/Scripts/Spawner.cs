using System.Collections.Generic;
using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    /// <summary>
    /// Spawns enemies at random spawn points. Polls DifficultyManager for interval.
    /// Keep logic linear & readable for teaching.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/Gameplay/Spawner")]
    public class Spawner : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Enemy prefab to spawn (simple capsule + Enemy.cs).")]
        public GameObject enemyPrefab;

        [Tooltip("Transforms where enemies can appear (drag in scene).")]
        public List<Transform> spawnPoints = new List<Transform>();

        private DifficultyManager _difficulty;
        private float _timer;

        /// <summary>Called by GameManager during Awake.</summary>
        public void Init(DifficultyManager difficulty)
        {
            _difficulty = difficulty;
            _timer = 0f;
        }

        private void Update()
        {
            if (_difficulty == null || enemyPrefab == null || spawnPoints.Count == 0)
                return;

            _timer += Time.deltaTime;
            float interval = Mathf.Max(0.01f, _difficulty.CurrentSpawnInterval);

            if (_timer >= interval)
            {
                _timer = 0f;
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            Transform p = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Instantiate(enemyPrefab, p.position, p.rotation);
        }

        // EXTENSION IDEAS
        // • Respect a "max simultaneous enemies" derived from DifficultyManager.
        // • Bias spawn point choice based on player position or distance.
        // • Spawn different enemy types based on difficulty thresholds.
    }
}
