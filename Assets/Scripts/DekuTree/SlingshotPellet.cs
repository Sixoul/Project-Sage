using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Deku Seed pellet projectile fired from the Fairy Slingshot.
    /// Hits eye switches, stuns Deku Babas/Gohma, and defeats Skullwalltulas.
    /// </summary>
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public class SlingshotPellet : MonoBehaviour
    {
        [SerializeField] private float speed = 32f;
        [SerializeField] private float damage = 4f; // 1 full heart
        [SerializeField] private float lifetime = 3.5f;

        private GameObject owner;
        private Rigidbody rb;
        private bool hasHit = false;

        public static GameObject SpawnPellet(Vector3 origin, Vector3 dir, GameObject shooter)
        {
            GameObject pellet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pellet.name = "Deku_Seed_Pellet";
            pellet.transform.position = origin;
            pellet.transform.localScale = Vector3.one * 0.22f;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.color = new Color(0.85f, 0.72f, 0.35f); // Golden amber Deku seed
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.85f, 0.72f, 0.35f) * 1.5f);
            pellet.GetComponent<MeshRenderer>().material = mat;

            var comp = pellet.AddComponent<SlingshotPellet>();
            comp.owner = shooter;
            comp.Initialize(dir);

            return pellet;
        }

        public void Initialize(Vector3 direction)
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = direction.normalized * speed;

            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;
            if (collision.gameObject == owner) return;

            hasHit = true;
            ContactPoint contact = collision.GetContact(0);

            var dmg = collision.gameObject.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                dmg.TakeDamage(new DamageInfo(damage, contact.point, rb.linearVelocity.normalized, 6f, owner, false));
            }

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(contact.point, contact.normal);
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            if (other.gameObject == owner || other.transform.IsChildOf(owner.transform)) return;

            var eye = other.GetComponentInParent<EyeSwitch>();
            if (eye != null)
            {
                hasHit = true;
                eye.ActivateSwitch();
                Destroy(gameObject);
            }
        }
    }
}
