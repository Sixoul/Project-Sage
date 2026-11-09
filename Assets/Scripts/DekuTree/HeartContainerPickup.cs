using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    public class HeartContainerPickup : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponentInParent<HealthSystem>();
            if (health != null)
            {
                health.IncreaseMaxHealth(4f); // +1 Full Heart container!
                health.Heal(health.MaxHP);

                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlaySpinWave(transform.position, 4f, 2);
                }

                Destroy(gameObject);
            }
        }
    }
}
