using System.Collections.Generic;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class UnitMover : MonoBehaviour
    {
        public float moveSpeed = 2.5f;
        public float moveSpeedMultiplier = 1f;

        private IReadOnlyList<Vector3> path;
        private int waypointIndex;
        private BaseController baseController;
        private float baseDamage;

        public void Initialize(IReadOnlyList<Vector3> pathWaypoints, BaseController baseTarget, float damageToBase)
        {
            path = pathWaypoints;
            waypointIndex = 0;
            baseController = baseTarget;
            baseDamage = Mathf.Max(0f, damageToBase);
        }

        private void Update()
        {
            if (path == null || path.Count == 0)
            {
                return;
            }

            if (waypointIndex >= path.Count)
            {
                Debug.Log("UnitMover: Reached end/base.");
                if (baseController != null)
                {
                    baseController.ApplyDamage(baseDamage);
                }
                Destroy(gameObject);
                return;
            }

            Vector3 target = path[waypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * moveSpeedMultiplier * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.01f)
            {
                waypointIndex++;
            }
        }
    }
}
