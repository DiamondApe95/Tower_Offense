using System;
using System.IO;
using UnityEngine;

namespace TowerConquest.Data
{
    public class JsonLoader
    {
        public string LoadText(string absolutePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(absolutePath))
                {
                    Debug.LogWarning("JsonLoader.LoadText called with empty path.");
                    return string.Empty;
                }

                if (!File.Exists(absolutePath))
                {
                    Debug.LogWarning($"JsonLoader could not find file at {absolutePath}.");
                    return string.Empty;
                }

                Debug.Log($"Loading text from {absolutePath}.");
                return File.ReadAllText(absolutePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"JsonLoader failed to load {absolutePath}. Exception: {exception.Message}");
                return string.Empty;
            }
        }
    }
}
