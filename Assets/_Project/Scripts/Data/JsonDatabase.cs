using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TowerOffense.Data
{
    public class JsonDatabase
    {
        private readonly Dictionary<string, string> jsonByName = new Dictionary<string, string>();
        private readonly DataValidator validator = new DataValidator();

        public void LoadAll()
        {
            jsonByName.Clear();

            string directory = Path.Combine(Application.streamingAssetsPath, "Data", "JSON");
            if (!Directory.Exists(directory))
            {
                Debug.LogWarning($"JSON directory not found at {directory}.");
                return;
            }

            string[] files = Directory.GetFiles(directory, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                jsonByName[fileName] = File.ReadAllText(filePath);
            }

            Debug.Log($"Loaded {jsonByName.Count} JSON files from {directory}.");

            if (!validator.Validate())
            {
                Debug.LogError("JSON data validation failed.");
            }
        }
    }
}
