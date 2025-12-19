using UnityEngine;
using TowerOffense.Combat;

namespace TowerOffense.Gameplay.Entities
{
    public class UnitAttack
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Attack(TargetingSystem target)
        {
            Debug.Log("Stub method called.");
        }

    }
}
