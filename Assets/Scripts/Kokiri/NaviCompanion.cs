using UnityEngine;
using ZeldaOoT.Player;
using ZeldaOoT.Inventory;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Navi the Fairy companion.
    /// Orbits Link with glowing wings, fairy dust trail, and context-sensitive advice.
    /// </summary>
    public class NaviCompanion : MonoBehaviour
    {
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private float orbitDistance = 0.85f;
        [SerializeField] private float orbitSpeed = 2.4f;
        [SerializeField] private float bobSpeed = 4.2f;
        [SerializeField] private float bobHeight = 0.15f;

        private Transform leftWing;
        private Transform rightWing;
        private float angle = 0f;

        public static GameObject CreateNavi(Transform player)
        {
            GameObject naviRoot = new GameObject("Navi_The_Fairy");
            naviRoot.transform.position = player.position + Vector3.up * 1.5f + Vector3.right * 0.8f;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material glowMat = new Material(s);
            Color fairyColor = new Color(0.35f, 0.85f, 1f, 0.95f);
            glowMat.color = fairyColor;
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", fairyColor * 3.5f);

            // Glowing Fairy Orb
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Navi_Core";
            orb.transform.SetParent(naviRoot.transform, false);
            orb.transform.localScale = Vector3.one * 0.22f;
            orb.GetComponent<MeshRenderer>().material = glowMat;
            Destroy(orb.GetComponent<Collider>());

            // Fairy Light
            Light light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = fairyColor;
            light.range = 5.5f;
            light.intensity = 2.5f;

            // Translucent Fluttering Wings
            Material wingMat = new Material(s);
            wingMat.color = new Color(0.85f, 0.95f, 1f, 0.6f);
            wingMat.SetFloat("_Surface", 1f);
            wingMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            GameObject lWing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lWing.name = "Wing_Left";
            lWing.transform.SetParent(orb.transform, false);
            lWing.transform.localPosition = new Vector3(-0.16f, 0.12f, 0f);
            lWing.transform.localScale = new Vector3(0.28f, 0.45f, 1f);
            lWing.GetComponent<MeshRenderer>().material = wingMat;
            Destroy(lWing.GetComponent<Collider>());

            GameObject rWing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rWing.name = "Wing_Right";
            rWing.transform.SetParent(orb.transform, false);
            rWing.transform.localPosition = new Vector3(0.16f, 0.12f, 0f);
            rWing.transform.localScale = new Vector3(0.28f, 0.45f, 1f);
            rWing.GetComponent<MeshRenderer>().material = wingMat;
            Destroy(rWing.GetComponent<Collider>());

            var navi = naviRoot.AddComponent<NaviCompanion>();
            navi.targetPlayer = player;
            navi.leftWing = lWing.transform;
            navi.rightWing = rWing.transform;

            return naviRoot;
        }

        private void Update()
        {
            if (targetPlayer == null)
            {
                var player = FindFirstObjectByType<PlayerLocomotion>();
                if (player != null) targetPlayer = player.transform;
                else return;
            }

            // Orbit around player's head
            angle += orbitSpeed * Time.deltaTime;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * orbitDistance, 1.45f + Mathf.Sin(Time.time * bobSpeed) * bobHeight, Mathf.Sin(angle) * orbitDistance);
            transform.position = Vector3.Lerp(transform.position, targetPlayer.position + offset, 12f * Time.deltaTime);

            // Wing flutter
            float wingFlap = Mathf.Sin(Time.time * 45f) * 35f;
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0f, -wingFlap, 25f);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0f, wingFlap, -25f);

            // Press 'C' for Navi Hint
            if (UnityEngine.Input.GetKeyDown(KeyCode.C) && ZeldaDialogueBox.Instance != null && !ZeldaDialogueBox.Instance.IsOpen)
            {
                ProvideHint();
            }
        }

        public void ProvideHint()
        {
            var inv = targetPlayer.GetComponent<PlayerInventory>();
            string[] hint;

            if (inv == null || (!inv.HasKokiriSword && !inv.HasDekuShield))
            {
                hint = new string[]
                {
                    "Hey! Listen!",
                    "The Great Deku Tree has called for you!",
                    "Before Mido will let us pass, we need to find the Kokiri Sword in the training grounds and buy the Deku Shield at the shop!"
                };
            }
            else if (!inv.HasKokiriSword)
            {
                hint = new string[]
                {
                    "Hey! Look!",
                    "We still need a sword! Search the crawlspace hole past the rolling boulder in the forest meadow!"
                };
            }
            else if (!inv.HasDekuShield)
            {
                hint = new string[]
                {
                    "Hey! Listen!",
                    "You need 40 Rupees to buy the Deku Shield from the Kokiri Shop. Slash grass or jump across river rocks to find Rupees!"
                };
            }
            else if (!inv.HasFairySlingshot)
            {
                hint = new string[]
                {
                    "Hey! Listen!",
                    "Inside the Great Deku Tree, search the upper branches for the Fairy Slingshot to hit eye switches from afar!"
                };
            }
            else
            {
                hint = new string[]
                {
                    "Watch out! The monster Queen Gohma is lurking within the roots of the Great Deku Tree!",
                    "Shoot her glowing red eye with the Slingshot to stun her, then attack with your Kokiri Sword!"
                };
            }

            ZeldaDialogueBox.Instance.ShowDialogue("Navi", hint);
        }
    }
}
