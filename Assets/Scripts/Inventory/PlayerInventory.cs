using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ZeldaOoT.Combat;
using ZeldaOoT.Items;

namespace ZeldaOoT.Inventory
{
    /// <summary>
    /// Manages player's equipped weapon, shield, and items.
    /// Provides dynamic socket attachments and live BotW-style swapping.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Equipment Sockets")]
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private Transform leftHandSocket;
        [SerializeField] private Transform backSheathSocket;

        public List<EquipmentData> Weapons { get; private set; } = new List<EquipmentData>();
        public List<EquipmentData> Bows { get; private set; } = new List<EquipmentData>();
        public List<EquipmentData> Shields { get; private set; } = new List<EquipmentData>();
        public List<EquipmentData> Armors { get; private set; } = new List<EquipmentData>();
        public List<EquipmentData> Materials { get; private set; } = new List<EquipmentData>();
        public List<EquipmentData> Items { get; private set; } = new List<EquipmentData>();

        public EquipmentData ActiveWeapon { get; private set; }
        public EquipmentData ActiveBow { get; private set; }
        public EquipmentData ActiveShield { get; private set; }
        public EquipmentData ActiveArmor { get; private set; }
        public EquipmentData ActiveItem { get; private set; }

        public event Action<EquipmentData> OnWeaponChanged;
        public event Action<EquipmentData> OnBowChanged;
        public event Action<EquipmentData> OnShieldChanged;
        public event Action<EquipmentData> OnArmorChanged;
        public event Action<EquipmentData> OnItemChanged;

        private GameObject currentWeaponVisual;
        private GameObject currentShieldVisual;

        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        private void Awake()
        {
            InitializeDefaultInventory();
        }

        private void Start()
        {
            // Equip defaults
            if (Weapons.Count > 0) EquipWeapon(Weapons[0]);
            if (Bows.Count > 0) EquipBow(Bows[0]);
            if (Shields.Count > 0) EquipShield(Shields[0]);
            if (Armors.Count > 0) EquipArmor(Armors[0]);
            if (Items.Count > 0) EquipItem(Items[0]);
        }

