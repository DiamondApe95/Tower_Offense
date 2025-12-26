using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.Debug
{
    public class GizmoDrawer
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void DrawGizmos()
        {
            Log.Info("Stub method called.");
        }

    }
}
