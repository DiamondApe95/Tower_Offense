using UnityEngine;
using TowerConquest.Core;
using TowerConquest.Audio;

namespace TowerConquest.Combat
{
    /// <summary>
    /// ProjectileController: Steuert Projektile mit verschiedenen Flugbahnen und Effekten.
    /// </summary>
    public class ProjectileController : MonoBehaviour
    {
        public enum TrajectoryType
        {
            Direct,     // Geradeaus zum Ziel
            Arc,        // Bogenförmige Flugbahn
            Homing,     // Verfolgend
            Spiral,     // Spiralförmig
            Ballistic   // Ballistisch (Parabel)
        }

        [Header("Movement")]
        public TrajectoryType trajectory = TrajectoryType.Direct;
        public float speed = 15f;
        public float arcHeight = 3f;
        public float homingStrength = 5f;
        public float spiralRadius = 0.5f;
        public float spiralSpeed = 5f;

        [Header("Damage")]
        public float damage = 10f;
        public string damageType = "physical";
        public bool destroyOnHit = true;

        [Header("AoE")]
        public bool hasAoE = false;
        public float aoeRadius = 2f;
        public float aoeDamageMultiplier = 0.5f;

        [Header("Effects")]
        public string[] effectsOnHit;
        public float effectDuration = 2f;

        [Header("Visual")]
        public TrailRenderer trailRenderer;
        public ParticleSystem hitEffect;
        public Light projectileLight;

        [Header("Lifetime")]
        public float maxLifetime = 5f;
        public float hitDelay = 0f;

        [Header("Audio")]
        public AudioClip launchSound;
        public AudioClip hitSound;

        // Runtime
        private Transform target;
        private Vector3 targetPosition;
        private GameObject source;
        private Vector3 startPosition;
        private float flightTime;
        private float spiralAngle;
        private bool hasHit;
        private bool isLaunched;

        private void Update()
        {
            if (!isLaunched || hasHit) return;

            flightTime += Time.deltaTime;

            if (flightTime >= maxLifetime)
            {
                DestroyProjectile();
                return;
            }

            // Update target position
            if (target != null)
            {
                targetPosition = target.position + Vector3.up * 0.5f;
            }

            // Move based on trajectory
            switch (trajectory)
            {
                case TrajectoryType.Direct:
                    MoveDirect();
                    break;

                case TrajectoryType.Arc:
                    MoveArc();
                    break;

                case TrajectoryType.Homing:
                    MoveHoming();
                    break;

                case TrajectoryType.Spiral:
                    MoveSpiral();
                    break;

                case TrajectoryType.Ballistic:
                    MoveBallistic();
                    break;
            }

            // Check for collision
            CheckCollision();
        }

        /// <summary>
        /// Startet das Projektil zum Ziel.
        /// </summary>
        public void Launch(Transform targetTransform, float projectileDamage, string type, GameObject sourceObj)
        {
            target = targetTransform;
            targetPosition = targetTransform != null ? targetTransform.position + Vector3.up * 0.5f : transform.position + transform.forward * 10f;
            damage = projectileDamage;
            damageType = type ?? "physical";
            source = sourceObj;
            startPosition = transform.position;
            flightTime = 0f;
            spiralAngle = 0f;
            hasHit = false;
            isLaunched = true;

            // Look at target
            Vector3 lookDir = targetPosition - transform.position;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // Play launch sound
            if (launchSound != null)
            {
                AudioSource.PlayClipAtPoint(launchSound, transform.position, 0.5f);
            }
        }

        /// <summary>
        /// Startet das Projektil zu einer Position.
        /// </summary>
        public void Launch(Vector3 position)
        {
            Launch(null, damage, damageType, null);
            targetPosition = position;
        }

        private void MoveDirect()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void MoveArc()
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            float progress = Mathf.Clamp01(flightTime * speed / distance);

            // Horizontale Interpolation
            Vector3 flatStart = new Vector3(startPosition.x, 0, startPosition.z);
            Vector3 flatEnd = new Vector3(targetPosition.x, 0, targetPosition.z);
            Vector3 flatPos = Vector3.Lerp(flatStart, flatEnd, progress);

            // Vertikale Parabel
            float heightStart = startPosition.y;
            float heightEnd = targetPosition.y;
            float arcY = Mathf.Lerp(heightStart, heightEnd, progress);
            arcY += arcHeight * Mathf.Sin(progress * Mathf.PI);

