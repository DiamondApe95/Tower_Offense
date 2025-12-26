using TowerConquest.Core;
using TowerConquest.Debug;
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
            Log.Info($"Unlocked levels: {string.Join(", ", progress.unlockedLevelIds)}");
            Log.Info($"Completed levels: {string.Join(", ", progress.completedLevelIds)}");
        }

        public void DebugSelectLevel(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                Log.Warning("DebugSelectLevel called with empty level id.");
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
            Log.Info($"Debug selected level: {levelId}");

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
                Log.Error("LevelGameplay scene not found in build settings!");
            }
        }

        public void Open()
        {
            Log.Info("Stub method called.");
        }

    }
}
