using System.Collections;
using UnityEngine;
using ZeldaOoT.Inventory;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Deku Stick item used in the Great Deku Tree.
    /// Can catch fire from torches, burns down over 12 seconds,
    /// ignites unlit torches, and incinerates spider webs.
    /// </summary>
    public class DekuStickWeapon : MonoBehaviour
    {
        [SerializeField] private float burnDuration = 12f;
        public bool IsLit { get; private set; } = false;

        private float remainingBurnTime;
        private Light stickFlameLight;
        private GameObject flameEffectObj;

        private void Start()
        {
            // Trigger collider for touching fires/webs
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(0.3f, 1.2f, 0.3f);
            }
        }

        private void Update()
        {
            if (IsLit)
            {
                remainingBurnTime -= Time.deltaTime;
                if (stickFlameLight != null)
                {
                    // Flicker flame
                    stickFlameLight.intensity = 2.0f + Mathf.Sin(Time.time * 25f) * 0.4f;
                }

                if (remainingBurnTime <= 0f)
                {
                    ExtinguishStick(true);
                }
            }
        }

        public void Ignite()
        {
            if (IsLit) return;
            IsLit = true;
            remainingBurnTime = burnDuration;

            // Flame Point Light
            GameObject lightObj = new GameObject("StickFlameLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f); // Stick tip

            stickFlameLight = lightObj.AddComponent<Light>();
            stickFlameLight.type = LightType.Point;
            stickFlameLight.color = new Color(1f, 0.65f, 0.2f);
            stickFlameLight.range = 5.5f;
            stickFlameLight.intensity = 2.5f;

            // Visual flame quad
            flameEffectObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flameEffectObj.name = "Flame_Visual";
            flameEffectObj.transform.SetParent(lightObj.transform, false);
            flameEffectObj.transform.localScale = Vector3.one * 0.35f;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material fMat = new Material(s);
            fMat.color = new Color(1f, 0.7f, 0.15f);
            fMat.EnableKeyword("_EMISSION");
            fMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.15f) * 3f);
            flameEffectObj.GetComponent<MeshRenderer>().material = fMat;
            Destroy(flameEffectObj.GetComponent<Collider>());

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(lightObj.transform.position, Vector3.up);
            }
        }

        public void ExtinguishStick(bool wasBurnedOut)
        {
            IsLit = false;
            if (stickFlameLight != null) Destroy(stickFlameLight.gameObject);
            if (flameEffectObj != null) Destroy(flameEffectObj);

            if (wasBurnedOut)
            {
                // Decrement Deku Sticks count in inventory
                var inv = GetComponentInParent<PlayerInventory>();
                if (inv != null)
                {
                    inv.DekuSticks = Mathf.Max(0, inv.DekuSticks - 1);
                }
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // If stick is unlit, check if touching a lit torch/flame
            if (!IsLit)
            {
                var torch = other.GetComponentInParent<TorchSwitchPuzzle>();
                if (torch != null && torch.IsLit)
                {
                    Ignite();
                    return;
                }
            }

            // If stick is lit, check if touching an unlit torch
            if (IsLit)
            {
                var torch = other.GetComponentInParent<TorchSwitchPuzzle>();
                if (torch != null && !torch.IsLit)
                {
                    torch.IgniteTorch();
                }

                // Check if touching spider web
                var web = other.GetComponentInParent<SpiderWebObstacle>();
                if (web != null)
                {
                    web.BurnWeb();
                }
            }
        }
    }
}
