using UnityEngine;
using UnityEngine.UI;
using TowerConquest.Gameplay;

namespace TowerConquest.UI
{
    /// <summary>
    /// UI component that displays construction progress on a construction site
    /// Shows "0/3" format for builder progress
    /// </summary>
    public class ConstructionProgressUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Image backgroundBar;

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
        [SerializeField] private Color inProgressColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color completeColor = Color.green;
        [SerializeField] private float barWidth = 1.5f;
        [SerializeField] private float barHeight = 0.2f;

        private ConstructionSite targetSite;
        private TrapConstructionSite targetTrapSite;
        private Camera mainCamera;
        private bool isInitialized;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Initialize for a tower construction site
        /// </summary>
        public void Initialize(ConstructionSite site)
        {
            targetSite = site;
            targetTrapSite = null;
            isInitialized = true;

            // Subscribe to events
            site.OnBuilderCountChanged += OnBuilderProgressChanged;
            site.OnConstructionComplete += OnSiteComplete;
            site.OnConstructionDestroyed += OnSiteDestroyed;

            CreateUI();
            UpdateDisplay();
        }

        /// <summary>
        /// Initialize for a trap construction site
        /// </summary>
        public void Initialize(TrapConstructionSite site)
        {
            targetTrapSite = site;
            targetSite = null;
            isInitialized = true;

            // Subscribe to events
            site.OnBuilderArrived += OnTrapBuilderProgressChanged;
            site.OnConstructionComplete += OnTrapSiteComplete;
            site.OnConstructionDestroyed += OnTrapSiteDestroyed;

            CreateUI();
            UpdateDisplay();
        }

