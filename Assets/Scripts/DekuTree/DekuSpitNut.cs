using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
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
