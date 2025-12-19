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
                    UnityEngine.Debug.LogWarning("JsonLoader.LoadText called with empty path.");
                    return string.Empty;
                }

                if (!File.Exists(absolutePath))
                {
                    UnityEngine.Debug.LogWarning($"JsonLoader could not find file at {absolutePath}.");
                    return string.Empty;
                }

                UnityEngine.Debug.Log($"Loading text from {absolutePath}.");
                return File.ReadAllText(absolutePath);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"JsonLoader failed to load {absolutePath}. Exception: {exception.Message}");
                return string.Empty;
            }
        }
    }
}
