using System.Collections.Generic;
using TowerOffense.Gameplay.Cards;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        public string levelId = "lvl_01_etruria_outpost";

        public DeckManager deck;
        public HandManager hand;
        public CardPlayResolver resolver;

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

            var testDeck = new List<string>
            {
                "unit_tank_legionary",
                "unit_swarm_auxilia",
                "spell_fire_pot",
                "unit_knight",
                "unit_archer",
                "spell_freeze",
                "unit_mage",
                "spell_heal"
            };

            deck = new DeckManager();
            hand = new HandManager { handSize = 5 };
            resolver = new CardPlayResolver();

            deck.Initialize(testDeck, 123);
            hand.FillToHandSize(deck);
            Run.handCardIds = new List<string>(hand.hand);

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
            if (hand == null || deck == null || resolver == null || hand.hand.Count == 0)
            {
                Debug.LogWarning("Cannot play first card - hand/deck/resolver not ready.");
                return;
            }

            string cardId = hand.hand[0];
            hand.PlayCard(cardId, deck, resolver);
            Run.handCardIds = new List<string>(hand.hand);
            Debug.Log($"Hand after play: {string.Join(", ", hand.hand)}");
        }
    }
}
