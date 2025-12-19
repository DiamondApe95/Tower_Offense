namespace TowerConquest.Data
{
    [System.Serializable]
    public class HeroDefinition
    {
        public string id;
        public string display_name;
        public string role;
        public string rarity;
        public string[] tags;
        public StatsDto stats;
        public AttackDto attack;
        public AbilityDto[] abilities;

        [System.Serializable]
        public class StatsDto
        {
            public float hp;
            public float armor;
            public float move_speed;
            public float size;
        }

        [System.Serializable]
        public class AttackDto
        {
            public bool enabled;
            public string attack_type;
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
        public class AbilityDto
        {
            public string id;
            public string type;
            public string trigger;
            public float cooldown_seconds;
            public float interval_seconds;
            public EffectDto[] effects;
        }

        [System.Serializable]
        public class EffectDto
        {
            public string effect_type;
            public string damage_type;
            public string stat;
            public string mode;
            public float value;
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
