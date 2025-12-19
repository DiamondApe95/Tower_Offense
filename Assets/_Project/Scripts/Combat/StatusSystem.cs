using UnityEngine;

namespace TowerOffense.Combat
{
    public class StatusSystem
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ApplyStatus(string statusId)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
