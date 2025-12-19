using System;
using System.Collections.Generic;

namespace TowerOffense.Saving
{
    [Serializable]
    public class PlayerProgress
    {
        public List<string> unlockedLevelIds = new();
        public List<string> completedLevelIds = new();
        public string lastSelectedLevelId;
    }
}
