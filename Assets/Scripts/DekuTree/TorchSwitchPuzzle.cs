using System;
using System.Collections.Generic;
using UnityEngine;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Ancient torch brazier inside the Great Deku Tree.
    /// Can be ignited with a burning Deku Stick. When connected torches
    /// in a puzzle group are ignited, unbars doors or activates platforms.
    /// </summary>
    public class TorchSwitchPuzzle : MonoBehaviour
    {
        [Header("Torch State")]
        [SerializeField] private bool startsLit = false;
        [SerializeField] private GameObject puzzleDoorBars;

        public bool IsLit { get; private set; } = false;
        public static event Action OnTorchLit;

        private Light flameLight;
        private GameObject flameVisual;

        public static GameObject CreateTorch(Vector3 position, bool litByDefault, GameObject doorBars = null)
        {
            GameObject torchObj = new GameObject("DekuTree_Torch");
            torchObj.transform.position = position;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material woodMat = new Material(s) { color = new Color(0.42f, 0.28f, 0.15f) };
            Material metalMat = new Material(s) { color = new Color(0.3f, 0.3f, 0.32f) };

            // Pedestal column
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Torch_Post";
            post.transform.SetParent(torchObj.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            post.transform.localScale = new Vector3(0.35f, 0.75f, 0.35f);
            post.GetComponent<MeshRenderer>().material = woodMat;

            // Bowl / Brazier
            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Torch_Bowl";
            bowl.transform.SetParent(torchObj.transform, false);
            bowl.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            bowl.transform.localScale = new Vector3(0.65f, 0.15f, 0.65f);
            bowl.GetComponent<MeshRenderer>().material = metalMat;

            // Trigger for Deku Stick
            var col = torchObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.0f;
            col.center = new Vector3(0f, 1.6f, 0f);

            var comp = torchObj.AddComponent<TorchSwitchPuzzle>();
            comp.startsLit = litByDefault;
            comp.puzzleDoorBars = doorBars;

            return torchObj;
        }

        private void Start()
        {
            if (startsLit)
            {
                IgniteTorch();
            }
        }

        public void IgniteTorch()
        {
            if (IsLit) return;
            IsLit = true;

            // Flame Light
            GameObject lightObj = new GameObject("FlameLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            flameLight = lightObj.AddComponent<Light>();
            flameLight.type = LightType.Point;
            flameLight.color = new Color(1f, 0.6f, 0.15f);
            flameLight.range = 8.5f;
            flameLight.intensity = 3.5f;

            // Visual Flame Quad
            flameVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flameVisual.name = "Flame_Mesh";
            flameVisual.transform.SetParent(lightObj.transform, false);
            flameVisual.transform.localScale = new Vector3(0.6f, 0.8f, 1f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material fMat = new Material(s);
            fMat.color = new Color(1f, 0.65f, 0.1f);
            fMat.EnableKeyword("_EMISSION");
            fMat.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.1f) * 3f);
            flameVisual.GetComponent<MeshRenderer>().material = fMat;
            Destroy(flameVisual.GetComponent<Collider>());

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(lightObj.transform.position, Vector3.up);
            }

            // Check if connected door bars should be opened
            if (puzzleDoorBars != null)
            {
                puzzleDoorBars.SetActive(false);
            }

            OnTorchLit?.Invoke();
        }

        private void Update()
        {
            if (IsLit && flameLight != null)
            {
                flameLight.intensity = 3.5f + Mathf.Sin(Time.time * 20f) * 0.6f;
            }
        }
    }
}
