using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZeldaOoT.Input;
using ZeldaOoT.Inventory;
using ZeldaOoT.CameraRig;

namespace ZeldaOoT.UI
{
    /// <summary>
    /// Horizon Zero Dawn style Weapon Wheel & D-Pad Quick Accessory Menu built with UI Toolkit:
    /// - Hold WeaponWheel key (Q / Middle Mouse / Gamepad Left Bumper) to open radial wheel with 0.08x bullet-time slow motion.
    /// - 6-segment circular radial layout arranged geometrically (Weapons & Shields).
    /// - Radial selection follows mouse or right thumbstick angle with instant visual slice highlighting and glowing accents.
    /// - Center inspection panel displays weapon/shield stats, name, and glowing holographic icon.
    /// - Smooth release instantly equips selected weapon/shield and resumes real-time gameplay.
    /// - D-Pad Quick Accessory bar at bottom center (Left/Right arrow keys or Gamepad D-pad)
    ///   cycles potions, bombs, ocarina, bow, and down arrow uses/activates current item!
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HorizonWheelMenuUI : MonoBehaviour
    {
        [Header("Slow Motion")]
        [SerializeField] private float slowTimeScale = 0.08f;

        private UIDocument uiDocument;
        private ZeldaInputReader inputReader;
        private PlayerInventory playerInventory;

        private VisualElement root;

        // Radial Wheel Elements
        private VisualElement wheelContainer;
        private VisualElement wheelRing;
        private VisualElement centerInfoCard;
        private Label centerIcon;
        private Label centerName;
        private Label centerStat;
        private Label centerType;
        private List<VisualElement> sliceElements = new List<VisualElement>();
        private List<EquipmentData> wheelItems = new List<EquipmentData>();
        private int selectedWheelIndex = -1;
        private bool isWheelOpen = false;

        // D-Pad Accessory Bar Elements
        private VisualElement dpadContainer;
        private Label dpadItemIcon;
        private Label dpadItemName;
        private Label dpadItemCount;
        private List<EquipmentData> accessoryList = new List<EquipmentData>();
        private int accessoryIndex = 0;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            inputReader = ZeldaInputReader.Instance;
            playerInventory = FindFirstObjectByType<PlayerInventory>();

            BuildUIHierarchy();
            PopulateAccessoryList();
            HideWheel();
            UpdateDPadDisplay();
        }

