using System.Collections;
using System.Collections.Generic;
using TowerConquest.Debug;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TowerConquest.Core;
using TowerConquest.Saving;
using TowerConquest.Data;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TowerConquest.UI.MainMenu
{
    /// <summary>
    /// AutoMenuGenerator: Generiert ein komplettes Hauptmenü mit einem Klick.
    /// Erstellt Canvas, Panels, Buttons, Settings und alle notwendigen Komponenten.
    /// </summary>
    public class AutoMenuGenerator : MonoBehaviour
    {
        [Header("=== ONE-CLICK MENU GENERATION ===")]
        [Tooltip("Generiert das komplette Hauptmenü beim Start")]
        public bool autoGenerateOnStart = false;

        [Header("Menu Style")]
        public Color primaryColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        public Color secondaryColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        public Color accentColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        public Color textColor = Color.white;
        public Color buttonHoverColor = new Color(0.3f, 0.5f, 0.7f, 1f);

        [Header("Title Settings")]
        public string gameTitle = "TOWER CONQUEST";
        public string gameSubtitle = "Offense & Defense";
        public int titleFontSize = 72;
        public int subtitleFontSize = 24;

        [Header("Background")]
        [Tooltip("Background texture for RawImage (supports any image format)")]
        public Texture backgroundTexture;
        public Color backgroundColor = new Color(0.05f, 0.08f, 0.12f, 1f);

        [Header("Generated References")]
        public Canvas generatedCanvas;
        public MainMenuController menuController;

        // Private references
        private GameObject mainPanel;
        private GameObject settingsPanel;
        private GameObject levelSelectPanel;
        private GameObject creditsPanel;

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                LoadBackgroundTexture();
                GenerateCompleteMenu();

                // Ensure button listeners are set up after menu regeneration
                StartCoroutine(SetupButtonListenersDelayed());
            }
        }

        private IEnumerator SetupButtonListenersDelayed()
        {
            // Wait one frame to ensure all components are initialized
            yield return null;

            if (menuController != null)
            {
                menuController.SetupButtonListeners();
                Log.Info("AutoMenuGenerator: Button listeners refreshed after menu generation.");
            }
        }

        private void LoadBackgroundTexture()
        {
#if UNITY_EDITOR
            if (backgroundTexture == null)
            {
                string bgPath = "Assets/_Project/Menu/Main_Menu_Background.png";
                backgroundTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(bgPath);
                if (backgroundTexture != null)
                {
                    Log.Info($"AutoMenuGenerator: Loaded background texture from {bgPath}");
                }
            }
#endif
        }

        [ContextMenu("★ Generate Complete Menu (One-Click)")]
        public void GenerateCompleteMenu()
        {
            Log.Info("AutoMenuGenerator: Starting Complete Menu Generation...");
            LoadBackgroundTexture();

            CleanupOldMenu();
            CreateCanvas();
            CreateMainPanel();
            CreateSettingsPanel();
            CreateLevelSelectPanel();
            CreateCreditsPanel();
            SetupMenuController();
            SetupCamera();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            Log.Info("AutoMenuGenerator: ★ Complete Menu Generation finished! Menu is ready.");
        }

        [ContextMenu("Cleanup Old Menu")]
        public void CleanupOldMenu()
        {
            var oldCanvas = GameObject.Find("MainMenuCanvas");
            if (oldCanvas != null)
            {
                DestroyImmediate(oldCanvas);
            }

            var oldEventSystem = GameObject.Find("MenuEventSystem");
            if (oldEventSystem != null)
            {
                DestroyImmediate(oldEventSystem);
            }

            var oldController = FindFirstObjectByType<MainMenuController>();
            if (oldController != null && oldController.gameObject != gameObject)
            {
                DestroyImmediate(oldController.gameObject);
            }

            Log.Info("AutoMenuGenerator: Cleaned up old menu objects.");
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("MainMenuCanvas");
            generatedCanvas = canvasGO.AddComponent<Canvas>();
            generatedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            generatedCanvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            var existingEventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                var eventSystemGO = new GameObject("MenuEventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            // Background using RawImage for better texture compatibility
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgRawImage = bgGO.AddComponent<RawImage>();
            if (backgroundTexture != null)
            {
                bgRawImage.texture = backgroundTexture;
                bgRawImage.color = Color.white; // Full color to show texture properly
            }
            else
            {
                bgRawImage.color = backgroundColor;
            }

            Log.Info("AutoMenuGenerator: Canvas created with RawImage background.");
        }

        private void CreateMainPanel()
        {
            mainPanel = CreatePanel("MainPanel", generatedCanvas.transform);

            // Title Container
            var titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(mainPanel.transform, false);
            var titleRect = titleContainer.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.sizeDelta = new Vector2(800, 200);

            // Game Title
            var titleText = CreateText("GameTitle", titleContainer.transform, gameTitle,
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                new Vector2(-400, -40), new Vector2(400, 40), titleFontSize);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = accentColor;

            // Subtitle
            var subtitleText = CreateText("Subtitle", titleContainer.transform, gameSubtitle,
                new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
                new Vector2(-300, -20), new Vector2(300, 20), subtitleFontSize);
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = textColor;

            // Button Container
            var buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(mainPanel.transform, false);
            var btnContRect = buttonContainer.AddComponent<RectTransform>();
            btnContRect.anchorMin = new Vector2(0.5f, 0.2f);
            btnContRect.anchorMax = new Vector2(0.5f, 0.6f);
            btnContRect.sizeDelta = new Vector2(300, 350);

            var vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Buttons
            var playBtn = CreateMenuButton("PlayButton", buttonContainer.transform, "PLAY", primaryColor);
            var continueBtn = CreateMenuButton("ContinueButton", buttonContainer.transform, "CONTINUE", primaryColor);
            var settingsBtn = CreateMenuButton("SettingsButton", buttonContainer.transform, "SETTINGS", secondaryColor);
            var creditsBtn = CreateMenuButton("CreditsButton", buttonContainer.transform, "CREDITS", secondaryColor);
            var quitBtn = CreateMenuButton("QuitButton", buttonContainer.transform, "QUIT", new Color(0.6f, 0.2f, 0.2f, 1f));

            // Store references
            if (menuController == null)
            {
                menuController = gameObject.GetComponent<MainMenuController>();
                if (menuController == null)
                {
                    menuController = gameObject.AddComponent<MainMenuController>();
                }
            }

            menuController.mainPanel = mainPanel;
            menuController.playButton = playBtn.GetComponent<Button>();
            menuController.continueButton = continueBtn.GetComponent<Button>();
            menuController.settingsButton = settingsBtn.GetComponent<Button>();
            menuController.creditsButton = creditsBtn.GetComponent<Button>();
            menuController.quitButton = quitBtn.GetComponent<Button>();

            // Version Text
            var versionText = CreateText("VersionText", mainPanel.transform, "v1.0.0",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(20, 20), new Vector2(150, 30), 14);
            versionText.alignment = TextAnchor.LowerLeft;
            versionText.color = new Color(1, 1, 1, 0.5f);
            menuController.versionText = versionText;

            Log.Info("AutoMenuGenerator: Main panel created.");
        }

        private void CreateSettingsPanel()
        {
            settingsPanel = CreatePanel("SettingsPanel", generatedCanvas.transform);
            settingsPanel.SetActive(false);

            // Title
            var titleText = CreateText("SettingsTitle", settingsPanel.transform, "SETTINGS",
                new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f),
                new Vector2(-200, -30), new Vector2(200, 30), 48);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = accentColor;

            // Settings Container
            var container = new GameObject("SettingsContainer");
            container.transform.SetParent(settingsPanel.transform, false);
            var contRect = container.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.3f, 0.2f);
            contRect.anchorMax = new Vector2(0.7f, 0.8f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;

            var bgImg = container.AddComponent<Image>();
            bgImg.color = secondaryColor;

            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Audio Section
            CreateSettingsLabel("AudioLabel", container.transform, "AUDIO");
            var masterSlider = CreateSliderSetting("MasterVolume", container.transform, "Master Volume", 1f);
            var musicSlider = CreateSliderSetting("MusicVolume", container.transform, "Music Volume", 0.7f);
            var sfxSlider = CreateSliderSetting("SFXVolume", container.transform, "SFX Volume", 1f);

            // Graphics Section
            CreateSettingsLabel("GraphicsLabel", container.transform, "GRAPHICS");
            var fullscreenToggle = CreateToggleSetting("Fullscreen", container.transform, "Fullscreen", true);
            var qualityDropdown = CreateDropdownSetting("Quality", container.transform, "Quality",
                new List<string> { "Low", "Medium", "High", "Ultra" }, 2);

            // Bottom Buttons
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(settingsPanel.transform, false);
            var rowRect = buttonRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.3f, 0.05f);
            rowRect.anchorMax = new Vector2(0.7f, 0.12f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            var hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var applyBtn = CreateMenuButton("ApplyButton", buttonRow.transform, "APPLY", primaryColor);
            var backBtn = CreateMenuButton("BackButton", buttonRow.transform, "BACK", secondaryColor);

            // Store references
            menuController.settingsPanel = settingsPanel;
            menuController.masterVolumeSlider = masterSlider;
            menuController.musicVolumeSlider = musicSlider;
            menuController.sfxVolumeSlider = sfxSlider;
            menuController.fullscreenToggle = fullscreenToggle;
            menuController.qualityDropdown = qualityDropdown;
            menuController.applySettingsButton = applyBtn.GetComponent<Button>();
            menuController.backToMainButton = backBtn.GetComponent<Button>();

            Log.Info("AutoMenuGenerator: Settings panel created.");
        }

        private void CreateLevelSelectPanel()
        {
            levelSelectPanel = CreatePanel("LevelSelectPanel", generatedCanvas.transform);
            levelSelectPanel.SetActive(false);

            // Title
            var titleText = CreateText("LevelSelectTitle", levelSelectPanel.transform, "SELECT MODE",
                new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f),
                new Vector2(-200, -30), new Vector2(200, 30), 48);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = accentColor;

            // Mode Buttons Container
            var modeContainer = new GameObject("ModeContainer");
            modeContainer.transform.SetParent(levelSelectPanel.transform, false);
            var contRect = modeContainer.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.2f, 0.35f);
            contRect.anchorMax = new Vector2(0.8f, 0.75f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;

            var hlg = modeContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Offense Mode Card
            var offenseCard = CreateModeCard("OffenseCard", modeContainer.transform,
                "OFFENSE MODE", "Lead your army to victory!\nSpawn units, cast spells,\nand destroy the enemy base.",
                new Color(0.6f, 0.3f, 0.2f, 1f));

            // Defense Mode Card
            var defenseCard = CreateModeCard("DefenseCard", modeContainer.transform,
                "DEFENSE MODE", "Build towers and defend!\nPlace defenses, upgrade towers,\nand stop the enemy waves.",
                new Color(0.2f, 0.4f, 0.6f, 1f));

            // Back Button
            var backBtn = CreateMenuButton("BackFromLevelSelect", levelSelectPanel.transform, "BACK", secondaryColor);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.1f);
            backRect.anchorMax = new Vector2(0.5f, 0.1f);
            backRect.sizeDelta = new Vector2(200, 50);
            backRect.anchoredPosition = Vector2.zero;

            // Store references
            menuController.levelSelectPanel = levelSelectPanel;
            menuController.offenseModeButton = offenseCard.GetComponentInChildren<Button>();
            menuController.defenseModeButton = defenseCard.GetComponentInChildren<Button>();
            menuController.backFromLevelSelectButton = backBtn.GetComponent<Button>();

            Log.Info("AutoMenuGenerator: Level select panel created.");
        }

        private void CreateCreditsPanel()
        {
            creditsPanel = CreatePanel("CreditsPanel", generatedCanvas.transform);
            creditsPanel.SetActive(false);

            // Title
            var titleText = CreateText("CreditsTitle", creditsPanel.transform, "CREDITS",
                new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f),
                new Vector2(-200, -30), new Vector2(200, 30), 48);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = accentColor;

            // Credits Content
            var contentContainer = new GameObject("CreditsContent");
            contentContainer.transform.SetParent(creditsPanel.transform, false);
            var contRect = contentContainer.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.2f, 0.2f);
            contRect.anchorMax = new Vector2(0.8f, 0.8f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;

            var bgImg = contentContainer.AddComponent<Image>();
            bgImg.color = secondaryColor;

            var creditsText = CreateText("CreditsText", contentContainer.transform,
                "TOWER CONQUEST\n\n" +
                "Game Design & Programming\n" +
                "Development Team\n\n" +
                "Art & Visual Design\n" +
                "Art Team\n\n" +
                "Sound & Music\n" +
                "Audio Team\n\n" +
                "Special Thanks\n" +
                "To all our supporters!\n\n" +
                "Built with Unity",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-250, -200), new Vector2(250, 200), 20);
            creditsText.alignment = TextAnchor.MiddleCenter;

            // Back Button
            var backBtn = CreateMenuButton("BackFromCredits", creditsPanel.transform, "BACK", secondaryColor);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.08f);
            backRect.anchorMax = new Vector2(0.5f, 0.08f);
            backRect.sizeDelta = new Vector2(200, 50);
            backRect.anchoredPosition = Vector2.zero;

            menuController.creditsPanel = creditsPanel;
            menuController.backFromCreditsButton = backBtn.GetComponent<Button>();

            Log.Info("AutoMenuGenerator: Credits panel created.");
        }

        private void SetupMenuController()
        {
            if (menuController == null)
            {
                menuController = gameObject.GetComponent<MainMenuController>();
                if (menuController == null)
                {
                    menuController = gameObject.AddComponent<MainMenuController>();
                }
            }

            Log.Info("AutoMenuGenerator: Menu controller configured.");
        }

        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                mainCam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = backgroundColor;
            mainCam.orthographic = true;
            mainCam.transform.position = new Vector3(0, 0, -10);

            Log.Info("AutoMenuGenerator: Camera configured.");
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================

        private GameObject CreatePanel(string name, Transform parent)
        {
            var panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            var rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return panelGO;
        }

        private Text CreateText(string name, Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, int fontSize)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.color = textColor;
            textComp.alignment = TextAnchor.MiddleLeft;

            return textComp;
        }

        private GameObject CreateMenuButton(string name, Transform parent, string text, Color bgColor)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 50);

            var image = btnGO.AddComponent<Image>();
            image.color = bgColor;

            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 1f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            button.colors = colors;

            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 22;
            textComp.fontStyle = FontStyle.Bold;
            textComp.color = textColor;
            textComp.alignment = TextAnchor.MiddleCenter;

            return btnGO;
        }

        private void CreateSettingsLabel(string name, Transform parent, string text)
        {
            var labelGO = new GameObject(name);
            labelGO.transform.SetParent(parent, false);

            var rect = labelGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 30);

            var textComp = labelGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 18;
            textComp.fontStyle = FontStyle.Bold;
            textComp.color = accentColor;
            textComp.alignment = TextAnchor.MiddleLeft;
        }

        private Slider CreateSliderSetting(string name, Transform parent, string label, float defaultValue)
        {
            var containerGO = new GameObject(name + "Container");
            containerGO.transform.SetParent(parent, false);

            var rect = containerGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 40);

            var hlg = containerGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(containerGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.color = textColor;
            labelText.alignment = TextAnchor.MiddleLeft;

            // Slider
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(containerGO.transform, false);
            var sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(200, 20);

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = primaryColor;

            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = accentColor;

            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;
            slider.targetGraphic = handleImage;

            return slider;
        }

        private Toggle CreateToggleSetting(string name, Transform parent, string label, bool defaultValue)
        {
            var containerGO = new GameObject(name + "Container");
            containerGO.transform.SetParent(parent, false);

            var rect = containerGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 40);

            var hlg = containerGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(containerGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.color = textColor;
            labelText.alignment = TextAnchor.MiddleLeft;

            // Toggle
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(containerGO.transform, false);
            var toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(30, 30);

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(toggleGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(bgGO.transform, false);
            var checkRect = checkGO.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImage = checkGO.AddComponent<Image>();
            checkImage.color = accentColor;

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = defaultValue;

            return toggle;
        }

        private Dropdown CreateDropdownSetting(string name, Transform parent, string label, List<string> options, int defaultIndex)
        {
            var containerGO = new GameObject(name + "Container");
            containerGO.transform.SetParent(parent, false);

            var rect = containerGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 40);

            var hlg = containerGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(containerGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.color = textColor;
            labelText.alignment = TextAnchor.MiddleLeft;

            // Dropdown
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(containerGO.transform, false);
            var dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(200, 30);

            var dropdownImage = dropdownGO.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var dropdown = dropdownGO.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;

            // Caption Text
            var captionGO = new GameObject("Label");
            captionGO.transform.SetParent(dropdownGO.transform, false);
            var captionRect = captionGO.AddComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 0);
            captionRect.offsetMax = new Vector2(-25, 0);
            var captionText = captionGO.AddComponent<Text>();
            captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            captionText.fontSize = 14;
            captionText.color = textColor;
            captionText.alignment = TextAnchor.MiddleLeft;
            dropdown.captionText = captionText;

            // Template
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            var templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
            templateGO.SetActive(false);

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            viewportGO.AddComponent<Image>();

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);

            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 28);
            var itemToggle = itemGO.AddComponent<Toggle>();

            var itemBgGO = new GameObject("Item Background");
            itemBgGO.transform.SetParent(itemGO.transform, false);
            var itemBgRect = itemBgGO.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;
            var itemBgImage = itemBgGO.AddComponent<Image>();
            itemBgImage.color = primaryColor;

            var itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);
            var itemLabelText = itemLabelGO.AddComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabelText.fontSize = 14;
            itemLabelText.color = textColor;
            itemLabelText.alignment = TextAnchor.MiddleLeft;

            itemToggle.targetGraphic = itemBgImage;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            dropdown.options.Clear();
            foreach (var opt in options)
            {
                dropdown.options.Add(new Dropdown.OptionData(opt));
            }
            dropdown.value = defaultIndex;
            dropdown.RefreshShownValue();

            return dropdown;
        }

        private GameObject CreateModeCard(string name, Transform parent, string title, string description, Color cardColor)
        {
            var cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);

            var rect = cardGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(350, 300);

            var image = cardGO.AddComponent<Image>();
            image.color = cardColor;

            var button = cardGO.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = cardColor;
            colors.highlightedColor = new Color(cardColor.r + 0.1f, cardColor.g + 0.1f, cardColor.b + 0.1f, 1f);
            colors.pressedColor = new Color(cardColor.r * 0.8f, cardColor.g * 0.8f, cardColor.b * 0.8f, 1f);
            button.colors = colors;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(cardGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.7f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, 0);
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = title;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = textColor;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Description
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(cardGO.transform, false);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.15f);
            descRect.anchorMax = new Vector2(1, 0.65f);
            descRect.offsetMin = new Vector2(20, 0);
            descRect.offsetMax = new Vector2(-20, 0);
            var descText = descGO.AddComponent<Text>();
            descText.text = description;
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 16;
            descText.color = new Color(1, 1, 1, 0.8f);
            descText.alignment = TextAnchor.MiddleCenter;

            return cardGO;
        }
    }
}
