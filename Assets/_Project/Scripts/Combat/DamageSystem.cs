namespace TowerConquest.Combat
{
    public static class DamageSystem
    {
        public static void Apply(UnityEngine.GameObject target, float amount)
        {
            if (target == null)
            {
                return;
            }

            HealthComponent health = target.GetComponent<HealthComponent>();
            if (health == null)
            {
                return;
            }

            health.ApplyDamage(amount);
        }
    }
}
