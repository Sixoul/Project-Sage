using System.Collections;
using UnityEngine;
using ZeldaOoT.Combat;

namespace ZeldaOoT.Items
{
    /// <summary>
    /// Zelda-style Bomb. Can be thrown or placed, bounces, flashes with fuse timer,
    /// and explodes dealing radial damage and destroying crates/obstacles.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BombItem : MonoBehaviour
    {
        [SerializeField] private float fuseTime = 3.5f;
        [SerializeField] private float explosionRadius = 4.5f;
        [SerializeField] private float damage = 8f; // 2 full hearts
        [SerializeField] private float explosionForce = 12f;

        private Rigidbody rb;
        private MeshRenderer meshRenderer;
        private GameObject owner;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        public void Initialize(Vector3 throwVelocity, GameObject thrower)
        {
            owner = thrower;
            if (rb != null)
            {
                rb.linearVelocity = throwVelocity;
            }
            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            float elapsed = 0f;
            Color baseColor = Color.blue;
            Color flashColor = Color.red;

            while (elapsed < fuseTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fuseTime;

                // Flash speed increases as fuse burns down
                float frequency = Mathf.Lerp(4f, 25f, progress);
                float flashT = Mathf.PingPong(elapsed * frequency, 1f);

                if (meshRenderer != null && meshRenderer.material != null)
                {
                    meshRenderer.material.color = Color.Lerp(baseColor, flashColor, flashT);
                }

                yield return null;
            }

            Explode();
        }

        private void Explode()
        {
            // Explosion visual sphere
            GameObject boom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boom.name = "ExplosionVisual";
            boom.transform.position = transform.position;
            boom.transform.localScale = Vector3.one * (explosionRadius * 1.8f);
            var col = boom.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = boom.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1f, 0.5f, 0.1f, 0.8f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f) * 4f);
                mr.material = mat;
            }
            Destroy(boom, 0.25f);

            // Blast damage to nearby damageables and physics rigidbodies
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    Vector3 blastDir = (dmg.Transform.position - transform.position).normalized;
                    blastDir.y = 0.5f;
                    dmg.TakeDamage(new DamageInfo(damage, hit.ClosestPoint(transform.position), blastDir, explosionForce, owner, false));
                }

                var otherRb = hit.attachedRigidbody;
                if (otherRb != null && !otherRb.isKinematic)
                {
                    otherRb.AddExplosionForce(explosionForce * 60f, transform.position, explosionRadius, 1.5f);
                }
            }

            Destroy(gameObject);
        }
    }
}
