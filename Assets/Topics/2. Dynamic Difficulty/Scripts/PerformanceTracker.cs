using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    // The [AddComponentMenu] tag adds this component (as any Monobehaviour is a component in Unity) to the menu in inspector where you add a component, with subfolders if specified

    /// <summary>
    /// Tracks player's performance. MVP = lifetime accuracy (hits / shots).
    /// Students can extend to rolling windows, time-to-kill, damage taken, etc.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/DDA/Performance Tracker")]
    public class PerformanceTracker : MonoBehaviour
    {
        [Header("Runtime (read-only)")]
        [SerializeField, Tooltip("Total shots the player fired (lifetime).")]
        private int shotsFired;

        [SerializeField, Tooltip("Total shots that hit an enemy (lifetime).")]
        private int shotsHit;

        /// <summary>Accuracy is a value between [0..1]. If no shots fired yet, returns 0 (gentle start).</summary>
        public float Accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f; // This syntax declares the float Accuracy directly as being equal to shots hit/fired, or if none fired, 0

        /// <summary>Call once at scene start by GameManager.</summary>
        public void Init()
        {
            shotsFired = 0;
            shotsHit = 0;
        }

        /// <summary>Called by PlayerController when a projectile is spawned.</summary>
        public void RegisterShotFired()
        {
            shotsFired++;
        }

        /// <summary>Called by Projectile when it hits an enemy.</summary>
        public void RegisterShotHit()
        {
            shotsHit++;
        }

        // 
        // EXTENSION IDEAS
        //  Add rolling window metrics (e.g., last 10s accuracy using a queue of timestamps).
        //  Track damageTaken, averageTimeToKill, killStreaks, movement activity, etc.
        //  Expose them via properties for DifficultyManager to consume.
        //
    }
}
