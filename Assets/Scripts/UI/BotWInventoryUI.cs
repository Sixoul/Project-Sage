using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZeldaOoT.Inventory;
using ZeldaOoT.Input;
using ZeldaOoT.CameraRig;

namespace ZeldaOoT.UI
{
    /// <summary>
    /// Full-screen Breath of the Wild style pause inventory menu:
    /// - Top category tabs: Weapons, Bows, Shields, Armor, Materials.
    /// - 4-column item grid with stats and equipped indicators.
    /// - Right-side detailed inspection card with lore description and Equip/Unequip button.
    /// - Pauses game (Time.timeScale = 0) and unlocks mouse cursor.
    /// - Toggled with 'I' key, 'Escape', or Gamepad Start.
    /// </summary>
    public class BotWInventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PlayerInventory playerInventory;

        private ZeldaInputReader inputReader;

        private VisualElement root;
        private VisualElement menuBackdrop;
        private VisualElement tabContainer;
        private VisualElement gridContainer;
        private VisualElement detailsPanel;

        // Details Panel Elements
        private Label detailTitle;
        private Label detailSubType;
        private Label detailStat;
        private Label detailDesc;
        private Label detailIcon;
        private Button detailEquipBtn;

        private EquipmentCategory currentCategory = EquipmentCategory.Weapon;
        private EquipmentData selectedItem = null;
        private bool isOpen = false;

        private readonly EquipmentCategory[] categories = new EquipmentCategory[]
        {
            EquipmentCategory.Weapon,
            EquipmentCategory.Bow,
            EquipmentCategory.Shield,
            EquipmentCategory.Armor,
            EquipmentCategory.Material
        };

        private readonly string[] categoryNames = new string[]
        {
            "⚔️ WEAPONS",
            "🏹 BOWS",
            "🛡️ SHIELDS",
            "👕 ARMOR",
            "🎒 ITEMS"
        };

        private List<Button> tabButtons = new List<Button>();

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void Start()
        {
            inputReader = ZeldaInputReader.Instance;
            if (uiDocument != null)
            {
                BuildUIHierarchy();
                CloseInventory(true);
            }
        }

        private void Update()
        {
            if (inputReader == null) inputReader = ZeldaInputReader.Instance;
            if (inputReader == null) return;

            // Toggle Inventory
            if (inputReader.InventoryPressed)
            {
                if (isOpen)
                {
                    CloseInventory();
                }
                else
                {
                    OpenInventory();
                }
            }

            if (isOpen)
            {
                // Tab navigation with Q and E / Arrow keys
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.eKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
                    {
                        SwitchTabDelta(1);
                    }
                    else if (kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
                    {
                        SwitchTabDelta(-1);
                    }
                }
            }
        }

        public void OpenInventory()
        {
            if (isOpen) return;
            isOpen = true;

            Time.timeScale = 0f;
            ZeldaCursorManager.Instance?.SetMenuOpen(true);

            if (menuBackdrop != null)
            {
                menuBackdrop.style.display = DisplayStyle.Flex;
            }

            SelectCategory(currentCategory);
        }

        public void CloseInventory(bool restoreTime = true)
        {
            isOpen = false;
            if (restoreTime)
            {
                Time.timeScale = 1f;
                ZeldaCursorManager.Instance?.SetMenuOpen(false);
            }

            if (menuBackdrop != null)
            {
                menuBackdrop.style.display = DisplayStyle.None;
            }
        }

        private void SwitchTabDelta(int delta)
        {
            int idx = Array.IndexOf(categories, currentCategory);
            idx = (idx + delta + categories.Length) % categories.Length;
            SelectCategory(categories[idx]);
        }

        private void SelectCategory(EquipmentCategory category)
        {
            currentCategory = category;

            // Update Tab styling
            for (int i = 0; i < tabButtons.Count; i++)
            {
                bool active = categories[i] == currentCategory;
                tabButtons[i].style.backgroundColor = active ? new Color(0.2f, 0.5f, 0.7f, 0.4f) : new Color(0.06f, 0.1f, 0.15f, 0.6f);
                tabButtons[i].style.borderBottomColor = active ? new Color(0.95f, 0.75f, 0.2f) : Color.clear;
                tabButtons[i].style.borderBottomWidth = active ? 4 : 0;
                tabButtons[i].style.color = active ? new Color(1f, 0.9f, 0.4f) : new Color(0.7f, 0.8f, 0.9f);
            }

            PopulateGrid();
        }

