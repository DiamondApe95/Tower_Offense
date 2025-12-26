using System;
using TowerConquest.Debug;
using System.IO;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Saving
{
    public class SaveManager
    {
        private const string ProgressFileName = "progress.json";
        private const string RunFileName = "run.json";
        private readonly JsonSaveSerializer serializer = new JsonSaveSerializer();

        public PlayerProgress LoadProgress()
        {
            string path = GetProgressPath();

            if (!File.Exists(path))
            {
                Log.Info($"No progress file found at {path}.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                PlayerProgress progress = serializer.FromJson<PlayerProgress>(json);
                Log.Info($"Loaded progress from {path}.");
                return progress;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load progress from {path}: {ex}");
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
                Log.Info($"Saved progress to {path}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save progress to {path}: {ex}");
            }
        }

        public PlayerProgress GetOrCreateProgress()
        {
            PlayerProgress progress = LoadProgress();
            if (progress != null)
            {
                return progress;
            }

            progress = new PlayerProgress();

            if (ServiceLocator.TryGet(out JsonDatabase database) && database.Levels != null && database.Levels.Count > 0)
            {
                string firstLevelId = database.Levels[0]?.id;
                if (!string.IsNullOrWhiteSpace(firstLevelId))
                {
                    progress.unlockedLevelIds.Add(firstLevelId);
                    progress.lastSelectedLevelId = firstLevelId;
                }
            }

            SaveProgress(progress);
            return progress;
        }

        public void SaveRun(RunSnapshot snapshot)
        {
            string path = GetRunPath();

            try
            {
                string json = serializer.ToJson(snapshot);
                File.WriteAllText(path, json);
                Log.Info($"Saved run snapshot to {path}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save run snapshot to {path}: {ex}");
            }
        }

        public RunSnapshot LoadRun()
        {
            string path = GetRunPath();

            if (!File.Exists(path))
            {
                Log.Info($"No run snapshot file found at {path}.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                RunSnapshot snapshot = serializer.FromJson<RunSnapshot>(json);
                Log.Info($"Loaded run snapshot from {path}.");
                return snapshot;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load run snapshot from {path}: {ex}");
                return null;
            }
        }

        public string GetProgressPath()
        {
            return Path.Combine(Application.persistentDataPath, ProgressFileName);
        }

        private string GetRunPath()
        {
            return Path.Combine(Application.persistentDataPath, RunFileName);
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

            Log.Info("Debug save/load: writing progress.");
            SaveProgress(progress);

            Log.Info("Debug save/load: reading progress.");
            PlayerProgress loaded = LoadProgress();

            if (loaded == null)
            {
                Log.Warning("Debug save/load: no progress loaded.");
                return;
            }

            Log.Info($"Debug save/load: lastSelectedLevelId={loaded.lastSelectedLevelId}, unlocked={loaded.unlockedLevelIds.Count}, completed={loaded.completedLevelIds.Count}");
        }
    }
}
