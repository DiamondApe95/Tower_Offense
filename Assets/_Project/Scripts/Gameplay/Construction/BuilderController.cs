using UnityEngine;
using UnityEngine.AI;
using TowerConquest.Combat;

namespace TowerConquest.Gameplay
{
    /// <summary>
    /// Controls a builder unit that constructs towers and traps
    /// </summary>
    public class BuilderController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float arrivalDistance = 1.5f;

        [Header("Runtime")]
        [SerializeField] private ConstructionSite targetSite;
        [SerializeField] private TrapConstructionSite targetTrapSite;
        [SerializeField] private bool isAssigned = false;
        [SerializeField] private bool hasArrived = false;

        private NavMeshAgent agent;
        private HealthComponent healthComponent;
        private GoldManager.Team ownerTeam;
        private Transform targetPosition;

        public bool IsAssigned => isAssigned;
        public bool HasArrived => hasArrived;
        public ConstructionSite TargetSite => targetSite;
        public TrapConstructionSite TargetTrapSite => targetTrapSite;
        public GoldManager.Team OwnerTeam => ownerTeam;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
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
        /// Assign this builder to a trap construction site
        /// </summary>
        public void AssignToTrapSite(TrapConstructionSite trapSite)
        {
            if (trapSite == null)
            {
                Debug.LogWarning("[BuilderController] Cannot assign to null trap site");
                return;
            }

            if (isAssigned)
            {
                Debug.LogWarning("[BuilderController] Builder already assigned");
                return;
            }

            targetTrapSite = trapSite;
            targetPosition = trapSite.transform;
            isAssigned = true;
            hasArrived = false;

            // Listen for site destruction
            targetTrapSite.OnConstructionDestroyed += OnTrapSiteDestroyed;

            MoveToTarget();

            Debug.Log($"[BuilderController] Assigned to trap site: {trapSite.TrapID}");
        }

        private void MoveToTarget()
        {
            if (agent == null) return;

            Vector3 destination = Vector3.zero;
            if (targetSite != null)
            {
                destination = targetSite.transform.position;
            }
            else if (targetTrapSite != null)
            {
                destination = targetTrapSite.transform.position;
            }

            if (destination != Vector3.zero)
            {
                agent.SetDestination(destination);
                Debug.Log("[BuilderController] Moving to construction site");
            }
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
            targetPosition = site.transform;
            isAssigned = true;
            hasArrived = false;

            // Listen for site destruction
            targetSite.OnConstructionDestroyed += OnSiteDestroyed;

            MoveToTarget();

            Debug.Log($"[BuilderController] Assigned to site: {site.TowerID}");
        }

        private void Update()
        {
            if (!isAssigned || hasArrived) return;
            if (targetSite == null && targetTrapSite == null) return;

            // Check if we've arrived
            if (agent != null && !agent.pathPending && agent.remainingDistance <= arrivalDistance)
            {
                OnReachedSite();
            }
        }

        private void OnReachedSite()
        {
            if (hasArrived) return;

            hasArrived = true;
            Debug.Log("[BuilderController] Reached construction site");

            // Notify the appropriate construction site
            if (targetSite != null)
            {
                targetSite.OnBuilderArrived();
                targetSite.OnConstructionComplete += OnConstructionComplete;
            }
            else if (targetTrapSite != null)
            {
                targetTrapSite.RegisterBuilderArrival();
                targetTrapSite.OnConstructionComplete += OnTrapConstructionComplete;
            }
        }

        private void OnConstructionComplete(ConstructionSite site)
        {
            Debug.Log("[BuilderController] Construction complete, builder leaving");
            UnassignFromSite();
            Destroy(gameObject, 0.5f);
        }

        private void OnTrapConstructionComplete(TrapConstructionSite site)
        {
            Debug.Log("[BuilderController] Trap construction complete, builder leaving");
            UnassignFromSite();
            Destroy(gameObject, 0.5f);
        }

        private void OnSiteDestroyed(ConstructionSite site)
        {
            Debug.Log("[BuilderController] Construction site destroyed");
            UnassignFromSite();
            Destroy(gameObject, 0.5f);
        }

        private void OnTrapSiteDestroyed(TrapConstructionSite site)
        {
            Debug.Log("[BuilderController] Trap construction site destroyed");
            UnassignFromSite();
            Destroy(gameObject, 0.5f);
        }

        private void UnassignFromSite()
        {
            if (targetSite != null)
            {
                targetSite.OnConstructionDestroyed -= OnSiteDestroyed;
                targetSite.OnConstructionComplete -= OnConstructionComplete;
            }

            if (targetTrapSite != null)
            {
                targetTrapSite.OnConstructionDestroyed -= OnTrapSiteDestroyed;
                targetTrapSite.OnConstructionComplete -= OnTrapConstructionComplete;
            }

            targetSite = null;
            targetTrapSite = null;
            targetPosition = null;
            isAssigned = false;
            hasArrived = false;
        }

        private void OnBuilderDied()
        {
            Debug.Log("[BuilderController] Builder killed");
            UnassignFromSite();
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
            Vector3 target = Vector3.zero;
            if (targetSite != null)
            {
                target = targetSite.transform.position;
            }
            else if (targetTrapSite != null)
            {
                target = targetTrapSite.transform.position;
            }

            if (isAssigned && target != Vector3.zero)
            {
                Gizmos.color = hasArrived ? Color.green : Color.yellow;
                Gizmos.DrawLine(transform.position, target);
            }
        }
    }
}
