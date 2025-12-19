using UnityEngine;
using Object = UnityEngine.Object;

namespace TowerOffense.Data
{
    public class PrefabRegistry
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Register(string key, Object prefab)
        {
            Debug.Log("Stub method called.");
        }

        public Object Get(string key)
        {
            Debug.Log($"Fetching prefab {key}.");
            return null;
        }

    }
}