        private void BuildUIHierarchy()
        {
            root = uiDocument.rootVisualElement;

            // ==========================================
            // 1. HORIZON RADIAL WEAPON WHEEL CONTAINER
            // ==========================================
            wheelContainer = new VisualElement();
            wheelContainer.name = "HorizonWheelContainer";
            wheelContainer.style.position = Position.Absolute;
            wheelContainer.style.width = Length.Percent(100);
            wheelContainer.style.height = Length.Percent(100);
            wheelContainer.style.alignItems = Align.Center;
            wheelContainer.style.justifyContent = Justify.Center;
            wheelContainer.style.backgroundColor = new Color(0.02f, 0.04f, 0.07f, 0.65f); // subtle cinematic vignette
            wheelContainer.style.display = DisplayStyle.None;

            // Wheel Outer Ring / Hub
            wheelRing = new VisualElement();
            wheelRing.name = "WheelHub";
            wheelRing.style.width = 440;
            wheelRing.style.height = 440;
            wheelRing.style.position = Position.Relative;
            wheelRing.style.alignItems = Align.Center;
            wheelRing.style.justifyContent = Justify.Center;
            wheelContainer.Add(wheelRing);

            // Center Info Card inside wheel ring
            centerInfoCard = new VisualElement();
            centerInfoCard.name = "WheelCenterCard";
            centerInfoCard.style.width = 160;
            centerInfoCard.style.height = 160;
            centerInfoCard.style.backgroundColor = new Color(0.06f, 0.12f, 0.18f, 0.95f);
            centerInfoCard.style.borderTopWidth = 2;
            centerInfoCard.style.borderBottomWidth = 2;
            centerInfoCard.style.borderLeftWidth = 2;
            centerInfoCard.style.borderRightWidth = 2;
            centerInfoCard.style.borderTopColor = new Color(0.2f, 0.85f, 1f, 0.8f);
            centerInfoCard.style.borderBottomColor = new Color(0.2f, 0.85f, 1f, 0.8f);
            centerInfoCard.style.borderLeftColor = new Color(0.2f, 0.85f, 1f, 0.8f);
            centerInfoCard.style.borderRightColor = new Color(0.2f, 0.85f, 1f, 0.8f);
            centerInfoCard.style.borderTopLeftRadius = 80;
            centerInfoCard.style.borderTopRightRadius = 80;
            centerInfoCard.style.borderBottomLeftRadius = 80;
            centerInfoCard.style.borderBottomRightRadius = 80;
            centerInfoCard.style.alignItems = Align.Center;
            centerInfoCard.style.justifyContent = Justify.Center;
            centerInfoCard.style.paddingLeft = 8;
            centerInfoCard.style.paddingRight = 8;

            centerIcon = new Label("⚔️");
            centerIcon.style.fontSize = 32;
            centerIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerIcon.style.marginBottom = 2;
            centerInfoCard.Add(centerIcon);

            centerName = new Label("Master Sword");
            centerName.style.fontSize = 13;
            centerName.style.unityFontStyleAndWeight = FontStyle.Bold;
            centerName.style.color = Color.white;
            centerName.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerName.style.whiteSpace = WhiteSpace.Normal;
            centerInfoCard.Add(centerName);

            centerStat = new Label("ATK 30");
            centerStat.style.fontSize = 12;
            centerStat.style.unityFontStyleAndWeight = FontStyle.Bold;
            centerStat.style.color = new Color(0.3f, 0.9f, 1f);
            centerStat.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerInfoCard.Add(centerStat);

            centerType = new Label("One-Handed");
            centerType.style.fontSize = 10;
            centerType.style.color = new Color(0.7f, 0.8f, 0.9f);
            centerType.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerInfoCard.Add(centerType);

            wheelRing.Add(centerInfoCard);
            root.Add(wheelContainer);

            // ==========================================
            // 2. HORIZON STYLE D-PAD QUICK ACCESSORY BAR
            // ==========================================
            dpadContainer = new VisualElement();
            dpadContainer.name = "HorizonDPadAccessoryBar";
            dpadContainer.style.position = Position.Absolute;
            dpadContainer.style.bottom = 26;
            dpadContainer.style.left = Length.Percent(50);
            dpadContainer.transform.position = new Vector3(-130, 0, 0); // half width offset
            dpadContainer.style.width = 260;
            dpadContainer.style.height = 54;
            dpadContainer.style.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            dpadContainer.style.borderTopWidth = 2;
            dpadContainer.style.borderBottomWidth = 2;
            dpadContainer.style.borderLeftWidth = 2;
            dpadContainer.style.borderRightWidth = 2;
            dpadContainer.style.borderTopColor = new Color(0.2f, 0.8f, 1f, 0.75f);
            dpadContainer.style.borderBottomColor = new Color(0.2f, 0.8f, 1f, 0.75f);
            dpadContainer.style.borderLeftColor = new Color(0.2f, 0.8f, 1f, 0.75f);
            dpadContainer.style.borderRightColor = new Color(0.2f, 0.8f, 1f, 0.75f);
            dpadContainer.style.borderTopLeftRadius = 12;
            dpadContainer.style.borderTopRightRadius = 12;
            dpadContainer.style.borderBottomLeftRadius = 12;
            dpadContainer.style.borderBottomRightRadius = 12;
            dpadContainer.style.flexDirection = FlexDirection.Row;
            dpadContainer.style.alignItems = Align.Center;
            dpadContainer.style.justifyContent = Justify.SpaceBetween;
            dpadContainer.style.paddingLeft = 12;
            dpadContainer.style.paddingRight = 12;

            // Left arrow cycle button
            Label leftArrow = new Label("◀");
            leftArrow.style.fontSize = 16;
            leftArrow.style.color = new Color(0.3f, 0.85f, 1f);
            leftArrow.style.unityFontStyleAndWeight = FontStyle.Bold;
            dpadContainer.Add(leftArrow);

            // Center slot
            VisualElement dpadItemCenter = new VisualElement();
            dpadItemCenter.style.flexDirection = FlexDirection.Row;
            dpadItemCenter.style.alignItems = Align.Center;

            dpadItemIcon = new Label("💣");
            dpadItemIcon.style.fontSize = 24;
            dpadItemIcon.style.marginRight = 8;
            dpadItemCenter.Add(dpadItemIcon);

            VisualElement dpadTextStack = new VisualElement();
            dpadItemName = new Label("Remote Bomb");
            dpadItemName.style.fontSize = 13;
            dpadItemName.style.unityFontStyleAndWeight = FontStyle.Bold;
            dpadItemName.style.color = Color.white;
            dpadTextStack.Add(dpadItemName);

            dpadItemCount = new Label("Press [Down / E] to Use");
            dpadItemCount.style.fontSize = 10;
            dpadItemCount.style.color = new Color(0.3f, 0.9f, 0.4f);
            dpadTextStack.Add(dpadItemCount);
            dpadItemCenter.Add(dpadTextStack);

            dpadContainer.Add(dpadItemCenter);

            // Right arrow cycle button
            Label rightArrow = new Label("▶");
            rightArrow.style.fontSize = 16;
            rightArrow.style.color = new Color(0.3f, 0.85f, 1f);
            rightArrow.style.unityFontStyleAndWeight = FontStyle.Bold;
            dpadContainer.Add(rightArrow);

            root.Add(dpadContainer);
        }

