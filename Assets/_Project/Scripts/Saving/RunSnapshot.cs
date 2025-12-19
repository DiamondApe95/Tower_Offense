using System;
using System.Collections.Generic;

namespace TowerOffense.Saving
{
    [Serializable]
    public class RunSnapshot
    {
        public string levelId;
        public int waveIndex;
        public int maxWaves;
        public float speed;
        public List<string> drawPile = new();
        public List<string> discardPile = new();
        public List<string> hand = new();
        public bool isPlanning;
        public bool isAttacking;
        public bool isFinished;
        public bool isVictory;
    }
}
