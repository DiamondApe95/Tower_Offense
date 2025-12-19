using TowerOffense.Core;
using TowerOffense.Saving;
using UnityEngine;

namespace TowerOffense.UI
{
    public class WorldMapUI : MonoBehaviour
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        private void Start()
        {
            SaveManager saveManager = ServiceLocator.Get<SaveManager>();
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            Debug.Log($"Unlocked levels: {string.Join(", ", progress.unlockedLevelIds)}");
            Debug.Log($"Completed levels: {string.Join(", ", progress.completedLevelIds)}");
        }

        public void DebugSelectLevel(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                Debug.LogWarning("DebugSelectLevel called with empty level id.");
                return;
            }

            SaveManager saveManager = ServiceLocator.Get<SaveManager>();
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            progress.lastSelectedLevelId = levelId;
            if (!progress.unlockedLevelIds.Contains(levelId))
            {
                progress.unlockedLevelIds.Add(levelId);
            }

            saveManager.SaveProgress(progress);
            Debug.Log($"Debug selected level: {levelId}");
        }

        public void Open()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
