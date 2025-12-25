using TowerConquest.Combat;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class TowerController : MonoBehaviour
    {
        public float range = 6f;
        public float damage = 20f;
        public float attacksPerSecond = 1f;
        public TowerDefinition.EffectDto[] effects;
        public float EstimatedDps { get; private set; }

        private float scanTimer;
        private float attackTimer;
        private UnitController currentTarget;
        private readonly EffectResolver effectResolver = new EffectResolver();
        private EntityRegistry entityRegistry;

        private void Awake()
        {
            UpdateDpsCache();
            ServiceLocator.TryGet(out entityRegistry);
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.RegisterTower(this);
            }
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.UnregisterTower(this);
            }
        }

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
                if (effects != null && effects.Length > 0)
                {
                    effectResolver.ApplyEffects(gameObject, currentTarget.gameObject, effects);
                }

                UnityEngine.Debug.Log($"Tower hit unit {currentTarget.UnitId}");
            }
        }

        private void AcquireTarget()
        {
            UnitController[] units;
            if (entityRegistry != null)
            {
                units = entityRegistry.GetAllUnits();
            }
            else
            {
                units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            }

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

        public void UpdateDpsCache()
        {
            EstimatedDps = Mathf.Max(0f, damage) * Mathf.Max(0.1f, attacksPerSecond);
        }
    }
}
