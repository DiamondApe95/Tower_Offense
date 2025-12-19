using System;
using System.IO;
using UnityEngine;

namespace TowerOffense.Saving
{
    public class SaveManager
    {
        private const string ProgressFileName = "player_progress.json";
        private readonly JsonSaveSerializer serializer = new JsonSaveSerializer();

        public PlayerProgress LoadProgress()
        {
            string path = GetProgressPath();

            if (!File.Exists(path))
            {
                Debug.Log($"No progress file found at {path}.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                PlayerProgress progress = serializer.FromJson<PlayerProgress>(json);
                Debug.Log($"Loaded progress from {path}.");
                return progress;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load progress from {path}: {ex}");
                return null;
            }
        }

        public void SaveProgress(PlayerProgress progress)
        {
            string path = GetProgressPath();

            try
            {
                string json = serializer.ToJson(progress);
                File.WriteAllText(path, json);
                Debug.Log($"Saved progress to {path}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save progress to {path}: {ex}");
            }
        }

        public string GetProgressPath()
        {
            return Path.Combine(Application.persistentDataPath, ProgressFileName);
        }

        public void DebugWriteAndRead()
        {
            var progress = new PlayerProgress
            {
                lastSelectedLevelId = "level_debug"
            };
            progress.unlockedLevelIds.Add("level_1");
            progress.unlockedLevelIds.Add("level_2");
            progress.completedLevelIds.Add("level_1");

            Debug.Log("Debug save/load: writing progress.");
            SaveProgress(progress);

            Debug.Log("Debug save/load: reading progress.");
            PlayerProgress loaded = LoadProgress();

            if (loaded == null)
            {
                Debug.LogWarning("Debug save/load: no progress loaded.");
                return;
            }

            Debug.Log($"Debug save/load: lastSelectedLevelId={loaded.lastSelectedLevelId}, unlocked={loaded.unlockedLevelIds.Count}, completed={loaded.completedLevelIds.Count}");
        }
    }
}
