using UnityEngine;

namespace TowerConquest.UI
{
    /// <summary>
    /// Handles safe area adjustments for mobile devices to prevent UI cutoff
    /// Automatically adjusts RectTransform to respect device safe areas (notches, rounded corners, etc.)
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Additional margin in pixels beyond the safe area")]
        [SerializeField] private float additionalMargin = 20f;

        [Tooltip("Minimum margin to enforce even on devices without safe area constraints")]
        [SerializeField] private float minimumMargin = 30f;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            // Check if screen size or safe area changed (orientation change, etc.)
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply additional margin
            float marginX = (additionalMargin / Screen.width);
            float marginY = (additionalMargin / Screen.height);

            anchorMin.x = Mathf.Max(anchorMin.x + marginX, minimumMargin / Screen.width);
            anchorMin.y = Mathf.Max(anchorMin.y + marginY, minimumMargin / Screen.height);
            anchorMax.x = Mathf.Min(anchorMax.x - marginX, 1f - (minimumMargin / Screen.width));
            anchorMax.y = Mathf.Min(anchorMax.y - marginY, 1f - (minimumMargin / Screen.height));

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            // Reset offsets to 0 since we're using anchors to define the safe area
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Debug.Log($"[SafeAreaHandler] Applied safe area: {safeArea} with {additionalMargin}px additional margin");
        }
    }
}
