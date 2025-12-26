using UnityEngine;
using TowerConquest.Debug;
using TowerConquest.Combat;

namespace TowerConquest.Gameplay.Entities
{
    public class UnitAttack
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Attack(TargetingSystem target)
        {
            Log.Info("Stub method called.");
        }

    }
}
