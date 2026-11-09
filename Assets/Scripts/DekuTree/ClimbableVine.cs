using UnityEngine;
using ZeldaOoT.Player;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Climbable Ivy / Vines in the Great Deku Tree.
    /// Allows Link to climb up walls to reach 2F and 3F balconies.
    /// </summary>
    public class ClimbableVine : MonoBehaviour
    {
        [SerializeField] private float climbSpeed = 3.5f;
        [SerializeField] private float topLedgeY = 8.5f;

        public static GameObject CreateVine(Vector3 position, Vector3 scale, Quaternion rotation, float topHeight)
        {
            GameObject vineObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vineObj.name = "Climbable_Ivy_Vine";
            vineObj.transform.position = position;
            vineObj.transform.localScale = scale;
            vineObj.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material vineMat = new Material(s) { color = new Color(0.18f, 0.52f, 0.16f) };
            vineObj.GetComponent<MeshRenderer>().material = vineMat;

            var col = vineObj.GetComponent<Collider>();
            col.isTrigger = true;

            var comp = vineObj.AddComponent<ClimbableVine>();
            comp.topLedgeY = topHeight;

            return vineObj;
        }

        private void OnTriggerStay(Collider other)
        {
            var cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                // If player is holding W (Up) or S (Down)
                float v = UnityEngine.Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(v) > 0.1f)
                {
                    Vector3 climbMotion = Vector3.up * (v * climbSpeed * Time.deltaTime);
                    cc.Move(climbMotion);
                }
            }
        }
    }
}
