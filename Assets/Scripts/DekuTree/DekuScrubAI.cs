using System.Collections;
using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Deku Scrub enemy in the Great Deku Tree.
    /// Hides underground in its bush, pops up, spits Deku Nuts at Link.
    /// Deflecting the nut with the Deku Shield stuns the scrub.
    /// Participates in the iconic "2-3-1" order puzzle.
    /// </summary>
    public class DekuScrubAI : MonoBehaviour, IDamageable
    {
        [SerializeField] private int scrubNumber = 2; // 1, 2, or 3
        [SerializeField] private float spitInterval = 2.4f;

        public int ScrubNumber => scrubNumber;
        public bool IsStunned => isStunned;
        public bool IsAlive => !isDefeated;
        public Transform Transform => transform;

        private bool isPoppedUp = false;
        private bool isStunned = false;
        private bool isDefeated = false;
        private float spitTimer = 0f;
        private Transform playerTarget;
        private DekuScrubOrderPuzzle puzzleManager;

        private GameObject bushCap;
        private GameObject scrubBody;

        public static GameObject CreateScrub(Vector3 position, int number, DekuScrubOrderPuzzle puzzle)
        {
            GameObject scrubRoot = new GameObject($"DekuScrub_{number}");
            scrubRoot.transform.position = position;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material barkMat = new Material(s) { color = new Color(0.45f, 0.28f, 0.12f) }; // Wooden bark
            Material leafMat = new Material(s) { color = new Color(0.22f, 0.65f, 0.2f) };  // Foliage cap
            Material snoutMat = new Material(s) { color = new Color(0.85f, 0.5f, 0.2f) };  // Wooden blowpipe snout
            Material eyeMat = new Material(s);
            eyeMat.color = Color.yellow;
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", Color.yellow * 2f);

            // Bush Base Mound
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush_Cap";
            bush.transform.SetParent(scrubRoot.transform, false);
            bush.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            bush.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            bush.GetComponent<MeshRenderer>().material = leafMat;
            Destroy(bush.GetComponent<Collider>());

            // Scrub Body
            GameObject body = new GameObject("Scrub_Body");
            body.transform.SetParent(scrubRoot.transform, false);
            body.transform.localPosition = Vector3.zero;

            // Trunk head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Scrub_Head";
            head.transform.SetParent(body.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            head.GetComponent<MeshRenderer>().material = barkMat;
            Destroy(head.GetComponent<Collider>());

            // Snout blowpipe
            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            snout.name = "Scrub_Snout";
            snout.transform.SetParent(body.transform, false);
            snout.transform.localPosition = new Vector3(0f, 0.75f, 0.35f);
            snout.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            snout.transform.localScale = new Vector3(0.2f, 0.22f, 0.2f);
            snout.GetComponent<MeshRenderer>().material = snoutMat;
            Destroy(snout.GetComponent<Collider>());

            // Yellow eyes
            for (int e = -1; e <= 1; e += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = e < 0 ? "Eye_L" : "Eye_R";
                eye.transform.SetParent(body.transform, false);
                eye.transform.localPosition = new Vector3(e * 0.16f, 0.95f, 0.2f);
                eye.transform.localScale = Vector3.one * 0.12f;
                eye.GetComponent<MeshRenderer>().material = eyeMat;
                Destroy(eye.GetComponent<Collider>());
            }

            // Collider
            var col = scrubRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.65f, 0f);
            col.radius = 0.45f;
            col.height = 1.3f;

            var ai = scrubRoot.AddComponent<DekuScrubAI>();
            ai.scrubNumber = number;
            ai.puzzleManager = puzzle;
            ai.bushCap = bush;
            ai.scrubBody = body;

            return scrubRoot;
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        private void Update()
        {
            if (isDefeated || isStunned) return;

            if (playerTarget == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTarget = p.transform;
                else return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Pop up if player is in range (4m to 16m)
            if (distToPlayer < 16f && distToPlayer > 2.5f)
            {
                if (!isPoppedUp) PopUp();

                // Face player
                Vector3 lookDir = (playerTarget.position - transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 8f * Time.deltaTime);
                }

                spitTimer += Time.deltaTime;
                if (spitTimer >= spitInterval)
                {
                    spitTimer = 0f;
                    SpitNut();
                }
            }
            else
            {
                // Duck into ground if player is too far or too close
                if (isPoppedUp) DuckUnderground();
            }
        }

        private void PopUp()
        {
            isPoppedUp = true;
            if (scrubBody != null) scrubBody.SetActive(true);
        }

        private void DuckUnderground()
        {
            isPoppedUp = false;
            if (scrubBody != null) scrubBody.SetActive(false);
        }

        private void SpitNut()
        {
            Vector3 spawnP = transform.position + transform.forward * 0.6f + Vector3.up * 0.75f;
            GameObject nut = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nut.name = "Deku_Nut_Projectile";
            nut.transform.position = spawnP;
            nut.transform.localScale = Vector3.one * 0.28f;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material nMat = new Material(s) { color = new Color(0.72f, 0.45f, 0.18f) };
            nut.GetComponent<MeshRenderer>().material = nMat;

            var rb = nut.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Vector3 dir = (playerTarget.position + Vector3.up * 0.8f - spawnP).normalized;
            rb.linearVelocity = dir * 14f;

            var nutComp = nut.AddComponent<DekuSpitNut>();
            nutComp.Initialize(gameObject, this);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (isDefeated || isStunned) return;

            StunScrub();
        }

        public void StunScrub()
        {
            if (isStunned || isDefeated) return;
            isStunned = true;

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(transform.position + Vector3.up * 0.7f, Vector3.up);
            }

            if (puzzleManager != null)
            {
                puzzleManager.OnScrubStunned(this, scrubNumber);
            }
        }

        public void ResetScrub()
        {
            isStunned = false;
            DuckUnderground();
        }

        public void MarkDefeated()
        {
            isDefeated = true;
            isStunned = true;
        }
    }

    /// <summary>
    /// Projectile spat by Deku Scrub.
    /// If Link blocks with Deku Shield, deflects back at scrub!
    /// </summary>
    public class DekuSpitNut : MonoBehaviour
    {
        private GameObject ownerScrub;
        private DekuScrubAI scrubAI;
        private bool isDeflected = false;
        private Rigidbody rb;

        public void Initialize(GameObject owner, DekuScrubAI ai)
        {
            ownerScrub = owner;
            scrubAI = ai;
            rb = GetComponent<Rigidbody>();
            Destroy(gameObject, 4f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if hit player's shield block
            var combat = collision.gameObject.GetComponentInParent<PlayerCombat>();
            if (combat != null && combat.IsBlocking && !isDeflected)
            {
                // Deflect nut directly back at owner scrub!
                isDeflected = true;
                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlayBlockSpark(collision.GetContact(0).point, collision.GetContact(0).normal);
                }

                if (ownerScrub != null)
                {
                    Vector3 returnDir = (ownerScrub.transform.position + Vector3.up * 0.7f - transform.position).normalized;
                    rb.linearVelocity = returnDir * 18f;
                }
                return;
            }

            // If deflected and hits scrub
            if (isDeflected && collision.gameObject == ownerScrub)
            {
                if (scrubAI != null) scrubAI.StunScrub();
                Destroy(gameObject);
                return;
            }

            // Hits player
            var dmg = collision.gameObject.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive && collision.gameObject != ownerScrub)
            {
                dmg.TakeDamage(new DamageInfo(2f, transform.position, rb.linearVelocity.normalized, 6f, ownerScrub, false));
            }

            Destroy(gameObject);
        }
    }
}
