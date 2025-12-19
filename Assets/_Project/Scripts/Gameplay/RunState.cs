using System.Collections.Generic;

namespace TowerOffense.Gameplay
{
    public class RunState
    {
        public string levelId;
        public int waveIndex;
        public int maxWaves;
        public float speed = 1f;
        public List<string> deckUnitIds = new();
        public List<string> deckSpellIds = new();
        public List<string> handCardIds = new();
        public bool isPlanning;
        public bool isAttacking;
        public bool isFinished;
        public bool isVictory;
        public int heroEveryNWaves = 5;
        public bool heroAvailableThisWave;
        public string selectedHeroId;
    }
}