        private void InitializeDefaultInventory()
        {
            // 1. Weapons
            Weapons.Add(new EquipmentData("master_sword", "Master Sword", EquipmentCategory.Weapon, "One-Handed Sword", 30, new Color(0.2f, 0.6f, 1f), "⚔️", "The legendary sword that seals the darkness. Its blade gleams with holy light and repels evil."));
            Weapons.Add(new EquipmentData("kokiri_sword", "Kokiri Sword", EquipmentCategory.Weapon, "Shortsword", 12, new Color(0.4f, 0.8f, 0.2f), "🗡️", "A small wooden-hilted blade treasured by the Kokiri. Light and nimble."));
            Weapons.Add(new EquipmentData("biggoron_sword", "Biggoron's Sword", EquipmentCategory.Weapon, "Two-Handed Blade", 50, new Color(1f, 0.5f, 0.1f), "⚔️", "A colossal two-handed blade forged by the giant Goron master smith. Deals heavy impact damage."));
            Weapons.Add(new EquipmentData("guardian_blade", "Guardian Blade", EquipmentCategory.Weapon, "Ancient Weapon", 42, new Color(0.2f, 0.9f, 0.9f), "✨", "A blade crafted with lost ancient Sheikah technology. Emits a cutting edge of pure energy."));

            // 2. Bows
            Bows.Add(new EquipmentData("fairy_bow", "Fairy Bow", EquipmentCategory.Bow, "Sacred Bow", 20, new Color(0.2f, 0.8f, 0.4f), "🏹", "A treasured sacred bow of the Forest Temple. Highly accurate and fires rapid shots."));
            Bows.Add(new EquipmentData("heros_bow", "Hero's Bow", EquipmentCategory.Bow, "Reinforced Bow", 35, new Color(0.8f, 0.6f, 0.2f), "🏹", "A sturdy bow passed down to champions. Imbued with greater draw strength and range."));

            // 3. Shields
            Shields.Add(new EquipmentData("hylian_shield", "Hylian Shield", EquipmentCategory.Shield, "Steel Shield", 90, new Color(0.1f, 0.3f, 0.8f), "🛡️", "A traditional shield passed down through the Hyrulean royal family. Boasts peerless defense."));
            Shields.Add(new EquipmentData("deku_shield", "Deku Shield", EquipmentCategory.Shield, "Wooden Shield", 25, new Color(0.6f, 0.4f, 0.1f), "🛡️", "A lightweight wooden shield carved from the bark of the Great Deku Tree. Vulnerable to fire."));
            Shields.Add(new EquipmentData("mirror_shield", "Mirror Shield", EquipmentCategory.Shield, "Reflective Shield", 75, new Color(0.95f, 0.95f, 1f), "✨", "A polished mirrored shield that reflects sunlight and repels mystical projectile curses."));

            // 4. Armors / Tunics
            Armors.Add(new EquipmentData("kokiri_tunic", "Kokiri Tunic", EquipmentCategory.Armor, "Hero Garb", 8, new Color(0.12f, 0.55f, 0.2f), "👕", "The traditional green tunic of the Kokiri forest dwellers. Comfortable and durable."));
            Armors.Add(new EquipmentData("goron_tunic", "Goron Tunic", EquipmentCategory.Armor, "Heat-Resistant", 14, new Color(0.85f, 0.18f, 0.12f), "👕", "A crimson tunic woven from fiery Dodongo hide. Protects against scorching heat and magma."));
            Armors.Add(new EquipmentData("zora_tunic", "Zora Tunic", EquipmentCategory.Armor, "Aquatic Garb", 12, new Color(0.12f, 0.45f, 0.92f), "👕", "A sapphire blue tunic woven by the Zora royal artisans. Extends lung capacity underwater."));

            // 5. Materials & Key Items
            Materials.Add(new EquipmentData("remote_bomb", "Remote Bomb", EquipmentCategory.Material, "Explosive Device", 30, new Color(0.3f, 0.4f, 0.9f), "💣", "A blue sphere filled with concussive gunpowder. Detonates to destroy cracked rocks and crates."));
            Materials.Add(new EquipmentData("ocarina_of_time", "Ocarina of Time", EquipmentCategory.Material, "Sacred Instrument", 0, new Color(0.2f, 0.5f, 1f), "🎶", "The mystical ocarina made of pure time-crystal. Plays songs that manipulate the winds of time."));
            Materials.Add(new EquipmentData("hookshot", "Hookshot", EquipmentCategory.Material, "Traversal Tool", 10, new Color(0.7f, 0.7f, 0.75f), "⛓️", "A chain-driven grappling hook that pulls Link toward wooden targets and chests."));
            Materials.Add(new EquipmentData("small_key", "Small Key", EquipmentCategory.Material, "Dungeon Key", 0, new Color(1f, 0.85f, 0.2f), "🗝️", "An ornate brass key that unlocks one locked door inside ancient temples."));

            // Quick Items list sync
            Items.Clear();
            Items.Add(Bows[0]);
            Items.Add(Materials[0]);
        }

        public void SetSockets(Transform rightHand, Transform leftHand, Transform back)
        {
            rightHandSocket = rightHand;
            leftHandSocket = leftHand;
            backSheathSocket = back;
        }

        public void Equip(EquipmentData item)
        {
            if (item == null) return;
            switch (item.category)
            {
                case EquipmentCategory.Weapon:
                    EquipWeapon(item);
                    break;
                case EquipmentCategory.Bow:
                    EquipBow(item);
                    break;
                case EquipmentCategory.Shield:
                    EquipShield(item);
                    break;
                case EquipmentCategory.Armor:
                    EquipArmor(item);
                    break;
                case EquipmentCategory.Material:
                case EquipmentCategory.Item:
                    EquipItem(item);
                    break;
            }
        }

        public void EquipWeapon(EquipmentData weapon)
        {
            if (weapon == null) return;
            ActiveWeapon = weapon;
            UpdateWeaponVisual();
            OnWeaponChanged?.Invoke(ActiveWeapon);
        }

