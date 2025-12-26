namespace TowerConquest.Data
{
    [System.Serializable]
    public class UpgradeLevel
    {
        public int level;
        public int fameCost;
        public float hpBonus;
        public float damageBonus;
        public float speedBonus; // For units
        public float rangeBonus; // For towers
        public float armorBonus;
        public float attackSpeedBonus;
    }
}
