using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Manages map boundaries and provides validation for positions
    /// </summary>
    public class MapBoundary : MonoBehaviour
    {
        public static MapBoundary Instance { get; private set; }

        [Header("Map Boundaries")]
        [Tooltip("Minimum X and Z coordinates for the playable area")]
        public Vector3 minBounds = new Vector3(-10f, 0f, -10f);

        [Tooltip("Maximum X and Z coordinates for the playable area")]
        public Vector3 maxBounds = new Vector3(10f, 0f, 10f);

        [Header("Safety Margins")]
        [Tooltip("Additional margin to keep units/towers away from the edge")]
        public float safetyMargin = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Check if a position is within map boundaries
        /// </summary>
        public bool IsWithinBounds(Vector3 position)
        {
            return position.x >= minBounds.x && position.x <= maxBounds.x &&
                   position.z >= minBounds.z && position.z <= maxBounds.z;
        }

        /// <summary>
        /// Check if a position is within safe boundaries (with margin)
        /// </summary>
        public bool IsWithinSafeBounds(Vector3 position)
        {
            float minX = minBounds.x + safetyMargin;
            float maxX = maxBounds.x - safetyMargin;
            float minZ = minBounds.z + safetyMargin;
            float maxZ = maxBounds.z - safetyMargin;

            return position.x >= minX && position.x <= maxX &&
                   position.z >= minZ && position.z <= maxZ;
        }

        /// <summary>
        /// Clamp a position to be within map boundaries
        /// </summary>
        public Vector3 ClampToBounds(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
                position.y,
                Mathf.Clamp(position.z, minBounds.z, maxBounds.z)
            );
        }

        /// <summary>
        /// Clamp a position to be within safe boundaries (with margin)
        /// </summary>
        public Vector3 ClampToSafeBounds(Vector3 position)
        {
            float minX = minBounds.x + safetyMargin;
            float maxX = maxBounds.x - safetyMargin;
            float minZ = minBounds.z + safetyMargin;
            float maxZ = maxBounds.z - safetyMargin;

            return new Vector3(
                Mathf.Clamp(position.x, minX, maxX),
                position.y,
                Mathf.Clamp(position.z, minZ, maxZ)
            );
        }

        /// <summary>
        /// Set boundaries based on map dimensions
        /// </summary>
        public void SetBoundaries(Vector3 origin, float width, float height, float tileSize)
        {
            minBounds = origin;
            maxBounds = origin + new Vector3(width * tileSize, 0f, height * tileSize);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw map boundaries
            Gizmos.color = Color.yellow;
            DrawBoundaryBox(minBounds, maxBounds);

            // Draw safe boundaries
            if (safetyMargin > 0)
            {
                Gizmos.color = Color.green;
                Vector3 safeMin = minBounds + new Vector3(safetyMargin, 0, safetyMargin);
                Vector3 safeMax = maxBounds - new Vector3(safetyMargin, 0, safetyMargin);
                DrawBoundaryBox(safeMin, safeMax);
            }
        }

        private void DrawBoundaryBox(Vector3 min, Vector3 max)
        {
            Vector3 p1 = new Vector3(min.x, 0, min.z);
            Vector3 p2 = new Vector3(max.x, 0, min.z);
            Vector3 p3 = new Vector3(max.x, 0, max.z);
            Vector3 p4 = new Vector3(min.x, 0, max.z);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
        }
#endif
    }
}