        private void CreateUI()
        {
            if (worldCanvas != null) return;

            // Create world space canvas
            GameObject canvasObj = new GameObject("ProgressCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = offset;

            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 100;

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 100;

            var rectTransform = canvasObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(barWidth * 100, barHeight * 100 + 50);
            rectTransform.localScale = Vector3.one * 0.01f;

            // Create background bar
            GameObject bgBarObj = new GameObject("BackgroundBar");
            bgBarObj.transform.SetParent(canvasObj.transform, false);

            var bgRect = bgBarObj.AddComponent<RectTransform>();
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(barWidth * 100, barHeight * 100);

            backgroundBar = bgBarObj.AddComponent<Image>();
            backgroundBar.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Create progress bar
            GameObject progressBarObj = new GameObject("ProgressBar");
            progressBarObj.transform.SetParent(canvasObj.transform, false);

            var progressRect = progressBarObj.AddComponent<RectTransform>();
            progressRect.anchoredPosition = Vector2.zero;
            progressRect.sizeDelta = new Vector2(0, barHeight * 100);
            progressRect.pivot = new Vector2(0, 0.5f);
            progressRect.anchorMin = new Vector2(0, 0.5f);
            progressRect.anchorMax = new Vector2(0, 0.5f);
            progressRect.anchoredPosition = new Vector2(-barWidth * 50, 0);

            progressBar = progressBarObj.AddComponent<Image>();
            progressBar.color = inProgressColor;

            // Create text
            GameObject textObj = new GameObject("ProgressText");
            textObj.transform.SetParent(canvasObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, barHeight * 100 + 20);
            textRect.sizeDelta = new Vector2(100, 40);

            progressText = textObj.AddComponent<Text>();
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressText.fontSize = 28;
            progressText.fontStyle = FontStyle.Bold;
            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.color = Color.white;

            // Add outline for visibility
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Billboard effect - face camera
            if (worldCanvas != null && mainCamera != null)
            {
                worldCanvas.transform.rotation = mainCamera.transform.rotation;
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            int current = 0;
            int required = 1;
            float progress = 0f;
            bool isComplete = false;

            if (targetSite != null)
            {
                current = targetSite.CurrentBuilders;
                required = targetSite.RequiredBuilders;
                progress = targetSite.ConstructionProgress;
                isComplete = targetSite.IsComplete;
            }
            else if (targetTrapSite != null)
            {
                current = targetTrapSite.CurrentBuilders;
                required = targetTrapSite.RequiredBuilders;
                progress = targetTrapSite.ConstructionProgress;
                isComplete = targetTrapSite.IsComplete;
            }

            // Update text
            if (progressText != null)
            {
                progressText.text = $"{current}/{required}";
                progressText.color = isComplete ? completeColor : Color.white;
            }

            // Update progress bar
            if (progressBar != null)
            {
                var rect = progressBar.rectTransform;
                rect.sizeDelta = new Vector2(barWidth * 100 * progress, barHeight * 100);
                progressBar.color = isComplete ? completeColor : inProgressColor;
            }
        }

        private void OnBuilderProgressChanged(int current, int required)
        {
            UpdateDisplay();
        }

        private void OnTrapBuilderProgressChanged(int current, int required)
        {
            UpdateDisplay();
        }

        private void OnSiteComplete(ConstructionSite site)
        {
            UpdateDisplay();
            // Hide after short delay
            Invoke(nameof(Hide), 0.5f);
        }

        private void OnTrapSiteComplete(TrapConstructionSite site)
        {
            UpdateDisplay();
            Invoke(nameof(Hide), 0.5f);
        }

        private void OnSiteDestroyed(ConstructionSite site)
        {
            Cleanup();
        }

        private void OnTrapSiteDestroyed(TrapConstructionSite site)
        {
            Cleanup();
        }

        private void Hide()
        {
            if (worldCanvas != null)
            {
                worldCanvas.gameObject.SetActive(false);
            }
        }

        private void Cleanup()
        {
            if (targetSite != null)
            {
                targetSite.OnBuilderCountChanged -= OnBuilderProgressChanged;
                targetSite.OnConstructionComplete -= OnSiteComplete;
                targetSite.OnConstructionDestroyed -= OnSiteDestroyed;
            }

            if (targetTrapSite != null)
            {
                targetTrapSite.OnBuilderArrived -= OnTrapBuilderProgressChanged;
                targetTrapSite.OnConstructionComplete -= OnTrapSiteComplete;
                targetTrapSite.OnConstructionDestroyed -= OnTrapSiteDestroyed;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (targetSite != null)
            {
                targetSite.OnBuilderCountChanged -= OnBuilderProgressChanged;
                targetSite.OnConstructionComplete -= OnSiteComplete;
                targetSite.OnConstructionDestroyed -= OnSiteDestroyed;
            }

            if (targetTrapSite != null)
            {
                targetTrapSite.OnBuilderArrived -= OnTrapBuilderProgressChanged;
                targetTrapSite.OnConstructionComplete -= OnTrapSiteComplete;
                targetTrapSite.OnConstructionDestroyed -= OnTrapSiteDestroyed;
            }
        }

        /// <summary>
        /// Static helper to attach progress UI to a construction site
        /// </summary>
        public static ConstructionProgressUI AttachTo(ConstructionSite site)
        {
            var uiObj = new GameObject("ConstructionProgressUI");
            uiObj.transform.SetParent(site.transform);
            uiObj.transform.localPosition = Vector3.zero;

            var ui = uiObj.AddComponent<ConstructionProgressUI>();
            ui.Initialize(site);

            return ui;
        }

        /// <summary>
        /// Static helper to attach progress UI to a trap construction site
        /// </summary>
        public static ConstructionProgressUI AttachTo(TrapConstructionSite site)
        {
            var uiObj = new GameObject("ConstructionProgressUI");
            uiObj.transform.SetParent(site.transform);
            uiObj.transform.localPosition = Vector3.zero;

            var ui = uiObj.AddComponent<ConstructionProgressUI>();
            ui.Initialize(site);

            return ui;
        }
    }
}
