using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    // The [AddComponentMenu] tag adds this component (as any Monobehaviour is a component in Unity) to the menu in inspector where you add a component, with subfolders if specified

    /// <summary>
    /// Converts current performance into difficulty and derived outputs.
    /// MVP: only spawn interval is derived (min..max).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/DDA/Difficulty Manager")]
    public class DifficultyManager : MonoBehaviour
    {
        // By convention, private member variables are named using camelCase and with a leading _ for example: _myVariableName to make it easier to spot private fields

        private DDAConfig _config;
        private PerformanceTracker _performance;

        // Cached values for HUD / polling.
        private float _difficulty01;   // 0..1
        private float _currentSpawnInterval; // seconds

        /// <summary>Difficulty scalar is in range [0..1] based on current performance.</summary>
        public float Difficulty => _difficulty01;

        /// <summary>Spawn interval in seconds derived from Difficulty.</summary>
        public float CurrentSpawnInterval => _currentSpawnInterval; // This is functionally a public getter function for the private _curentSpawnInterval field

        /// <summary>Setup from GameManager.</summary>
        public void Init(DDAConfig config, PerformanceTracker performance)
        {
            _config = config;   
            _performance = performance;
            Recompute(); // compute initial values so systems can poll immediately.
        }

        private void Update()
        {
            // In MVP we recompute every frame.
            Recompute();
        }

        private void Recompute()
        {
            // 1) Measure performance (MVP: accuracy)
            float accuracy = Mathf.Clamp01(_performance.Accuracy);

            // 2) Map performance > difficulty (curve or direct)
            float rawDifficulty = _config.useCurve
                ? Mathf.Clamp01(_config.performanceToDifficulty.Evaluate(accuracy))
                : accuracy;

            // 3) Optional smoothing (off in MVP)
            if (_config.difficultySmoothing > 0f)
            {
                _difficulty01 = Mathf.Lerp(_difficulty01, rawDifficulty, 1f - _config.difficultySmoothing);
            }
            else
            {
                _difficulty01 = rawDifficulty;
            }

            // 4) Derive outputs. Here: spawn interval (inverse relationship).
            // When difficulty=0 -> interval = minSpawnInterval (slower spawns)
            // When difficulty=1 -> interval = maxSpawnInterval (faster spawns)
            _currentSpawnInterval = Mathf.Lerp(_config.minSpawnInterval, _config.maxSpawnInterval, _difficulty01);
        }

        // EXTENSION IDEAS
        // • Blend multiple metrics (e.g., weighted sum, max, piecewise).
        // • Add hysteresis (prevent oscillation) or floor/ceiling rates per minute.
        // • Add more derived outputs: enemy speed, max simultaneous, health, spawn distance bias…
    }
}
