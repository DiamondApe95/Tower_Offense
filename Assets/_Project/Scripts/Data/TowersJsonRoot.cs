namespace TowerOffense.Data
{
    [System.Serializable]
    public class TowersJsonRoot
    {
        public string version;
        public TowerDefinition[] tower_definitions;
        public TrapDefinition[] trap_definitions;
    }
}
