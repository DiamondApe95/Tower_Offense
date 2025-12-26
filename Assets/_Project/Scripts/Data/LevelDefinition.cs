namespace TowerConquest.Data
{
    [System.Serializable]
    public class LevelDefinition
    {
        public string id;
        public string display_name;
        public string description;
        public string region_id;
        public int recommended_power;
        public float estimated_duration_seconds;

        // NEW: Civilization & AI Settings
        public string playerCivilization; // Civilization for player
        public string enemyCivilization; // Civilization for AI
        public string aiDifficulty; // easy, normal, hard
        public string aiStrategy; // aggressive, defensive, balanced

        // NEW: Gold Economy
        public int startGold; // Starting gold for both player and AI

        // NEW: Fame Rewards
        public FameRewardDto fameReward;

        // NEW: Unlock Requirements
        public string unlockRequirement; // Level ID that must be completed first (null = unlocked from start)

        public ConditionDto win_condition; // DEPRECATED
        public ConditionDto lose_condition; // DEPRECATED
        public BaseDto @base; // Player base settings
        public BaseDto enemyBase; // AI base settings
        public SpawnPointDto[] spawn_points;
        public PathDto[] paths;
        public EnemyDefensesDto enemy_defenses; // DEPRECATED - AI will build dynamically
        public PlayerRulesDto player_rules; // DEPRECATED - uses UnitDeck instead

        [System.Serializable]
        public class FameRewardDto
        {
            public int victory; // Fame for winning
            public int defeat; // Fame for losing
            public BonusConditionDto[] bonus; // Bonus fame conditions
        }

        [System.Serializable]
        public class BonusConditionDto
        {
            public string condition; // e.g., "win_under_5_min", "no_tower_lost"
            public int fame;
        }

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
