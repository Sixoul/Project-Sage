using UnityEngine;
using ZeldaOoT.Combat;

namespace ZeldaOoT.Items
{
    /// <summary>
    /// Arrow projectile fired from the Fairy Bow.
    /// Deals ranged damage to enemies and activates target bullseyes.
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 35f;
        [SerializeField] private float gravity = 8f;
        [SerializeField] private float damage = 4f;
        [SerializeField] private float lifetime = 6f;

        private Vector3 velocity;
        private GameObject shooter;
        private bool hasHit = false;

        public void Initialize(Vector3 direction, GameObject owner)
        {
            shooter = owner;
            velocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (hasHit) return;

            velocity.y -= gravity * Time.deltaTime;
            Vector3 step = velocity * Time.deltaTime;

            if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, step.magnitude))
            {
                if (hit.transform.gameObject == shooter) return;

                hasHit = true;
                transform.position = hit.point;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(new DamageInfo(damage, hit.point, velocity.normalized, 8f, shooter, true));
                }

                // Parent to hit object and destroy after short delay
                transform.SetParent(hit.transform);
                Destroy(gameObject, 2f);
            }
            else
            {
                transform.position += step;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(velocity);
                }
            }
        }
    }
}
