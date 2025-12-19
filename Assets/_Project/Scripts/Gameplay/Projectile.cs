using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float speed;
    private float damage;

    public void Init(Transform target, float speed, float damage)
    {
        this.target = target;
        this.speed = speed;
        this.damage = damage;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position);
        float distThisFrame = speed * Time.deltaTime;

        // Treffer, wenn wir nah genug sind
        if (dir.magnitude <= distThisFrame)
        {
            HitTarget();
            return;
        }

        transform.position += dir.normalized * distThisFrame;
        transform.forward = dir.normalized;
    }

    private void HitTarget()
    {
        var hp = target.GetComponent<EnemyHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
