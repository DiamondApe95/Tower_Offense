using UnityEngine;

namespace TowerOffense.Data
{
    public class JsonLoader
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public string LoadText(string path)
        {
            UnityEngine.Debug.Log($"Loading text from {path}.");
            return string.Empty;
        }

    }
}
