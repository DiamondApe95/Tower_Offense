using UnityEngine;
using TowerConquest.Debug;

public class HUDButtons : MonoBehaviour
{
    public void OnStartWaveClicked()
    {
        Log.Info("HUD: Start Wave clicked");
    }

    public void OnPauseClicked()
    {
        Log.Info("HUD: Pause clicked");
    }

    public void OnSpeedx1Clicked()
    {
        Log.Info("HUD: Speed x1 clicked");
    }

    public void OnSpeedx2Clicked()
    {
        Log.Info("HUD: Speed x2 clicked");
    }
}
