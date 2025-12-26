using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Combat
{
    public class ArmorSystem
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public int Mitigate(int damage)
        {
            Log.Info("Mitigating damage.");
            return damage;
        }

    }
}
