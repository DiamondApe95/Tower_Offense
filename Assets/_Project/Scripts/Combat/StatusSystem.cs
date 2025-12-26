using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Combat
{
    public class StatusSystem
    {
        public void ApplySlow(GameObject target, float slowPercent, float duration)
        {
            if (target == null)
            {
                Log.Warning("ApplySlow called with null target.");
                return;
            }

            SlowStatus slow = target.GetComponent<SlowStatus>();
            if (slow == null)
            {
                slow = target.AddComponent<SlowStatus>();
                slow.Initialize(slowPercent, duration);
                return;
            }

            slow.Refresh(slowPercent, duration);
        }

        public void ApplyBurn(GameObject target, float tickDamage, float tickInterval, float duration)
        {
            if (target == null)
            {
                Log.Warning("ApplyBurn called with null target.");
                return;
            }

            BurnStatus burn = target.AddComponent<BurnStatus>();
            burn.Initialize(tickDamage, tickInterval, duration);
        }

        public void ApplyArmorShred(GameObject target, float amount, float duration)
        {
            if (target == null)
            {
                Log.Warning("ApplyArmorShred called with null target.");
                return;
            }

            ArmorShredStatus shred = target.GetComponent<ArmorShredStatus>();
            if (shred == null)
            {
                shred = target.AddComponent<ArmorShredStatus>();
                shred.Initialize(amount, duration);
                return;
            }

            shred.Refresh(amount, duration);
        }
    }
}
