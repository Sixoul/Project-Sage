using UnityEngine;
using ZeldaOoT.Combat;

namespace ZeldaOoT.Environment
{
    /// <summary>
    /// Destructible wooden crate that shatters into physics debris when hit by sword slashes or bombs.
    /// </summary>
    public class DestructibleCrate : MonoBehaviour, IDamageable
    {
        [SerializeField] private int debrisCount = 6;
        public bool IsAlive { get; private set; } = true;
        public Transform Transform => transform;

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;
            IsAlive = false;

            // Spawn splinter debris
            for (int i = 0; i < debrisCount; i++)
            {
                GameObject splinter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                splinter.name = "CrateDebris";
                splinter.transform.position = transform.position + Random.insideUnitSphere * 0.4f;
                splinter.transform.localScale = new Vector3(Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.4f), Random.Range(0.2f, 0.4f));

                var mr = splinter.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(0.55f, 0.35f, 0.15f); // Wood brown
                    mr.material = mat;
                }

                var rb = splinter.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.linearVelocity = (damageInfo.HitDirection + Random.insideUnitSphere * 0.8f) * Random.Range(4f, 8f);

                Destroy(splinter, 2.5f);
            }

            Destroy(gameObject);
        }
    }
}