            Vector3 newPos = new Vector3(flatPos.x, arcY, flatPos.z);

            // Rotation in Bewegungsrichtung
            Vector3 moveDir = newPos - transform.position;
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDir);
            }

            transform.position = newPos;
        }

        private void MoveHoming()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            Vector3 desiredVelocity = direction * speed;

            // Smooth rotation towards target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, homingStrength * Time.deltaTime);

            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void MoveSpiral()
        {
            spiralAngle += spiralSpeed * Time.deltaTime;

            Vector3 direction = (targetPosition - transform.position).normalized;
            Vector3 baseMovement = direction * speed * Time.deltaTime;

            // Spiral offset
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right);
            Vector3 spiralOffset = (right * Mathf.Cos(spiralAngle) + up * Mathf.Sin(spiralAngle)) * spiralRadius;

            transform.position += baseMovement + spiralOffset * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void MoveBallistic()
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            float totalTime = distance / speed;
            float progress = Mathf.Clamp01(flightTime / totalTime);

            // Horizontale Interpolation
            Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, progress);

            // Vertikale Parabel (ballistische Kurve)
            float gravity = arcHeight * 4f;
            float initialVelocityY = (targetPosition.y - startPosition.y) / totalTime + 0.5f * gravity * totalTime;
            float y = startPosition.y + initialVelocityY * flightTime - 0.5f * gravity * flightTime * flightTime;

            Vector3 newPos = new Vector3(horizontalPos.x, y, horizontalPos.z);

            // Rotation in Bewegungsrichtung
            Vector3 moveDir = newPos - transform.position;
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDir);
            }

            transform.position = newPos;
        }

        private void CheckCollision()
        {
            float hitDistance = 0.5f;

            if (Vector3.Distance(transform.position, targetPosition) <= hitDistance)
            {
                OnHit(target);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Prüfe ob es ein gültiges Ziel ist
            if (target != null && other.transform != target) return;

            var health = other.GetComponent<HealthComponent>();
            if (health != null)
            {
                OnHit(other.transform);
            }
        }

        private void OnHit(Transform hitTarget)
        {
            if (hasHit) return;
            hasHit = true;

            // Delayed hit
            if (hitDelay > 0)
            {
                Invoke(nameof(ApplyDamage), hitDelay);
            }
            else
            {
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            // Hit sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.5f);
            }

            // Hit effect
            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration + 1f);
            }

            // Apply damage
            if (hasAoE)
            {
                ApplyAoEDamage();
            }
            else
            {
                ApplySingleDamage();
            }

            // Destroy projectile
            if (destroyOnHit)
            {
                DestroyProjectile();
            }
        }

        private void ApplySingleDamage()
        {
            if (target == null) return;

            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(damage, damageType, source);
                ApplyEffects(target);
            }
        }

        private void ApplyAoEDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius);

            foreach (var hit in hits)
            {
                var health = hit.GetComponent<HealthComponent>();
                if (health == null) continue;

                // Entfernung berechnen
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damageMultiplier = distance < 0.5f ? 1f : aoeDamageMultiplier;

                health.TakeDamage(damage * damageMultiplier, damageType, source);
                ApplyEffects(hit.transform);
            }
        }

        private void ApplyEffects(Transform target)
        {
            if (effectsOnHit == null || effectsOnHit.Length == 0) return;

            var statusSystem = target.GetComponent<StatusSystem>();
            if (statusSystem == null) return;

            foreach (string effectId in effectsOnHit)
            {
                switch (effectId.ToLower())
                {
                    case "slow":
                        statusSystem.ApplySlow(0.5f, effectDuration);
                        break;

                    case "burn":
                        statusSystem.ApplyBurn(damage * 0.1f, effectDuration);
                        break;

                    case "freeze":
                        statusSystem.ApplySlow(0.9f, effectDuration);
                        break;

                    case "stun":
                        statusSystem.ApplySlow(1f, effectDuration * 0.5f);
                        break;
                }
            }
        }

        private void DestroyProjectile()
        {
            isLaunched = false;

            // Trail fade out
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            // AoE Radius
            if (hasAoE)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
                Gizmos.DrawWireSphere(transform.position, aoeRadius);
            }

            // Target line
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