        private void BuildUIHierarchy()
        {
            root = uiDocument.rootVisualElement;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

            // Full-screen backdrop
            menuBackdrop = new VisualElement();
            menuBackdrop.name = "BotW_Inventory_Backdrop";
            menuBackdrop.style.position = Position.Absolute;
            menuBackdrop.style.left = 0;
            menuBackdrop.style.right = 0;
            menuBackdrop.style.top = 0;
            menuBackdrop.style.bottom = 0;
            menuBackdrop.style.backgroundColor = new Color(0.04f, 0.07f, 0.11f, 0.95f);
            menuBackdrop.style.paddingLeft = 40;
            menuBackdrop.style.paddingRight = 40;
            menuBackdrop.style.paddingTop = 25;
            menuBackdrop.style.paddingBottom = 25;
            root.Add(menuBackdrop);

            // 1. TOP HEADER (Title & Category Tabs)
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 20;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = new Color(0.15f, 0.4f, 0.6f, 0.5f);
            header.style.paddingBottom = 12;
            menuBackdrop.Add(header);

            // Left Title
            Label title = new Label("SHEIKAH SLATE  //  INVENTORY");
            title.style.fontSize = 22;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.35f, 0.85f, 1f);
            header.Add(title);

            // Tabs Row
            tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            header.Add(tabContainer);

            tabButtons.Clear();
            for (int i = 0; i < categories.Length; i++)
            {
                int catIndex = i;
                Button tabBtn = new Button(() => SelectCategory(categories[catIndex]));
                tabBtn.text = categoryNames[i];
                tabBtn.style.fontSize = 15;
                tabBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                tabBtn.style.paddingLeft = 16;
                tabBtn.style.paddingRight = 16;
                tabBtn.style.paddingTop = 8;
                tabBtn.style.paddingBottom = 8;
                tabBtn.style.marginLeft = 6;
                tabBtn.style.marginRight = 6;
                tabBtn.style.borderTopLeftRadius = 6;
                tabBtn.style.borderTopRightRadius = 6;
                tabBtn.style.borderBottomLeftRadius = 0;
                tabBtn.style.borderBottomRightRadius = 0;
                tabBtn.style.borderTopWidth = 1;
                tabBtn.style.borderLeftWidth = 1;
                tabBtn.style.borderRightWidth = 1;
                tabBtn.style.borderTopColor = new Color(0.2f, 0.5f, 0.7f, 0.4f);
                tabBtn.style.borderLeftColor = new Color(0.2f, 0.5f, 0.7f, 0.4f);
                tabBtn.style.borderRightColor = new Color(0.2f, 0.5f, 0.7f, 0.4f);

                tabContainer.Add(tabBtn);
                tabButtons.Add(tabBtn);
            }

            // 2. MAIN BODY (Grid on left, Details on right)
            VisualElement mainBody = new VisualElement();
            mainBody.style.flexDirection = FlexDirection.Row;
            mainBody.style.flexGrow = 1;
            menuBackdrop.Add(mainBody);

            // Grid Container (Left 65%)
            VisualElement gridWrapper = new VisualElement();
            gridWrapper.style.width = Length.Percent(65);
            gridWrapper.style.paddingRight = 20;

            var gridTitle = new Label("GEAR POUCH");
            gridTitle.style.fontSize = 14;
            gridTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            gridTitle.style.color = new Color(0.5f, 0.75f, 0.9f);
            gridTitle.style.marginBottom = 10;
            gridWrapper.Add(gridTitle);

            ScrollView scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            gridContainer = new VisualElement();
            gridContainer.style.flexDirection = FlexDirection.Row;
            gridContainer.style.flexWrap = Wrap.Wrap;
            scroll.Add(gridContainer);
            gridWrapper.Add(scroll);
            mainBody.Add(gridWrapper);

            // Details Panel (Right 35%)
            detailsPanel = new VisualElement();
            detailsPanel.style.width = Length.Percent(35);
            detailsPanel.style.backgroundColor = new Color(0.06f, 0.1f, 0.16f, 0.9f);
            detailsPanel.style.borderTopLeftRadius = 10;
            detailsPanel.style.borderTopRightRadius = 10;
            detailsPanel.style.borderBottomLeftRadius = 10;
            detailsPanel.style.borderBottomRightRadius = 10;
            detailsPanel.style.borderTopWidth = 2;
            detailsPanel.style.borderLeftWidth = 2;
            detailsPanel.style.borderRightWidth = 2;
            detailsPanel.style.borderBottomWidth = 2;
            detailsPanel.style.borderTopColor = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            detailsPanel.style.borderLeftColor = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            detailsPanel.style.borderRightColor = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            detailsPanel.style.borderBottomColor = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            detailsPanel.style.paddingLeft = 24;
            detailsPanel.style.paddingRight = 24;
            detailsPanel.style.paddingTop = 24;
            detailsPanel.style.paddingBottom = 24;
            mainBody.Add(detailsPanel);

            detailIcon = new Label("⚔️");
            detailIcon.style.fontSize = 52;
            detailIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            detailIcon.style.marginBottom = 12;
            detailsPanel.Add(detailIcon);

