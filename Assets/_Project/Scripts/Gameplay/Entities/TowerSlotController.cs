using TowerConquest.Core;
using TowerConquest.Data;
using UnityEngine;

namespace TowerConquest.Gameplay.Entities
{
    public class TowerSlotController : MonoBehaviour
    {
        private LevelController levelController;
        private string towerId;
        private int tier;
        private bool isOccupied;

        public void Initialize(LevelController controller, string towerDefinitionId, int towerTier)
        {
            levelController = controller;
            towerId = towerDefinitionId;
            tier = towerTier;
            isOccupied = false;
        }

        private void OnMouseDown()
        {
            if (levelController == null || levelController.Run == null)
            {
                UnityEngine.Debug.LogWarning("TowerSlotController: No LevelController assigned.");
                return;
            }

            if (levelController.Run.gameMode != GameMode.Defense || !levelController.Run.isPlanning)
            {
                UnityEngine.Debug.Log("TowerSlotController: Not in build phase.");
                return;
            }

            if (isOccupied)
            {
                UnityEngine.Debug.Log("TowerSlotController: Slot already occupied.");
                return;
            }

            PlaceTower();
        }

        private void PlaceTower()
        {
            if (string.IsNullOrWhiteSpace(towerId))
            {
                UnityEngine.Debug.LogWarning("TowerSlotController: No tower id configured.");
                return;
            }

            GameObject towerObject = null;
            if (ServiceLocator.TryGet(out PrefabRegistry registry))
            {
                towerObject = registry.CreateOrFallback(towerId);
            }

            if (towerObject == null)
            {
                towerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerObject.name = $"{towerId}_Fallback";
            }

            towerObject.transform.position = transform.position;
            towerObject.transform.rotation = transform.rotation;

            TowerController towerController = towerObject.GetComponent<TowerController>();
            if (towerController == null)
            {
                towerController = towerObject.AddComponent<TowerController>();
            }

            ConfigureTower(towerController, towerId, tier);
            isOccupied = true;
            UnityEngine.Debug.Log($"TowerSlotController: Placed tower '{towerId}' at slot.");
        }

        private static void ConfigureTower(TowerController towerController, string towerDefinitionId, int tier)
        {
            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            TowerDefinition towerDefinition = database.FindTower(towerDefinitionId);
            TowerDefinition.TierDto tierDefinition = GetTowerTier(towerDefinition, tier);

            if (tierDefinition?.attack != null)
            {
                towerController.range = tierDefinition.attack.range > 0f ? tierDefinition.attack.range : towerController.range;
                towerController.damage = tierDefinition.attack.base_damage > 0f ? tierDefinition.attack.base_damage : towerController.damage;
                towerController.attacksPerSecond = tierDefinition.attack.attacks_per_second > 0f ? tierDefinition.attack.attacks_per_second : towerController.attacksPerSecond;
            }

            towerController.effects = tierDefinition?.effects;
            towerController.UpdateDpsCache();
        }

        private static TowerDefinition.TierDto GetTowerTier(TowerDefinition towerDefinition, int requestedTier)
        {
            if (towerDefinition == null || towerDefinition.tiers == null || towerDefinition.tiers.Length == 0)
            {
                return null;
            }

            foreach (TowerDefinition.TierDto tier in towerDefinition.tiers)
            {
                if (tier != null && tier.tier == requestedTier)
                {
                    return tier;
                }
            }

            return towerDefinition.tiers[0];
        }
    }
}
