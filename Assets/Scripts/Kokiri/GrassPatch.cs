using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Slashes when struck by Link's sword, dropping Rupees, Hearts, or Deku Seeds.
    /// Re-creates classic Ocarina of Time forest foliage slashing.
    /// </summary>
    public class GrassPatch : MonoBehaviour, IDamageable
    {
        public bool IsAlive => !isCut;
        public Transform Transform => transform;

        private bool isCut = false;

        public static GameObject CreateGrassPatch(Vector3 position, Material grassMat)
        {
            GameObject patch = new GameObject("Grass_Patch");
            patch.transform.position = position;

            // Clump of 3-4 criss-crossed grass blades
            for (int i = 0; i < 3; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(patch.transform, false);
                blade.transform.localPosition = Vector3.up * 0.35f;
                blade.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                blade.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

                var col = blade.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mr = blade.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = grassMat;
            }

            // Box collider for sword interaction
            var boxCol = patch.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.35f, 0f);
            boxCol.size = new Vector3(0.9f, 0.7f, 0.9f);
            boxCol.isTrigger = false; // solid to swords, pass-through to players

            patch.AddComponent<GrassPatch>();
            return patch;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (isCut) return;
            CutGrass();
        }

        public void CutGrass()
        {
            if (isCut) return;
            isCut = true;

            // Grass cutting particles
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(transform.position + Vector3.up * 0.3f, Vector3.up);
            }

            // Random drop
            float roll = Random.value;
            if (roll < 0.50f)
            {
                // Green rupee (50%)
                RupeePickup.SpawnRupee(transform.position, RupeeType.Green);
            }
            else if (roll < 0.70f)
            {
                // Blue rupee (20%)
                RupeePickup.SpawnRupee(transform.position, RupeeType.Blue);
            }
            else if (roll < 0.85f)
            {
                // Deku seeds drop (15%)
                RupeePickup.SpawnRupee(transform.position, RupeeType.Green);
            }

            // Shrink and destroy
            StartCoroutine(CutAnimation());
        }

        private System.Collections.IEnumerator CutAnimation()
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.12f;
                transform.localScale = Vector3.Lerp(initScale, new Vector3(initScale.x * 1.2f, 0.05f, initScale.z * 1.2f), t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
