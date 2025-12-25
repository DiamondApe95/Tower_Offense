using System;
using System.Collections.Generic;
using TowerConquest.Combat;
using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class UnitController : MonoBehaviour
    {
        public string UnitId { get; private set; }
        public float BaseDamage { get; private set; } = 50f;
        public float AttacksPerSecond { get; private set; } = 1f;
        public bool IsAlive { get; private set; } = true;

        // Compatibility properties for AI systems
        public float damage => BaseDamage;
        public float attacksPerSecond => AttacksPerSecond;
        public float moveSpeed => unitMover != null ? unitMover.moveSpeed : 0f;

        // Events
        public event Action<UnitController> OnUnitDestroyed;
        public event Action<UnitController, float> OnDamageTaken;
        public event Action<UnitController> OnReachedGoal;

        private HealthComponent healthComponent;
        private UnitMover unitMover;

        private void OnEnable()
        {
            IsAlive = true;
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.RegisterUnit(this);
            }
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet(out EntityRegistry registry))
            {
                registry.UnregisterUnit(this);
            }
        }

        public void Initialize(string unitId, IReadOnlyList<Vector3> path, BaseController baseController)
        {
            UnitId = unitId;
            IsAlive = true;

            UnitDefinition definition = ServiceLocator.Get<JsonDatabase>()?.FindUnit(unitId);
            float hp = definition?.stats?.hp ?? 100f;
            float armor = definition?.stats?.armor ?? 0f;
            float moveSpeed = definition?.stats?.move_speed ?? 2.5f;
            float size = definition?.stats?.size ?? 1f;

            BaseDamage = definition?.attack?.base_damage > 0f ? definition.attack.base_damage : 50f;
            AttacksPerSecond = definition?.attack?.attacks_per_second > 0f ? definition.attack.attacks_per_second : 1f;

            transform.localScale = Vector3.one * Mathf.Max(0.5f, size);

            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }

            healthComponent.Initialize(hp, armor);
            healthComponent.OnDeath -= HandleDeath;
            healthComponent.OnDeath += HandleDeath;
            healthComponent.OnDamaged -= HandleDamage;
            healthComponent.OnDamaged += HandleDamage;

            unitMover = GetComponent<UnitMover>();
            if (unitMover == null)
            {
                unitMover = gameObject.AddComponent<UnitMover>();
            }

            unitMover.moveSpeed = moveSpeed;
            unitMover.OnReachedGoal -= HandleReachedGoal;
            unitMover.OnReachedGoal += HandleReachedGoal;
            unitMover.Initialize(path, baseController, BaseDamage);
        }

        private void HandleDeath()
        {
            if (!IsAlive) return;
            IsAlive = false;

            OnUnitDestroyed?.Invoke(this);
        }

        private void HandleDamage(float damage, string damageType, GameObject source)
        {
            OnDamageTaken?.Invoke(this, damage);
        }

        private void HandleReachedGoal()
        {
            OnReachedGoal?.Invoke(this);
            DestroyUnit();
        }

        public void DestroyUnit()
        {
            if (!IsAlive) return;
            IsAlive = false;

            OnUnitDestroyed?.Invoke(this);
        }

        public float GetHealthPercent()
        {
            if (healthComponent == null) return 0f;
            return healthComponent.HealthPercent;
        }

        public float GetCurrentHealth()
        {
            if (healthComponent == null) return 0f;
            return healthComponent.CurrentHp;
        }

        public void ResetForPooling()
        {
            IsAlive = true;
            UnitId = null;

            if (healthComponent != null)
            {
                healthComponent.OnDeath -= HandleDeath;
                healthComponent.OnDamaged -= HandleDamage;
            }

            if (unitMover != null)
            {
                unitMover.OnReachedGoal -= HandleReachedGoal;
            }
        }
    }
}
