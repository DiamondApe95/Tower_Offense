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

        public void Initialize(string unitId, IReadOnlyList<Vector3> path, BaseController baseController)
        {
            UnitId = unitId;

            UnitDefinition definition = ServiceLocator.Get<JsonDatabase>().FindUnit(unitId);
            float hp = definition?.stats?.hp ?? 100f;
            float armor = definition?.stats?.armor ?? 0f;
            float moveSpeed = definition?.stats?.move_speed ?? 2.5f;
            float size = definition?.stats?.size ?? 1f;

            BaseDamage = definition?.attack?.base_damage > 0f ? definition.attack.base_damage : 50f;

            transform.localScale = Vector3.one * Mathf.Max(0.5f, size);

            HealthComponent health = GetComponent<HealthComponent>();
            if (health == null)
            {
                health = gameObject.AddComponent<HealthComponent>();
            }

            health.Initialize(hp, armor);

            UnitMover mover = GetComponent<UnitMover>();
            if (mover == null)
            {
                mover = gameObject.AddComponent<UnitMover>();
            }

            mover.moveSpeed = moveSpeed;
            mover.Initialize(path, baseController, BaseDamage);
        }
    }
}
