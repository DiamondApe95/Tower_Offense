using UnityEngine;

namespace TowerOffense.Core
{
    public class ServiceLocator
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Register<T>(T service)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public T Resolve<T>()
        {
            UnityEngine.Debug.Log("Resolving service.");
            return default;
        }

    }
}
