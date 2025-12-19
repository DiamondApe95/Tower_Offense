using TowerOffense.Combat;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class TowerController : MonoBehaviour
    {
        [SerializeField] private float range = 6f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float attacksPerSecond = 1f;

        private const float ScanIntervalSeconds = 0.25f;

        private float scanTimer;
        private float attackTimer;
        private UnitController currentTarget;

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                ScanForTarget();
                scanTimer = ScanIntervalSeconds;
            }

            if (currentTarget == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distance > range)
            {
                currentTarget = null;
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                FireAtTarget();
                attackTimer = attacksPerSecond > 0f ? 1f / attacksPerSecond : 1f;
            }
        }

        public void Initialize(float rangeValue, float damageValue, float attacksPerSecondValue)
        {
            if (rangeValue > 0f)
            {
                range = rangeValue;
            }

            if (damageValue > 0f)
            {
                damage = damageValue;
            }

            if (attacksPerSecondValue > 0f)
            {
                attacksPerSecond = attacksPerSecondValue;
            }
        }

        private void ScanForTarget()
        {
            UnitController[] units = FindObjectsOfType<UnitController>();
            UnitController closestUnit = null;
            float closestDistance = float.MaxValue;

            for (int index = 0; index < units.Length; index++)
            {
                UnitController unit = units[index];
                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance <= range && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestUnit = unit;
                }
            }

            currentTarget = closestUnit;
        }

        private void FireAtTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            DamageSystem.Apply(currentTarget.gameObject, damage);
            Debug.Log($"Tower hit unit {currentTarget.name} for {damage}.");
        }
    }
}
