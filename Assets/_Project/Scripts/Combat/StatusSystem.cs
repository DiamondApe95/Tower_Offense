using UnityEngine;

namespace TowerOffense.Combat
{
    public class StatusSystem
    {
        public void ApplySlow(GameObject target, float slowPercent, float duration)
        {
            if (target == null)
            {
                Debug.LogWarning("ApplySlow called with null target.");
                return;
            }

            SlowStatus existing = target.GetComponent<SlowStatus>();
            if (existing != null)
            {
                Object.Destroy(existing);
            }

            SlowStatus slow = target.AddComponent<SlowStatus>();
            slow.Initialize(slowPercent, duration);
        }

        public void ApplyBurn(GameObject target, float tickDamage, float tickInterval, float duration)
        {
            if (target == null)
            {
                Debug.LogWarning("ApplyBurn called with null target.");
                return;
            }

            BurnStatus burn = target.AddComponent<BurnStatus>();
            burn.Initialize(tickDamage, tickInterval, duration);
        }
    }
}
