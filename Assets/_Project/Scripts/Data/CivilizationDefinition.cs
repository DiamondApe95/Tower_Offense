using System.Collections.Generic;

namespace TowerConquest.Data
{
    [System.Serializable]
    public class CivilizationDefinition
    {
        public string id;
        public string name;
        public string description;
        public string color; // Hex color code
        public string[] availableUnits; // Unit IDs
        public string[] availableTowers; // Tower IDs
        public string[] availableHeroes; // Hero IDs
        public string specialAbility; // Ability ID
        public int unlockCost; // Fame cost to unlock (0 = default unlocked)
        public string prefabPath;
        public string iconPath;
        public bool isDefault; // Whether this is the default civilization
    }
}
