using UnityEngine;

namespace TowerOffense.UI
{
    public class TooltipView
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ShowTooltip(string text)
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
