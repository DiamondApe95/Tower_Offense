using UnityEngine;

namespace TowerConquest.Debug
{
    public static class Log
    {
        public static void Info(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void Warning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void Error(object message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
