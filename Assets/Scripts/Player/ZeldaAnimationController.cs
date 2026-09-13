using System.Collections;
using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.Water;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Player
{
    /// <summary>
    /// Procedural animation controller providing authentic Zelda visual feedback:
    /// idle breathing, running lean, floppy cap tail physics,
    /// 3-step sword combo with dynamic body twists, procedural luminous sword slash arcs,
    /// charging stance with blade vibration/energy sparks, and 360 Spin Attacks.
    /// </summary>
    public class ZeldaAnimationController : MonoBehaviour
    {
        [Header("Model Roots")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform swordArm;
        [SerializeField] private Transform shieldArm;
        [SerializeField] private Transform capTail;

        private PlayerLocomotion locomotion;
        private PlayerCombat combat;
        private ZeldaDodgeController dodge;
        private WaterSwimController swim;
        private HealthSystem health;

        private Vector3 initialModelPos;
        private Quaternion initialModelRot;
        private Quaternion initialSwordRot;
        private Quaternion initialShieldRot;
        private Quaternion initialCapTailRot;

        private float walkCycle = 0f;
        private Coroutine attackAnimRoutine;
        private Coroutine chargeAnimRoutine;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            combat = GetComponent<PlayerCombat>();
            dodge = GetComponent<ZeldaDodgeController>();
            swim = GetComponent<WaterSwimController>();
            health = GetComponent<HealthSystem>();

            if (modelRoot == null) modelRoot = transform.Find("ModelRoot") ?? transform;

            initialModelPos = modelRoot.localPosition;
            initialModelRot = modelRoot.localRotation;
        }

        private void Start()
        {
            if (combat != null)
            {
                combat.OnAttackStarted += HandleAttackAnim;
                combat.OnBlockStateChanged += HandleBlockAnim;
                combat.OnChargeChanged += HandleChargeAnim;
                combat.OnSpinAttackExecuted += HandleSpinAttackAnim;
            }

            if (dodge != null)
            {
                dodge.OnSideHop += HandleSideHopAnim;
                dodge.OnBackflip += HandleBackflipAnim;
            }

            if (health != null)
            {
                health.OnDamaged += (info) => StartCoroutine(DamageBlinkRoutine());
            }
        }

        public void SetupModelTransforms(Transform root, Transform sword, Transform shield)
        {
            modelRoot = root;
            swordArm = sword;
            shieldArm = shield;
            if (modelRoot != null)
            {
                initialModelPos = modelRoot.localPosition;
                initialModelRot = modelRoot.localRotation;
            }
            if (swordArm != null) initialSwordRot = swordArm.localRotation;
            if (shieldArm != null) initialShieldRot = shieldArm.localRotation;
        }

        public void SetupCapTail(Transform tail)
        {
            capTail = tail;
            if (capTail != null) initialCapTailRot = capTail.localRotation;
        }

        private void Update()
        {
            if (modelRoot == null) return;

            // Animate based on state
            if (locomotion.CurrentState == PlayerState.Grounded)
            {
                AnimateLocomotionAndIdle();
            }
            else if (locomotion.CurrentState == PlayerState.Swimming)
            {
                AnimateSwimBob();
            }
            else if (locomotion.CurrentState == PlayerState.Diving)
            {
                AnimateDiving();
            }

            AnimateCapTail();
        }

        private void AnimateLocomotionAndIdle()
        {
            if (combat != null && combat.IsAttacking) return;

            float speed = locomotion.Velocity.magnitude;

            if (speed > 0.4f)
            {
                // Running / Walking stride
                walkCycle += Time.deltaTime * speed * 2.6f;

                float bobY = Mathf.Abs(Mathf.Sin(walkCycle * 2f)) * 0.08f;
                float tiltZ = Mathf.Sin(walkCycle) * 3.5f;
                float leanForward = Mathf.Clamp(speed * 2.2f, 0f, 14f); // Natural forward running lean

                modelRoot.localPosition = initialModelPos + new Vector3(0f, bobY, 0f);
                modelRoot.localRotation = initialModelRot * Quaternion.Euler(leanForward, 0f, tiltZ);

                // Arm swinging while running
                if (swordArm != null && !combat.IsCharging && !combat.IsBlocking)
                {
                    float armSwing = Mathf.Sin(walkCycle) * 22f;
                    swordArm.localRotation = initialSwordRot * Quaternion.Euler(armSwing, 0f, 0f);
                }

                if (combat != null && combat.IsBlocking)
                {
                    if (shieldArm != null)
                    {
                        shieldArm.localRotation = Quaternion.Euler(-60f, 25f, -15f);
                    }
                }
                else if (shieldArm != null)
                {
                    float armSwing = -Mathf.Sin(walkCycle) * 18f;
                    shieldArm.localRotation = initialShieldRot * Quaternion.Euler(armSwing, 0f, 0f);
                }
            }
            else
            {
                // Idle breathing bob
                walkCycle = 0f;
                float breath = Mathf.Sin(Time.time * 2.5f) * 0.015f;
                modelRoot.localPosition = Vector3.Lerp(modelRoot.localPosition, initialModelPos + new Vector3(0f, breath, 0f), 8f * Time.deltaTime);
                modelRoot.localRotation = Quaternion.Slerp(modelRoot.localRotation, initialModelRot, 8f * Time.deltaTime);

                if (swordArm != null && !combat.IsCharging && !combat.IsBlocking)
                {
                    swordArm.localRotation = Quaternion.Slerp(swordArm.localRotation, initialSwordRot, 8f * Time.deltaTime);
                }

                if (combat != null && combat.IsBlocking)
                {
                    if (shieldArm != null)
                    {
                        shieldArm.localRotation = Quaternion.Euler(-60f, 25f, -15f);
                    }
                }
                else if (shieldArm != null)
                {
                    shieldArm.localRotation = Quaternion.Slerp(shieldArm.localRotation, initialShieldRot, 8f * Time.deltaTime);
                }
            }
        }

        private void AnimateCapTail()
        {
            if (capTail == null) return;

            // Cap tail bounces with vertical motion and trails behind movement
            float tailSway = Mathf.Sin(Time.time * 6f) * 4f;
            float tailDrop = Mathf.Clamp(locomotion.Velocity.magnitude * 4f, 0f, 25f);
            capTail.localRotation = initialCapTailRot * Quaternion.Euler(tailDrop + tailSway, 0f, 0f);
        }

        private void AnimateSwimBob()
        {
            float swimT = Time.time * 4.5f;
            float bobY = Mathf.Sin(swimT) * 0.09f;
            float pitch = 28f; // Tilted forward in swimming posture
            modelRoot.localPosition = initialModelPos + new Vector3(0f, bobY, 0f);
            modelRoot.localRotation = Quaternion.Euler(pitch, 0f, Mathf.Sin(swimT * 0.5f) * 6f);
        }

        private void AnimateDiving()
        {
            float diveT = Time.time * 6f;
            float bobY = Mathf.Sin(diveT) * 0.05f;
            float pitch = 78f; // Streamlined horizontal dive posture
            float roll = Mathf.Sin(diveT * 0.7f) * 6f;

            modelRoot.localPosition = initialModelPos + new Vector3(0f, bobY, 0f);
            modelRoot.localRotation = Quaternion.Slerp(modelRoot.localRotation, Quaternion.Euler(pitch, 0f, roll), 8f * Time.deltaTime);

            // Streamline arms backwards while diving through water
            if (swordArm != null && !combat.IsCharging)
            {
                swordArm.localRotation = Quaternion.Slerp(swordArm.localRotation, Quaternion.Euler(50f, 0f, 0f), 8f * Time.deltaTime);
            }
            if (shieldArm != null)
            {
                shieldArm.localRotation = Quaternion.Slerp(shieldArm.localRotation, Quaternion.Euler(50f, 0f, 0f), 8f * Time.deltaTime);
            }
        }

        private void HandleAttackAnim(int step)
        {
            if (attackAnimRoutine != null) StopCoroutine(attackAnimRoutine);
            attackAnimRoutine = StartCoroutine(SlashAnimationRoutine(step));
        }

        private IEnumerator SlashAnimationRoutine(int step)
        {
            float duration = (step == 3) ? 0.44f : (step == 4 ? 0.48f : 0.32f);
            float elapsed = 0f;

            Quaternion startRot = swordArm != null ? swordArm.localRotation : Quaternion.identity;
            bool slashTrailSpawned = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (step == 1)
                {
                    // === COMBO 1: HORIZONTAL SLASH (Right to Left) ===
                    if (t < 0.22f)
                    {
                        // Coiling windup
                        float w = t / 0.22f;
                        if (swordArm != null) swordArm.localRotation = Quaternion.Euler(15f, Mathf.Lerp(0f, -85f, w), -25f);
                        modelRoot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, -22f, w), 0f);
                    }
                    else
                    {
                        // Explosive whip slice across chest
                        float s = (t - 0.22f) / 0.78f;
                        float eased = Mathf.SmoothStep(0f, 1f, s);
                        if (swordArm != null) swordArm.localRotation = Quaternion.Euler(18f, Mathf.Lerp(-85f, 90f, eased), 25f);
                        modelRoot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-22f, 25f, eased), 0f);

                        // Spawn curved luminous slash arc
                        if (!slashTrailSpawned && s >= 0.1f)
                        {
                            slashTrailSpawned = true;
                            CreateSlashArc(transform.position + Vector3.up * 1.05f + transform.forward * 0.75f, Vector3.up, -60f, 120f, 1.35f, new Color(0.85f, 0.95f, 1f, 0.9f));
                        }
                    }
                }
                else if (step == 2)
                {
                    // === COMBO 2: REVERSE RISING SLASH (Left to Right) ===
                    if (t < 0.2f)
                    {
                        float w = t / 0.2f;
                        if (swordArm != null) swordArm.localRotation = Quaternion.Euler(-15f, Mathf.Lerp(90f, 85f, w), 20f);
                        modelRoot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(25f, 30f, w), 0f);
                    }
                    else
                    {
                        float s = (t - 0.2f) / 0.8f;
                        float eased = Mathf.SmoothStep(0f, 1f, s);
                        if (swordArm != null) swordArm.localRotation = Quaternion.Euler(35f, Mathf.Lerp(85f, -80f, eased), -30f);
                        modelRoot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(30f, -25f, eased), 0f);

                        if (!slashTrailSpawned && s >= 0.1f)
                        {
                            slashTrailSpawned = true;
                            Vector3 diagNormal = (transform.up + transform.right * 0.4f).normalized;
                            CreateSlashArc(transform.position + Vector3.up * 1.15f + transform.forward * 0.75f, diagNormal, 45f, -120f, 1.4f, new Color(0.2f, 0.85f, 1f, 0.95f));
                        }
                    }
                }
                else if (step == 3)
                {
                    // === COMBO 3: HEROIC OVERHEAD CLEAVE / FINISHER ===
                    if (t < 0.3f)
                    {
                        // High overhead windup
                        float w = t / 0.3f;
                        if (swordArm != null) swordArm.localRotation = Quaternion.Euler(Mathf.Lerp(20f, -105f, w), 0f, 0f);
                        modelRoot.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -12f, w), 0f, 0f);
                    }
                    else
                    {
                        // Downward ground slam
                        float s = (t - 0.3f) / 0.7f;
                        float eased = Mathf.Pow(s, 2.5f); // Fast downward snap
                        if (swordArm != null) swordArm.localRotation = Quaternion.Euler(Mathf.Lerp(-105f, 75f, eased), 0f, 0f);
                        modelRoot.localRotation = Quaternion.Euler(Mathf.Lerp(-12f, 18f, eased), 0f, 0f);

                        if (!slashTrailSpawned && s >= 0.15f)
                        {
                            slashTrailSpawned = true;
                            CreateSlashArc(transform.position + Vector3.up * 1.1f + transform.forward * 0.9f, transform.right, -80f, 140f, 1.55f, new Color(1f, 0.9f, 0.3f, 0.95f));
                        }
                    }
                }
                else if (step == 4)
                {
                    // === JUMP ATTACK: DOWNWARD PLUNGE ===
                    float overhead = Mathf.Lerp(-95f, 80f, Mathf.Pow(t, 2f));
                    if (swordArm != null) swordArm.localRotation = Quaternion.Euler(overhead, 0f, 0f);
                    modelRoot.localRotation = Quaternion.Euler(15f, 0f, 0f);

                    if (!slashTrailSpawned && t >= 0.25f)
                    {
                        slashTrailSpawned = true;
                        CreateSlashArc(transform.position + Vector3.up * 0.8f + transform.forward * 0.8f, transform.right, -90f, 150f, 1.6f, new Color(0.3f, 0.9f, 1f, 0.95f));
                    }
                }

                yield return null;
            }

            // Smoothly ease back to ready stance
            float resetTime = 0.12f;
            float resetElapsed = 0f;
            Quaternion currentArmRot = swordArm != null ? swordArm.localRotation : initialSwordRot;
            Quaternion currentBodyRot = modelRoot.localRotation;

            while (resetElapsed < resetTime)
            {
                resetElapsed += Time.deltaTime;
                float r = resetElapsed / resetTime;
                if (swordArm != null) swordArm.localRotation = Quaternion.Slerp(currentArmRot, initialSwordRot, r);
                modelRoot.localRotation = Quaternion.Slerp(currentBodyRot, initialModelRot, r);
                yield return null;
            }

            if (swordArm != null) swordArm.localRotation = initialSwordRot;
            modelRoot.localRotation = initialModelRot;
        }

        private void HandleBlockAnim(bool isBlocking)
        {
            if (shieldArm != null)
            {
                // Bring shield arm up and forward across the chest facing forward (+X brings arm up and forward, -Y angles across chest)
                shieldArm.localRotation = isBlocking ? Quaternion.Euler(55f, -30f, 15f) : initialShieldRot;
            }
        }

        private void HandleChargeAnim(int level)
        {
            if (level > 0)
            {
                if (chargeAnimRoutine != null) StopCoroutine(chargeAnimRoutine);
                chargeAnimRoutine = StartCoroutine(ChargePoseRoutine(level));
            }
            else
            {
                if (chargeAnimRoutine != null) StopCoroutine(chargeAnimRoutine);
                if (swordArm != null) swordArm.localRotation = initialSwordRot;
            }
        }

        private IEnumerator ChargePoseRoutine(int level)
        {
            while (true)
            {
                // Sword drawn back low near right hip, vibrating with energy
                float freq = level == 2 ? 45f : 28f;
                float amp = level == 2 ? 4.5f : 2.2f;
                float shake = Mathf.Sin(Time.time * freq) * amp;

                if (swordArm != null)
                {
                    swordArm.localRotation = Quaternion.Euler(38f, -78f + shake, -45f);
                }

                // Lower stance slightly
                modelRoot.localPosition = initialModelPos + new Vector3(0f, -0.08f, 0f);

                // Emit charge spark particle
                if (Time.frameCount % (level == 2 ? 4 : 8) == 0)
                {
                    CreateChargeSpark(level);
                }

                yield return null;
            }
        }

        private void HandleSpinAttackAnim(int level)
        {
            if (attackAnimRoutine != null) StopCoroutine(attackAnimRoutine);
            attackAnimRoutine = StartCoroutine(SpinAttackAnimationRoutine(level));
        }

        private IEnumerator SpinAttackAnimationRoutine(int level)
        {
            float duration = level == 2 ? 0.65f : 0.44f;
            float totalSpin = level == 2 ? 720f : 360f;
            float elapsed = 0f;

            // Spawn radiant full-circle slash arc
            Color spinColor = level == 2 ? new Color(0.2f, 0.9f, 1f, 0.95f) : new Color(0.35f, 1f, 0.45f, 0.9f);
            CreateSlashArc(transform.position + Vector3.up * 0.95f, Vector3.up, 0f, 360f, level == 2 ? 2.4f : 1.8f, spinColor, 0.35f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentAngle = Mathf.Lerp(0f, totalSpin, Mathf.SmoothStep(0f, 1f, t));
                modelRoot.localRotation = Quaternion.Euler(0f, currentAngle, 0f);

                // Sword held horizontal flat at waist height
                if (swordArm != null) swordArm.localRotation = Quaternion.Euler(18f, 95f, 0f);

                yield return null;
            }

            if (swordArm != null) swordArm.localRotation = initialSwordRot;
            modelRoot.localRotation = initialModelRot;
        }

        private void CreateSlashArc(Vector3 center, Vector3 normal, float startAngle, float sweepAngle, float radius, Color color, float lifetime = 0.24f)
        {
            GameObject arcObj = new GameObject("SwordSlashArc");
            arcObj.transform.position = center;
            arcObj.transform.rotation = Quaternion.LookRotation(normal);

            MeshFilter mf = arcObj.AddComponent<MeshFilter>();
            MeshRenderer mr = arcObj.AddComponent<MeshRenderer>();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(urpLit);
            mat.color = color;
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glow
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mr.material = mat;

            // Generate ribbon crescent mesh
            int segments = 24;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            int[] triangles = new int[segments * 6];

            float innerRad = radius * 0.55f;
            float outerRad = radius;

            for (int i = 0; i <= segments; i++)
            {
                float frac = (float)i / segments;
                float angle = (startAngle + sweepAngle * frac) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * innerRad, sin * innerRad, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * outerRad, sin * outerRad, 0f);

                if (i < segments)
                {
                    int root = i * 2;
                    triangles[i * 6] = root;
                    triangles[i * 6 + 1] = root + 1;
                    triangles[i * 6 + 2] = root + 2;

                    triangles[i * 6 + 3] = root + 1;
                    triangles[i * 6 + 4] = root + 3;
                    triangles[i * 6 + 5] = root + 2;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            Destroy(arcObj, lifetime);
        }

        private void CreateChargeSpark(int level)
        {
            Vector3 sparkPos = (swordArm != null ? swordArm.position : transform.position) + transform.forward * 0.6f + Random.insideUnitSphere * 0.2f;
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(sparkPos, Random.onUnitSphere);
            }
        }

        private void HandleSideHopAnim(bool isRight)
        {
            StartCoroutine(SideHopTiltRoutine(isRight));
        }

        private IEnumerator SideHopTiltRoutine(bool isRight)
        {
            float duration = 0.38f;
            float elapsed = 0f;
            float tiltAmount = isRight ? -25f : 25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float tilt = Mathf.Sin(t * Mathf.PI) * tiltAmount;
                modelRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
                yield return null;
            }

            modelRoot.localRotation = initialModelRot;
        }

        private void HandleBackflipAnim()
        {
            StartCoroutine(BackflipRoutine());
        }

        private IEnumerator BackflipRoutine()
        {
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float pitch = Mathf.Lerp(0f, -360f, t);
                modelRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                yield return null;
            }

            modelRoot.localRotation = initialModelRot;
        }

        private IEnumerator DamageBlinkRoutine()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < 4; i++)
            {
                foreach (var r in renderers) r.enabled = false;
                yield return new WaitForSeconds(0.08f);
                foreach (var r in renderers) r.enabled = true;
                yield return new WaitForSeconds(0.08f);
            }
        }
    }
}
