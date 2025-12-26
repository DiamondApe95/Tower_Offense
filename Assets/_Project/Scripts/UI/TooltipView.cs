using UnityEngine;
using TowerConquest.Debug;

namespace TowerConquest.UI
{
    public class TooltipView
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ShowTooltip(string text)
        {
            Log.Info("Stub method called.");
        }

    }
}
