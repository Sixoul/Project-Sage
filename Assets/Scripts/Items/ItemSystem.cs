using System;
using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.DekuTree;
using ZeldaOoT.Inventory;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Items
{
    public enum ItemType
    {
        None,
        Bow,
        Bomb,
        Hookshot,
        Slingshot,
        DekuStick,
        DekuNut
    }

    /// <summary>
    /// Handles player's equipped secondary item (Bow, Bomb, etc.).
    /// </summary>
    public class ItemSystem : MonoBehaviour
    {
        [Header("Item References")]
        [SerializeField] private ItemType currentItem = ItemType.Bow;
        [SerializeField] private Transform itemSpawnSocket;

        public ItemType CurrentItem => currentItem;
        public event Action<ItemType> OnItemUsed;

        private ZTargetSystem zTargetSystem;

        private void Awake()
        {
            zTargetSystem = GetComponent<ZTargetSystem>();
        }

        public void SetCurrentItem(ItemType item)
        {
            currentItem = item;
        }

        public void UseCurrentItem(Transform cameraTransform)
        {
            if (currentItem == ItemType.None) return;

            Vector3 spawnPos = itemSpawnSocket != null ? itemSpawnSocket.position : transform.position + transform.forward * 0.8f + Vector3.up * 1.2f;

            // Target direction: if locked on, aim at locked target; otherwise aim along camera forward
            Vector3 aimDir = cameraTransform != null ? cameraTransform.forward : transform.forward;
            if (zTargetSystem != null && zTargetSystem.IsLockedOn && zTargetSystem.CurrentTarget != null)
            {
                aimDir = (zTargetSystem.CurrentTarget.position - spawnPos).normalized;
            }

            switch (currentItem)
            {
                case ItemType.Bow:
                    FireArrow(spawnPos, aimDir);
                    break;
                case ItemType.Bomb:
                    ThrowBomb(spawnPos, aimDir);
                    break;
                case ItemType.Slingshot:
                    FireSlingshot(spawnPos, aimDir);
                    break;
                case ItemType.DekuStick:
                    UseDekuStick();
                    break;
                case ItemType.DekuNut:
                    ThrowDekuNut(spawnPos, aimDir);
                    break;
            }

            OnItemUsed?.Invoke(currentItem);
        }

        private void FireArrow(Vector3 origin, Vector3 dir)
        {
            GameObject arrowObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrowObj.name = "Arrow";
            arrowObj.transform.position = origin;
            arrowObj.transform.localScale = new Vector3(0.06f, 0.5f, 0.06f);
            arrowObj.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);

            var mr = arrowObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.6f, 0.4f, 0.2f);
                mr.material = mat;
            }

            var arrowComp = arrowObj.AddComponent<ArrowProjectile>();
            arrowComp.Initialize(dir, gameObject);
        }

        private void ThrowBomb(Vector3 origin, Vector3 dir)
        {
            GameObject bombObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bombObj.name = "Bomb";
            bombObj.transform.position = origin + Vector3.up * 0.5f;
            bombObj.transform.localScale = Vector3.one * 0.55f;

            var rb = bombObj.AddComponent<Rigidbody>();
            rb.mass = 2f;

            var mr = bombObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = Color.blue;
                mr.material = mat;
            }

            var bombComp = bombObj.AddComponent<BombItem>();
            Vector3 throwVel = (dir + Vector3.up * 0.45f).normalized * 9.5f;
            bombComp.Initialize(throwVel, gameObject);
        }

        private void FireSlingshot(Vector3 origin, Vector3 dir)
        {
            var inv = GetComponent<PlayerInventory>();
            if (inv != null && inv.DekuSeeds <= 0) return;
            if (inv != null) inv.DekuSeeds--;

            SlingshotPellet.SpawnPellet(origin, dir, gameObject);
        }

        private void UseDekuStick()
        {
            var inv = GetComponent<PlayerInventory>();
            if (inv != null && inv.DekuSticks <= 0) return;

            // Spawn or swing stick
            var existingStick = GetComponentInChildren<DekuStickWeapon>();
            if (existingStick != null)
            {
                // Already holding stick, perform swing
                return;
            }

            GameObject stickObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stickObj.name = "Held_Deku_Stick";
            stickObj.transform.SetParent(itemSpawnSocket != null ? itemSpawnSocket : transform, false);
            stickObj.transform.localPosition = new Vector3(0.3f, 0.2f, 0.4f);
            stickObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            stickObj.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material wood = new Material(s) { color = new Color(0.45f, 0.28f, 0.12f) };
            stickObj.GetComponent<MeshRenderer>().material = wood;

            stickObj.AddComponent<DekuStickWeapon>();
        }

        private void ThrowDekuNut(Vector3 origin, Vector3 dir)
        {
            var inv = GetComponent<PlayerInventory>();
            if (inv != null && inv.DekuNuts <= 0) return;
            if (inv != null) inv.DekuNuts--;

            // Flash effect stunning enemies in 8m radius
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlaySpinWave(transform.position, 6.0f, 2);
            }

            Collider[] hits = Physics.OverlapSphere(origin, 8f);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    dmg.TakeDamage(new DamageInfo(1f, hit.transform.position, Vector3.up, 4f, gameObject, false));
                }
            }
        }
    }
}
