using System;
using TowerConquest.Debug;
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
                    Log.Warning("JsonLoader.LoadText called with empty path.");
                    return string.Empty;
                }

                if (!File.Exists(absolutePath))
                {
                    Log.Warning($"JsonLoader could not find file at {absolutePath}.");
                    return string.Empty;
                }

                Log.Info($"Loading text from {absolutePath}.");
                return File.ReadAllText(absolutePath);
            }
            catch (Exception exception)
            {
                Log.Warning($"JsonLoader failed to load {absolutePath}. Exception: {exception.Message}");
                return string.Empty;
            }
        }
    }
}
