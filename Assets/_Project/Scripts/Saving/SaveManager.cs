using UnityEngine;

namespace TowerOffense.Saving
{
    public class SaveManager
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Save()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public void Load()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
