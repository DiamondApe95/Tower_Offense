namespace TowerConquest.Data
{
    [System.Serializable]
    public class AbilityDefinition
    {
        public string id;
        public string name;
        public string civilization; // Which civilization this ability belongs to
        public string description;
        public float cooldown;
        public EffectDto[] effects;
        public string vfxPrefab;
        public string sfx;
        public int[] upgradeCosts; // Fame costs for upgrade levels

        [System.Serializable]
        public class EffectDto
        {
            public string type; // buff_armor, buff_damage, damage_aoe, heal, spawn_units, etc.
            public string target; // all_friendly_units, all_enemy_units, single_target, area, etc.
            public float value;
            public float duration; // For buffs/debuffs
            public float radius; // For area effects
            public string spawnUnitId; // For spawn effects
            public int spawnCount; // For spawn effects
        }
    }

    [System.Serializable]
    public class AbilitiesJsonRoot
    {
        public AbilityDefinition[] abilities;
    }
}
