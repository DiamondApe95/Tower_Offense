using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float waypointReachDistance = 0.1f;

    private Transform[] waypoints;
    private int currentIndex = 0;

    public void Init(Transform[] pathWaypoints)
    {
        waypoints = pathWaypoints;
        currentIndex = 0;

        // Start genau auf den ersten WP setzen (optional)
        if (waypoints != null && waypoints.Length > 0)
            transform.position = waypoints[0].position;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (currentIndex >= waypoints.Length) return;

        Transform target = waypoints[currentIndex];
        Vector3 dir = (target.position - transform.position);
        float dist = dir.magnitude;

        if (dist <= waypointReachDistance)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
            {
                // Ziel erreicht
                Destroy(gameObject);
                return;
            }

            target = waypoints[currentIndex];
            dir = (target.position - transform.position);
        }

        Vector3 move = dir.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;

        // Optional: Richtung anschauen
        if (move.sqrMagnitude > 0.0001f)
            transform.forward = move.normalized;
    }
}
