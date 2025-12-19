using UnityEngine;

namespace TowerOffense.Data
{
    public class HeroDefinition
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void LogSummary()
        {
            Debug.Log("Stub method called.");
        }

    }
}
