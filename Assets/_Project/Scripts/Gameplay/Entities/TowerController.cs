using TowerOffense.Combat;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class TowerController : MonoBehaviour
    {
        public float range = 6f;
        public float damage = 20f;
        public float attacksPerSecond = 1f;

        private float scanTimer;
        private float attackTimer;
        private UnitController currentTarget;

        private void Update()
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= 0.25f)
            {
                scanTimer = 0f;
                AcquireTarget();
            }

            if (currentTarget == null)
            {
                return;
            }

            attackTimer += Time.deltaTime;
            float attackInterval = attacksPerSecond > 0f ? 1f / attacksPerSecond : 0.25f;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                DamageSystem.Apply(currentTarget.gameObject, damage);
                Debug.Log($"Tower hit unit {currentTarget.UnitId}");
            }
        }

        private void AcquireTarget()
        {
            UnitController[] units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            UnitController closest = null;
            float closestDistance = float.MaxValue;

            Vector3 towerPosition = transform.position;
            foreach (UnitController unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(towerPosition, unit.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closest = unit;
                    closestDistance = distance;
                }
            }

            currentTarget = closest;
        }
    }
}
