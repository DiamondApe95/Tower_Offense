namespace TowerConquest.Data
{
    [System.Serializable]
    public class UnitsJsonRoot
    {
        public string version;
        public UnitDefinition[] unit_definitions;
        public SpellDefinition[] spell_definitions;
    }
}
