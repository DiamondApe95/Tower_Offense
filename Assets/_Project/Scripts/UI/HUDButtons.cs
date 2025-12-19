using UnityEngine;

public class HUDButtons : MonoBehaviour
{
    public void OnStartWaveClicked()
    {
        Debug.Log("HUD: Start Wave clicked");
    }

    public void OnPauseClicked()
    {
        Debug.Log("HUD: Pause clicked");
    }

    public void OnSpeedx1Clicked()
    {
        Debug.Log("HUD: Speed x1 clicked");
    }

    public void OnSpeedx2Clicked()
    {
        Debug.Log("HUD: Speed x2 clicked");
    }
}
