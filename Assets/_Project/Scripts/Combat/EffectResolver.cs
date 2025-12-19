using UnityEngine;

namespace TowerOffense.Combat
{
    public class EffectResolver
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ResolveEffect(string effectId)
        {
            Debug.Log("Stub method called.");
        }

    }
}
