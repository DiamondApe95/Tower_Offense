using UnityEngine;

namespace TowerOffense.Combat
{
    public static class DamageSystem
    {
        public static void Apply(GameObject target, float amount)
        {
            if (target == null)
            {
                Debug.LogWarning("DamageSystem.Apply called with null target.");
                return;
            }

            var health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                Debug.LogWarning($"DamageSystem.Apply could not find HealthComponent on {target.name}.");
                return;
            }

            health.ApplyDamage(amount);
        }
    }
}
