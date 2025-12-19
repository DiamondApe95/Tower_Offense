using System.Collections.Generic;
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
        private readonly CardHandView handView = new CardHandView();

        public void Initialize(LevelController level)
        {
            this.level = level;
            handView.handContainer = handContainer;
            handView.cardViewPrefab = cardViewPrefab;

            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveAllListeners();
                startWaveButton.onClick.AddListener(OnStartWaveClicked);
            }

            if (speedButton != null)
            {
                speedButton.onClick.RemoveAllListeners();
                speedButton.onClick.AddListener(OnSpeedClicked);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (level == null || level.Run == null)
            {
                return;
            }

            if (waveText != null)
            {
                int currentWave = Mathf.Clamp(level.Run.waveIndex + 1, 1, level.Run.maxWaves);
                waveText.text = $"Wave {currentWave}/{level.Run.maxWaves}";
            }

            if (speedText != null)
            {
                float speedValue = level.speedController != null ? level.speedController.CurrentSpeed : 1f;
                speedText.text = $"{speedValue:0.#}x";
            }

            List<string> handCards = level.hand != null ? level.hand.hand : level.Run.handCardIds;
            handView.Render(handCards, OnCardClicked);
        }

        private void OnStartWaveClicked()
        {
            if (level == null)
            {
                return;
            }

            level.StartWave();
            Refresh();
        }

        private void OnSpeedClicked()
        {
            if (level == null)
            {
                return;
            }

            level.ToggleSpeed();
            Refresh();
        }

        private void OnCardClicked(string cardId)
        {
            if (level == null)
            {
                return;
            }

            level.PlayCard(cardId);
            Refresh();
        }
    }
}

// Setup-Anleitung:
// 1. Canvas erstellen und Buttons/Text/HandContainer hinzufügen.
// 2. CardView Prefab mit Button + Label-Text anlegen.
// 3. LevelHUD-Komponente hinzufügen und Referenzen (Buttons, Texts, HandContainer, CardView Prefab) setzen.
