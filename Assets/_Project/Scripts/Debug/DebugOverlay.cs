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

            // Check for Live Battle Level Controller
            LiveBattleLevelController level = Object.FindFirstObjectByType<LiveBattleLevelController>();
            if (level == null)
            {
                GUI.Box(new Rect(10, 10, 220, 80), "Debug Overlay");
                GUI.Label(new Rect(20, 35, 200, 20), "No LiveBattleLevelController found.");
                if (GUI.Button(new Rect(20, 55, 200, 20), "Reload JSON (F5)"))
                {
                    ReloadJson();
                }
                return;
            }

            // Display Live Battle info
            float playerBaseHp = level.GetPlayerBaseHPPercent() * 100f;
            float enemyBaseHp = level.GetEnemyBaseHPPercent() * 100f;
            int playerGold = level.PlayerGold?.CurrentGold ?? 0;
            string battleTime = level.GetFormattedBattleTime();
            string status = level.IsBattleActive ? "ACTIVE" : (level.IsBattleEnded ? "ENDED" : "WAITING");

            GUI.Box(new Rect(10, 10, 280, 180), "Debug Overlay - Live Battle");
            GUI.Label(new Rect(20, 35, 260, 20), $"Status: {status}");
            GUI.Label(new Rect(20, 55, 260, 20), $"Battle Time: {battleTime}");
            GUI.Label(new Rect(20, 75, 260, 20), $"Player Base: {playerBaseHp:0.#}%");
            GUI.Label(new Rect(20, 95, 260, 20), $"Enemy Base: {enemyBaseHp:0.#}%");
            GUI.Label(new Rect(20, 115, 260, 20), $"Player Gold: {playerGold}");

            if (GUI.Button(new Rect(20, 140, 200, 20), "Reload JSON (F5)"))
            {
                ReloadJson();
            }
        }

        private void ReloadJson()
        {
            if (!Application.isPlaying)
            {
                UnityEngine.Debug.LogWarning("DebugOverlay: JSON reload only available in Play Mode.");
                return;
            }

            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            database.LoadAll();
            UnityEngine.Debug.Log("DebugOverlay: JSON data reloaded.");
        }
    }
}
