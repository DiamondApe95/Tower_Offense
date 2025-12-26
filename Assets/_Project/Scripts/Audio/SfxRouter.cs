using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Audio
{
    public class SfxRouter
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Route(string sfxId)
        {
            Log.Info("Stub method called.");
        }

    }
}
