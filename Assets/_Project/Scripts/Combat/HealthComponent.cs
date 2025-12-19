using UnityEngine;

namespace TowerOffense.Combat
{
    public class HealthComponent
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ApplyDamage(int amount)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
