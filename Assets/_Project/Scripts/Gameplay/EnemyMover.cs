using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float waypointReachDistance = 0.1f;

    [Header("Alternative Path (Vector3-basiert)")]
    [Tooltip("Falls Transform-Waypoints zerstört werden, nutze gecachte Positionen")]
    public bool usePositionCache = true;

    private Transform[] waypoints;
    private Vector3[] cachedPositions;
    private int currentIndex = 0;
    private bool isInitialized = false;

    public void Init(Transform[] pathWaypoints)
    {
        waypoints = pathWaypoints;
        currentIndex = 0;
        isInitialized = false;

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("EnemyMover: No waypoints provided.");
            return;
        }

        // Cache Positionen um MissingReferenceException zu vermeiden
        if (usePositionCache)
        {
            cachedPositions = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    cachedPositions[i] = waypoints[i].position;
                }
            }
        }

        // Start genau auf den ersten WP setzen
        transform.position = GetWaypointPosition(0);
        isInitialized = true;
    }

    public void Init(Vector3[] pathPositions)
    {
        cachedPositions = pathPositions;
        waypoints = null;
        currentIndex = 0;

        if (cachedPositions == null || cachedPositions.Length == 0)
        {
            Debug.LogWarning("EnemyMover: No path positions provided.");
            return;
        }

        transform.position = cachedPositions[0];
        isInitialized = true;
    }

    private Vector3 GetWaypointPosition(int index)
    {
        // Priorisiere gecachte Positionen
        if (cachedPositions != null && index < cachedPositions.Length)
        {
            return cachedPositions[index];
        }

        // Fallback auf Transform wenn vorhanden und nicht zerstört
        if (waypoints != null && index < waypoints.Length && waypoints[index] != null)
        {
            return waypoints[index].position;
        }

        // Fallback: aktuelle Position
        return transform.position;
    }

    private int GetWaypointCount()
    {
        if (cachedPositions != null)
        {
            return cachedPositions.Length;
        }

        if (waypoints != null)
        {
            return waypoints.Length;
        }

        return 0;
    }

    private void Update()
    {
        if (!isInitialized) return;

        int waypointCount = GetWaypointCount();
        if (waypointCount == 0) return;
        if (currentIndex >= waypointCount) return;

        Vector3 targetPos = GetWaypointPosition(currentIndex);
        Vector3 dir = targetPos - transform.position;
        float dist = dir.magnitude;

        if (dist <= waypointReachDistance)
        {
            currentIndex++;

            if (currentIndex >= waypointCount)
            {
                // Ziel erreicht - Base Damage oder Despawn
                OnReachedEnd();
                return;
            }

            targetPos = GetWaypointPosition(currentIndex);
            dir = targetPos - transform.position;
        }

        Vector3 move = dir.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;

        // Richtung anschauen
        if (move.sqrMagnitude > 0.0001f)
        {
            transform.forward = move.normalized;
        }
    }

    protected virtual void OnReachedEnd()
    {
        // Kann überschrieben werden für spezielle Logik (Base-Damage, etc.)
        Destroy(gameObject);
    }

    public float GetProgressPercent()
    {
        int count = GetWaypointCount();
        if (count <= 1) return 1f;
        return (float)currentIndex / (count - 1);
    }

    public int CurrentWaypointIndex => currentIndex;
    public bool HasReachedEnd => currentIndex >= GetWaypointCount();
}
