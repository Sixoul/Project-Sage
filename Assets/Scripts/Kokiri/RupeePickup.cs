using UnityEngine;
using ZeldaOoT.Inventory;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Kokiri
{
    public enum RupeeType
    {
        Green = 1,
        Blue = 5,
        Red = 20
    }

    /// <summary>
    /// Collectible Zelda Rupee diamond with spinning bobbing animation,
    /// dynamic color glow, and collection audio/VFX.
    /// </summary>
    public class RupeePickup : MonoBehaviour
    {
        [SerializeField] private RupeeType type = RupeeType.Green;
        [SerializeField] private float rotationSpeed = 160f;
        [SerializeField] private float bobSpeed = 3.5f;
        [SerializeField] private float bobHeight = 0.12f;

        private Vector3 startPos;
        private bool collected = false;

        public static GameObject SpawnRupee(Vector3 position, RupeeType rupeeType)
        {
            GameObject rupee = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rupee.name = $"Rupee_{rupeeType}";
            rupee.transform.position = position + Vector3.up * 0.3f;
            rupee.transform.localScale = new Vector3(0.25f, 0.45f, 0.12f); // Flattened hexagonal diamond shape

            var col = rupee.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var mr = rupee.GetComponent<MeshRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);

            Color rupeeColor = rupeeType switch
            {
                RupeeType.Green => new Color(0.15f, 0.95f, 0.25f),
                RupeeType.Blue => new Color(0.18f, 0.45f, 1f),
                RupeeType.Red => new Color(1f, 0.2f, 0.18f),
                _ => Color.green
            };

            mat.color = rupeeColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", rupeeColor * 1.5f);
            if (mr != null) mr.material = mat;

            var pickup = rupee.AddComponent<RupeePickup>();
            pickup.type = rupeeType;
            return rupee;
        }

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (collected) return;

            // Spin & Bob
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            var inv = other.GetComponentInParent<PlayerInventory>();
            if (inv != null)
            {
                collected = true;
                inv.AddRupees((int)type);

                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlayHitSpark(transform.position, Vector3.up);
                }

                // Pickup pop animation
                StartCoroutine(CollectAnimation());
            }
        }

        private System.Collections.IEnumerator CollectAnimation()
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;
            Vector3 p = transform.position;

            while (elapsed < 0.22f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.22f;
                transform.position = p + Vector3.up * (t * 0.8f);
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
