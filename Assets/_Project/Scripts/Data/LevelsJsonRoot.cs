namespace TowerConquest.Data
{
    [System.Serializable]
    public class LevelsJsonRoot
    {
        public string version;
        public GlobalRulesDto global_rules;
        public RegionDto[] regions;
        public LevelDefinition[] levels;
    }

    [System.Serializable]
    public class GlobalRulesDto
    {
        public string mode;
        public int hand_size;
        public DrawRulesDto draw_rules;
        public WaveRulesDto wave_rules;

        [System.Serializable]
        public class DrawRulesDto
        {
            public bool draw_on_play;
            public bool reshuffle_on_empty;
        }

        [System.Serializable]
        public class WaveRulesDto
        {
            public bool phase_based;
            public bool allow_mid_wave_spawns;
            public int hero_every_n_waves;
            public float[] speed_modes;
        }
    }

    [System.Serializable]
    public class RegionDto
    {
        public string id;
        public string display_name;
    }
}
