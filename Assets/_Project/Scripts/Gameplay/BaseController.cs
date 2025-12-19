using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class BaseController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ApplyDamage(int amount)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
