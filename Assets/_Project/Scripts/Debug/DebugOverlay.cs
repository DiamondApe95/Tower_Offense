using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay;
using TowerConquest.Gameplay.Entities;
using UnityEngine;

namespace TowerConquest.Debug
{
    public class DebugOverlay : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F1;
        public KeyCode reloadKey = KeyCode.F5;
        public bool isVisible;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }

            if (Input.GetKeyDown(reloadKey))
            {
                ReloadJson();
            }
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            LevelController level = Object.FindFirstObjectByType<LevelController>();
            if (level == null || level.Run == null)
            {
                GUI.Box(new Rect(10, 10, 220, 80), "Debug Overlay");
                GUI.Label(new Rect(20, 35, 200, 20), "No LevelController found.");
                return;
            }

            int wave = level.Run.waveIndex;
            float baseHp = level.BaseHp;
            int units = level.ActiveUnits;
            float dps = level.EstimatedDps;

            GUI.Box(new Rect(10, 10, 240, 140), "Debug Overlay");
            GUI.Label(new Rect(20, 35, 220, 20), $"Wave: {wave}/{level.Run.maxWaves}");
            GUI.Label(new Rect(20, 55, 220, 20), $"Base HP: {baseHp:0.#}");
            GUI.Label(new Rect(20, 75, 220, 20), $"Active Units: {units}");
            GUI.Label(new Rect(20, 95, 220, 20), $"Estimated DPS: {dps:0.#}");

            if (GUI.Button(new Rect(20, 115, 200, 20), "Reload JSON (F5)"))
            {
                ReloadJson();
            }
        }

        private void ReloadJson()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("DebugOverlay: JSON reload only available in Play Mode.");
                return;
            }

            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            database.LoadAll();
            Debug.Log("DebugOverlay: JSON data reloaded.");
        }
    }
}
