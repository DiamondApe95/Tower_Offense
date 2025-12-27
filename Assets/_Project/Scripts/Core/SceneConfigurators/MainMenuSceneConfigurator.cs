using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TowerConquest.Debug;

namespace TowerConquest.Core
{
    /// <summary>
    /// Automatically configures the MainMenu scene on load.
    /// Sets background image and ensures EventSystem works correctly.
    /// </summary>
    public class MainMenuSceneConfigurator : MonoBehaviour
    {
        [Header("Background Settings")]
        [Tooltip("Path to the background image in Assets/_Project/Menu/")]
        public string backgroundImagePath = "Assets/_Project/Menu/Main_Menu_Background.png";

        [Header("References")]
        public Image backgroundImage;
        public Canvas mainCanvas;

        private void Awake()
        {
            ConfigureScene();
        }

        private void Start()
        {
            // Ensure configuration on Start as well (for late-loading assets)
            ConfigureBackground();
            EnsureEventSystem();
        }

        [ContextMenu("Configure Scene")]
        public void ConfigureScene()
        {
            Log.Info("MainMenuSceneConfigurator: Configuring MainMenu scene...");

            FindReferences();
            ConfigureBackground();
            EnsureEventSystem();
            EnsureButtonsWork();

            Log.Info("MainMenuSceneConfigurator: Configuration complete.");
        }

        private void FindReferences()
        {
            // Find canvas if not assigned
            if (mainCanvas == null)
            {
                mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas == null)
                {
                    var canvasGO = GameObject.Find("MainMenuCanvas");
                    if (canvasGO != null)
                    {
                        mainCanvas = canvasGO.GetComponent<Canvas>();
                    }
                }
            }

            // Find background image if not assigned
            if (backgroundImage == null && mainCanvas != null)
            {
                var bgTransform = mainCanvas.transform.Find("Background");
                if (bgTransform != null)
                {
                    backgroundImage = bgTransform.GetComponent<Image>();
                }
            }
        }

        private void ConfigureBackground()
        {
#if UNITY_EDITOR
            if (backgroundImage == null)
            {
                Log.Warning("MainMenuSceneConfigurator: No background Image found to configure.");
                return;
            }

            // Load background sprite
            Sprite bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(backgroundImagePath);
            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.preserveAspect = true;
                Log.Info($"MainMenuSceneConfigurator: Background image set from {backgroundImagePath}");
            }
            else
            {
                Log.Warning($"MainMenuSceneConfigurator: Could not load background from {backgroundImagePath}");
            }
#endif
        }

        private void EnsureEventSystem()
        {
            // Check for EventSystem
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();

            if (eventSystem == null)
            {
                // Create EventSystem
                var eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
                Log.Info("MainMenuSceneConfigurator: Created EventSystem with InputSystemUIInputModule.");
            }
            else
            {
                // Ensure correct input module
                var standaloneModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    DestroyImmediate(standaloneModule);
                    Log.Info("MainMenuSceneConfigurator: Removed obsolete StandaloneInputModule.");
                }

                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    Log.Info("MainMenuSceneConfigurator: Added InputSystemUIInputModule.");
                }
            }
        }

        private void EnsureButtonsWork()
        {
            // Ensure all buttons have proper navigation and are interactable
            var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (!button.interactable)
                {
                    button.interactable = true;
                }

                // Ensure button has a graphic for raycasting
                if (button.targetGraphic == null)
                {
                    var image = button.GetComponent<Image>();
                    if (image != null)
                    {
                        button.targetGraphic = image;
                    }
                }
            }

            Log.Info($"MainMenuSceneConfigurator: Verified {buttons.Length} buttons.");
        }
    }
}
