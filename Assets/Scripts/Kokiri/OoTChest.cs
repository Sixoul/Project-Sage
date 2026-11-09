using System.Collections;
using UnityEngine;
using ZeldaOoT.Inventory;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Authentic Ocarina of Time Treasure Chest.
    /// Hinged opening lid, item float-up presentation, radiant fanfare particles,
    /// and inventory awarding with dialogue popup.
    /// </summary>
    public class OoTChest : MonoBehaviour
    {
        [Header("Chest Contents")]
        [SerializeField] private string itemId = "kokiri_sword";
        [SerializeField] private string itemName = "Kokiri Sword";
        [SerializeField] private string itemDescription = "You found the Kokiri Sword! A small wooden-hilted blade treasured by the Kokiri. Equip it to slash enemies!";
        [SerializeField] private Color itemColor = new Color(0.4f, 0.85f, 0.25f);

        private Transform lidTransform;
        private bool isOpen = false;
        private bool playerNearby = false;

        public static GameObject CreateChest(Vector3 position, Quaternion rotation, string id, string name, string desc, Color color)
        {
            GameObject chestRoot = new GameObject($"Chest_{name}");
            chestRoot.transform.position = position;
            chestRoot.transform.rotation = rotation;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material woodMat = new Material(s) { color = new Color(0.45f, 0.26f, 0.12f) };
            Material goldMat = new Material(s) { color = new Color(0.95f, 0.8f, 0.2f) };

            // Base box
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Chest_Base";
            baseBox.transform.SetParent(chestRoot.transform, false);
            baseBox.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            baseBox.transform.localScale = new Vector3(1.2f, 0.7f, 0.8f);
            baseBox.GetComponent<MeshRenderer>().material = woodMat;

            // Gold Trim Base
            GameObject goldTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goldTrim.name = "Chest_GoldTrim";
            goldTrim.transform.SetParent(chestRoot.transform, false);
            goldTrim.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            goldTrim.transform.localScale = new Vector3(1.22f, 0.2f, 0.82f);
            goldTrim.GetComponent<MeshRenderer>().material = goldMat;

            // Hinged Lid Pivot
            GameObject lidPivot = new GameObject("Chest_Lid_Pivot");
            lidPivot.transform.SetParent(chestRoot.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.7f, -0.4f); // Back edge hinge

            // Lid Mesh
            GameObject lidMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidMesh.name = "Chest_Lid";
            lidMesh.transform.SetParent(lidPivot.transform, false);
            lidMesh.transform.localPosition = new Vector3(0f, 0.15f, 0.4f);
            lidMesh.transform.localScale = new Vector3(1.24f, 0.3f, 0.84f);
            lidMesh.GetComponent<MeshRenderer>().material = woodMat;

            // Trigger Zone
            var triggerCol = chestRoot.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(2.5f, 2.0f, 2.5f);
            triggerCol.center = new Vector3(0f, 0.8f, 0.5f);

            var chest = chestRoot.AddComponent<OoTChest>();
            chest.lidTransform = lidPivot.transform;
            chest.itemId = id;
            chest.itemName = name;
            chest.itemDescription = desc;
            chest.itemColor = color;

            return chestRoot;
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
            if (playerNearby && !isOpen && UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                OpenChest();
            }
        }

        public void OpenChest()
        {
            if (isOpen) return;
            isOpen = true;

            StartCoroutine(OpenChestRoutine());
        }

        private IEnumerator OpenChestRoutine()
        {
            // 1. Smoothly swing open lid
            float duration = 0.8f;
            float elapsed = 0f;
            Quaternion startRot = lidTransform.localRotation;
            Quaternion endRot = Quaternion.Euler(-105f, 0f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                lidTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            // 2. Spawn item reward floating up
            GameObject rewardVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rewardVisual.name = $"Reward_{itemName}";
            rewardVisual.transform.position = transform.position + Vector3.up * 0.7f;
            rewardVisual.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
            var col = rewardVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = rewardVisual.GetComponent<MeshRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.color = itemColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", itemColor * 2f);
            mr.material = mat;

            // Float up animation with spinning
            elapsed = 0f;
            float floatTime = 1.2f;
            Vector3 startPos = rewardVisual.transform.position;
            Vector3 endPos = startPos + Vector3.up * 1.4f;

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(startPos, Vector3.up);
            }

            while (elapsed < floatTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatTime;
                rewardVisual.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                rewardVisual.transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
                yield return null;
            }

            Destroy(rewardVisual, 0.2f);

            // 3. Award item to Player Inventory
            var playerInv = FindFirstObjectByType<PlayerInventory>();
            if (playerInv != null)
            {
                playerInv.AwardSpecialItem(itemId, itemName);
            }

            // 4. Pop up Dialogue
            if (ZeldaDialogueBox.Instance != null)
            {
                ZeldaDialogueBox.Instance.ShowDialogue("Treasure Found!", new string[] { itemDescription });
            }
        }
    }
}
