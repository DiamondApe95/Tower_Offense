using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class HeroController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ActivateSkill()
        {
            Debug.Log("Stub method called.");
        }

    }
}