        private void PopulateAccessoryList()
        {
            if (playerInventory == null) return;
            accessoryList.Clear();

            // Bows
            foreach (var b in playerInventory.Bows) accessoryList.Add(b);
            // Materials & tools
            foreach (var m in playerInventory.Materials) accessoryList.Add(m);

            if (accessoryList.Count == 0)
            {
                accessoryList.Add(new EquipmentData("remote_bomb", "Remote Bomb", EquipmentCategory.Material, "Explosive", 30, Color.blue, "💣", "Explosive projectile."));
            }
        }

        private void Update()
        {
            if (inputReader == null) inputReader = ZeldaInputReader.Instance;
            if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (inputReader == null || playerInventory == null) return;

            // 1. HORIZON RADIAL WHEEL OPEN / HOLD / CLOSE
            if (inputReader.WeaponWheelHeld)
            {
                if (!isWheelOpen)
                {
                    OpenWheel();
                }
                UpdateWheelSelection();
            }
            else if (isWheelOpen)
            {
                ConfirmAndCloseWheel();
            }

            // 2. D-PAD ACCESSORY NAVIGATION
            if (inputReader.DPadLeftPressed)
            {
                CycleAccessory(-1);
            }
            else if (inputReader.DPadRightPressed)
            {
                CycleAccessory(1);
            }
            else if (inputReader.DPadDownPressed)
            {
                UseCurrentAccessory();
            }
        }

        private void OpenWheel()
        {
            isWheelOpen = true;

            // Bullet-time slow-mo (Horizon style)
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = 0.02f * slowTimeScale;
            ZeldaCursorManager.Instance?.SetMenuOpen(true);

            wheelContainer.style.display = DisplayStyle.Flex;

            // Collect weapons & shields for the wheel (up to 8 items)
            wheelItems.Clear();
            foreach (var w in playerInventory.Weapons) wheelItems.Add(w);
            foreach (var s in playerInventory.Shields) wheelItems.Add(s);

            BuildWheelSegments();

            // Default selected index to current active weapon
            selectedWheelIndex = 0;
            if (playerInventory.ActiveWeapon != null)
            {
                int idx = wheelItems.FindIndex(x => x.id == playerInventory.ActiveWeapon.id);
                if (idx >= 0) selectedWheelIndex = idx;
            }
            HighlightSelectedSlice(selectedWheelIndex);
        }

