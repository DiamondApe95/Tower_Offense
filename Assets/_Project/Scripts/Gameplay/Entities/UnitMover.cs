using System;
using TowerConquest.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class UnitMover : MonoBehaviour
    {
        public float moveSpeed = 2.5f;
        public float moveSpeedMultiplier = 1f;

        // Events
        public event Action OnReachedGoal;
        public event Action<int> OnWaypointReached;

        public bool HasReachedGoal { get; private set; }
        public float Progress => path != null && path.Count > 1 ? (float)waypointIndex / (path.Count - 1) : 0f;
        public int CurrentWaypointIndex => waypointIndex;

        private IReadOnlyList<Vector3> path;
        private int waypointIndex;
        private BaseController baseController;
        private float baseDamage;
        private bool isMoving;

        public void Initialize(IReadOnlyList<Vector3> pathWaypoints, BaseController baseTarget, float damageToBase)
        {
            path = pathWaypoints;
            waypointIndex = 0;
            baseController = baseTarget;
            baseDamage = Mathf.Max(0f, damageToBase);
            HasReachedGoal = false;
            isMoving = true;

            // Start auf ersten Waypoint setzen
            if (path != null && path.Count > 0)
            {
                transform.position = path[0];
            }
        }

        private void Update()
        {
            if (!isMoving || HasReachedGoal)
            {
                return;
            }

            if (path == null || path.Count == 0)
            {
                return;
            }

            if (waypointIndex >= path.Count)
            {
                ReachGoal();
                return;
            }

            Vector3 target = path[waypointIndex];
            float speed = moveSpeed * moveSpeedMultiplier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, speed);

            // Rotation zur Bewegungsrichtung
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.forward = direction.normalized;
            }

            if (Vector3.Distance(transform.position, target) <= 0.01f)
            {
                waypointIndex++;
                OnWaypointReached?.Invoke(waypointIndex);

                if (waypointIndex >= path.Count)
                {
                    ReachGoal();
                }
            }
        }

        private void ReachGoal()
        {
            if (HasReachedGoal) return;

            HasReachedGoal = true;
            isMoving = false;

            Log.Info("UnitMover: Reached end/base.");

            if (baseController != null)
            {
                baseController.ApplyDamage(baseDamage);
            }

            OnReachedGoal?.Invoke();
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void Pause()
        {
            isMoving = false;
        }

        public void Resume()
        {
            if (!HasReachedGoal)
            {
                isMoving = true;
            }
        }

        public void ResetForPooling()
        {
            waypointIndex = 0;
            HasReachedGoal = false;
            isMoving = false;
            path = null;
            baseController = null;
            moveSpeedMultiplier = 1f;
        }
    }
}
