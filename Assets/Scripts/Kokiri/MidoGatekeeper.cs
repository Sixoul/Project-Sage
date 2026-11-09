using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ZeldaOoT.Inventory;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Mido the Kokiri Boss / Gatekeeper.
    /// Blocks the path to the Great Deku Tree until Link has both
    /// the Kokiri Sword and Deku Shield equipped.
    /// </summary>
    public class MidoGatekeeper : MonoBehaviour
    {
        [SerializeField] private Transform gateBarrier;
        private bool isPathUnlocked = false;
        private bool playerNearby = false;

        public static GameObject CreateMido(Vector3 position, Quaternion rotation)
        {
            GameObject midoRoot = new GameObject("NPC_Mido");
            midoRoot.transform.position = position;
            midoRoot.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material skinMat = new Material(s) { color = new Color(0.98f, 0.85f, 0.72f) };
            Material greenMat = new Material(s) { color = new Color(0.18f, 0.55f, 0.22f) };
            Material hairMat = new Material(s) { color = new Color(0.85f, 0.45f, 0.15f) }; // Reddish brown hair
            Material beltMat = new Material(s) { color = new Color(0.35f, 0.2f, 0.1f) };

            // Body (Green tunic)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Mido_Body";
            body.transform.SetParent(midoRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            body.GetComponent<MeshRenderer>().material = greenMat;
            Destroy(body.GetComponent<Collider>());

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Mido_Head";
            head.transform.SetParent(midoRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            head.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);
            head.GetComponent<MeshRenderer>().material = skinMat;
            Destroy(head.GetComponent<Collider>());

            // Hair / Hat
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Mido_Hair";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.2f, -0.05f);
            hair.transform.localScale = new Vector3(1.05f, 0.8f, 1.05f);
            hair.GetComponent<MeshRenderer>().material = hairMat;
            Destroy(hair.GetComponent<Collider>());

            // Arms crossed (Stubborn boss stance)
            GameObject arms = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arms.name = "Mido_ArmsCrossed";
            arms.transform.SetParent(midoRoot.transform, false);
            arms.transform.localPosition = new Vector3(0f, 0.72f, 0.22f);
            arms.transform.localScale = new Vector3(0.65f, 0.16f, 0.22f);
            arms.GetComponent<MeshRenderer>().material = greenMat;
            Destroy(arms.GetComponent<Collider>());

            // Solid Capsule Collider & Interaction Trigger
            var col = midoRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.radius = 0.45f;
            col.height = 1.5f;

            var trigger = midoRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.4f;

            var mido = midoRoot.AddComponent<MidoGatekeeper>();
            return midoRoot;
        }

        public void SetGateBarrier(Transform barrier)
        {
            gateBarrier = barrier;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerInventory>() != null)
            {
                playerNearby = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerInventory>() != null)
            {
                playerNearby = false;
            }
        }

        private void Update()
        {
            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (playerNearby && !isPathUnlocked && ePressed)
            {
                TalkToMido();
            }
        }

        public void TalkToMido()
        {
            var inv = FindFirstObjectByType<PlayerInventory>();
            bool hasSword = inv != null && (inv.HasKokiriSword || (inv.ActiveWeapon != null && inv.ActiveWeapon.id.Contains("sword")));
            bool hasShield = inv != null && (inv.HasDekuShield || inv.ActiveShield != null);

            if (hasSword && hasShield)
            {
                // Unlocked!
                string[] passDialogue = new string[]
                {
                    "WHAT?! You actually have a Kokiri Sword AND a Deku Shield?!",
                    "...Humph! Fine! Even though you're just Link, you look somewhat equipped.",
                    "The Great Deku Tree is waiting for you at the end of the forest path. Go on through!"
                };

                ZeldaDialogueBox.Instance.ShowDialogue("Mido", passDialogue, () =>
                {
                    UnlockGate();
                });
            }
            else
            {
                // Blocked!
                string missing = "";
                if (!hasSword && !hasShield) missing = "You don't have a sword OR a shield!";
                else if (!hasSword) missing = "You still need a sword! Search the crawlspace in the training grounds!";
                else missing = "You still need a shield! Go buy a Deku Shield at the Kokiri Shop for 40 Rupees!";

                string[] blockDialogue = new string[]
                {
                    "Hey, you! Link! You can't go through here!",
                    "The Great Deku Tree summoned you, but you don't even have proper gear!",
                    missing,
                    "How do you expect to survive out there without them?! Come back when you're equipped!"
                };

                ZeldaDialogueBox.Instance.ShowDialogue("Mido", blockDialogue);
            }
        }

        private void UnlockGate()
        {
            isPathUnlocked = true;
            StartCoroutine(StepAsideRoutine());
        }

        private IEnumerator StepAsideRoutine()
        {
            // Mido steps to the side
            float elapsed = 0f;
            Vector3 startP = transform.position;
            Vector3 targetP = startP + transform.right * 2.2f;

            while (elapsed < 0.8f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startP, targetP, elapsed / 0.8f);
                yield return null;
            }

            // Lower or disable gate barrier if present
            if (gateBarrier != null)
            {
                gateBarrier.gameObject.SetActive(false);
            }
        }
    }
}
