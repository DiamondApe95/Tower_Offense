using UnityEngine;

namespace TowerOffense.Saving
{
    public class JsonSaveSerializer
    {
        public string ToJson<T>(T obj)
        {
            return JsonUtility.ToJson(obj, true);
        }

        public T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
