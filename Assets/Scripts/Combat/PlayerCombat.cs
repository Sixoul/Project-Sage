using System;
using System.Collections;
using UnityEngine;
using ZeldaOoT.Player;

namespace ZeldaOoT.Combat
{
    /// <summary>
    /// Implements Zelda sword combat (3-hit combo, jump attack) and shield blocking.
    /// Manages attack timings, hitbox activation windows, and directional shield defense.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private MeleeHitbox activeHitbox;
        [SerializeField] private float baseDamage = 2f;
        [SerializeField] private float comboResetTime = 0.6f;
        [SerializeField] private float maxBlockAngle = 65f; // Frontal arc: 130 degrees total

        public bool IsAttacking { get; private set; }
        public bool IsBlocking { get; private set; }
        public bool IsCharging { get; private set; }
        public int ChargeLevel { get; private set; } = 0; // 0 = none, 1 = charged, 2 = super charged
        public int ComboStep { get; private set; } = 0;

        public event Action<int> OnAttackStarted; // combo index: 1, 2, 3
        public event Action OnAttackEnded;
        public event Action<bool> OnBlockStateChanged; // isBlocking
        public event Action<Vector3> OnBlockSuccess; // hit point
        public event Action<int> OnChargeChanged; // level
        public event Action<int> OnSpinAttackExecuted; // level

        private PlayerLocomotion playerLocomotion;
        private HealthSystem healthSystem;
        private Coroutine currentAttackRoutine;
        private float lastAttackEndTime = 0f;
        private float attackHoldTimer = 0f;
        private bool isTrackingHold = false;

        private void Awake()
        {
            playerLocomotion = GetComponent<PlayerLocomotion>();
            healthSystem = GetComponent<HealthSystem>();
        }

