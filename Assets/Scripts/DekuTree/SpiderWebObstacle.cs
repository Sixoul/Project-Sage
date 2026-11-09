using System.Collections;
using UnityEngine;
using ZeldaOoT.VFX;
using ZeldaOoT.Player;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Spider web blocking doorways or floor openings in the Great Deku Tree.
    /// Can be destroyed in two authentic OoT ways:
    /// 1. Incinerated by touching a burning Deku Stick / flame.
    /// 2. Smashed through by diving from 3F with high terminal velocity!
    /// </summary>
    public class SpiderWebObstacle : MonoBehaviour
    {
        [SerializeField] private bool isFloorWeb = false;
        [SerializeField] private float minBreakVelocity = 12f;

        private bool isDestroyed = false;
        private MeshRenderer meshRenderer;

        public static GameObject CreateWeb(Vector3 position, Vector3 scale, Quaternion rotation, bool isFloor)
        {
            GameObject webObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            webObj.name = isFloor ? "DekuTree_Giant_Floor_Web" : "DekuTree_Door_Web";
            webObj.transform.position = position;
            webObj.transform.localScale = scale;
            webObj.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material webMat = new Material(s);
            webMat.color = new Color(0.9f, 0.95f, 1f, 0.72f);
            webMat.SetFloat("_Surface", 1f); // Transparent
            webMat.SetFloat("_Blend", 0f);
            webMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            webMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            webMat.SetInt("_ZWrite", 0);
            webMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            webObj.GetComponent<MeshRenderer>().material = webMat;

            var comp = webObj.AddComponent<SpiderWebObstacle>();
            comp.isFloorWeb = isFloor;
            return webObj;
        }

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDestroyed) return;

            // 1. Check for player high terminal dive impact (Floor Web)
            var player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null && isFloorWeb)
            {
                float downSpeed = -player.Velocity.y;
                if (downSpeed >= minBreakVelocity)
                {
                    BreakWebWithDive(player.transform.position);
                    return;
                }
            }

            // 2. Check for burning torch or Deku stick
            var stick = other.GetComponentInParent<DekuStickWeapon>();
            if (stick != null && stick.IsLit)
            {
                BurnWeb();
            }
        }

        public void BurnWeb()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            // Explosion/Fire particles
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayBombExplosion(transform.position);
            }

            StartCoroutine(BurnRoutine());
        }

        public void BreakWebWithDive(Vector3 impactPos)
        {
            if (isDestroyed) return;
            isDestroyed = true;

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlaySpinWave(impactPos, 3.5f, 1);
            }

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 0.1f);
        }

        private IEnumerator BurnRoutine()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < 0.45f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.45f;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