        public void EquipBow(EquipmentData bow)
        {
            if (bow == null) return;
            ActiveBow = bow;
            var itemSys = GetComponent<ItemSystem>();
            if (itemSys != null) itemSys.SetCurrentItem(ItemType.Bow);
            OnBowChanged?.Invoke(ActiveBow);
        }

        public void EquipShield(EquipmentData shield)
        {
            if (shield == null) return;
            ActiveShield = shield;
            UpdateShieldVisual();
            OnShieldChanged?.Invoke(ActiveShield);
        }

        public void EquipArmor(EquipmentData armor)
        {
            if (armor == null) return;
            ActiveArmor = armor;
            UpdateArmorVisual();
            OnArmorChanged?.Invoke(ActiveArmor);
        }

        public void EquipItem(EquipmentData item)
        {
            if (item == null) return;
            ActiveItem = item;

            var itemSys = GetComponent<ItemSystem>();
            if (itemSys != null)
            {
                if (item.id.Contains("bow")) itemSys.SetCurrentItem(ItemType.Bow);
                else if (item.id.Contains("bomb")) itemSys.SetCurrentItem(ItemType.Bomb);
            }

            OnItemChanged?.Invoke(ActiveItem);
        }

        public void Unequip(EquipmentCategory category)
        {
            switch (category)
            {
                case EquipmentCategory.Weapon:
                    ActiveWeapon = null;
                    if (currentWeaponVisual != null) SafeDestroy(currentWeaponVisual);
                    OnWeaponChanged?.Invoke(null);
                    break;
                case EquipmentCategory.Bow:
                    ActiveBow = null;
                    OnBowChanged?.Invoke(null);
                    break;
                case EquipmentCategory.Shield:
                    ActiveShield = null;
                    if (currentShieldVisual != null) SafeDestroy(currentShieldVisual);
                    OnShieldChanged?.Invoke(null);
                    break;
            }
        }

