using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    /// <summary>
    /// Central tuning surface for the lab. Everything you can tweak lives here!
    /// Created via Assets > Create > DDA > DDA Config.
    /// </summary>
    [CreateAssetMenu(menuName = "DDA/DDA Config", fileName = "DDAConfig")] // This type of tag creates a shortcut to create this kind of asset within the editor
    public class DDAConfig : ScriptableObject
    {
        // [HEADER TAGS] Header tags add a little section title in the inspector to organise serialized fields
        // [TOOLTIP TAGS] Tooltip tags add a little tooltip to the field when you hover over it in the inspector, helpful!
        // [MIN, RANGE etc TAGS] You can add tags to floats and other scalable types to add sliders and hard limits on the value in inspector for convenience


        [Header("Mapping: Performance (Accuracy 0..1) > Difficulty 0..1")]
        [Tooltip("If true, evaluate accuracy through this curve. If false, use accuracy directly.")]
        public bool useCurve = false;

        [Tooltip("X: Accuracy (0..1). Y: Difficulty (0..1). Default: linear.")]
        public AnimationCurve performanceToDifficulty = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Spawner Tuning")]
        [Tooltip("When difficulty = 0 (struggling), interval is longest (slow spawns).")]
        [Min(0.01f)] public float minSpawnInterval = 2.0f;

        [Tooltip("When difficulty = 1 (performing well), interval is shortest (fast spawns).")]
        [Min(0.01f)] public float maxSpawnInterval = 0.5f;

        [Header("Optional Smoothing (MVP off)")]
        [Tooltip("0 = no smoothing, 0.9 = heavy smoothing. Not used in MVP; feel free to enable in DifficultyManager.")]
        [Range(0f, 0.99f)] public float difficultySmoothing = 0f;

        /* 
         EXTENSION IDEAS
         Add fields for other outputs (enemy speed, simultaneous count caps, health bands, etc.)
         Add weights for combining multiple performance metrics (TTK, damage taken, etc.)
        */
    }
}
