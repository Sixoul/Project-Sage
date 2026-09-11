using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZeldaOoT.Input;
using ZeldaOoT.Inventory;

namespace ZeldaOoT.UI
{
    /// <summary>
    /// Breath of the Wild Style Quick-Swap Equipment Menu built with UI Toolkit.
    /// Holding a menu hotkey slows time and displays a horizontal equipment carousel.
    /// Cycling selections and releasing hotkey equips instantly.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BotWQuickSwapUI : MonoBehaviour
    {
        [Header("Slow-Mo Settings")]
        [SerializeField] private float slowTimeScale = 0.08f;

        private UIDocument uiDocument;
        private ZeldaInputReader inputReader;
        private PlayerInventory playerInventory;

        private VisualElement root;
        private VisualElement menuContainer;
        private Label categoryLabel;
        private VisualElement cardStrip;
        private Label itemNameLabel;
        private Label itemDescLabel;
        private Label itemStatLabel;

        private bool isMenuOpen = false;
        private EquipmentCategory currentCategory = EquipmentCategory.Weapon;
        private List<EquipmentData> currentList = new List<EquipmentData>();
        private int selectedIndex = 0;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            inputReader = ZeldaInputReader.Instance;
            playerInventory = FindFirstObjectByType<PlayerInventory>();

            BuildUIHierarchy();
            HideMenu();
        }

        private void BuildUIHierarchy()
        {
            root = uiDocument.rootVisualElement;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.position = Position.Absolute;

            // Main menu backdrop / container
            menuContainer = new VisualElement();
            menuContainer.name = "BotWQuickMenu";
            menuContainer.style.position = Position.Absolute;
            menuContainer.style.bottom = 120;
            menuContainer.style.left = 0;
            menuContainer.style.right = 0;
            menuContainer.style.alignItems = Align.Center;
            menuContainer.style.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            menuContainer.style.borderTopWidth = 3;
            menuContainer.style.borderBottomWidth = 3;
            menuContainer.style.borderTopColor = new Color(0.2f, 0.8f, 1f, 0.7f);
            menuContainer.style.borderBottomColor = new Color(0.2f, 0.8f, 1f, 0.7f);
            menuContainer.style.paddingTop = 15;
            menuContainer.style.paddingBottom = 20;

            // Category Title (e.g. "WEAPONS", "SHIELDS", "ITEMS")
            categoryLabel = new Label("WEAPONS");
            categoryLabel.style.fontSize = 22;
            categoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            categoryLabel.style.color = new Color(0.85f, 0.95f, 1f);
            categoryLabel.style.marginBottom = 10;
            menuContainer.Add(categoryLabel);

            // Horizontal card strip
            cardStrip = new VisualElement();
            cardStrip.style.flexDirection = FlexDirection.Row;
            cardStrip.style.justifyContent = Justify.Center;
            cardStrip.style.alignItems = Align.Center;
            cardStrip.style.marginBottom = 12;
            menuContainer.Add(cardStrip);

            // Selected item name & stat
            itemNameLabel = new Label("Master Sword");
            itemNameLabel.style.fontSize = 20;
            itemNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            itemNameLabel.style.color = Color.white;
            menuContainer.Add(itemNameLabel);

            itemStatLabel = new Label("Attack Power: 30");
            itemStatLabel.style.fontSize = 15;
            itemStatLabel.style.color = new Color(0.2f, 0.85f, 1f);
            itemStatLabel.style.marginBottom = 4;
            menuContainer.Add(itemStatLabel);

            itemDescLabel = new Label("Description");
            itemDescLabel.style.fontSize = 13;
            itemDescLabel.style.color = new Color(0.75f, 0.75f, 0.75f);
            menuContainer.Add(itemDescLabel);

            root.Add(menuContainer);
        }

        private void Update()
        {
            if (inputReader == null) inputReader = ZeldaInputReader.Instance;
            if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (inputReader == null || playerInventory == null) return;

            // Open checks
            if (!isMenuOpen)
            {
                if (inputReader.QuickWeaponHeld) OpenMenu(EquipmentCategory.Weapon);
                else if (inputReader.QuickShieldHeld) OpenMenu(EquipmentCategory.Shield);
                else if (inputReader.QuickItemHeld) OpenMenu(EquipmentCategory.Item);
            }
            else
            {
                // Active menu check: when hotkey released, close menu and equip
                bool stillHeld = (currentCategory == EquipmentCategory.Weapon && inputReader.QuickWeaponHeld) ||
                                 (currentCategory == EquipmentCategory.Shield && inputReader.QuickShieldHeld) ||
                                 (currentCategory == EquipmentCategory.Item && inputReader.QuickItemHeld);

                if (!stillHeld)
                {
                    ConfirmAndCloseMenu();
                }
                else
                {
                    // Handle cycling
                    if (Mathf.Abs(inputReader.QuickCycleInput) > 0.1f)
                    {
                        int delta = inputReader.QuickCycleInput > 0 ? 1 : -1;
                        CycleSelection(delta);
                    }
                }
            }
        }

