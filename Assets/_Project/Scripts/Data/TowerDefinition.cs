namespace TowerConquest.Data
{
    [System.Serializable]
    public class TowerDefinition
    {
        public string id;
        public string display_name;
        public string category;
        public string[] tags;

        // NEW: Gold, Civilization & Construction
        public string civilization; // Which civilization this tower belongs to
        public int goldCost; // Cost to build tower
        public int goldReward; // Gold rewarded for destroying this tower
        public float constructionTime; // Time for builders to construct
        public int requiredBuilders; // Number of builders needed

        public TierDto[] tiers; // DEPRECATED - will use upgradeLevels instead

        // NEW: Base Stats & Upgrade System
        public StatsDto baseStats;
        public AttackDto baseAttack;
        public UpgradeLevel[] upgradeLevels;
        public string prefabPath;
        public string constructionSitePrefabPath;

        [System.Serializable]
        public class TierDto
        {
            public int tier;
            public StatsDto stats;
            public AttackDto attack;
            public EffectDto[] effects;
        }

        [System.Serializable]
        public class StatsDto
        {
            public float hp;
            public float armor;
            public float constructionHP; // HP of construction site before tower is built
        }

        [System.Serializable]
        public class AttackDto
        {
            public string damage_type;
            public float base_damage;
            public float attacks_per_second;
            public float range;
            public float splash_radius;
            public TargetingDto targeting;
        }

        [System.Serializable]
        public class TargetingDto
        {
            public string priority;
            public bool can_target_air;
        }

        [System.Serializable]
        public class EffectDto
        {
            public string effect_type;
            public string damage_type;
            public float value;
            public string stat;
            public string mode;
            public float duration_seconds;
            public AreaDto area;
            public StatusDto status;
        }

        [System.Serializable]
        public class AreaDto
        {
            public string shape;
            public float radius;
            public float angle_degrees;
        }

        [System.Serializable]
        public class StatusDto
        {
            public string apply;
            public float chance;
            public float duration_seconds;
            public float slow_percent;
            public float tick_damage;
            public float tick_interval_seconds;
        }
    }
}
