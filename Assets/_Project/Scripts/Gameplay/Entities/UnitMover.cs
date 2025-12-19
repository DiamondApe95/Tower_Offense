using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class UnitMover : MonoBehaviour
    {
        public float moveSpeed = 2.5f;

        private IReadOnlyList<Vector3> path;
        private int waypointIndex;

        public void Initialize(IReadOnlyList<Vector3> pathPoints)
        {
            path = pathPoints;
            waypointIndex = 0;
        }

        private void Update()
        {
            if (path == null || path.Count == 0 || waypointIndex >= path.Count)
            {
                return;
            }

            Vector3 target = path[waypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.01f)
            {
                waypointIndex++;
                if (waypointIndex >= path.Count)
                {
                    Debug.Log("Reached end/base");
                    Destroy(gameObject);
                }
            }
        }
    }
}
