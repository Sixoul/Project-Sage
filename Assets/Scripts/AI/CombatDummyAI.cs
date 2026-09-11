using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeldaOoT.Combat;

namespace ZeldaOoT.AI
{
    public enum DummyState
    {
        Idle,
        Circling,
        TelegraphingAttack,
        Attacking,
        HitStun,
        Defeated
    }

    /// <summary>
    /// Interactive Sparring Dummy / Stalfos enemy for testing Zelda combat mechanics:
    /// lock-on target, hit reactions, and telegraphed attacks for testing shield blocks and dodges.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(LockOnTarget))]
    public class CombatDummyAI : MonoBehaviour, IDamageable
    {
        [Header("AI Settings")]
        [SerializeField] private float detectionRadius = 15f;
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float attackCooldown = 3.5f;
        [SerializeField] private float telegraphDuration = 1.0f; // Windup warning

        public DummyState State { get; private set; } = DummyState.Idle;
        public bool IsAlive => healthSystem != null && healthSystem.IsAlive;
        public Transform Transform => transform;

        private HealthSystem healthSystem;
        private Transform playerTarget;
        private MeshRenderer meshRenderer;
        private Color defaultColor = new Color(0.7f, 0.2f, 0.2f); // Crimson enemy
        private float lastAttackTime = 0f;
        private CharacterController characterController;
        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private Vector3 initialScale;

        private static readonly Queue<CombatDummyAI> respawnQueue = new Queue<CombatDummyAI>();
        private static CombatDummyAI queueWorker = null;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            characterController = GetComponent<CharacterController>();
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;
            initialScale = transform.localScale;
        }

        private void Start()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDamaged += HandleDamaged;
                healthSystem.OnDeath += HandleDeath;
            }

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        private void Update()
        {
            if (!IsAlive || State == DummyState.HitStun || State == DummyState.Defeated) return;

            if (playerTarget == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTarget = player.transform;
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Always face player when in combat range
            Vector3 lookDir = (playerTarget.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 6f * Time.deltaTime);
            }

            if (State == DummyState.TelegraphingAttack || State == DummyState.Attacking)
            {
                return;
            }

            if (distToPlayer <= detectionRadius)
            {
                if (distToPlayer > attackRange)
                {
                    // Advance towards player
                    Vector3 moveDir = lookDir.normalized * moveSpeed;
                    moveDir.y = -9.8f;
                    if (characterController != null) characterController.Move(moveDir * Time.deltaTime);
                    State = DummyState.Circling;
                }
                else
                {
                    // In attack range
                    if (Time.time - lastAttackTime > attackCooldown)
                    {
                        StartCoroutine(ExecuteTelegraphedAttack());
                    }
                }
            }
            else
            {
                State = DummyState.Idle;
            }
        }

        private IEnumerator ExecuteTelegraphedAttack()
        {
            State = DummyState.TelegraphingAttack;
            lastAttackTime = Time.time;

            // Telegraph windup: Flash bright orange/yellow so player has time to raise shield or dodge
            float elapsed = 0f;
            while (elapsed < telegraphDuration)
            {
                elapsed += Time.deltaTime;
                float flash = Mathf.PingPong(elapsed * 8f, 1f);
                if (meshRenderer != null && meshRenderer.material != null)
                {
                    meshRenderer.material.color = Color.Lerp(defaultColor, Color.yellow, flash);
                }
                yield return null;
            }

            // Swing Attack
            State = DummyState.Attacking;
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = Color.red;
            }

            yield return new WaitForSeconds(0.15f);

            // Deal damage if player is still in front and within range
            if (playerTarget != null)
            {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toPlayer);

                if (dist <= attackRange * 1.3f && angle < 75f)
                {
                    var playerDmg = playerTarget.GetComponent<IDamageable>();
                    if (playerDmg != null)
                    {
                        DamageInfo info = new DamageInfo(2f, transform.position + transform.forward * 1.5f, transform.forward, 6f, gameObject, true);
                        playerDmg.TakeDamage(info);
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);

            // Reset back to normal
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = defaultColor;
            }

            State = DummyState.Idle;
        }

        private void HandleDamaged(DamageInfo info)
        {
            StartCoroutine(HitStunRoutine(info.HitDirection, info.KnockbackForce));
        }

        private IEnumerator HitStunRoutine(Vector3 knockDir, float knockForce)
        {
            State = DummyState.HitStun;
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = Color.white; // Hit flash
            }

            // Apply knockback
            float timer = 0.25f;
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (characterController != null)
                {
                    characterController.Move(knockDir * knockForce * Time.deltaTime);
                }
                yield return null;
            }

            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = defaultColor;
            }

            if (IsAlive)
            {
                State = DummyState.Idle;
            }
        }

        private void HandleDeath()
        {
            State = DummyState.Defeated;
            StartCoroutine(DeathAndEnqueueRoutine());
        }

        private IEnumerator DeathAndEnqueueRoutine()
        {
            // Disable CharacterController during death so Link won't collide with defeated dummy
            if (characterController != null) characterController.enabled = false;

            // Fall over / shrink away
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / 0.6f);
                yield return null;
            }
            transform.localScale = Vector3.zero;

            // Register into sequential respawn queue
            if (!respawnQueue.Contains(this))
            {
                respawnQueue.Enqueue(this);
            }

            // Ensure queue coordinator is actively processing
            if (queueWorker == null || !queueWorker.gameObject.activeInHierarchy)
            {
                queueWorker = this;
                StartCoroutine(ProcessRespawnQueueRoutine());
            }
        }

        private static IEnumerator ProcessRespawnQueueRoutine()
        {
            while (respawnQueue.Count > 0)
            {
                // Take longer to respawn enemies (8.0 seconds)
                yield return new WaitForSeconds(8.0f);

                if (respawnQueue.Count > 0)
                {
                    CombatDummyAI nextDummy = respawnQueue.Dequeue();
                    if (nextDummy != null)
                    {
                        nextDummy.ExecuteRespawn();
                    }
                }

                // Make them respawn one at a time: wait 4.0 seconds between individual respawns
                if (respawnQueue.Count > 0)
                {
                    yield return new WaitForSeconds(4.0f);
                }
            }

            queueWorker = null;
        }

        private void ExecuteRespawn()
        {
            // Snap back to initial spawn position and rotation on ground
            transform.position = initialSpawnPosition;
            transform.rotation = initialSpawnRotation;
            transform.localScale = initialScale;

            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = defaultColor;
            }

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            if (healthSystem != null)
            {
                healthSystem.Revive(healthSystem.MaxHP);
            }

            State = DummyState.Idle;
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDamaged -= HandleDamaged;
                healthSystem.OnDeath -= HandleDeath;
            }

            if (queueWorker == this)
            {
                queueWorker = null;
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(damageInfo);
            }
        }
    }
}
