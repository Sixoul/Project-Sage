using UnityEngine;

namespace ZeldaOoT.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public float KnockbackForce;
        public GameObject Attacker;
        public bool CanBeBlocked;

        public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitDirection, float knockbackForce = 5f, GameObject attacker = null, bool canBeBlocked = true)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            KnockbackForce = knockbackForce;
            Attacker = attacker;
            CanBeBlocked = canBeBlocked;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
        bool IsAlive { get; }
        Transform Transform { get; }
    }
}
