using System.Collections.Generic;
using TowerOffense.Gameplay.Cards;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";

        public RunState Run { get; private set; }
        public LevelStateMachine Fsm { get; private set; }
        public WaveController Waves { get; private set; }
        public DeckManager deck;
        public HandManager hand;
        public CardPlayResolver resolver;

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

            deck = new DeckManager();
            hand = new HandManager();
            resolver = new CardPlayResolver();

            List<string> testDeck = new()
            {
                "unit_tank_legionary",
                "unit_swarm_auxilia",
                "spell_fire_pot",
                "unit_tank_legionary",
                "unit_swarm_auxilia",
                "spell_fire_pot",
                "unit_tank_legionary",
                "unit_swarm_auxilia"
            };

            deck.Initialize(testDeck, seed: 123);
            hand.handSize = 5;
            hand.FillToHandSize(deck);
            Run.handCardIds = new List<string>(hand.hand);

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

        [ContextMenu("DEBUG Play First Card")]
        private void DebugPlayFirstCard()
        {
            if (hand == null || hand.hand.Count == 0)
            {
                Debug.LogWarning("No cards in hand to play.");
                return;
            }

            string cardId = hand.hand[0];
            resolver?.Resolve(cardId);
            hand.hand.RemoveAt(0);
            hand.FillToHandSize(deck);
            Run.handCardIds = new List<string>(hand.hand);
            Debug.Log($"New hand: {string.Join(", ", hand.hand)}");
        }
    }
}
