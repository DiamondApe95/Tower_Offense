using UnityEngine;

namespace TowerOffense.Core
{
    public class EventBus
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Publish(string eventName)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public void Subscribe(string eventName)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
