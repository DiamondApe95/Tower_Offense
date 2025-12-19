using System.Collections.Generic;

namespace TowerConquest.Gameplay
{
    public class RunState
    {
        public string levelId;
        public int waveIndex;
        public int maxWaves;
        public float speed = 1f;
        public int energy;
        public int maxEnergyPerWave = 10;
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
        public GameMode gameMode = GameMode.Offense;
        public bool allowMidWaveSpawns;
    }
}
