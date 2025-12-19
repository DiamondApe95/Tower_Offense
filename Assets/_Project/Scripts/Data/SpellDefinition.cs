namespace TowerOffense.Data
{
    [System.Serializable]
    public class SpellDefinition
    {
        public string id;
        public string display_name;
        public CardDto card;
        public TargetingDto targeting;
        public EffectDto[] effects;

        [System.Serializable]
        public class CardDto
        {
            public int cost;
            public string hand_group;
            public float cooldown_seconds;
        }

        [System.Serializable]
        public class TargetingDto
        {
            public string mode;
            public float max_range;
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
