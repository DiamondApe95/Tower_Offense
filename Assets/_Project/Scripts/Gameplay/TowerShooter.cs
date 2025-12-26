using UnityEngine;
using TowerConquest.Debug;

public class TowerShooter : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public GameObject projectilePrefab;

    [Header("Stats")]
    public float range = 6f;
    public float fireRate = 1.2f;   // Schüsse pro Sekunde
    public float damage = 3f;
    public float projectileSpeed = 10f;

    [Header("Targeting")]
    public LayerMask enemyMask;     // Layer: Enemy

    private Transform currentTarget;
    private float fireCooldown;

    private void Update()
    {
        fireCooldown -= Time.deltaTime;

        if (currentTarget == null || !IsTargetInRange(currentTarget))
        {
            currentTarget = FindTarget();
        }

        if (currentTarget != null)
        {
            AimAt(currentTarget);

            if (fireCooldown <= 0f)
            {
                Shoot(currentTarget);
                fireCooldown = 1f / fireRate;
            }
        }
    }

    private bool IsTargetInRange(Transform t)
    {
        return Vector3.Distance(transform.position, t.position) <= range;
    }

    private Transform FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyMask);
        if (hits.Length == 0) return null;

        // Nächstes Ziel
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = h.transform;
            }
        }

        return best;
    }

    private void AimAt(Transform t)
    {
        Vector3 look = t.position - transform.position;
        look.y = 0f; // nur horizontal drehen
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look);
    }

    private void Shoot(Transform t)
    {
        if (projectilePrefab == null)
        {
            Log.Error("TowerShooter: projectilePrefab missing!");
            return;
        }

        Transform fp = firePoint != null ? firePoint : transform;

        GameObject go = Instantiate(projectilePrefab, fp.position, fp.rotation);
        Projectile p = go.GetComponent<Projectile>();
        if (p == null) p = go.AddComponent<Projectile>();

        p.Init(t, projectileSpeed, damage);
    }

    // Debug: Reichweite im Editor anzeigen
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
