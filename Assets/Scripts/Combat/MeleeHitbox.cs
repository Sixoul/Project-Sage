using System.Collections.Generic;
using UnityEngine;

namespace ZeldaOoT.Combat
{
    /// <summary>
    /// Trigger hitbox attached to weapon/sword for striking enemies and destructible objects.
    /// Handles hit registration, damage application, impact sparks, and hit-stop.
    /// </summary>
    public class MeleeHitbox : MonoBehaviour
    {
        [Header("Hitbox Settings")]
        [SerializeField] private Collider hitCollider;
        [SerializeField] private LayerMask hitMask = ~0;

        private float currentDamage = 2f;
        private float currentKnockback = 6f;
        private GameObject attackerOwner;
        private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();
        private bool isActive = false;

        private void Awake()
        {
            if (hitCollider == null) hitCollider = GetComponent<Collider>();
            if (hitCollider != null)
            {
                hitCollider.isTrigger = true;
                hitCollider.enabled = false;
            }
        }

        public void ActivateHitbox(float damage, float knockback, GameObject owner)
        {
            currentDamage = damage;
            currentKnockback = knockback;
            attackerOwner = owner;
            alreadyHit.Clear();
            isActive = true;
            if (hitCollider != null) hitCollider.enabled = true;
        }

        public void DeactivateHitbox()
        {
            isActive = false;
            if (hitCollider != null) hitCollider.enabled = false;
            alreadyHit.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            if (attackerOwner != null && (other.gameObject == attackerOwner || other.transform.IsChildOf(attackerOwner.transform))) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && !alreadyHit.Contains(damageable) && damageable.IsAlive)
            {
                alreadyHit.Add(damageable);

                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitDir = (damageable.Transform.position - (attackerOwner != null ? attackerOwner.transform.position : transform.position)).normalized;
                hitDir.y = 0.2f;

                DamageInfo dmg = new DamageInfo(currentDamage, hitPoint, hitDir, currentKnockback, attackerOwner, true);
                damageable.TakeDamage(dmg);

                // Spawn simple hit spark / flash effect
                CreateHitSpark(hitPoint);
            }
        }

        private void CreateHitSpark(Vector3 point)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "HitSpark";
            spark.transform.position = point;
            spark.transform.localScale = Vector3.one * 0.35f;
            var col = spark.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = spark.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1f, 0.9f, 0.2f, 1f); // Zelda classic yellow/white flash
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.yellow * 2f);
                mr.material = mat;
            }

            Destroy(spark, 0.12f);
        }
    }
}
