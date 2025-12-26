using System;
using TowerConquest.Debug;
using TowerConquest.Combat;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Represents a construction site for a trap being built
    /// Traps require only 1 builder
    /// </summary>
    public class TrapConstructionSite : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string trapId;
        [SerializeField] private int requiredBuilders = 1;
        [SerializeField] private float maxHP = 50f;

        [Header("Runtime")]
        [SerializeField] private int currentBuilders = 0;
        [SerializeField] private float currentHP;
        [SerializeField] private bool isComplete = false;

        private GoldManager.Team ownerTeam;
        private HealthComponent healthComponent;

        public string TrapID => trapId;
        public int RequiredBuilders => requiredBuilders;
        public int CurrentBuilders => currentBuilders;
        public float ConstructionProgress => (float)currentBuilders / requiredBuilders;
        public bool IsComplete => isComplete;
        public GoldManager.Team OwnerTeam => ownerTeam;
        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;

        public event Action<TrapConstructionSite> OnConstructionComplete;
        public event Action<TrapConstructionSite> OnConstructionDestroyed;
        public event Action<int, int> OnBuilderArrived; // (current, required)

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }

            currentHP = maxHP;
            healthComponent.OnDeath += OnSiteDestroyed;
        }

        public void Initialize(string tId, int builders, float hp, GoldManager.Team team)
        {
            trapId = tId;
            requiredBuilders = Mathf.Max(1, builders);
            maxHP = hp;
            currentHP = hp;
            ownerTeam = team;
            currentBuilders = 0;
            isComplete = false;

            if (healthComponent != null)
            {
                healthComponent.Initialize(maxHP, 0f);
            }

            Log.Info($"[TrapConstructionSite] Initialized: {trapId}, Team: {team}, Builders needed: {requiredBuilders}");
        }

        /// <summary>
        /// Called when a builder reaches the site
        /// </summary>
        public void RegisterBuilderArrival()
        {
            if (isComplete)
            {
                Log.Warning("[TrapConstructionSite] Builder arrived but construction already complete");
                return;
            }

            currentBuilders++;
            Log.Info($"[TrapConstructionSite] Builder arrived ({currentBuilders}/{requiredBuilders})");

            OnBuilderArrived?.Invoke(currentBuilders, requiredBuilders);

            if (currentBuilders >= requiredBuilders)
            {
                CompleteConstruction();
            }
        }

        private void CompleteConstruction()
        {
            if (isComplete) return;

            isComplete = true;
            Log.Info($"[TrapConstructionSite] Construction complete for {trapId}");

            OnConstructionComplete?.Invoke(this);
        }

        /// <summary>
        /// Take damage to the construction site
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isComplete) return;

            if (healthComponent != null)
            {
                healthComponent.TakeDamage(damage, "Physical", null);
            }
            else
            {
                currentHP -= damage;
                if (currentHP <= 0)
                {
                    OnSiteDestroyed();
                }
            }
        }

        private void OnSiteDestroyed()
        {
            if (isComplete) return;

            Log.Info($"[TrapConstructionSite] Destroyed: {trapId}");
            OnConstructionDestroyed?.Invoke(this);

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= OnSiteDestroyed;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isComplete ? Color.green : new Color(0.6f, 0.4f, 0.2f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);

            if (!isComplete)
            {
                float progress = (float)currentBuilders / Mathf.Max(1, requiredBuilders);
                Vector3 start = transform.position + Vector3.up * 1.5f;
                Vector3 end = start + Vector3.right * progress;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