        private void Start()
        {
            if (healthSystem != null)
            {
                // Hook into damage handling to intercept with shield block
                healthSystem.OnDamaged += HandleDamageAttempt;
            }
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDamaged -= HandleDamageAttempt;
            }
        }

        public void SetHitbox(MeleeHitbox hitbox)
        {
            activeHitbox = hitbox;
        }

        public void SetBlock(bool block)
        {
            if (IsAttacking) return;

            if (IsBlocking != block)
            {
                IsBlocking = block;
                OnBlockStateChanged?.Invoke(IsBlocking);

                if (IsBlocking)
                {
                    if (playerLocomotion != null && playerLocomotion.CurrentState == PlayerState.Grounded)
                    {
                        playerLocomotion.SetState(PlayerState.Blocking);
                    }
                }
                else
                {
                    if (playerLocomotion != null && playerLocomotion.CurrentState == PlayerState.Blocking)
                    {
                        playerLocomotion.SetState(PlayerState.Grounded);
                    }
                }
            }
        }

        public void ProcessAttackInput(bool attackPressed, bool attackHeld, bool attackReleased, bool isAirborne)
        {
            if (IsAttacking) return;

            // Airborne attack triggered immediately
            if (isAirborne && attackPressed)
            {
                currentAttackRoutine = StartCoroutine(PerformJumpAttack());
                return;
            }

            // Grounded attack hold / charge detection
            if (attackPressed)
            {
                isTrackingHold = true;
                attackHoldTimer = 0f;
            }

            if (isTrackingHold && attackHeld)
            {
                attackHoldTimer += Time.deltaTime;

                // Begin charging after holding for 0.32s
                if (attackHoldTimer >= 0.32f)
                {
                    if (!IsCharging)
                    {
                        IsCharging = true;
                        ChargeLevel = 1;
                        OnChargeChanged?.Invoke(ChargeLevel);
                    }
                    else if (attackHoldTimer >= 1.2f && ChargeLevel < 2)
                    {
                        // Super Charge!
                        ChargeLevel = 2;
                        OnChargeChanged?.Invoke(ChargeLevel);
                    }
                }
            }

            if (attackReleased && isTrackingHold)
            {
                isTrackingHold = false;

                if (IsCharging)
                {
                    // Unleash Spin Attack!
                    currentAttackRoutine = StartCoroutine(PerformSpinAttack(ChargeLevel));
                    IsCharging = false;
                    ChargeLevel = 0;
                    OnChargeChanged?.Invoke(0);
                }
                else
                {
                    // Regular combo tap
                    ExecuteComboStep();
                }

                attackHoldTimer = 0f;
            }
        }

        private void ExecuteComboStep()
        {
            if (IsAttacking) return;

            // Check combo timer reset
            if (Time.time - lastAttackEndTime > comboResetTime)
            {
                ComboStep = 0;
            }

            ComboStep++;
            if (ComboStep > 3) ComboStep = 1;

            currentAttackRoutine = StartCoroutine(PerformComboAttack(ComboStep));
        }

        public bool TryAttack(bool isAirborne)
        {
            if (IsAttacking) return false;

            if (isAirborne)
            {
                currentAttackRoutine = StartCoroutine(PerformJumpAttack());
            }
            else
            {
                ExecuteComboStep();
            }

            return true;
        }

        private IEnumerator PerformComboAttack(int step)
        {
            IsAttacking = true;
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Attacking);

            OnAttackStarted?.Invoke(step);

            float attackDuration = step == 3 ? 0.45f : 0.35f;
            float damageMult = step == 3 ? 2f : 1f;

            // Small forward step during swing
            Vector3 lunge = transform.forward * (step == 3 ? 4.5f : 2.5f);
            var cc = GetComponent<CharacterController>();

            float elapsed = 0f;
            bool hitboxFired = false;

            while (elapsed < attackDuration)
            {
                elapsed += Time.deltaTime;

                // Lunge forward during first half
                if (elapsed < attackDuration * 0.5f && cc != null)
                {
                    cc.Move(lunge * Time.deltaTime);
                }

                // Activate hitbox during mid-swing
                if (!hitboxFired && elapsed >= attackDuration * 0.2f && elapsed <= attackDuration * 0.8f)
                {
                    hitboxFired = true;
                    if (activeHitbox != null)
                    {
                        activeHitbox.ActivateHitbox(baseDamage * damageMult, 6f * damageMult, gameObject);
                    }
                }

                if (hitboxFired && elapsed > attackDuration * 0.8f)
                {
                    if (activeHitbox != null) activeHitbox.DeactivateHitbox();
                }

                yield return null;
            }

            if (activeHitbox != null) activeHitbox.DeactivateHitbox();

            IsAttacking = false;
            lastAttackEndTime = Time.time;
            OnAttackEnded?.Invoke();

            if (playerLocomotion != null && playerLocomotion.CurrentState == PlayerState.Attacking)
            {
                playerLocomotion.SetState(PlayerState.Grounded);
            }
        }

        private IEnumerator PerformJumpAttack()
        {
            IsAttacking = true;
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Attacking);

            OnAttackStarted?.Invoke(4); // 4 = Jump Attack

            float duration = 0.5f;
            float damageMult = 2.5f;
            var cc = GetComponent<CharacterController>();

            if (activeHitbox != null)
            {
                activeHitbox.ActivateHitbox(baseDamage * damageMult, 10f, gameObject);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (cc != null)
                {
                    cc.Move((transform.forward * 5f + Vector3.down * 8f) * Time.deltaTime);
                }
                yield return null;
            }

            if (activeHitbox != null) activeHitbox.DeactivateHitbox();

            IsAttacking = false;
            lastAttackEndTime = Time.time;
            ComboStep = 0;
            OnAttackEnded?.Invoke();

            if (playerLocomotion != null && playerLocomotion.CurrentState == PlayerState.Attacking)
            {
                playerLocomotion.SetState(PlayerState.Grounded);
            }
        }

        private IEnumerator PerformSpinAttack(int level)
        {
            IsAttacking = true;
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Attacking);

            OnAttackStarted?.Invoke(5); // 5 = Spin Attack
            OnSpinAttackExecuted?.Invoke(level);

            float duration = level == 2 ? 0.65f : 0.45f;
            float radius = level == 2 ? 6.5f : 4.8f;
            float damage = baseDamage * (level == 2 ? 6f : 3.5f);
            float knockback = level == 2 ? 18f : 12f;

            // Spawn Zelda Spin Attack energy wave ring
            CreateSpinWaveEffect(radius, level);

            // Deal 360 radial damage to all targets in radius
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.8f, radius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

                var dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    Vector3 blastDir = (dmg.Transform.position - transform.position).normalized;
                    blastDir.y = 0.35f;
                    dmg.TakeDamage(new DamageInfo(damage, hit.ClosestPoint(transform.position), blastDir, knockback, gameObject, true));
                }
            }

            yield return new WaitForSeconds(duration);

            IsAttacking = false;
            lastAttackEndTime = Time.time;
            ComboStep = 0;
            OnAttackEnded?.Invoke();

            if (playerLocomotion != null && playerLocomotion.CurrentState == PlayerState.Attacking)
            {
                playerLocomotion.SetState(PlayerState.Grounded);
            }
        }

        private void CreateSpinWaveEffect(float radius, int level)
        {
            GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wave.name = "SpinAttackWave";
            wave.transform.position = transform.position + Vector3.up * 0.4f;
            wave.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

            var col = wave.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = wave.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                Color waveColor = level == 2 ? new Color(0.2f, 0.85f, 1f) : new Color(0.3f, 1f, 0.4f); // Cyan Great Spin vs Green Spin
                mat.color = waveColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", waveColor * 4f);
                mr.material = mat;
            }

            Destroy(wave, 0.28f);
        }

        private void HandleDamageAttempt(DamageInfo info)
        {
            // If blocking and attack came from the front, deflect it and negate damage
            if (IsBlocking && info.CanBeBlocked)
            {
                Vector3 toAttacker = info.Attacker != null ? (info.Attacker.transform.position - transform.position) : -info.HitDirection;
                toAttacker.y = 0f;
                float angle = Vector3.Angle(transform.forward, toAttacker);

                if (angle <= maxBlockAngle)
                {
                    // Blocked! Heal back the damage taken
                    healthSystem.Heal(info.Amount);
                    OnBlockSuccess?.Invoke(info.HitPoint);

                    // Create blue shield deflect spark
                    CreateBlockSpark(info.HitPoint != Vector3.zero ? info.HitPoint : transform.position + transform.forward * 0.8f + Vector3.up * 1f);
                }
            }
        }

        private void CreateBlockSpark(Vector3 point)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "BlockDeflectSpark";
            spark.transform.position = point;
            spark.transform.localScale = Vector3.one * 0.45f;
            var col = spark.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = spark.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.7f, 1f, 1f); // Zelda shield blue deflect flash
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.cyan * 3f);
                mr.material = mat;
            }

            Destroy(spark, 0.15f);
        }
    }
}
