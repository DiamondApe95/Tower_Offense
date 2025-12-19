using TMPro;
using UnityEngine;

public class HUDView : MonoBehaviour
{
    public TMP_Text goldText;
    public TMP_Text livesText;
    public TMP_Text waveText;
    public TMP_Text phaseText;

    // Test: Wenn du Play drückst, werden diese Werte gesetzt
    private void Start()
    {
        SetGold(100);
        SetLives(20);
        SetWave(1, 10);
        SetPhase("Planning");
    }

    public void SetGold(int value) => goldText.text = $"Gold: {value}";
    public void SetLives(int value) => livesText.text = $"Lives: {value}";
    public void SetWave(int current, int total) => waveText.text = $"Wave: {current}/{total}";
    public void SetPhase(string phase) => phaseText.text = $"Phase: {phase}";
}
