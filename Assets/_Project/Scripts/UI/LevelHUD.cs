using UnityEngine;
using UnityEngine.UI;
using TowerOffense.Gameplay;

namespace TowerOffense.UI
{
    public class LevelHUD : MonoBehaviour
    {
        public Button startWaveButton;
        public Button speedButton;
        public Transform handContainer;
        public CardView cardViewPrefab;
        public Text waveText;
        public Text speedText;

        private LevelController level;
        private CardHandView handView;

        public void Initialize(LevelController level)
        {
            this.level = level;
            handView = new CardHandView(handContainer, cardViewPrefab);

            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveAllListeners();
                startWaveButton.onClick.AddListener(() =>
                {
                    this.level?.StartWave();
                    Refresh();
                });
            }

            if (speedButton != null)
            {
                speedButton.onClick.RemoveAllListeners();
                speedButton.onClick.AddListener(() =>
                {
                    this.level?.ToggleSpeed();
                    Refresh();
                });
            }

            Refresh();
        }

        public void Refresh()
        {
            if (level == null)
            {
                return;
            }

            if (waveText != null)
            {
                int waveIndex = level.Run?.waveIndex ?? 0;
                int maxWaves = level.Run?.maxWaves ?? 0;
                waveText.text = $"Wave {waveIndex}/{maxWaves}";
            }

            if (speedText != null)
            {
                speedText.text = $"{level.CurrentSpeed:0.##}x";
            }

            handView?.Render(level.Hand, cardId =>
            {
                level.PlayCard(cardId);
                Refresh();
            });
        }
    }
}

// Setup-Anleitung:
// 1. Erstelle ein Canvas mit Buttons, Texten und einem HandContainer (z.B. Horizontal Layout).
// 2. Erstelle ein CardView Prefab mit Button + Text und hänge CardView.cs daran.
// 3. Hänge LevelHUD.cs an ein HUD-GameObject und setze Referenzen für Buttons, Texte, HandContainer und CardView Prefab.
// 4. Rufe LevelHUD.Initialize(level) beim Start des Levels auf.
