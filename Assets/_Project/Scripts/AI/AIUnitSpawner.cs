using UnityEngine;
using TowerConquest.Debug;
using TowerConquest.Gameplay;
using TowerConquest.Data;

namespace TowerConquest.AI
{
    /// <summary>
    /// Handles unit spawning for AI
    /// </summary>
    public class AIUnitSpawner
    {
        private AICommander commander;
        private GoldManager goldManager;
        private JsonDatabase database;
        private Transform aiBase;

        private CostSystem costSystem;
        private LiveBattleSpawnController liveBattleSpawner;

        public void Initialize(AICommander cmd, GoldManager gold, JsonDatabase db, Transform baseTransform)
        {
            commander = cmd;
            goldManager = gold;
            database = db;
            aiBase = baseTransform;

            costSystem = new CostSystem(database);
        }

        public void SetLiveBattleSpawner(LiveBattleSpawnController spawner)
        {
            liveBattleSpawner = spawner;
        }

        /// <summary>
        /// Attempt to spawn a unit (for AI)
        /// Note: AI bypasses normal cooldown system - uses decision timing instead
        /// </summary>
        public bool TrySpawnUnit(string unitId)
        {
            var unitDef = database.GetUnit(unitId);
            if (unitDef == null)
            {
                Log.Warning($"[AIUnitSpawner] Unit not found: {unitId}");
                return false;
            }

            // Check if we can afford it
            if (!costSystem.CanAffordUnit(goldManager, unitId))
            {
                return false;
            }

            // Find slot index in AI deck for this unit
            int slotIndex = FindUnitSlotIndex(unitId);
            if (slotIndex < 0)
            {
                Log.Warning($"[AIUnitSpawner] Unit {unitId} not in AI deck");
                return false;
            }

            // Use spawner to spawn unit (handles gold, cooldown, and instantiation)
            if (liveBattleSpawner != null && liveBattleSpawner.TrySpawnUnit(slotIndex))
            {
                Log.Info($"[AIUnitSpawner] Spawned {unitId} from slot {slotIndex}");
                return true;
            }

            Log.Warning($"[AIUnitSpawner] Failed to spawn {unitId}");
            return false;
        }

        /// <summary>
        /// Find the slot index of a unit in the AI deck
        /// </summary>
        private int FindUnitSlotIndex(string unitId)
        {
            var deck = commander.GetDeck();
            if (deck == null || deck.SelectedUnits == null) return -1;

            for (int i = 0; i < deck.SelectedUnits.Count; i++)
            {
                if (deck.SelectedUnits[i] == unitId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