            detailTitle = new Label("Item Name");
            detailTitle.style.fontSize = 24;
            detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailTitle.style.color = new Color(0.95f, 0.85f, 0.35f);
            detailTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            detailTitle.style.marginBottom = 4;
            detailsPanel.Add(detailTitle);

            detailSubType = new Label("Category");
            detailSubType.style.fontSize = 13;
            detailSubType.style.color = new Color(0.6f, 0.8f, 0.9f);
            detailSubType.style.unityTextAlign = TextAnchor.MiddleCenter;
            detailSubType.style.marginBottom = 15;
            detailsPanel.Add(detailSubType);

            detailStat = new Label("Attack: 30");
            detailStat.style.fontSize = 18;
            detailStat.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailStat.style.color = new Color(0.4f, 0.95f, 0.6f);
            detailStat.style.unityTextAlign = TextAnchor.MiddleCenter;
            detailStat.style.marginBottom = 18;
            detailsPanel.Add(detailStat);

            detailDesc = new Label("Item description...");
            detailDesc.style.fontSize = 14;
            detailDesc.style.color = new Color(0.85f, 0.9f, 0.95f);
            detailDesc.style.whiteSpace = WhiteSpace.Normal;
            detailDesc.style.marginBottom = 24;
            detailDesc.style.flexGrow = 1;
            detailsPanel.Add(detailDesc);

            detailEquipBtn = new Button(OnEquipButtonClicked);
            detailEquipBtn.text = "EQUIP";
            detailEquipBtn.style.fontSize = 16;
            detailEquipBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            detailEquipBtn.style.backgroundColor = new Color(0.15f, 0.55f, 0.35f);
            detailEquipBtn.style.color = Color.white;
            detailEquipBtn.style.paddingTop = 10;
            detailEquipBtn.style.paddingBottom = 10;
            detailEquipBtn.style.borderTopLeftRadius = 6;
            detailEquipBtn.style.borderTopRightRadius = 6;
            detailEquipBtn.style.borderBottomLeftRadius = 6;
            detailEquipBtn.style.borderBottomRightRadius = 6;
            detailsPanel.Add(detailEquipBtn);

