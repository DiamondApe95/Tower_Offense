using System.Collections.Generic;
using TowerConquest.Debug;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Saving;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TowerConquest.UI.WorldMap
{
    /// <summary>
    /// AutoWorldMapGenerator: Generiert eine komplette World Map mit einem Klick.
    /// Erstellt Canvas, Hintergrund, Level-Buttons, Navigation und alle notwendigen Komponenten.
    /// </summary>
    public class AutoWorldMapGenerator : MonoBehaviour
    {
        [Header("=== ONE-CLICK WORLD MAP GENERATION ===")]
        [Tooltip("Generiert die komplette World Map beim Start")]
        public bool autoGenerateOnStart = true;

        [Header("Background")]
        [Tooltip("Hintergrundbild für die World Map")]
        public Sprite backgroundSprite;
        public Color backgroundColor = new Color(0.15f, 0.2f, 0.25f, 1f);

        [Header("Style")]
        public Color primaryColor = new Color(0.3f, 0.5f, 0.4f, 1f);
        public Color secondaryColor = new Color(0.2f, 0.25f, 0.3f, 0.95f);
        public Color accentColor = new Color(0.9f, 0.75f, 0.3f, 1f);
        public Color textColor = Color.white;
        public Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        public Color unlockedColor = new Color(0.4f, 0.6f, 0.4f, 1f);
        public Color completedColor = new Color(0.3f, 0.7f, 0.4f, 1f);

        [Header("Level Icons")]
        public Sprite lockedIcon;
        public Sprite unlockedIcon;
        public Sprite completedIcon;
        public Sprite perfectIcon;

        [Header("Generated References")]
        public Canvas generatedCanvas;
        public WorldMapController mapController;

        // Private references
        private GameObject backgroundPanel;
        private GameObject levelButtonContainer;
        private GameObject infoPanel;
        private GameObject navigationPanel;

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                GenerateCompleteWorldMap();
            }
        }

        [ContextMenu("★ Generate Complete World Map (One-Click)")]
        public void GenerateCompleteWorldMap()
        {
            Log.Info("AutoWorldMapGenerator: Starting Complete World Map Generation...");

            CleanupOldWorldMap();
            LoadBackgroundSprite();
            CreateCanvas();
            CreateBackground();
            CreateLevelButtonContainer();
            CreateNavigationPanel();
            CreateInfoPanel();
            SetupWorldMapController();
            SetupCamera();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            Log.Info("AutoWorldMapGenerator: ★ Complete World Map Generation finished! Map is ready.");
        }

        private void LoadBackgroundSprite()
        {
#if UNITY_EDITOR
            if (backgroundSprite == null)
            {
                string bgPath = "Assets/_Project/MAP/World_MAP/World_Map_Background.png";
                backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
                if (backgroundSprite != null)
                {
                    Log.Info($"AutoWorldMapGenerator: Loaded background from {bgPath}");
                }
            }
#endif
        }

        [ContextMenu("Cleanup Old World Map")]
        public void CleanupOldWorldMap()
        {
            var oldCanvas = GameObject.Find("WorldMapCanvas");
            if (oldCanvas != null)
            {
                DestroyImmediate(oldCanvas);
            }

            var oldEventSystem = GameObject.Find("WorldMapEventSystem");
            if (oldEventSystem != null)
            {
                DestroyImmediate(oldEventSystem);
            }

            var oldController = FindFirstObjectByType<WorldMapController>();
            if (oldController != null && oldController.gameObject != gameObject)
            {
                DestroyImmediate(oldController.gameObject);
            }

            Log.Info("AutoWorldMapGenerator: Cleaned up old world map objects.");
        }

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("WorldMapCanvas");
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
                var eventSystemGO = new GameObject("WorldMapEventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            Log.Info("AutoWorldMapGenerator: Canvas created.");
        }

        private void CreateBackground()
        {
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(generatedCanvas.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            if (backgroundSprite != null)
            {
                bgImage.sprite = backgroundSprite;
                bgImage.type = Image.Type.Simple;
                bgImage.preserveAspect = false;
                bgImage.color = Color.white;
            }
            else
            {
                bgImage.color = backgroundColor;
            }

            backgroundPanel = bgGO;
            Log.Info("AutoWorldMapGenerator: Background created.");
        }

        private void CreateLevelButtonContainer()
        {
            // Container für alle Level-Buttons
            var containerGO = new GameObject("LevelButtonContainer");
            containerGO.transform.SetParent(generatedCanvas.transform, false);

            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.2f);
            containerRect.anchorMax = new Vector2(0.9f, 0.85f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Grid Layout für Level-Buttons
            var gridLayout = containerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 120);
            gridLayout.spacing = new Vector2(30, 30);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            levelButtonContainer = containerGO;
            Log.Info("AutoWorldMapGenerator: Level button container created.");
        }

        private void CreateNavigationPanel()
        {
            // Navigation Panel (unten)
            var navGO = new GameObject("NavigationPanel");
            navGO.transform.SetParent(generatedCanvas.transform, false);

            var navRect = navGO.AddComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0, 0);
            navRect.anchorMax = new Vector2(1, 0.12f);
            navRect.offsetMin = Vector2.zero;
            navRect.offsetMax = Vector2.zero;

            var navImage = navGO.AddComponent<Image>();
            navImage.color = secondaryColor;

            // Horizontal Layout für Buttons
            var hlg = navGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.padding = new RectOffset(50, 50, 15, 15);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Back Button
            var backBtn = CreateButton("BackButton", navGO.transform, "< ZURÜCK", secondaryColor, 200, 50);

            // Fame/Shop Button
            var fameBtn = CreateButton("FameShopButton", navGO.transform, "RUHM-SHOP", accentColor, 200, 50);

            navigationPanel = navGO;
            Log.Info("AutoWorldMapGenerator: Navigation panel created.");
        }

        private void CreateInfoPanel()
        {
            // Info Panel (rechts, standardmäßig ausgeblendet)
            var infoGO = new GameObject("LevelInfoPanel");
            infoGO.transform.SetParent(generatedCanvas.transform, false);

            var infoRect = infoGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.65f, 0.2f);
            infoRect.anchorMax = new Vector2(0.95f, 0.8f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            var infoImage = infoGO.AddComponent<Image>();
            infoImage.color = secondaryColor;

            // Vertical Layout
            var vlg = infoGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Level Name
            var nameGO = CreateTextElement("LevelName", infoGO.transform, "Level Name", 32, FontStyle.Bold, accentColor);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(0, 50);

            // Beschreibung
            var descGO = CreateTextElement("LevelDescription", infoGO.transform, "Level Beschreibung...", 18, FontStyle.Normal, textColor);
            var descRect = descGO.GetComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(0, 100);

            // Schwierigkeit
            var diffGO = CreateTextElement("LevelDifficulty", infoGO.transform, "Schwierigkeit: Normal", 20, FontStyle.Normal, textColor);
            var diffRect = diffGO.GetComponent<RectTransform>();
            diffRect.sizeDelta = new Vector2(0, 40);

            // Spacer
            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(infoGO.transform, false);
            var spacerRect = spacerGO.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(0, 50);
            var spacerLayout = spacerGO.AddComponent<LayoutElement>();
            spacerLayout.flexibleHeight = 1;

            // Play Button
            var playBtn = CreateButton("PlayButton", infoGO.transform, "SPIELEN", primaryColor, 200, 60);
            var playBtnLayout = playBtn.AddComponent<LayoutElement>();
            playBtnLayout.preferredHeight = 60;
            playBtnLayout.preferredWidth = 200;

            // Standardmäßig ausblenden
            infoGO.SetActive(false);

            infoPanel = infoGO;
            Log.Info("AutoWorldMapGenerator: Info panel created.");
        }

        private void SetupWorldMapController()
        {
            if (mapController == null)
            {
                mapController = gameObject.GetComponent<WorldMapController>();
                if (mapController == null)
                {
                    mapController = gameObject.AddComponent<WorldMapController>();
                }
            }

            // Referenzen setzen via Reflection (da SerializeField)
            var type = typeof(WorldMapController);

            // Level Button Container
            var containerField = type.GetField("levelButtonContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (containerField != null && levelButtonContainer != null)
            {
                containerField.SetValue(mapController, levelButtonContainer.transform);
            }

            // Background Image
            var bgField = type.GetField("backgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bgField != null && backgroundPanel != null)
            {
                bgField.SetValue(mapController, backgroundPanel.GetComponent<Image>());
            }

            // Navigation Buttons
            if (navigationPanel != null)
            {
                var backBtn = navigationPanel.transform.Find("BackButton");
                var fameBtn = navigationPanel.transform.Find("FameShopButton");

                var backField = type.GetField("backToMenuButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (backField != null && backBtn != null)
                {
                    backField.SetValue(mapController, backBtn.GetComponent<Button>());
                }

                var fameField = type.GetField("fameShopButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fameField != null && fameBtn != null)
                {
                    fameField.SetValue(mapController, fameBtn.GetComponent<Button>());
                }
            }

            // Info Panel
            if (infoPanel != null)
            {
                var infoPanelField = type.GetField("levelInfoPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (infoPanelField != null)
                {
                    infoPanelField.SetValue(mapController, infoPanel);
                }

                var nameText = infoPanel.transform.Find("LevelName");
                var descText = infoPanel.transform.Find("LevelDescription");
                var diffText = infoPanel.transform.Find("LevelDifficulty");
                var playBtn = infoPanel.transform.Find("PlayButton");

                var nameField = type.GetField("levelNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nameField != null && nameText != null)
                {
                    nameField.SetValue(mapController, nameText.GetComponent<Text>());
                }

                var descField = type.GetField("levelDescriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (descField != null && descText != null)
                {
                    descField.SetValue(mapController, descText.GetComponent<Text>());
                }

                var diffField = type.GetField("levelDifficultyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (diffField != null && diffText != null)
                {
                    diffField.SetValue(mapController, diffText.GetComponent<Text>());
                }

                var playField = type.GetField("playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playField != null && playBtn != null)
                {
                    playField.SetValue(mapController, playBtn.GetComponent<Button>());
                }
            }

            // Status Colors
            var lockedColorField = type.GetField("lockedColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lockedColorField != null)
            {
                lockedColorField.SetValue(mapController, lockedColor);
            }

            var unlockedColorField = type.GetField("unlockedColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (unlockedColorField != null)
            {
                unlockedColorField.SetValue(mapController, unlockedColor);
            }

            var completedColorField = type.GetField("completedColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (completedColorField != null)
            {
                completedColorField.SetValue(mapController, completedColor);
            }

            Log.Info("AutoWorldMapGenerator: World Map controller configured.");
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

            Log.Info("AutoWorldMapGenerator: Camera configured.");
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================

        private GameObject CreateButton(string name, Transform parent, string text, Color bgColor, float width, float height)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var image = btnGO.AddComponent<Image>();
            image.color = bgColor;

            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f, 1f);
            colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 1f);
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
            textComp.fontSize = 20;
            textComp.fontStyle = FontStyle.Bold;
            textComp.color = textColor;
            textComp.alignment = TextAnchor.MiddleCenter;

            return btnGO;
        }

        private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, FontStyle style, Color color)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            textGO.AddComponent<RectTransform>();

            var textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.fontStyle = style;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;

            return textGO;
        }
    }
}
