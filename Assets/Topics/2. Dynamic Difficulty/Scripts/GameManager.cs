using UnityEngine;


// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    // The [DisallowMultipleComponent] tag prevents there from being more than one of these! Useful for things like a game manager

    // The [AddComponentMenu] tag adds this component (as any Monobehaviour is a component in Unity) to the menu in inspector where you add a component, with subfolders if specified

    /// <summary>
    /// Scene-level wiring for demo. Holds references and initializes managers.
    /// Keep this tiny so students can "see the graph" at a glance.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/Core/Game Manager")]
    public class GameManager : MonoBehaviour
    {
        [Header("Config (ScriptableObject)")]
        [Tooltip("All tweakable values for the demo live here.")]
        public DDAConfig config;

        [Header("Managers")]
        public PerformanceTracker performance;
        public DifficultyManager difficulty;
        public Spawner spawner;

        private void Awake()
        {
            // Basic null checks (useful for beginners).
            if (!config) Debug.LogError("GameManager: Missing DDAConfig.");
            if (!performance) Debug.LogError("GameManager: Missing PerformanceTracker.");
            if (!difficulty) Debug.LogError("GameManager: Missing DifficultyManager.");
            if (!spawner) Debug.LogError("GameManager: Missing Spawner.");

            // Init order: performance > difficulty > spawner.
            performance.Init();                                             // MVP: lifetime accuracy only.
            difficulty.Init(config, performance);                           // Maps accuracy > difficulty > spawn interval.
            spawner.Init(difficulty);                                       // Pulls spawn interval from DifficultyManager.
        }
    }
}