            // 3. FOOTER PROMPT BAR
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;
            footer.style.marginTop = 15;
            footer.style.paddingTop = 10;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.2f, 0.4f, 0.6f, 0.4f);
            menuBackdrop.Add(footer);

            Label promptLeft = new Label("[Q / E] Switch Category    [Click] Select Item    [Double-Click] Equip");
            promptLeft.style.fontSize = 13;
            promptLeft.style.color = new Color(0.6f, 0.75f, 0.9f);
            footer.Add(promptLeft);

            Label promptRight = new Label("[I / Esc] Close Menu");
            promptRight.style.fontSize = 13;
            promptRight.style.color = new Color(0.85f, 0.75f, 0.3f);
            footer.Add(promptRight);
        }

        private void PopulateGrid()
        {
            if (gridContainer == null || playerInventory == null) return;
            gridContainer.Clear();

            List<EquipmentData> items = currentCategory switch
            {
                EquipmentCategory.Weapon => playerInventory.Weapons,
                EquipmentCategory.Bow => playerInventory.Bows,
                EquipmentCategory.Shield => playerInventory.Shields,
                EquipmentCategory.Armor => playerInventory.Armors,
                EquipmentCategory.Material => playerInventory.Materials,
                _ => new List<EquipmentData>()
            };

            EquipmentData currentActive = currentCategory switch
            {
                EquipmentCategory.Weapon => playerInventory.ActiveWeapon,
                EquipmentCategory.Bow => playerInventory.ActiveBow,
                EquipmentCategory.Shield => playerInventory.ActiveShield,
                EquipmentCategory.Armor => playerInventory.ActiveArmor,
                EquipmentCategory.Material => playerInventory.ActiveItem,
                _ => null
            };

            if (items.Count > 0)
            {
                if (selectedItem == null || !items.Contains(selectedItem))
                {
                    selectedItem = currentActive ?? items[0];
                }
            }
            else
            {
                selectedItem = null;
            }

            foreach (var item in items)
            {
                var card = CreateItemCard(item, item == currentActive, item == selectedItem);
                gridContainer.Add(card);
            }

            UpdateDetailsPanel();
        }

        private VisualElement CreateItemCard(EquipmentData item, bool isEquipped, bool isSelected)
        {
            VisualElement card = new VisualElement();
            card.style.width = 135;
            card.style.height = 135;
            card.style.marginRight = 12;
            card.style.marginBottom = 12;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.alignItems = Align.Center;
            card.style.justifyContent = Justify.SpaceBetween;

            // Background & Border
            card.style.backgroundColor = isSelected ? new Color(0.15f, 0.3f, 0.45f, 0.9f) : new Color(0.08f, 0.12f, 0.18f, 0.8f);
            card.style.borderTopWidth = isSelected ? 3 : 1;
            card.style.borderLeftWidth = isSelected ? 3 : 1;
            card.style.borderRightWidth = isSelected ? 3 : 1;
            card.style.borderBottomWidth = isSelected ? 3 : 1;

            Color borderColor = isSelected ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.2f, 0.45f, 0.65f, 0.5f);
            card.style.borderTopColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;
            card.style.borderBottomColor = borderColor;

            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;

            // Top Row: Stat Badge & Equipped Tag
            VisualElement topRow = new VisualElement();
            topRow.style.width = Length.Percent(100);
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;

            Label statLabel = new Label(item.powerValue > 0 ? $"{item.powerValue}" : "");
            statLabel.style.fontSize = 12;
            statLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statLabel.style.color = new Color(0.4f, 0.9f, 0.6f);
            topRow.Add(statLabel);

            if (isEquipped)
            {
                Label eqBadge = new Label("EQ");
                eqBadge.style.fontSize = 10;
                eqBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
                eqBadge.style.color = new Color(0.2f, 0.95f, 0.95f);
                eqBadge.style.backgroundColor = new Color(0.1f, 0.4f, 0.4f, 0.8f);
                eqBadge.style.paddingLeft = 4;
                eqBadge.style.paddingRight = 4;
                eqBadge.style.borderTopLeftRadius = 3;
                eqBadge.style.borderTopRightRadius = 3;
                eqBadge.style.borderBottomLeftRadius = 3;
                eqBadge.style.borderBottomRightRadius = 3;
                topRow.Add(eqBadge);
            }
            card.Add(topRow);

            // Icon Center
            Label iconLbl = new Label(item.iconSymbol);
            iconLbl.style.fontSize = 36;
            iconLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(iconLbl);

            // Name Bottom
            Label nameLbl = new Label(item.displayName);
            nameLbl.style.fontSize = 11;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.color = isSelected ? new Color(1f, 0.9f, 0.4f) : new Color(0.85f, 0.9f, 0.95f);
            nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLbl.style.whiteSpace = WhiteSpace.Normal;
            card.Add(nameLbl);

            // Click Handler
            card.RegisterCallback<ClickEvent>(evt =>
            {
                selectedItem = item;
                if (evt.clickCount >= 2)
                {
                    OnEquipButtonClicked();
                }
                else
                {
                    PopulateGrid();
                }
            });

            return card;
        }

        private void UpdateDetailsPanel()
        {
            if (detailsPanel == null) return;

            if (selectedItem == null)
            {
                detailIcon.text = "—";
                detailTitle.text = "No Item Selected";
                detailSubType.text = "";
                detailStat.text = "";
                detailDesc.text = "Select an item from the pouch to view its attributes and equip it.";
                detailEquipBtn.style.display = DisplayStyle.None;
                return;
            }

            detailIcon.text = selectedItem.iconSymbol;
            detailTitle.text = selectedItem.displayName;
            detailSubType.text = selectedItem.subType ?? selectedItem.category.ToString();

            if (selectedItem.category == EquipmentCategory.Weapon || selectedItem.category == EquipmentCategory.Bow)
            {
                detailStat.text = $"Attack Power: {selectedItem.powerValue}";
            }
            else if (selectedItem.category == EquipmentCategory.Shield || selectedItem.category == EquipmentCategory.Armor)
            {
                detailStat.text = $"Defense Guard: {selectedItem.powerValue}";
            }
            else
            {
                detailStat.text = selectedItem.powerValue > 0 ? $"Power: {selectedItem.powerValue}" : "";
            }

            detailDesc.text = selectedItem.description;

            // Determine if equipped
            bool isEquipped = currentCategory switch
            {
                EquipmentCategory.Weapon => playerInventory.ActiveWeapon == selectedItem,
                EquipmentCategory.Bow => playerInventory.ActiveBow == selectedItem,
                EquipmentCategory.Shield => playerInventory.ActiveShield == selectedItem,
                EquipmentCategory.Armor => playerInventory.ActiveArmor == selectedItem,
                EquipmentCategory.Material => playerInventory.ActiveItem == selectedItem,
                _ => false
            };

            detailEquipBtn.style.display = DisplayStyle.Flex;
            detailEquipBtn.text = isEquipped ? "EQUIPPED" : "EQUIP";
            detailEquipBtn.style.backgroundColor = isEquipped ? new Color(0.2f, 0.4f, 0.35f) : new Color(0.18f, 0.55f, 0.85f);
        }

        private void OnEquipButtonClicked()
        {
            if (selectedItem == null || playerInventory == null) return;

            playerInventory.Equip(selectedItem);
            PopulateGrid();
        }
    }
}
