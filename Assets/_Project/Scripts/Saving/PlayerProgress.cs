using UnityEngine;

namespace TowerOffense.Saving
{
    public class PlayerProgress
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Reset()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
