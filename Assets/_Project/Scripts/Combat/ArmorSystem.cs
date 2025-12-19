using UnityEngine;

namespace TowerOffense.Combat
{
    public class ArmorSystem
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public int Mitigate(int damage)
        {
            Debug.Log("Mitigating damage.");
            return damage;
        }

    }
}
