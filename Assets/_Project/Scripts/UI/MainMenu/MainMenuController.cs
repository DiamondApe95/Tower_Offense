using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TowerConquest.Core;
using TowerConquest.Saving;

namespace TowerConquest.UI.MainMenu
{
    /// <summary>
    /// Hauptmenü-Controller: Verwaltet das Hauptmenü und seine Funktionen.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Panels")]
        public GameObject mainPanel;
        public GameObject settingsPanel;
        public GameObject levelSelectPanel;
        public GameObject creditsPanel;

        [Header("Main Menu Buttons")]
        public Button playButton;
        public Button continueButton;
        public Button settingsButton;
        public Button creditsButton;
        public Button quitButton;

        [Header("Navigation Buttons")]
        public Button backToMainButton;
        public Button backFromCreditsButton;
        public Button backFromLevelSelectButton;

        [Header("Mode Selection")]
        public Button offenseModeButton;
        public Button defenseModeButton;

        [Header("Settings Controls")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Toggle fullscreenToggle;
        public Dropdown qualityDropdown;
        public Button applySettingsButton;

        [Header("Version Display")]
        public Text versionText;

        [Header("Audio")]
        public AudioSource menuMusic;
        public AudioClip buttonClickSfx;

        private SaveManager saveManager;
        private GameSettings currentSettings;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out SaveManager sm))
            {
                saveManager = sm;
            }
            else
            {
                saveManager = new SaveManager();
            }

            LoadSettings();
            RefreshUI();
            ShowMainPanel();

            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }

        private void SetupButtonListeners()
        {
            // Main Menu
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Navigation
            if (backToMainButton != null)
                backToMainButton.onClick.AddListener(ShowMainPanel);
            if (backFromCreditsButton != null)
                backFromCreditsButton.onClick.AddListener(ShowMainPanel);
            if (backFromLevelSelectButton != null)
                backFromLevelSelectButton.onClick.AddListener(ShowMainPanel);

            // Mode Selection
            if (offenseModeButton != null)
                offenseModeButton.onClick.AddListener(() => StartGame(Gameplay.GameMode.Offense));
            if (defenseModeButton != null)
                defenseModeButton.onClick.AddListener(() => StartGame(Gameplay.GameMode.Defense));

            // Settings
            if (applySettingsButton != null)
                applySettingsButton.onClick.AddListener(ApplySettings);
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        private void RefreshUI()
        {
            // Continue Button nur anzeigen wenn Speicherstand vorhanden
            if (continueButton != null)
            {
                PlayerProgress progress = saveManager.GetOrCreateProgress();
                bool hasProgress = progress != null &&
                    (progress.completedLevelIds.Count > 0 || !string.IsNullOrEmpty(progress.lastSelectedLevelId));
                continueButton.gameObject.SetActive(hasProgress);
            }

            // Settings Werte setzen
            if (currentSettings != null)
            {
                if (masterVolumeSlider != null)
                    masterVolumeSlider.value = currentSettings.masterVolume;
                if (musicVolumeSlider != null)
                    musicVolumeSlider.value = currentSettings.musicVolume;
                if (sfxVolumeSlider != null)
                    sfxVolumeSlider.value = currentSettings.sfxVolume;
                if (fullscreenToggle != null)
                    fullscreenToggle.isOn = currentSettings.fullscreen;
                if (qualityDropdown != null)
                    qualityDropdown.value = currentSettings.qualityLevel;
            }
        }

        private void ShowMainPanel()
        {
            PlayButtonSound();
            SetPanelActive(mainPanel, true);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(levelSelectPanel, false);
            SetPanelActive(creditsPanel, false);
        }

        private void OnPlayClicked()
        {
            PlayButtonSound();
            SetPanelActive(mainPanel, false);
            SetPanelActive(levelSelectPanel, true);
        }

        private void OnContinueClicked()
        {
            PlayButtonSound();

            PlayerProgress progress = saveManager.GetOrCreateProgress();
            if (!string.IsNullOrEmpty(progress.lastSelectedLevelId))
            {
                LoadLevel(progress.lastSelectedLevelId);
            }
            else if (progress.unlockedLevelIds.Count > 0)
            {
                LoadLevel(progress.unlockedLevelIds[0]);
            }
            else
            {
                LoadLevel("lvl_01_etruria_outpost");
            }
        }

        private void OnSettingsClicked()
        {
            PlayButtonSound();
            SetPanelActive(mainPanel, false);
            SetPanelActive(settingsPanel, true);
        }

        private void OnCreditsClicked()
        {
            PlayButtonSound();
            SetPanelActive(mainPanel, false);
            SetPanelActive(creditsPanel, true);
        }

        private void OnQuitClicked()
        {
            PlayButtonSound();
            SaveSettings();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StartGame(Gameplay.GameMode mode)
        {
            PlayButtonSound();

            // Speichere gewählten Modus
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            progress.selectedGameMode = mode == Gameplay.GameMode.Offense ? "offense" : "defense";
            saveManager.SaveProgress(progress);

            // Lade erstes verfügbares Level
            string levelId = "lvl_01_etruria_outpost";
            if (progress.unlockedLevelIds.Count > 0)
            {
                levelId = progress.unlockedLevelIds[0];
            }

            LoadLevel(levelId);
        }

        private void LoadLevel(string levelId)
        {
            PlayerProgress progress = saveManager.GetOrCreateProgress();
            progress.lastSelectedLevelId = levelId;
            saveManager.SaveProgress(progress);

            if (Application.CanStreamedLevelBeLoaded("LevelGameplay"))
            {
                SceneManager.LoadScene("LevelGameplay");
            }
            else if (Application.CanStreamedLevelBeLoaded("WorldMap"))
            {
                SceneManager.LoadScene("WorldMap");
            }
            else
            {
                Debug.LogWarning("MainMenuController: No gameplay scene found to load.");
            }
        }

        private void LoadSettings()
        {
            currentSettings = GameSettings.Load();
            ApplySettingsToGame();
        }

        private void SaveSettings()
        {
            if (currentSettings != null)
            {
                currentSettings.Save();
            }
        }

        private void ApplySettings()
        {
            PlayButtonSound();
            ApplySettingsToGame();
            SaveSettings();
            ShowMainPanel();
        }

        private void ApplySettingsToGame()
        {
            if (currentSettings == null) return;

            // Audio
            AudioListener.volume = currentSettings.masterVolume;
            if (menuMusic != null)
            {
                menuMusic.volume = currentSettings.musicVolume;
            }

            // Grafik
            Screen.fullScreen = currentSettings.fullscreen;
            QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (currentSettings != null)
            {
                currentSettings.masterVolume = value;
                AudioListener.volume = value;
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (currentSettings != null)
            {
                currentSettings.musicVolume = value;
                if (menuMusic != null)
                {
                    menuMusic.volume = value;
                }
            }
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (currentSettings != null)
            {
                currentSettings.sfxVolume = value;
            }
        }

        private void OnFullscreenToggled(bool isOn)
        {
            if (currentSettings != null)
            {
                currentSettings.fullscreen = isOn;
            }
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private void PlayButtonSound()
        {
            if (buttonClickSfx != null && currentSettings != null)
            {
                AudioSource.PlayClipAtPoint(buttonClickSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero, currentSettings.sfxVolume);
            }
        }

        public void SelectLevel(string levelId)
        {
            PlayButtonSound();
            LoadLevel(levelId);
        }
    }
}
