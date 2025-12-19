using UnityEngine;

namespace TowerOffense.Combat
{
    public class AreaEffect
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Detonate()
        {
            Debug.Log("Stub method called.");
        }

    }
}
