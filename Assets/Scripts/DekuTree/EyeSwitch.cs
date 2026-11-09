using System;
using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Golden Eye Switch mounted above doors inside the Great Deku Tree.
    /// Closes and activates connected doors/ladders when shot by a Slingshot pellet.
    /// </summary>
    public class EyeSwitch : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameObject linkedDoorBars;
        [SerializeField] private GameObject linkedLadder;

        public bool IsAlive => !isActivated;
        public Transform Transform => transform;

        private bool isActivated = false;
        private MeshRenderer pupilRenderer;

        public static GameObject CreateEyeSwitch(Vector3 position, Quaternion rotation, GameObject doorBars = null, GameObject ladder = null)
        {
            GameObject switchRoot = new GameObject("DekuTree_EyeSwitch");
            switchRoot.transform.position = position;
            switchRoot.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material woodMat = new Material(s) { color = new Color(0.4f, 0.25f, 0.12f) };
            Material yellowEyeMat = new Material(s);
            yellowEyeMat.color = new Color(0.95f, 0.85f, 0.15f);
            yellowEyeMat.EnableKeyword("_EMISSION");
            yellowEyeMat.SetColor("_EmissionColor", new Color(0.95f, 0.85f, 0.15f) * 1.5f);

            Material blackPupil = new Material(s) { color = Color.black };

            // Circular wooden frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            frame.name = "Eye_Frame";
            frame.transform.SetParent(switchRoot.transform, false);
            frame.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            frame.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
            frame.GetComponent<MeshRenderer>().material = woodMat;

            // Sclera
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye_Sclera";
            eye.transform.SetParent(switchRoot.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            eye.transform.localScale = new Vector3(0.65f, 0.65f, 0.25f);
            eye.GetComponent<MeshRenderer>().material = yellowEyeMat;

            // Pupil
            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pupil.name = "Eye_Pupil";
            pupil.transform.SetParent(switchRoot.transform, false);
            pupil.transform.localPosition = new Vector3(0f, 0f, -0.16f);
            pupil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pupil.transform.localScale = new Vector3(0.22f, 0.05f, 0.45f); // Slit pupil
            pupil.GetComponent<MeshRenderer>().material = blackPupil;

            var comp = switchRoot.AddComponent<EyeSwitch>();
            comp.linkedDoorBars = doorBars;
            comp.linkedLadder = ladder;
            comp.pupilRenderer = eye.GetComponent<MeshRenderer>();

            return switchRoot;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (isActivated) return;
            ActivateSwitch();
        }

        public void ActivateSwitch()
        {
            if (isActivated) return;
            isActivated = true;

            // Change eye color to bright green
            if (pupilRenderer != null)
            {
                pupilRenderer.material.color = Color.green;
                pupilRenderer.material.SetColor("_EmissionColor", Color.green * 3f);
            }

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(transform.position, transform.forward);
            }

            // Unbar door
            if (linkedDoorBars != null)
            {
                linkedDoorBars.SetActive(false);
            }

            // Lower ladder
            if (linkedLadder != null)
            {
                linkedLadder.SetActive(true);
            }
        }
    }
}
