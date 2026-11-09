using UnityEngine;
using ZeldaOoT.Inventory;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Kokiri Village Shop.
    /// Allows Link to purchase the Deku Shield (40 Rupees), Deku Sticks, and Recovery Hearts.
    /// </summary>
    public class KokiriShop : MonoBehaviour
    {
        private bool playerNearby = false;

        public static GameObject CreateShopCounter(Vector3 position, Quaternion rotation)
        {
            GameObject shopRoot = new GameObject("Kokiri_Shop_Counter");
            shopRoot.transform.position = position;
            shopRoot.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material woodMat = new Material(s) { color = new Color(0.48f, 0.28f, 0.12f) };
            Material clothMat = new Material(s) { color = new Color(0.85f, 0.25f, 0.2f) }; // Red canopy

            // Wooden Counter Desk
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter_Desk";
            counter.transform.SetParent(shopRoot.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            counter.transform.localScale = new Vector3(2.6f, 1.0f, 0.8f);
            counter.GetComponent<MeshRenderer>().material = woodMat;

            // Canopy roof
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Shop_Canopy";
            canopy.transform.SetParent(shopRoot.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            canopy.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            canopy.transform.localScale = new Vector3(2.8f, 0.15f, 1.6f);
            canopy.GetComponent<MeshRenderer>().material = clothMat;

            // Shopkeeper NPC behind counter
            GameObject shopkeeper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shopkeeper.name = "Shopkeeper_NPC";
            shopkeeper.transform.SetParent(shopRoot.transform, false);
            shopkeeper.transform.localPosition = new Vector3(0f, 0.8f, -0.6f);
            shopkeeper.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            Material skMat = new Material(s) { color = new Color(0.2f, 0.6f, 0.8f) }; // Blue Kokiri clothes
            shopkeeper.GetComponent<MeshRenderer>().material = skMat;

            // Display Deku Shield on counter
            GameObject displayShield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            displayShield.name = "Display_Deku_Shield";
            displayShield.transform.SetParent(counter.transform, false);
            displayShield.transform.localPosition = new Vector3(-0.6f, 0.7f, 0f);
            displayShield.transform.localRotation = Quaternion.Euler(15f, 25f, 0f);
            displayShield.transform.localScale = new Vector3(0.35f, 0.45f, 0.08f);
            Material shieldWood = new Material(s) { color = new Color(0.6f, 0.4f, 0.1f) };
            displayShield.GetComponent<MeshRenderer>().material = shieldWood;

            // Interaction Trigger
            var trigger = shopRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 1.2f);
            trigger.size = new Vector3(3f, 2f, 2.5f);

            shopRoot.AddComponent<KokiriShop>();
            return shopRoot;
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
            if (playerNearby && UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                OpenShopMenu();
            }
        }

        public void OpenShopMenu()
        {
            var inv = FindFirstObjectByType<PlayerInventory>();
            if (inv == null) return;

            if (!inv.HasDekuShield)
            {
                if (inv.Rupees >= 40)
                {
                    // Can buy Deku Shield!
                    inv.SpendRupees(40);
                    inv.AwardSpecialItem("deku_shield", "Deku Shield");

                    if (ZeldaVFXManager.Instance != null)
                    {
                        ZeldaVFXManager.Instance.PlayHitSpark(transform.position + Vector3.up * 1f, Vector3.up);
                    }

                    string[] buyDialogue = new string[]
                    {
                        "Welcome to the Kokiri Shop!",
                        "Thank you! You bought the Deku Shield for 40 Rupees!",
                        "It's carved from the bark of the Great Deku Tree. Hold [Right Click] or [Ctrl] to deflect attacks!"
                    };
                    ZeldaDialogueBox.Instance.ShowDialogue("Kokiri Shopkeeper", buyDialogue);
                }
                else
                {
                    int needed = 40 - inv.Rupees;
                    string[] needRupees = new string[]
                    {
                        "Welcome to the Kokiri Shop!",
                        $"The Deku Shield costs 40 Rupees. You currently have {inv.Rupees} Rupees (Need {needed} more).",
                        "Tip: Slash grass around the village or jump across the river rocks to collect more Rupees!"
                    };
                    ZeldaDialogueBox.Instance.ShowDialogue("Kokiri Shopkeeper", needRupees);
                }
            }
            else
            {
                // Already has shield, offers Deku Sticks or Heart
                if (inv.Rupees >= 10)
                {
                    inv.SpendRupees(10);
                    var health = FindFirstObjectByType<HealthSystem>();
                    if (health != null) health.Heal(4f); // Heal 1 heart

                    string[] healed = new string[]
                    {
                        "Welcome to the Kokiri Shop!",
                        "You bought a Recovery Heart for 10 Rupees! Your health is restored!"
                    };
                    ZeldaDialogueBox.Instance.ShowDialogue("Kokiri Shopkeeper", healed);
                }
                else
                {
                    string[] browsing = new string[]
                    {
                        "Welcome to the Kokiri Shop!",
                        "You already have the Deku Shield! Recovery Hearts are 10 Rupees each."
                    };
                    ZeldaDialogueBox.Instance.ShowDialogue("Kokiri Shopkeeper", browsing);
                }
            }
        }
    }
}
