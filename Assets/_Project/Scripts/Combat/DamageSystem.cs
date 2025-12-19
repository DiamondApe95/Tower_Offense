using UnityEngine;

namespace TowerOffense.Combat
{
    public class DamageSystem
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public int Calculate(int baseDamage)
        {
            UnityEngine.Debug.Log("Calculating damage.");
            return baseDamage;
        }

    }
}
