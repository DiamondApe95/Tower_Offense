using System;
using UnityEngine;
using TowerConquest.Combat;
using TowerConquest.Data;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Represents a construction site for a tower being built
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string towerId;
        [SerializeField] private int requiredBuilders = 3;
        [SerializeField] private float maxHP = 100f;

        [Header("Runtime")]
        [SerializeField] private int currentBuilders = 0;
        [SerializeField] private float currentHP;
        [SerializeField] private bool isComplete = false;

        private GoldManager.Team ownerTeam;
        private HealthComponent healthComponent;

        public string TowerID => towerId;
        public int RequiredBuilders => requiredBuilders;
        public int CurrentBuilders => currentBuilders;
        public float ConstructionProgress => (float)currentBuilders / requiredBuilders;
        public bool IsComplete => isComplete;
        public GoldManager.Team OwnerTeam => ownerTeam;

        public event Action<ConstructionSite> OnConstructionComplete;
        public event Action<ConstructionSite> OnConstructionDestroyed;
        public event Action<int, int> OnBuilderCountChanged; // (current, required)

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
            towerId = tId;
            requiredBuilders = builders;
            maxHP = hp;
            currentHP = hp;
            ownerTeam = team;
            currentBuilders = 0;
            isComplete = false;

            if (healthComponent != null)
            {
                healthComponent.Initialize(maxHP, 0f);
            }

            Debug.Log($"[ConstructionSite] Initialized: {towerId}, Team: {team}, Builders needed: {requiredBuilders}");
        }

        /// <summary>
        /// Called when a builder reaches the site
        /// </summary>
        public void OnBuilderArrived()
        {
            if (isComplete)
            {
                Debug.LogWarning("[ConstructionSite] Builder arrived but construction already complete");
                return;
            }

            currentBuilders++;
            Debug.Log($"[ConstructionSite] Builder arrived ({currentBuilders}/{requiredBuilders})");

            OnBuilderCountChanged?.Invoke(currentBuilders, requiredBuilders);

            if (currentBuilders >= requiredBuilders)
            {
                CompleteConstruction();
            }
        }

        /// <summary>
        /// Complete the construction and spawn the tower
        /// </summary>
        private void CompleteConstruction()
        {
            if (isComplete) return;

            isComplete = true;
            Debug.Log($"[ConstructionSite] Construction complete for {towerId}");

            OnConstructionComplete?.Invoke(this);

            // The ConstructionManager will handle spawning the actual tower
            // and destroying this construction site
        }

        /// <summary>
        /// Take damage to the construction site
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isComplete) return;

            if (healthComponent != null)
            {
                healthComponent.TakeDamage(damage, "physical", null);
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

            Debug.Log($"[ConstructionSite] Destroyed: {towerId}");
            OnConstructionDestroyed?.Invoke(this);

            // Notify construction manager
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
            // Draw construction progress visualization
            Gizmos.color = isComplete ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

            if (!isComplete)
            {
                // Draw progress bar
                float progress = (float)currentBuilders / requiredBuilders;
                Vector3 start = transform.position + Vector3.up * 3f;
                Vector3 end = start + Vector3.right * 2f * progress;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
