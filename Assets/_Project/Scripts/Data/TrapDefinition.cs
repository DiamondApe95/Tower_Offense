using UnityEngine;

namespace TowerOffense.Data
{
    public class TrapDefinition
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void LogSummary()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
