using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TowerConquest.Core;

namespace TowerConquest.UI
{
    /// <summary>
    /// TooltipSystem: Globales Tooltip-System für UI-Elemente.
    /// Zeigt kontextuelle Hilfe und Informationen an.
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        [Header("Tooltip UI")]
        public GameObject tooltipPanel;
        public Text titleText;
        public Text descriptionText;
        public Text statsText;
        public Image iconImage;

        [Header("Settings")]
        public float showDelay = 0.5f;
        public float hideDelay = 0.1f;
        public Vector2 offset = new Vector2(15, -15);
        public bool followMouse = true;

        [Header("Animation")]
        public float fadeInDuration = 0.15f;
        public float fadeOutDuration = 0.1f;

        private RectTransform tooltipRect;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private Coroutine showCoroutine;
        private Coroutine fadeCoroutine;
        private bool isVisible;
        private TooltipData currentData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (tooltipPanel != null)
            {
                tooltipRect = tooltipPanel.GetComponent<RectTransform>();
                canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
                }

                parentCanvas = tooltipPanel.GetComponentInParent<Canvas>();
                tooltipPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (isVisible && followMouse && tooltipRect != null)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Zeigt ein Tooltip mit den angegebenen Daten.
        /// </summary>
        public void Show(TooltipData data)
        {
            if (tooltipPanel == null || data == null) return;

            // Prüfe ob Tooltips aktiviert sind
            GameSettings settings = GameSettings.Load();
            if (!settings.showTooltips) return;

            currentData = data;

            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
            }

            showCoroutine = StartCoroutine(ShowDelayed());
        }

        /// <summary>
        /// Zeigt ein einfaches Text-Tooltip.
        /// </summary>
        public void Show(string title, string description = null)
        {
            Show(new TooltipData
            {
                title = title,
                description = description
            });
        }

        /// <summary>
        /// Versteckt das aktuelle Tooltip.
        /// </summary>
        public void Hide()
        {
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeOut());
        }

        /// <summary>
        /// Versteckt das Tooltip sofort ohne Animation.
        /// </summary>
        public void HideImmediate()
        {
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            isVisible = false;
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private IEnumerator ShowDelayed()
        {
            yield return new WaitForSecondsRealtime(showDelay);

            PopulateTooltip(currentData);
            tooltipPanel.SetActive(true);
            UpdatePosition();

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            isVisible = true;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeCoroutine = null;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            isVisible = false;
            tooltipPanel.SetActive(false);
            fadeCoroutine = null;
        }

        private void PopulateTooltip(TooltipData data)
        {
            if (titleText != null)
            {
                titleText.text = data.title ?? "";
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(data.title));
            }

            if (descriptionText != null)
            {
                descriptionText.text = data.description ?? "";
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(data.description));
            }

            if (statsText != null)
            {
                statsText.text = data.stats ?? "";
                statsText.gameObject.SetActive(!string.IsNullOrEmpty(data.stats));
            }

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(data.icon != null);
            }

            // Layout aktualisieren
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        private void UpdatePosition()
        {
            if (tooltipRect == null || parentCanvas == null) return;

            Vector2 mousePos = Input.mousePosition;
            Vector2 targetPos = mousePos + offset;

            // Bildschirmgrenzen beachten
            float rightEdge = targetPos.x + tooltipRect.rect.width;
            float bottomEdge = targetPos.y - tooltipRect.rect.height;

            if (rightEdge > Screen.width)
            {
                targetPos.x = mousePos.x - tooltipRect.rect.width - offset.x;
            }

            if (bottomEdge < 0)
            {
                targetPos.y = mousePos.y + tooltipRect.rect.height - offset.y;
            }

            // In Canvas-Koordinaten umrechnen
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera && parentCanvas.worldCamera != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    targetPos,
                    parentCanvas.worldCamera,
                    out Vector2 localPos);

                tooltipRect.localPosition = localPos;
            }
            else
            {
                tooltipRect.position = targetPos;
            }
        }

        public bool IsVisible => isVisible;
    }

    /// <summary>
    /// Datenstruktur für Tooltip-Inhalte.
    /// </summary>
    [System.Serializable]
    public class TooltipData
    {
        public string title;
        public string description;
        public string stats;
        public Sprite icon;
        public Color titleColor = Color.white;
        public Color descriptionColor = new Color(0.8f, 0.8f, 0.8f);
    }

    /// <summary>
    /// Komponente für UI-Elemente, die Tooltips anzeigen sollen.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Tooltip Content")]
        public string title;
        [TextArea(2, 5)]
        public string description;
        public string stats;
        public Sprite icon;

        [Header("Settings")]
        public bool useCustomData = false;
        public TooltipData customData;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TooltipSystem.Instance == null) return;

            if (useCustomData && customData != null)
            {
                TooltipSystem.Instance.Show(customData);
            }
            else
            {
                TooltipSystem.Instance.Show(new TooltipData
                {
                    title = title,
                    description = description,
                    stats = stats,
                    icon = icon
                });
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipSystem.Instance != null)
            {
                TooltipSystem.Instance.Hide();
            }
        }

        /// <summary>
        /// Setzt den Tooltip-Inhalt dynamisch.
        /// </summary>
        public void SetContent(string newTitle, string newDescription = null, string newStats = null)
        {
            title = newTitle;
            description = newDescription;
            stats = newStats;
        }
    }
}
