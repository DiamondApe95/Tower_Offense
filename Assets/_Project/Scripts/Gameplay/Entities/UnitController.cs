using TowerOffense.Combat;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    [RequireComponent(typeof(HealthComponent))]
    public class UnitController : MonoBehaviour
    {
        private const float DefaultHp = 100f;

        private HealthComponent health;
        private bool initialized;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            if (health == null)
            {
                health = gameObject.AddComponent<HealthComponent>();
            }
        }

        private void Start()
        {
            if (!initialized)
            {
                Initialize(DefaultHp);
            }
        }

        public void Initialize(float hp = DefaultHp)
        {
            initialized = true;
            health.Initialize(hp);
        }
    }
}
