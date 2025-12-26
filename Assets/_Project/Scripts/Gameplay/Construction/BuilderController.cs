using UnityEngine;
using UnityEngine.AI;
using TowerConquest.Combat;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Controls a builder unit that constructs towers
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BuilderController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float arrivalDistance = 1.5f;

        [Header("Runtime")]
        [SerializeField] private ConstructionSite targetSite;
        [SerializeField] private bool isAssigned = false;
        [SerializeField] private bool hasArrived = false;

        private NavMeshAgent agent;
        private HealthComponent healthComponent;
        private GoldManager.Team ownerTeam;

        public bool IsAssigned => isAssigned;
        public bool HasArrived => hasArrived;
        public ConstructionSite TargetSite => targetSite;
        public GoldManager.Team OwnerTeam => ownerTeam;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = arrivalDistance;

            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
                healthComponent.Initialize(50f, 0f); // Builders are fragile
            }

            healthComponent.OnDeath += OnBuilderDied;
        }

        public void Initialize(GoldManager.Team team)
        {
            ownerTeam = team;
        }

        /// <summary>
        /// Assign this builder to a construction site
        /// </summary>
        public void AssignToSite(ConstructionSite site)
        {
            if (site == null)
            {
                Debug.LogWarning("[BuilderController] Cannot assign to null site");
                return;
            }

            if (isAssigned)
            {
                Debug.LogWarning("[BuilderController] Builder already assigned");
                return;
            }

            targetSite = site;
            isAssigned = true;
            hasArrived = false;

            // Listen for site destruction
            targetSite.OnConstructionDestroyed += OnSiteDestroyed;

            MoveToSite();

            Debug.Log($"[BuilderController] Assigned to site: {site.TowerID}");
        }

        private void MoveToSite()
        {
            if (targetSite == null || agent == null) return;

            agent.SetDestination(targetSite.transform.position);
            Debug.Log("[BuilderController] Moving to construction site");
        }

        private void Update()
        {
            if (!isAssigned || hasArrived || targetSite == null) return;

            // Check if we've arrived
            if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
            {
                OnReachedSite();
            }
        }

        private void OnReachedSite()
        {
            if (hasArrived || targetSite == null) return;

            hasArrived = true;
            Debug.Log("[BuilderController] Reached construction site");

            // Notify the construction site
            targetSite.OnBuilderArrived();

            // Builder stays at site until construction is complete or site is destroyed
            // Could add building animation here

            // Listen for construction completion
            targetSite.OnConstructionComplete += OnConstructionComplete;
        }

        private void OnConstructionComplete(ConstructionSite site)
        {
            Debug.Log("[BuilderController] Construction complete, builder leaving");

            // Unassign and return to base or become idle
            UnassignFromSite();

            // For now, destroy the builder
            // Later: could return to base or become available for next construction
            Destroy(gameObject, 1f);
        }

        private void OnSiteDestroyed(ConstructionSite site)
        {
            Debug.Log("[BuilderController] Construction site destroyed");

            // Unassign
            UnassignFromSite();

            // For now, destroy the builder
            // Later: could return to base or flee
            Destroy(gameObject, 1f);
        }

        private void UnassignFromSite()
        {
            if (targetSite != null)
            {
                targetSite.OnConstructionDestroyed -= OnSiteDestroyed;
                targetSite.OnConstructionComplete -= OnConstructionComplete;
            }

            targetSite = null;
            isAssigned = false;
            hasArrived = false;
        }

        private void OnBuilderDied()
        {
            Debug.Log("[BuilderController] Builder killed");

            // Unassign from site
            UnassignFromSite();

            // TODO: Award gold to killer
        }

        private void OnDestroy()
        {
            UnassignFromSite();

            if (healthComponent != null)
            {
                healthComponent.OnDeath -= OnBuilderDied;
            }
        }

        private void OnDrawGizmos()
        {
            if (isAssigned && targetSite != null)
            {
                Gizmos.color = hasArrived ? Color.green : Color.yellow;
                Gizmos.DrawLine(transform.position, targetSite.transform.position);
            }
        }
    }
}
