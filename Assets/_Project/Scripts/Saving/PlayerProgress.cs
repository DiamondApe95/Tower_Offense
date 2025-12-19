using System;
using System.Collections.Generic;

namespace TowerConquest.Saving
{
    [Serializable]
    public class PlayerProgress
    {
        public List<string> unlockedLevelIds = new();
        public List<string> completedLevelIds = new();
        public string lastSelectedLevelId;
    }
}