        private void UpdateArmorVisual()
        {
            if (ActiveArmor == null) return;
            Transform tunicTransform = transform.Find("ModelRoot/Tunic_Torso");
            if (tunicTransform != null)
            {
                var mr = tunicTransform.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Color c = ActiveArmor.id switch
                    {
                        "goron_tunic" => new Color(0.85f, 0.18f, 0.12f),
                        "zora_tunic" => new Color(0.12f, 0.45f, 0.92f),
                        _ => new Color(0.12f, 0.55f, 0.2f)
                    };
                    mr.material.color = c;
                }
            }
        }

        private void UpdateWeaponVisual()
        {
            if (currentWeaponVisual != null) SafeDestroy(currentWeaponVisual);
            if (rightHandSocket == null) return;

            // Generate stylized weapon visual
            currentWeaponVisual = new GameObject("Equipped_" + ActiveWeapon.displayName);
            currentWeaponVisual.transform.SetParent(rightHandSocket, false);
            currentWeaponVisual.transform.localPosition = new Vector3(0f, 0.28f, 0.12f);
            currentWeaponVisual.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material steelMat = new Material(urpLit) { color = new Color(0.92f, 0.94f, 0.96f) };
            steelMat.SetFloat("_Metallic", 0.92f);
            steelMat.SetFloat("_Smoothness", 0.9f);

            Material goldMat = new Material(urpLit) { color = new Color(1.0f, 0.82f, 0.15f) };
            goldMat.SetFloat("_Metallic", 0.85f);
            goldMat.SetFloat("_Smoothness", 0.85f);

            Material blueWingMat = new Material(urpLit) { color = new Color(0.12f, 0.28f, 0.75f) }; // Royal blue
            blueWingMat.SetFloat("_Smoothness", 0.7f);

            Material gripMat = new Material(urpLit) { color = new Color(0.28f, 0.16f, 0.45f) }; // Purple wrapped grip

            float bladeLength = ActiveWeapon.id == "biggoron_sword" ? 1.55f : (ActiveWeapon.id == "kokiri_sword" ? 0.75f : 1.08f);

            // 1. Blade Body
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(currentWeaponVisual.transform, false);
            blade.transform.localScale = new Vector3(0.09f, bladeLength, 0.025f);
            blade.transform.localPosition = new Vector3(0f, bladeLength * 0.5f + 0.08f, 0f);
            SafeDestroy(blade.GetComponent<Collider>());

            if (ActiveWeapon.id == "guardian_blade")
            {
                var guardianMat = new Material(urpLit) { color = new Color(0.2f, 0.9f, 1f) };
                guardianMat.EnableKeyword("_EMISSION");
                guardianMat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1f) * 3f);
                blade.GetComponent<MeshRenderer>().material = guardianMat;
            }
            else
            {
                blade.GetComponent<MeshRenderer>().material = steelMat;
            }

            // Blade Tip (Pointed triangular prism/cube)
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "BladeTip";
            tip.transform.SetParent(blade.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tip.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
            SafeDestroy(tip.GetComponent<Collider>());
            tip.GetComponent<MeshRenderer>().material = blade.GetComponent<MeshRenderer>().material;

            // 2. Crossguard (Master Sword Wings & Triforce Ricasso)
            GameObject guardCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guardCenter.name = "GuardCenter";
            guardCenter.transform.SetParent(currentWeaponVisual.transform, false);
            guardCenter.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            guardCenter.transform.localScale = new Vector3(0.16f, 0.08f, 0.08f);
            SafeDestroy(guardCenter.GetComponent<Collider>());
            guardCenter.GetComponent<MeshRenderer>().material = blueWingMat;

            // Golden Triforce Diamond on Ricasso
            GameObject ricassoTriforce = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ricassoTriforce.name = "Triforce_Ricasso";
            ricassoTriforce.transform.SetParent(guardCenter.transform, false);
            ricassoTriforce.transform.localPosition = new Vector3(0f, 0.3f, 0.02f);
            ricassoTriforce.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ricassoTriforce.transform.localScale = new Vector3(0.45f, 0.45f, 1.15f);
            SafeDestroy(ricassoTriforce.GetComponent<Collider>());
            ricassoTriforce.GetComponent<MeshRenderer>().material = goldMat;

            // Left & Right Swept Master Sword Wings
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.name = w < 0 ? "Wing_L" : "Wing_R";
                wing.transform.SetParent(guardCenter.transform, false);
                wing.transform.localPosition = new Vector3(w * 1.25f, 0.15f, 0f);
                wing.transform.localRotation = Quaternion.Euler(0f, 0f, w * -28f);
                wing.transform.localScale = new Vector3(1.5f, 0.65f, 0.85f);
                SafeDestroy(wing.GetComponent<Collider>());
                wing.GetComponent<MeshRenderer>().material = blueWingMat;
            }

            // 3. Purple Wrapped Grip & Gold Pommel
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "Hilt_Grip";
            grip.transform.SetParent(currentWeaponVisual.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            grip.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
            SafeDestroy(grip.GetComponent<Collider>());
            grip.GetComponent<MeshRenderer>().material = gripMat;

            GameObject pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "Pommel";
            pommel.transform.SetParent(grip.transform, false);
            pommel.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            pommel.transform.localScale = new Vector3(1.7f, 0.9f, 1.7f);
            SafeDestroy(pommel.GetComponent<Collider>());
            pommel.GetComponent<MeshRenderer>().material = goldMat;

            // Setup MeleeHitbox on weapon
            var hitboxCol = currentWeaponVisual.AddComponent<BoxCollider>();
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector3(0.45f, bladeLength + 0.3f, 0.45f);
            hitboxCol.center = new Vector3(0f, bladeLength * 0.5f, 0f);

            var hitbox = currentWeaponVisual.AddComponent<MeleeHitbox>();
            var combat = GetComponent<PlayerCombat>();
            if (combat != null)
            {
                combat.SetHitbox(hitbox);
            }
        }

        private void UpdateShieldVisual()
        {
            if (currentShieldVisual != null) SafeDestroy(currentShieldVisual);
            if (leftHandSocket == null) return;

            // Generate authentic Hylian Shield visual
            currentShieldVisual = new GameObject("Equipped_" + ActiveShield.displayName);
            currentShieldVisual.transform.SetParent(leftHandSocket, false);
            currentShieldVisual.transform.localPosition = new Vector3(-0.02f, 0.02f, 0.1f);
            currentShieldVisual.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(-60f, 25f, -15f));

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material steelMat = new Material(urpLit) { color = new Color(0.88f, 0.9f, 0.93f) };
            steelMat.SetFloat("_Metallic", 0.9f);
            steelMat.SetFloat("_Smoothness", 0.85f);

            Material shieldBlueMat = new Material(urpLit) { color = new Color(0.12f, 0.26f, 0.68f) }; // Royal blue
            shieldBlueMat.SetFloat("_Smoothness", 0.75f);

            Material goldMat = new Material(urpLit) { color = new Color(1.0f, 0.82f, 0.15f) };
            goldMat.SetFloat("_Metallic", 0.85f);
            goldMat.SetFloat("_Smoothness", 0.85f);

            Material redCrestMat = new Material(urpLit) { color = new Color(0.88f, 0.15f, 0.15f) }; // Hylian crimson

            // 1. Outer Steel Rim Border (Heater shield frame)
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "Shield_Steel_Rim";
            rim.transform.SetParent(currentShieldVisual.transform, false);
            rim.transform.localScale = new Vector3(0.54f, 0.74f, 0.06f);
            SafeDestroy(rim.GetComponent<Collider>());
            rim.GetComponent<MeshRenderer>().material = steelMat;

            // 2. Inner Royal Blue Shield Plate
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "ShieldPlate";
            plate.transform.SetParent(rim.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            plate.transform.localScale = new Vector3(0.88f, 0.88f, 0.9f);
            SafeDestroy(plate.GetComponent<Collider>());
            plate.GetComponent<MeshRenderer>().material = shieldBlueMat;

            // 3. Golden Triforce Emblem (Top center)
            GameObject triforceTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triforceTop.name = "Triforce_Top";
            triforceTop.transform.SetParent(plate.transform, false);
            triforceTop.transform.localPosition = new Vector3(0f, 0.26f, 0.52f);
            triforceTop.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            triforceTop.transform.localScale = new Vector3(0.22f, 0.22f, 0.1f);
            SafeDestroy(triforceTop.GetComponent<Collider>());
            triforceTop.GetComponent<MeshRenderer>().material = goldMat;

            // 4. Crimson Red Hylian Bird Crest (Winged relief)
            GameObject birdBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            birdBody.name = "Hylian_Bird_Crest";
            birdBody.transform.SetParent(plate.transform, false);
            birdBody.transform.localPosition = new Vector3(0f, -0.12f, 0.52f);
            birdBody.transform.localScale = new Vector3(0.18f, 0.28f, 0.1f);
            SafeDestroy(birdBody.GetComponent<Collider>());
            birdBody.GetComponent<MeshRenderer>().material = redCrestMat;

            // Wings
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.name = w < 0 ? "Wing_L" : "Wing_R";
                wing.transform.SetParent(birdBody.transform, false);
                wing.transform.localPosition = new Vector3(w * 0.95f, 0.2f, 0f);
                wing.transform.localRotation = Quaternion.Euler(0f, 0f, w * -30f);
                wing.transform.localScale = new Vector3(1.1f, 0.45f, 1f);
                SafeDestroy(wing.GetComponent<Collider>());
                wing.GetComponent<MeshRenderer>().material = redCrestMat;
            }

            // 5. Four Steel Rivets on Rim
            float[] rx = { -0.42f, 0.42f, -0.35f, 0.35f };
            float[] ry = { 0.44f, 0.44f, -0.44f, -0.44f };
            for (int r = 0; r < 4; r++)
            {
                GameObject rivet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rivet.name = $"Rivet_{r + 1}";
                rivet.transform.SetParent(rim.transform, false);
                rivet.transform.localPosition = new Vector3(rx[r], ry[r], 0.52f);
                rivet.transform.localScale = Vector3.one * 0.08f;
                SafeDestroy(rivet.GetComponent<Collider>());
                rivet.GetComponent<MeshRenderer>().material = steelMat;
            }
        }
    }
}
