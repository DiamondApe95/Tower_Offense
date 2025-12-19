namespace TowerOffense.Data
{
    [System.Serializable]
    public class LevelDefinition
    {
        public string id;
        public string display_name;
        public string region_id;
        public int recommended_power;
        public float estimated_duration_seconds;
        public ConditionDto win_condition;
        public ConditionDto lose_condition;
        public BaseDto @base;
        public SpawnPointDto[] spawn_points;
        public PathDto[] paths;
        public EnemyDefensesDto enemy_defenses;
        public PlayerRulesDto player_rules;

        [System.Serializable]
        public class ConditionDto
        {
            public string type;
        }

        [System.Serializable]
        public class BaseDto
        {
            public string id;
            public float hp;
            public float armor;
            public PositionDto position;
        }

        [System.Serializable]
        public class SpawnPointDto
        {
            public string id;
            public PositionDto position;
        }

        [System.Serializable]
        public class PathDto
        {
            public string id;
            public string from_spawn_id;
            public string to_base_id;
            public PositionDto[] waypoints;
        }

        [System.Serializable]
        public class EnemyDefensesDto
        {
            public TowerPlacementDto[] towers;
            public TrapPlacementDto[] traps;
        }

        [System.Serializable]
        public class TowerPlacementDto
        {
            public string instance_id;
            public string tower_id;
            public int tier;
            public PositionDto position;
            public float rotation_y_degrees;
        }

        [System.Serializable]
        public class TrapPlacementDto
        {
            public string instance_id;
            public string trap_id;
            public PositionDto position;
            public TrapSizeDto size;
        }

        [System.Serializable]
        public class TrapSizeDto
        {
            public float x;
            public float z;
        }

        [System.Serializable]
        public class PlayerRulesDto
        {
            public StartingDeckDto starting_deck;
            public string[] hero_pool;
            public int max_waves;
        }

        [System.Serializable]
        public class StartingDeckDto
        {
            public string[] unit_cards;
            public string[] spell_cards;
        }

        [System.Serializable]
        public class PositionDto
        {
            public float x;
            public float y;
            public float z;
        }
    }
}
