using UnityEngine;

namespace TowerOffense.Data
{
    public class JsonDatabase
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void LoadAll()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