        private void OpenMenu(EquipmentCategory category)
        {
            isMenuOpen = true;
            currentCategory = category;

            // Apply slow-mo bullet time
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = 0.02f * slowTimeScale;
            ZeldaOoT.CameraRig.ZeldaCursorManager.Instance?.SetMenuOpen(true);

            menuContainer.style.display = DisplayStyle.Flex;
            categoryLabel.text = category switch
            {
                EquipmentCategory.Weapon => "— WEAPONS —",
                EquipmentCategory.Shield => "— SHIELDS —",
                EquipmentCategory.Item => "— ITEMS —",
                _ => ""
            };

            currentList = category switch
            {
                EquipmentCategory.Weapon => playerInventory.Weapons,
                EquipmentCategory.Shield => playerInventory.Shields,
                EquipmentCategory.Item => playerInventory.Items,
                _ => new List<EquipmentData>()
            };

            // Set initial selected index to current active equipment
            selectedIndex = 0;
            EquipmentData active = category switch
            {
                EquipmentCategory.Weapon => playerInventory.ActiveWeapon,
                EquipmentCategory.Shield => playerInventory.ActiveShield,
                EquipmentCategory.Item => playerInventory.ActiveItem,
                _ => null
            };

            if (active != null)
            {
                int idx = currentList.FindIndex(x => x.id == active.id);
                if (idx >= 0) selectedIndex = idx;
            }

            RefreshCards();
        }

        private void CycleSelection(int delta)
        {
            if (currentList.Count == 0) return;
            selectedIndex = (selectedIndex + delta + currentList.Count) % currentList.Count;
            RefreshCards();
        }

        private void ConfirmAndCloseMenu()
        {
            if (selectedIndex >= 0 && selectedIndex < currentList.Count)
            {
                var chosen = currentList[selectedIndex];
                switch (currentCategory)
                {
                    case EquipmentCategory.Weapon:
                        playerInventory.EquipWeapon(chosen);
                        break;
                    case EquipmentCategory.Shield:
                        playerInventory.EquipShield(chosen);
                        break;
                    case EquipmentCategory.Item:
                        playerInventory.EquipItem(chosen);
                        break;
                }
            }

            HideMenu();
        }

        private void HideMenu()
        {
            isMenuOpen = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            ZeldaOoT.CameraRig.ZeldaCursorManager.Instance?.SetMenuOpen(false);
            if (menuContainer != null) menuContainer.style.display = DisplayStyle.None;
        }

        private void RefreshCards()
        {
            cardStrip.Clear();

            for (int i = 0; i < currentList.Count; i++)
            {
                var item = currentList[i];
                bool isSelected = (i == selectedIndex);

                VisualElement card = new VisualElement();
                card.style.width = 90;
                card.style.height = 95;
                card.style.marginLeft = 8;
                card.style.marginRight = 8;
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.Center;
                card.style.backgroundColor = isSelected ? new Color(0.15f, 0.35f, 0.5f, 0.95f) : new Color(0.08f, 0.12f, 0.16f, 0.8f);
                card.style.borderTopWidth = isSelected ? 3 : 1;
                card.style.borderBottomWidth = isSelected ? 3 : 1;
                card.style.borderLeftWidth = isSelected ? 3 : 1;
                card.style.borderRightWidth = isSelected ? 3 : 1;

                Color borderColor = isSelected ? new Color(1f, 0.85f, 0.2f) : new Color(0.3f, 0.4f, 0.5f, 0.5f);
                card.style.borderTopColor = borderColor;
                card.style.borderBottomColor = borderColor;
                card.style.borderLeftColor = borderColor;
                card.style.borderRightColor = borderColor;

                // Color badge icon
                VisualElement iconBadge = new VisualElement();
                iconBadge.style.width = 44;
                iconBadge.style.height = 44;
                iconBadge.style.backgroundColor = item.iconColor;
                iconBadge.style.marginBottom = 6;
                iconBadge.style.borderTopLeftRadius = 6;
                iconBadge.style.borderTopRightRadius = 6;
                iconBadge.style.borderBottomLeftRadius = 6;
                iconBadge.style.borderBottomRightRadius = 6;
                card.Add(iconBadge);

                // Small rating number
                Label statTag = new Label(item.powerValue.ToString());
                statTag.style.fontSize = 13;
                statTag.style.unityFontStyleAndWeight = FontStyle.Bold;
                statTag.style.color = Color.white;
                card.Add(statTag);

                cardStrip.Add(card);
            }

            if (selectedIndex >= 0 && selectedIndex < currentList.Count)
            {
                var sel = currentList[selectedIndex];
                itemNameLabel.text = sel.displayName;
                itemStatLabel.text = currentCategory switch
                {
                    EquipmentCategory.Weapon => $"Attack Power: {sel.powerValue}",
                    EquipmentCategory.Shield => $"Shield Guard: {sel.powerValue}",
                    EquipmentCategory.Item => $"Item Power: {sel.powerValue}",
                    _ => ""
                };
                itemDescLabel.text = sel.description;
            }
        }
    }
}
