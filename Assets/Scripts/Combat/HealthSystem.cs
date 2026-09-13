using System;
using UnityEngine;

namespace ZeldaOoT.Combat
{
    /// <summary>
    /// Classic Zelda Health System using quarters of hearts.
    /// E.g., 3 hearts = 12 HP (each full heart is 4 HP).
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [Tooltip("Max hearts. 1 heart = 4 HP.")]
        [SerializeField] private int maxHearts = 3;
        [SerializeField] private float invincibilityDuration = 0.8f;

        public int MaxHearts => maxHearts;
        public float CurrentHP { get; private set; }
        public float MaxHP => maxHearts * 4f;
        public bool IsAlive => CurrentHP > 0;
        public Transform Transform => transform;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<DamageInfo> OnDamaged;
        public event Action<DamageInfo> OnDamageBlocked;
        public event Action OnDeath;

        /// <summary>
        /// Optional delegate to intercept incoming damage before HP deduction.
        /// Returning true blocks/negates the attack completely.
        /// </summary>
        public Func<DamageInfo, bool> DamageFilter { get; set; }

        private float invincibilityTimer = 0f;
        public bool IsInvincible => invincibilityTimer > 0f;

        private void Awake()
        {
            CurrentHP = MaxHP;
        }

        private void Update()
        {
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }
        }

        public void SetInvincible(float duration)
        {
            invincibilityTimer = Mathf.Max(invincibilityTimer, duration);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || IsInvincible) return;

            // Check if damage is intercepted/blocked (e.g. Shield Block)
            if (DamageFilter != null && DamageFilter(damageInfo))
            {
                OnDamageBlocked?.Invoke(damageInfo);
                return;
            }

            CurrentHP = Mathf.Max(0f, CurrentHP - damageInfo.Amount);
            invincibilityTimer = invincibilityDuration;

            OnDamaged?.Invoke(damageInfo);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);

            if (CurrentHP <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void Revive(float hp = -1f)
        {
            CurrentHP = hp > 0f ? Mathf.Min(MaxHP, hp) : MaxHP;
            invincibilityTimer = 0f;
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void SetHearts(int hearts)
        {
            maxHearts = Mathf.Max(1, hearts);
            CurrentHP = MaxHP;
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }
    }
}
