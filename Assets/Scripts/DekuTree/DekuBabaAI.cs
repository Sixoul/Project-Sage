using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;
using ZeldaOoT.Kokiri;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Deku Baba - Carnivorous snapping forest plant.
    /// Lunges at Link, can be stunned with shield block/slingshot,
    /// and drops Deku Sticks or Deku Nuts when defeated.
    /// </summary>
    public class DekuBabaAI : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 8f;
        [SerializeField] private float lungeRange = 3.5f;

        public bool IsAlive => currentHealth > 0f;
        public Transform Transform => transform;

        private float currentHealth;
        private Transform playerTarget;
        private Transform headTransform;
        private Vector3 headOriginPos;
        private bool isLunging = false;
        private float lungeTimer = 0f;

        public static GameObject CreateDekuBaba(Vector3 position)
        {
            GameObject babaRoot = new GameObject("Enemy_DekuBaba");
            babaRoot.transform.position = position;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material stemMat = new Material(s) { color = new Color(0.18f, 0.48f, 0.16f) };
            Material headMat = new Material(s) { color = new Color(0.2f, 0.35f, 0.65f) }; // Deep indigo-blue bulb
            Material jawMat = new Material(s) { color = new Color(0.85f, 0.25f, 0.25f) };  // Red inner jaw

            // Base stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Baba_Stem";
            stem.transform.SetParent(babaRoot.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stem.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);
            stem.GetComponent<MeshRenderer>().material = stemMat;
            Destroy(stem.GetComponent<Collider>());

            // Head bulb
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Baba_Head";
            head.transform.SetParent(babaRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.1f, 0.2f);
            head.transform.localScale = new Vector3(0.65f, 0.55f, 0.85f);
            head.GetComponent<MeshRenderer>().material = headMat;
            Destroy(head.GetComponent<Collider>());

            // Jaws
            GameObject upperJaw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperJaw.name = "Jaw_Upper";
            upperJaw.transform.SetParent(head.transform, false);
            upperJaw.transform.localPosition = new Vector3(0f, 0.15f, 0.35f);
            upperJaw.transform.localScale = new Vector3(0.7f, 0.2f, 0.5f);
            upperJaw.GetComponent<MeshRenderer>().material = jawMat;
            Destroy(upperJaw.GetComponent<Collider>());

            // Hitbox Collider & LockOn
            var col = babaRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.radius = 0.55f;
            col.height = 1.6f;

            babaRoot.AddComponent<LockOnTarget>();

            var ai = babaRoot.AddComponent<DekuBabaAI>();
            ai.headTransform = head.transform;
            ai.headOriginPos = head.transform.localPosition;

            return babaRoot;
        }

        private void Start()
        {
            currentHealth = maxHealth;
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        private void Update()
        {
            if (currentHealth <= 0f) return;

            if (playerTarget == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTarget = p.transform;
                else return;
            }

            Vector3 toPlayer = (playerTarget.position - transform.position);
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist < 8.0f)
            {
                // Face player
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 8f * Time.deltaTime);

                lungeTimer += Time.deltaTime;
                if (dist <= lungeRange && lungeTimer > 1.8f)
                {
                    lungeTimer = 0f;
                    StartCoroutine(LungeAttack());
                }
            }
        }

        private System.Collections.IEnumerator LungeAttack()
        {
            isLunging = true;
            float elapsed = 0f;

            // Snap forward
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                headTransform.localPosition = headOriginPos + Vector3.forward * (elapsed / 0.25f * 1.6f);
                yield return null;
            }

            // Check hit on player
            Collider[] hits = Physics.OverlapSphere(headTransform.position, 0.8f);
            foreach (var hit in hits)
            {
                var dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive && hit.gameObject != gameObject)
                {
                    dmg.TakeDamage(new DamageInfo(2f, headTransform.position, transform.forward, 5f, gameObject, false));
                }
            }

            // Return back
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                headTransform.localPosition = Vector3.Lerp(headOriginPos + Vector3.forward * 1.6f, headOriginPos, elapsed / 0.35f);
                yield return null;
            }

            headTransform.localPosition = headOriginPos;
            isLunging = false;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (currentHealth <= 0f) return;

            currentHealth -= info.Amount;
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(info.HitPoint, Vector3.up);
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
                ZeldaVFXManager.Instance.PlayHitSpark(transform.position + Vector3.up * 0.8f, Vector3.up);
            }

            // 50% drop Deku Stick, 50% drop Deku Nut / Rupee
            if (Random.value < 0.5f)
            {
                RupeePickup.SpawnRupee(transform.position, RupeeType.Blue);
            }
            else
            {
                RupeePickup.SpawnRupee(transform.position, RupeeType.Green);
            }

            Destroy(gameObject, 0.05f);
        }
    }
}
