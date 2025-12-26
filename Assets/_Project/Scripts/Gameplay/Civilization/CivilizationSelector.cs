using UnityEngine;
using TowerConquest.Debug;
using TowerConquest.Data;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Handles civilization and unit selection before a level
    /// </summary>
    public class CivilizationSelector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CivilizationManager civilizationManager;

        [Header("Current Selection")]
        [SerializeField] private UnitDeck playerDeck;

        private string levelCivilizationId;

        private void Awake()
        {
            if (playerDeck == null)
            {
                playerDeck = new UnitDeck();
            }
        }

        public void Initialize(CivilizationManager civManager)
        {
            civilizationManager = civManager;
        }

        /// <summary>
        /// Start selection process for a level
        /// </summary>
        public void StartSelection(string civId)
        {
            levelCivilizationId = civId;
            playerDeck = new UnitDeck();
            playerDeck.SetCivilization(civId);

            Log.Info($"[CivilizationSelector] Started selection for civilization: {civId}");
        }

        /// <summary>
        /// Add unit to deck
        /// </summary>
        public bool SelectUnit(string unitId)
        {
            return playerDeck.AddUnit(unitId);
        }

        /// <summary>
        /// Remove unit from deck
        /// </summary>
        public bool DeselectUnit(string unitId)
        {
            return playerDeck.RemoveUnit(unitId);
        }

        /// <summary>
        /// Select hero
        /// </summary>
        public void SelectHero(string heroId)
        {
            playerDeck.SetHero(heroId);
        }

        /// <summary>
        /// Check if selection is complete and valid
        /// </summary>
        public bool IsSelectionComplete()
        {
            return playerDeck.IsValid();
        }

        /// <summary>
        /// Get the current deck
        /// </summary>
        public UnitDeck GetDeck()
        {
            return playerDeck;
        }

        /// <summary>
        /// Get available units for current civilization
        /// </summary>
        public System.Collections.Generic.List<UnitDefinition> GetAvailableUnits()
        {
            if (civilizationManager == null || string.IsNullOrEmpty(levelCivilizationId))
            {
                return new System.Collections.Generic.List<UnitDefinition>();
            }

            return civilizationManager.GetAvailableUnits(levelCivilizationId);
        }

        /// <summary>
        /// Get available heroes for current civilization
        /// </summary>
        public System.Collections.Generic.List<HeroDefinition> GetAvailableHeroes()
        {
            if (civilizationManager == null || string.IsNullOrEmpty(levelCivilizationId))
            {
                return new System.Collections.Generic.List<HeroDefinition>();
            }

            return civilizationManager.GetAvailableHeroes(levelCivilizationId);
        }

        /// <summary>
        /// Confirm selection and proceed to level
        /// </summary>
        public void ConfirmSelection()
        {
            if (!IsSelectionComplete())
            {
                Log.Warning("[CivilizationSelector] Cannot confirm incomplete selection");
                return;
            }

            Log.Info($"[CivilizationSelector] Selection confirmed: {playerDeck}");
            // Proceed to level loading
        }

        #if UNITY_EDITOR
        [ContextMenu("Print Deck")]
        private void DebugPrintDeck()
        {
            Log.Info(playerDeck.ToString());
        }
        #endif
    }
}
