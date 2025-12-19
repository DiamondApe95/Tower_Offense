using TowerConquest.Core;
using TowerConquest.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerConquest.UI
{
    public class WorldMapUI : MonoBehaviour
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        private void Start()
        {
            SaveManager saveManager = ServiceLocator.Get<SaveManager>();
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            UnityEngine.Debug.Log($"Unlocked levels: {string.Join(", ", progress.unlockedLevelIds)}");
            UnityEngine.Debug.Log($"Completed levels: {string.Join(", ", progress.completedLevelIds)}");
        }

        public void DebugSelectLevel(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                UnityEngine.Debug.LogWarning("DebugSelectLevel called with empty level id.");
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
            UnityEngine.Debug.Log($"Debug selected level: {levelId}");

            LoadLevelGameplay();
        }

        private void LoadLevelGameplay()
        {
            if (Application.CanStreamedLevelBeLoaded("LevelGameplay"))
            {
                SceneManager.LoadScene("LevelGameplay");
            }
            else
            {
                UnityEngine.Debug.LogError("LevelGameplay scene not found in build settings!");
            }
        }

        public void Open()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
