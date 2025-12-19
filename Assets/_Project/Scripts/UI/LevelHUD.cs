using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerConquest.Gameplay;
using TowerConquest.Gameplay.Cards;
using TowerConquest.Core;
using TowerConquest.Data;

namespace TowerConquest.UI
{
    public class LevelHUD : MonoBehaviour
    {
        public Button startWaveButton;
        public Button speedButton;
        public Transform handContainer;
        public CardView cardViewPrefab;
        public Text waveText;
        public Text speedText;
        public Text energyText;

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

            if (energyText != null)
            {
                energyText.text = $"Energy {level.Run.energy}/{level.Run.maxEnergyPerWave}";
            }

            List<CardViewModel> handCards = BuildHandModels();
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

        private List<CardViewModel> BuildHandModels()
        {
            var models = new List<CardViewModel>();
            List<string> handCards = level.hand != null ? level.hand.hand : level.Run.handCardIds;
            if (handCards == null)
            {
                return models;
            }

            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            foreach (string cardId in handCards)
            {
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    continue;
                }

                var model = new CardViewModel
                {
                    cardId = cardId,
                    displayName = cardId,
                    cost = 0,
                    group = "unknown",
                    isPlayable = true
                };

                if (cardId.StartsWith("unit_"))
                {
                    UnitDefinition unit = database.FindUnit(cardId);
                    model.displayName = unit?.display_name ?? cardId;
                    model.cost = unit?.card?.cost ?? 0;
                    model.group = unit?.card?.hand_group ?? "units";
                }
                else if (cardId.StartsWith("spell_"))
                {
                    SpellDefinition spell = database.FindSpell(cardId);
                    model.displayName = spell?.display_name ?? cardId;
                    model.cost = spell?.card?.cost ?? 0;
                    model.group = spell?.card?.hand_group ?? "spells";
                }

                model.isPlayable = level.Run.energy >= model.cost && level.CanPlayCard(cardId);
                models.Add(model);
            }

            return models;
        }
    }
}

// Setup-Anleitung:
// 1. Canvas erstellen und Buttons/Text/HandContainer hinzufügen.
// 2. CardView Prefab mit Button + Label-Text anlegen.
// 3. LevelHUD-Komponente hinzufügen und Referenzen (Buttons, Texts, HandContainer, CardView Prefab) setzen.
