using System.Collections.Generic;
using UnityEngine;
using TowerOffense.Gameplay.Cards;

namespace TowerOffense.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";

        public RunState Run { get; private set; }
        public LevelStateMachine Fsm { get; private set; }
        public WaveController Waves { get; private set; }

        private readonly SpeedController speedController = new SpeedController();
        private readonly DeckManager deckManager = new DeckManager();
        private readonly HandManager handManager = new HandManager();
        private readonly CardPlayResolver cardPlayResolver = new CardPlayResolver();

        public IReadOnlyList<string> Hand => handManager.hand;
        public float CurrentSpeed => speedController.CurrentSpeed;

        private void Start()
        {
            Run = new RunState
            {
                levelId = levelId,
                maxWaves = 5
            };

            Run.speed = speedController.CurrentSpeed;
            handManager.hand = Run.handCardIds;

            Fsm = new LevelStateMachine();

            Waves = GetComponent<WaveController>();
            if (Waves == null)
            {
                Waves = gameObject.AddComponent<WaveController>();
            }

            Fsm.EnterPlanning(Run);
        }

        public void StartWave()
        {
            Fsm.StartWave(Run);
            Waves.StartWave(this);
        }

        public void ToggleSpeed()
        {
            speedController.Toggle();
            if (Run != null)
            {
                Run.speed = speedController.CurrentSpeed;
            }
        }

        public void PlayCard(string cardId)
        {
            handManager.PlayCard(cardId, deckManager, cardPlayResolver);
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
