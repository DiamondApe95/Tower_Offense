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
        [Header("Wave Controls")]
        public Button startWaveButton;
        public Text startWaveButtonText;
        public Button speedButton;
        public Text speedText;

        [Header("Info Display")]
        public Text waveText;
        public Text energyText;
        public Text autoStartTimerText;
        public Text baseHpText;

        [Header("Card System")]
        public Transform handContainer;
        public CardView cardViewPrefab;

        [Header("Visual Settings")]
        public Color startButtonActiveColor = new Color(0.2f, 0.6f, 0.2f);
        public Color startButtonDisabledColor = new Color(0.4f, 0.4f, 0.4f);
        public Color autoStartColor = new Color(0.6f, 0.5f, 0.2f);

        private LevelController level;
        private readonly CardHandView handView = new CardHandView();
        private Image startWaveButtonImage;

        public void Initialize(LevelController controller)
        {
            level = controller;
            handView.handContainer = handContainer;
            handView.cardViewPrefab = cardViewPrefab;

            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveAllListeners();
                startWaveButton.onClick.AddListener(OnStartWaveClicked);
                startWaveButtonImage = startWaveButton.GetComponent<Image>();

                // Finde Button-Text falls nicht zugewiesen
                if (startWaveButtonText == null)
                {
                    startWaveButtonText = startWaveButton.GetComponentInChildren<Text>();
                }
            }

            if (speedButton != null)
            {
                speedButton.onClick.RemoveAllListeners();
                speedButton.onClick.AddListener(OnSpeedClicked);
            }

            Refresh();
        }

        private void Update()
        {
            // Aktualisiere Autostart-Timer in Echtzeit
            if (level != null && level.IsAutoStartPending)
            {
                UpdateAutoStartDisplay();
            }
        }

        public void Refresh()
        {
            if (level == null || level.Run == null)
            {
                return;
            }

            RefreshWaveInfo();
            RefreshSpeedDisplay();
            RefreshEnergyDisplay();
            RefreshBaseHpDisplay();
            RefreshStartWaveButton();
            RefreshAutoStartDisplay();
            RefreshHandCards();
        }

        private void RefreshWaveInfo()
        {
            if (waveText != null)
            {
                int displayWave = level.Run.isPlanning
                    ? Mathf.Clamp(level.Run.waveIndex + 1, 1, level.Run.maxWaves)
                    : level.Run.waveIndex;
                waveText.text = $"Wave {displayWave}/{level.Run.maxWaves}";
            }
        }

        private void RefreshSpeedDisplay()
        {
            if (speedText != null)
            {
                float speedValue = level.speedController != null ? level.speedController.CurrentSpeed : 1f;
                speedText.text = $"{speedValue:0.#}x";
            }
        }

        private void RefreshEnergyDisplay()
        {
            if (energyText != null)
            {
                energyText.text = $"Energy {level.Run.energy}/{level.Run.maxEnergyPerWave}";
            }
        }

        private void RefreshBaseHpDisplay()
        {
            if (baseHpText != null)
            {
                float hp = level.BaseHp;
                baseHpText.text = $"Base HP: {hp:F0}";
            }
        }

        private void RefreshStartWaveButton()
        {
            if (startWaveButton == null) return;

            bool canStart = level.Run.isPlanning && !level.Run.isFinished;
            startWaveButton.interactable = canStart;

            // Button-Text aktualisieren
            if (startWaveButtonText != null)
            {
                if (level.Run.isFinished)
                {
                    startWaveButtonText.text = level.Run.isVictory ? "VICTORY" : "DEFEAT";
                }
                else if (level.Run.isAttacking)
                {
                    startWaveButtonText.text = "WAVE ACTIVE";
                }
                else if (level.IsAutoStartPending)
                {
                    float remaining = level.AutoStartTimeRemaining;
                    startWaveButtonText.text = $"AUTO ({remaining:F1}s)";
                }
                else
                {
                    int nextWave = level.Run.waveIndex + 1;
                    startWaveButtonText.text = $"START WAVE {nextWave}";
                }
            }

            // Button-Farbe aktualisieren
            if (startWaveButtonImage != null)
            {
                if (level.IsAutoStartPending)
                {
                    startWaveButtonImage.color = autoStartColor;
                }
                else if (canStart)
                {
                    startWaveButtonImage.color = startButtonActiveColor;
                }
                else
                {
                    startWaveButtonImage.color = startButtonDisabledColor;
                }
            }
        }

        private void RefreshAutoStartDisplay()
        {
            if (autoStartTimerText == null) return;

            if (level.IsAutoStartPending)
            {
                autoStartTimerText.gameObject.SetActive(true);
                float remaining = level.AutoStartTimeRemaining;
                autoStartTimerText.text = $"Auto-Start in {remaining:F1}s";
            }
            else
            {
                autoStartTimerText.gameObject.SetActive(false);
            }
        }

        private void UpdateAutoStartDisplay()
        {
            RefreshStartWaveButton();
            RefreshAutoStartDisplay();
        }

        private void RefreshHandCards()
        {
            List<CardViewModel> handCards = BuildHandModels();
            handView.Render(handCards, OnCardClicked);
        }

        private void OnStartWaveClicked()
        {
            if (level == null) return;

            // Wenn Autostart aktiv, abbrechen statt starten
            if (level.IsAutoStartPending)
            {
                level.CancelAutoStart();
                Refresh();
                return;
            }

            level.StartWave();
            Refresh();
        }

        private void OnSpeedClicked()
        {
            if (level == null) return;

            level.ToggleSpeed();
            Refresh();
        }

        private void OnCardClicked(string cardId)
        {
            if (level == null) return;

            level.PlayCard(cardId);
            Refresh();
        }

        private List<CardViewModel> BuildHandModels()
        {
            var models = new List<CardViewModel>();

            if (level?.hand == null && level?.Run?.handCardIds == null)
            {
                return models;
            }

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

        public void ShowMessage(string message, float duration = 2f)
        {
            Debug.Log($"HUD Message: {message}");
            // Hier könnte ein Toast-System implementiert werden
        }
    }
}
