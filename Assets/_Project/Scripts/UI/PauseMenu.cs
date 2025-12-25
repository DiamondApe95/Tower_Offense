using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TowerConquest.Core;

namespace TowerConquest.UI
{
    /// <summary>
    /// PauseMenu: In-Game Pause-Menü mit Resume, Settings und Quit-Optionen.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject pausePanel;
        public GameObject settingsPanel;

        [Header("Pause Buttons")]
        public Button resumeButton;
        public Button settingsButton;
        public Button restartButton;
        public Button mainMenuButton;
        public Button quitButton;

        [Header("Settings Controls")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Button backFromSettingsButton;

        [Header("Input")]
        public Key pauseKey = Key.Escape;

        [Header("Audio")]
        public AudioClip buttonClickSfx;

        private bool isPaused;
        private float previousTimeScale;
        private GameSettings settings;

        private void Start()
        {
            settings = GameSettings.Load();
            SetupButtons();
            UpdateSettingsUI();

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            if (backFromSettingsButton != null)
                backFromSettingsButton.onClick.AddListener(CloseSettings);

            // Settings sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (isPaused) return;

            PlayButtonSound();
            isPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Trigger event
            if (ServiceLocator.TryGet(out EventBus eventBus))
            {
                eventBus.Publish(new GamePausedEvent(true));
            }

            Debug.Log("PauseMenu: Game paused.");
        }

        public void Resume()
        {
            if (!isPaused) return;

            PlayButtonSound();
            isPaused = false;
            Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            // Trigger event
            if (ServiceLocator.TryGet(out EventBus eventBus))
            {
                eventBus.Publish(new GamePausedEvent(false));
            }

            Debug.Log("PauseMenu: Game resumed.");
        }

        public void OpenSettings()
        {
            PlayButtonSound();

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            UpdateSettingsUI();
        }

        public void CloseSettings()
        {
            PlayButtonSound();
            SaveSettings();

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        public void Restart()
        {
            PlayButtonSound();
            Resume();

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.ReloadCurrentScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        public void ReturnToMainMenu()
        {
            PlayButtonSound();
            Resume();

            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.LoadMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }

        public void QuitGame()
        {
            PlayButtonSound();
            SaveSettings();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void UpdateSettingsUI()
        {
            if (settings == null) return;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = settings.masterVolume;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = settings.musicVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = settings.sfxVolume;
        }

        private void SaveSettings()
        {
            if (settings != null)
            {
                settings.Save();
                settings.Apply();
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (settings != null)
            {
                settings.masterVolume = value;
                AudioListener.volume = value;
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (settings != null)
            {
                settings.musicVolume = value;
            }
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (settings != null)
            {
                settings.sfxVolume = value;
            }
        }

        private void PlayButtonSound()
        {
            if (buttonClickSfx != null && settings != null)
            {
                AudioSource.PlayClipAtPoint(buttonClickSfx, Vector3.zero, settings.sfxVolume);
            }
        }

        public bool IsPaused => isPaused;
    }
}
