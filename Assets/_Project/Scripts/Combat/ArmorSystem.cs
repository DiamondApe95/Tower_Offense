using UnityEngine;

namespace TowerConquest.Combat
{
    public class ArmorSystem
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public int Mitigate(int damage)
        {
            UnityEngine.Debug.Log("Mitigating damage.");
            return damage;
        }

    }
}
