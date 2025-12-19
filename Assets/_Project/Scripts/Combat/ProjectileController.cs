using UnityEngine;

namespace TowerConquest.Combat
{
    public class ProjectileController
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Launch(Vector3 target)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
