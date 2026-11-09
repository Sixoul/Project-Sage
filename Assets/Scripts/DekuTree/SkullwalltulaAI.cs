using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;
using ZeldaOoT.Kokiri;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Skullwalltula clinging to ivy/vines inside the Great Deku Tree.
    /// Knocks Link down if he climbs near it. Can be shot down with the Fairy Slingshot.
    /// </summary>
    public class SkullwalltulaAI : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 4f;

        public bool IsAlive => currentHealth > 0f;
        public Transform Transform => transform;

        private float currentHealth;

        public static GameObject CreateSkullwalltula(Vector3 position, Quaternion rotation)
        {
            GameObject spiderRoot = new GameObject("Enemy_Skullwalltula");
            spiderRoot.transform.position = position;
            spiderRoot.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material skullMat = new Material(s) { color = new Color(0.92f, 0.9f, 0.82f) }; // Bone white skull
            Material bodyMat = new Material(s) { color = new Color(0.2f, 0.18f, 0.22f) }; // Dark purple body
            Material eyeMat = new Material(s);
            eyeMat.color = Color.red;
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", Color.red * 2f);

            // Abdomen / Skull face
            GameObject skull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skull.name = "Skull_Abdomen";
            skull.transform.SetParent(spiderRoot.transform, false);
            skull.transform.localScale = new Vector3(0.7f, 0.7f, 0.5f);
            skull.GetComponent<MeshRenderer>().material = skullMat;
            Destroy(skull.GetComponent<Collider>());

            // Eyes
            for (int e = -1; e <= 1; e += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = e < 0 ? "Eye_L" : "Eye_R";
                eye.transform.SetParent(skull.transform, false);
                eye.transform.localPosition = new Vector3(e * 0.25f, 0.15f, 0.45f);
                eye.transform.localScale = Vector3.one * 0.15f;
                eye.GetComponent<MeshRenderer>().material = eyeMat;
                Destroy(eye.GetComponent<Collider>());
            }

            // Legs
            for (int l = 0; l < 4; l++)
            {
                int side = (l % 2 == 0) ? -1 : 1;
                float yOff = (l < 2) ? 0.2f : -0.2f;

                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Spider_Leg_{l + 1}";
                leg.transform.SetParent(spiderRoot.transform, false);
                leg.transform.localPosition = new Vector3(side * 0.55f, yOff, 0f);
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * -55f);
                leg.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
                leg.GetComponent<MeshRenderer>().material = bodyMat;
                Destroy(leg.GetComponent<Collider>());
            }

            // Solid Sphere Collider & LockOn
            var col = spiderRoot.AddComponent<SphereCollider>();
            col.radius = 0.65f;
            spiderRoot.AddComponent<LockOnTarget>();

            var ai = spiderRoot.AddComponent<SkullwalltulaAI>();
            return spiderRoot;
        }

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (currentHealth <= 0f) return;

            currentHealth -= info.Damage;
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(info.HitPoint, transform.forward);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(transform.position, Vector3.up);
            }

            RupeePickup.SpawnRupee(transform.position, RupeeType.Green);
            Destroy(gameObject, 0.05f);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Knocks player off vine if they climb directly into it
            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive && currentHealth > 0f)
            {
                Vector3 push = (other.transform.position - transform.position).normalized;
                dmg.TakeDamage(new DamageInfo(2f, transform.position, push, 8f, gameObject, false));
            }
        }
    }
}
