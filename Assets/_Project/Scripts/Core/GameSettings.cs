using System;
using System.IO;
using UnityEngine;

namespace TowerConquest.Core
{
    /// <summary>
    /// Spieleinstellungen: Speichert Audio-, Grafik- und Gameplay-Einstellungen.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        private const string SETTINGS_FILE = "game_settings.json";

        [Header("Audio Settings")]
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;

        [Header("Graphics Settings")]
        public bool fullscreen = true;
        public int qualityLevel = 2;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public bool vsync = true;
        public int targetFrameRate = 60;

        [Header("Gameplay Settings")]
        public float gameSpeed = 1f;
        public bool showDamageNumbers = true;
        public bool showHealthBars = true;
        public bool autoStartWaves = false;
        public float autoStartDelay = 3f;
        public bool skipTutorials = false;

        [Header("Accessibility")]
        public bool colorBlindMode = false;
        public float uiScale = 1f;
        public bool screenShake = true;
        public bool showTooltips = true;

        [Header("Controls")]
        public float cameraSpeed = 5f;
        public float zoomSpeed = 2f;
        public bool invertZoom = false;

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SETTINGS_FILE);
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(GetSavePath(), json);
                Debug.Log($"GameSettings: Saved settings to {GetSavePath()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameSettings: Failed to save settings: {ex.Message}");
            }
        }

        public static GameSettings Load()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                Debug.Log("GameSettings: No settings file found, using defaults.");
                return new GameSettings();
            }

            try
            {
                string json = File.ReadAllText(path);
                GameSettings settings = JsonUtility.FromJson<GameSettings>(json);
                Debug.Log("GameSettings: Loaded settings from file.");
                return settings ?? new GameSettings();
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameSettings: Failed to load settings: {ex.Message}");
                return new GameSettings();
            }
        }

        public void Apply()
        {
            // Audio
            AudioListener.volume = masterVolume;

            // Grafik
            if (vsync)
            {
                QualitySettings.vSyncCount = 1;
            }
            else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = targetFrameRate;
            }

            QualitySettings.SetQualityLevel(qualityLevel);

            if (Screen.fullScreen != fullscreen ||
                Screen.width != resolutionWidth ||
                Screen.height != resolutionHeight)
            {
                Screen.SetResolution(resolutionWidth, resolutionHeight, fullscreen);
            }

            // Gameplay
            Time.timeScale = gameSpeed;

            Debug.Log("GameSettings: Applied settings to game.");
        }

        public void Reset()
        {
            masterVolume = 1f;
            musicVolume = 0.7f;
            sfxVolume = 1f;
            fullscreen = true;
            qualityLevel = 2;
            resolutionWidth = 1920;
            resolutionHeight = 1080;
            vsync = true;
            targetFrameRate = 60;
            gameSpeed = 1f;
            showDamageNumbers = true;
            showHealthBars = true;
            autoStartWaves = false;
            autoStartDelay = 3f;
            skipTutorials = false;
            colorBlindMode = false;
            uiScale = 1f;
            screenShake = true;
            showTooltips = true;
            cameraSpeed = 5f;
            zoomSpeed = 2f;
            invertZoom = false;

            Debug.Log("GameSettings: Reset to defaults.");
        }
    }
}
