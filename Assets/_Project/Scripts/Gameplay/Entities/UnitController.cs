using System.Collections.Generic;
using TowerOffense.Combat;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class UnitController : MonoBehaviour
    {
        public string UnitId { get; private set; }

        public void Initialize(string unitId, IReadOnlyList<Vector3> path)
        {
            UnitId = unitId;

            HealthComponent health = GetComponent<HealthComponent>();
            if (health == null)
            {
                health = gameObject.AddComponent<HealthComponent>();
            }

            health.Initialize(100f);

            UnitMover mover = GetComponent<UnitMover>();
            if (mover == null)
            {
                mover = gameObject.AddComponent<UnitMover>();
            }

            mover.Initialize(path);
        }
    }
}
