using System.Collections;
using UnityEngine;
using ZeldaOoT.Combat;

namespace ZeldaOoT.Environment
{
    /// <summary>
    /// Archery target bullseye that registers arrow and item hits, spins, and flashes.
    /// </summary>
    public class TargetBullseye : MonoBehaviour, IDamageable
    {
        [SerializeField] private float spinDegrees = 360f;
        public bool IsAlive => true;
        public Transform Transform => transform;

        private bool isSpinning = false;
        private MeshRenderer meshRenderer;
        private Color originalColor;

        private void Awake()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!isSpinning)
            {
                StartCoroutine(HitSpinRoutine());
            }
        }

        private IEnumerator HitSpinRoutine()
        {
            isSpinning = true;

            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = Color.yellow;
            }

            float duration = 0.5f;
            float elapsed = 0f;
            Quaternion startRot = transform.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float angle = (elapsed / duration) * spinDegrees;
                transform.rotation = startRot * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            transform.rotation = startRot;
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = originalColor;
            }

            isSpinning = false;
        }
    }
}
