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
        public List<string> flags = new();
    }
}