        private void BuildWheelSegments()
        {
            // Clear existing slice buttons
            foreach (var s in sliceElements)
            {
                s.RemoveFromHierarchy();
            }
            sliceElements.Clear();

            int count = wheelItems.Count;
            if (count == 0) return;

            float radius = 150f; // distance from center (hub width is 440, center is 220)
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                var item = wheelItems[i];
                // Start from top (-90 degrees)
                float angleDeg = -90f + i * angleStep;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                float x = Mathf.Cos(angleRad) * radius;
                float y = Mathf.Sin(angleRad) * radius;

                VisualElement slice = new VisualElement();
                slice.name = $"WheelSlice_{i}";
                slice.style.position = Position.Absolute;
                slice.style.width = 72;
                slice.style.height = 72;
                slice.style.left = 220 + x - 36;
                slice.style.top = 220 + y - 36;
                slice.style.backgroundColor = new Color(0.05f, 0.1f, 0.16f, 0.92f);
                slice.style.borderTopWidth = 2;
                slice.style.borderBottomWidth = 2;
                slice.style.borderLeftWidth = 2;
                slice.style.borderRightWidth = 2;
                slice.style.borderTopColor = new Color(0.2f, 0.8f, 1f, 0.5f);
                slice.style.borderBottomColor = new Color(0.2f, 0.8f, 1f, 0.5f);
                slice.style.borderLeftColor = new Color(0.2f, 0.8f, 1f, 0.5f);
                slice.style.borderRightColor = new Color(0.2f, 0.8f, 1f, 0.5f);
                slice.style.borderTopLeftRadius = 14;
                slice.style.borderTopRightRadius = 14;
                slice.style.borderBottomLeftRadius = 14;
                slice.style.borderBottomRightRadius = 14;
                slice.style.alignItems = Align.Center;
                slice.style.justifyContent = Justify.Center;

                Label icon = new Label(item.iconSymbol);
                icon.style.fontSize = 24;
                slice.Add(icon);

                Label subLabel = new Label(item.category == EquipmentCategory.Weapon ? $"⚔️{item.powerValue}" : $"🛡️{item.powerValue}");
                subLabel.style.fontSize = 9;
                subLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                subLabel.style.color = new Color(0.4f, 0.9f, 1f);
                slice.Add(subLabel);

                wheelRing.Add(slice);
                sliceElements.Add(slice);
            }
        }

        private void UpdateWheelSelection()
        {
            if (wheelItems.Count == 0) return;

            Vector2 pointer = inputReader.WheelPointerInput;
            if (pointer.sqrMagnitude > 0.15f)
            {
                // Calculate angle in degrees from top (clockwise)
                // In Unity UI, positive Y is down on screen, but input system Y is up
                float angle = Mathf.Atan2(-pointer.y, pointer.x) * Mathf.Rad2Deg; // -180 to 180
                // Normalize angle relative to top (-90 degrees)
                float normAngle = (angle + 90f + 360f) % 360f;

                float step = 360f / wheelItems.Count;
                int newIndex = Mathf.FloorToInt((normAngle + step * 0.5f) / step) % wheelItems.Count;

                if (newIndex != selectedWheelIndex)
                {
                    selectedWheelIndex = newIndex;
                    HighlightSelectedSlice(selectedWheelIndex);
                }
            }
        }

        private void HighlightSelectedSlice(int index)
        {
            for (int i = 0; i < sliceElements.Count; i++)
            {
                bool isSel = (i == index);
                var slice = sliceElements[i];

                slice.style.backgroundColor = isSel ? new Color(0.12f, 0.35f, 0.55f, 0.98f) : new Color(0.05f, 0.1f, 0.16f, 0.92f);
                Color borderColor = isSel ? new Color(0.3f, 0.95f, 1f, 1f) : new Color(0.2f, 0.8f, 1f, 0.5f);
                slice.style.borderTopColor = borderColor;
                slice.style.borderBottomColor = borderColor;
                slice.style.borderLeftColor = borderColor;
                slice.style.borderRightColor = borderColor;

                float scale = isSel ? 1.18f : 1.0f;
                slice.transform.scale = new Vector3(scale, scale, 1f);
            }

            if (index >= 0 && index < wheelItems.Count)
            {
                var item = wheelItems[index];
                centerIcon.text = item.iconSymbol;
                centerName.text = item.displayName;
                centerStat.text = item.category == EquipmentCategory.Weapon ? $"ATK +{item.powerValue}" : $"DEF +{item.powerValue}";
                centerType.text = item.subType;
                centerIcon.style.color = item.iconColor;
            }
        }

        private void ConfirmAndCloseWheel()
        {
            if (selectedWheelIndex >= 0 && selectedWheelIndex < wheelItems.Count)
            {
                var chosen = wheelItems[selectedWheelIndex];
                if (chosen.category == EquipmentCategory.Weapon)
                {
                    playerInventory.EquipWeapon(chosen);
                }
                else if (chosen.category == EquipmentCategory.Shield)
                {
                    playerInventory.EquipShield(chosen);
                }
            }

            HideWheel();
        }

        private void HideWheel()
        {
            isWheelOpen = false;
            wheelContainer.style.display = DisplayStyle.None;

            // Restore normal time scale
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            ZeldaCursorManager.Instance?.SetMenuOpen(false);
        }

        // ==========================================
        // D-PAD ACCESSORY CONTROLS
        // ==========================================
        private void CycleAccessory(int delta)
        {
            if (accessoryList.Count == 0) PopulateAccessoryList();
            if (accessoryList.Count == 0) return;

            accessoryIndex = (accessoryIndex + delta + accessoryList.Count) % accessoryList.Count;
            UpdateDPadDisplay();
        }

        private void UpdateDPadDisplay()
        {
            if (accessoryList.Count == 0) PopulateAccessoryList();
            if (accessoryList.Count == 0) return;

            var item = accessoryList[accessoryIndex];
            dpadItemIcon.text = item.iconSymbol;
            dpadItemName.text = item.displayName;

            // Sync with PlayerInventory active item/bow
            if (item.category == EquipmentCategory.Bow)
            {
                playerInventory.EquipBow(item);
            }
            else
            {
                playerInventory.EquipItem(item);
            }

            // Sync with Zelda ItemSystem
            var itemSystem = FindFirstObjectByType<ZeldaOoT.Items.ItemSystem>();
            if (itemSystem != null)
            {
                if (item.id.Contains("bow")) itemSystem.SetCurrentItem(ZeldaOoT.Items.ItemType.Bow);
                else if (item.id.Contains("bomb")) itemSystem.SetCurrentItem(ZeldaOoT.Items.ItemType.Bomb);
                else if (item.id.Contains("hookshot")) itemSystem.SetCurrentItem(ZeldaOoT.Items.ItemType.Hookshot);
            }
        }

        private void UseCurrentAccessory()
        {
            if (accessoryList.Count == 0) return;
            var item = accessoryList[accessoryIndex];

            var itemSystem = FindFirstObjectByType<ZeldaOoT.Items.ItemSystem>();
            if (itemSystem != null)
            {
                itemSystem.UseCurrentItem(Camera.main != null ? Camera.main.transform : null);
            }

            // Flash accessory bar momentarily for responsive tactile feedback
            dpadContainer.style.borderTopColor = new Color(1f, 0.9f, 0.2f, 1f);
            dpadContainer.style.borderBottomColor = new Color(1f, 0.9f, 0.2f, 1f);
            dpadContainer.schedule.Execute(() =>
            {
                dpadContainer.style.borderTopColor = new Color(0.2f, 0.8f, 1f, 0.75f);
                dpadContainer.style.borderBottomColor = new Color(0.2f, 0.8f, 1f, 0.75f);
            }).StartingIn(150);
        }
    }
}