using UnityEngine;

namespace TowerOffense.Core
{
    public class ServiceLocator
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Register<T>(T service)
        {
            Debug.Log("Stub method called.");
        }

        public T Resolve<T>()
        {
            Debug.Log("Resolving service.");
            return default;
        }

    }
}
