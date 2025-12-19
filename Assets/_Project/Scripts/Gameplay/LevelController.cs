using TowerOffense.Core;
using TowerOffense.Data;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";

        public RunState Run { get; private set; }
        public LevelStateMachine Fsm { get; private set; }
        public WaveController Waves { get; private set; }
        public PathManager Paths { get; private set; }
        public SpawnController Spawns { get; private set; }

        private LevelDefinition levelDefinition;

        private void Start()
        {
            Run = new RunState
            {
                levelId = levelId,
                maxWaves = 5
            };

            JsonDatabase database = ServiceLocator.Get<JsonDatabase>();
            levelDefinition = database.FindLevel(levelId);

            Paths = new PathManager();
            Paths.InitializeFromLevel(levelDefinition);

            Spawns = new SpawnController();
            Spawns.Initialize(levelDefinition, Paths);

            Fsm = new LevelStateMachine();

            Waves = GetComponent<WaveController>();
            if (Waves == null)
            {
                Waves = gameObject.AddComponent<WaveController>();
            }

            Fsm.EnterPlanning(Run);
        }

        [ContextMenu("Debug Spawn Unit (unit_tank_legionary)")]
        private void DebugSpawnUnitContextMenu()
        {
            DebugSpawnUnit("unit_tank_legionary");
        }

        public void DebugSpawnUnit(string unitId)
        {
            if (Spawns == null)
            {
                Debug.LogWarning("Spawn controller is not initialized.");
                return;
            }

            Spawns.SpawnUnit(unitId);
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
    }
}
