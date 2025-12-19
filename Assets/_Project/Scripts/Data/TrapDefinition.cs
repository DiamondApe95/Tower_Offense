namespace TowerConquest.Data
{
    [System.Serializable]
    public class TrapDefinition
    {
        public string id;
        public string display_name;
        public string category;
        public TriggerDto trigger;
        public EffectDto[] effects;

        [System.Serializable]
        public class TriggerDto
        {
            public string type;
            public float cooldown_seconds;
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
