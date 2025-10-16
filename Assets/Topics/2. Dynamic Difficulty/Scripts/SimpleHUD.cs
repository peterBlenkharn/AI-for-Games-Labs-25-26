using UnityEngine;

// We wrap everything inside a namespace to keep everything clean, and avoid any issues with clashing names across modules.
namespace DDA.Core
{
    /// <summary>
    /// Ultra-light HUD using IMGUI so no UI setup is required.
    /// Shows Accuracy, Difficulty and Spawn Interval in the top-left corner.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DDA Demo/Gameplay/Simple HUD")]
    public class SimpleHUD : MonoBehaviour
    {
        [Header("Refs")]
        public PerformanceTracker performance;
        public DifficultyManager difficulty;

        [Header("Style")]
        [Tooltip("Scale up for classroom projection readability.")]
        [Range(1f, 2.5f)] public float guiScale = 1.25f;

        private void OnGUI()
        {
            if (performance == null || difficulty == null) return;

            // Basic scaling for readability.
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * guiScale);

            GUILayout.BeginArea(new Rect(12, 12, 400, 200), GUI.skin.box);
            GUILayout.Label("<b>DDA Demo HUD</b>");
            GUILayout.Label($"Accuracy: {performance.Accuracy:P0}");
            GUILayout.Label($"Difficulty: {difficulty.Difficulty:0.00}");
            GUILayout.Label($"Spawn Interval: {difficulty.CurrentSpawnInterval:0.00}s");
            GUILayout.EndArea();

            GUI.matrix = old;
        }

        // EXTENSION IDEAS
        // • Add any new metrics you compute (TTK, damage/min, streaks).
        // • Replace with UI Toolkit or TextMeshPro later if desired.
    }
}
