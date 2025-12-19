using TowerOffense.Core;
using TowerOffense.Data;
using TowerOffense.Gameplay.Entities;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";

        public RunState Run { get; private set; }
        public LevelStateMachine Fsm { get; private set; }
        public WaveController Waves { get; private set; }

        private void Start()
        {
            Run = new RunState
            {
                levelId = levelId,
                maxWaves = 5
            };

            Fsm = new LevelStateMachine();

            Waves = GetComponent<WaveController>();
            if (Waves == null)
            {
                Waves = gameObject.AddComponent<WaveController>();
            }

            SpawnEnemyTowers();
            Fsm.EnterPlanning(Run);
        }

        public void StartWave()
        {
            Fsm.StartWave(Run);
            Waves.StartWave(this);
        }

        public void OnWaveSimulatedEnd()
        {
            Fsm.EndWave(Run);
            if (Run.waveIndex >= Run.maxWaves)
            {
                Fsm.Finish(Run, victory: true);
            }
        }

        private void SpawnEnemyTowers()
        {
            if (!ServiceLocator.TryGet(out JsonDatabase database))
            {
                Debug.LogWarning("LevelController could not find JsonDatabase.");
                return;
            }

            LevelDefinition level = database.FindLevel(levelId);
            if (level == null)
            {
                return;
            }

            LevelDefinition.TowerPlacementDto[] towers = level.enemy_defenses?.towers;
            if (towers == null || towers.Length == 0)
            {
                return;
            }

            foreach (LevelDefinition.TowerPlacementDto placement in towers)
            {
                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = $"Tower_{placement.instance_id}";
                tower.transform.position = new Vector3(
                    placement.position.x,
                    placement.position.y,
                    placement.position.z);
                tower.transform.rotation = Quaternion.Euler(0f, placement.rotation_y_degrees, 0f);

                TowerController controller = tower.AddComponent<TowerController>();
                ApplyTowerStats(database, placement, controller);
            }
        }

        private static void ApplyTowerStats(JsonDatabase database, LevelDefinition.TowerPlacementDto placement, TowerController controller)
        {
            TowerDefinition towerDefinition = database.FindTower(placement.tower_id);
            if (towerDefinition == null || towerDefinition.tiers == null || towerDefinition.tiers.Length == 0)
            {
                return;
            }

            TowerDefinition.TierDto tier = null;
            foreach (TowerDefinition.TierDto candidate in towerDefinition.tiers)
            {
                if (candidate.tier == placement.tier)
                {
                    tier = candidate;
                    break;
                }
            }

            tier ??= towerDefinition.tiers[0];
            if (tier.attack == null)
            {
                return;
            }

            controller.Initialize(tier.attack.range, tier.attack.base_damage, tier.attack.attacks_per_second);
        }
    }
}
