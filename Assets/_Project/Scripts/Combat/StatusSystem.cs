using UnityEngine;

namespace TowerOffense.Combat
{
    public class StatusSystem
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ApplyStatus(string statusId)
        {
            Debug.Log("Stub method called.");
        }

    }
}
