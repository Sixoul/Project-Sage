using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZeldaOoT.Combat;
using ZeldaOoT.Water;
using ZeldaOoT.Player;

namespace ZeldaOoT.UI
{
    /// <summary>
    /// Zelda HUD built with UI Toolkit:
    /// Displays Heart Containers, Oxygen/Diving meter, animated Z-Targeting reticle,
    /// and classic action button prompts.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ZeldaHUDUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        // HUD Elements
        private VisualElement heartsContainer;
        private VisualElement oxygenContainer;
        private VisualElement oxygenBarFill;
        private VisualElement zTargetReticle;
        private VisualElement rotatingReticleRing;
        private Label actionPromptLabel;

        private HealthSystem playerHealth;
        private WaterSwimController swimController;
        private ZTargetSystem zTargetSystem;
        private Camera mainCam;

        private readonly List<VisualElement> heartElements = new List<VisualElement>();

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            mainCam = Camera.main;
            var player = FindFirstObjectByType<PlayerLocomotion>();
            if (player != null)
            {
                playerHealth = player.GetComponent<HealthSystem>();
                swimController = player.GetComponent<WaterSwimController>();
                zTargetSystem = player.GetComponent<ZTargetSystem>();
            }

            BuildHUDHierarchy();

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHearts;
                UpdateHearts(playerHealth.CurrentHP, playerHealth.MaxHP);
            }

            if (swimController != null)
            {
                swimController.OnOxygenChanged += UpdateOxygen;
            }
        }

        private void BuildHUDHierarchy()
        {
            root = uiDocument.rootVisualElement;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.position = Position.Absolute;

            // 1. Hearts Container (Top-Left)
            heartsContainer = root.Q<VisualElement>("HeartsContainer");
            if (heartsContainer == null)
            {
                heartsContainer = new VisualElement();
                heartsContainer.name = "HeartsContainer";
                heartsContainer.style.position = Position.Absolute;
                heartsContainer.style.top = 24;
                heartsContainer.style.left = 28;
                heartsContainer.style.flexDirection = FlexDirection.Row;
                root.Add(heartsContainer);
            }

            // 2. Oxygen Gauge (Center-Top, hidden by default)
            oxygenContainer = root.Q<VisualElement>("OxygenContainer");
            if (oxygenContainer == null)
            {
                oxygenContainer = new VisualElement();
                oxygenContainer.name = "OxygenContainer";
                oxygenContainer.style.position = Position.Absolute;
                oxygenContainer.style.top = 35;
                oxygenContainer.style.left = Length.Percent(50);
                oxygenContainer.transform.position = new Vector3(-110, 0, 0); // half width offset
                oxygenContainer.style.width = 220;
                oxygenContainer.style.height = 18;
                oxygenContainer.style.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.85f);
                oxygenContainer.style.borderTopWidth = 2;
                oxygenContainer.style.borderBottomWidth = 2;
                oxygenContainer.style.borderLeftWidth = 2;
                oxygenContainer.style.borderRightWidth = 2;
                oxygenContainer.style.borderTopColor = new Color(0.2f, 0.8f, 1f);
                oxygenContainer.style.borderBottomColor = new Color(0.2f, 0.8f, 1f);
                oxygenContainer.style.borderLeftColor = new Color(0.2f, 0.8f, 1f);
                oxygenContainer.style.borderRightColor = new Color(0.2f, 0.8f, 1f);
                oxygenContainer.style.display = DisplayStyle.None;

                oxygenBarFill = new VisualElement();
                oxygenBarFill.name = "OxygenBarFill";
                oxygenBarFill.style.height = Length.Percent(100);
                oxygenBarFill.style.width = Length.Percent(100);
                oxygenBarFill.style.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
                oxygenContainer.Add(oxygenBarFill);

                root.Add(oxygenContainer);
            }
            else
            {
                oxygenBarFill = oxygenContainer.Q<VisualElement>("OxygenBarFill");
                if (oxygenBarFill == null)
                {
                    oxygenBarFill = new VisualElement();
                    oxygenBarFill.name = "OxygenBarFill";
                    oxygenBarFill.style.height = Length.Percent(100);
                    oxygenBarFill.style.width = Length.Percent(100);
                    oxygenBarFill.style.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
                    oxygenContainer.Add(oxygenBarFill);
                }
            }

            // 3. Z-Targeting Reticle (Polished Rotating Crosshair)
            zTargetReticle = root.Q<VisualElement>("ZTargetReticle");
            if (zTargetReticle == null)
            {
                zTargetReticle = new VisualElement();
                zTargetReticle.name = "ZTargetReticle";
                zTargetReticle.style.position = Position.Absolute;
                zTargetReticle.style.width = 64;
                zTargetReticle.style.height = 64;
                zTargetReticle.style.alignItems = Align.Center;
                zTargetReticle.style.justifyContent = Justify.Center;
                zTargetReticle.style.display = DisplayStyle.None;

                // Rotating Crosshair Ring
                rotatingReticleRing = new VisualElement();
                rotatingReticleRing.name = "RotatingCrosshairRing";
                rotatingReticleRing.style.position = Position.Absolute;
                rotatingReticleRing.style.width = 64;
                rotatingReticleRing.style.height = 64;
                rotatingReticleRing.style.alignItems = Align.Center;
                rotatingReticleRing.style.justifyContent = Justify.Center;

                // Circular outer ring
                VisualElement outerRing = new VisualElement();
                outerRing.style.position = Position.Absolute;
                outerRing.style.width = 46;
                outerRing.style.height = 46;
                outerRing.style.borderTopWidth = 1.5f;
                outerRing.style.borderBottomWidth = 1.5f;
                outerRing.style.borderLeftWidth = 1.5f;
                outerRing.style.borderRightWidth = 1.5f;
                Color ringColor = new Color(1f, 0.85f, 0.2f, 0.55f);
                outerRing.style.borderTopColor = ringColor;
                outerRing.style.borderBottomColor = ringColor;
                outerRing.style.borderLeftColor = ringColor;
                outerRing.style.borderRightColor = ringColor;
                outerRing.style.borderTopLeftRadius = 23;
                outerRing.style.borderTopRightRadius = 23;
                outerRing.style.borderBottomLeftRadius = 23;
                outerRing.style.borderBottomRightRadius = 23;
                rotatingReticleRing.Add(outerRing);

                Color crosshairColor = new Color(1f, 0.92f, 0.15f, 0.95f);

                // 4 Crosshair Tick Marks (Top, Bottom, Left, Right)
                VisualElement tickTop = new VisualElement();
                tickTop.style.position = Position.Absolute;
                tickTop.style.top = 0;
                tickTop.style.left = 30.5f;
                tickTop.style.width = 3;
                tickTop.style.height = 12;
                tickTop.style.backgroundColor = crosshairColor;
                tickTop.style.borderTopLeftRadius = 1.5f;
                tickTop.style.borderTopRightRadius = 1.5f;
                rotatingReticleRing.Add(tickTop);

                VisualElement tickBottom = new VisualElement();
                tickBottom.style.position = Position.Absolute;
                tickBottom.style.bottom = 0;
                tickBottom.style.left = 30.5f;
                tickBottom.style.width = 3;
                tickBottom.style.height = 12;
                tickBottom.style.backgroundColor = crosshairColor;
                tickBottom.style.borderBottomLeftRadius = 1.5f;
                tickBottom.style.borderBottomRightRadius = 1.5f;
                rotatingReticleRing.Add(tickBottom);

                VisualElement tickLeft = new VisualElement();
                tickLeft.style.position = Position.Absolute;
                tickLeft.style.left = 0;
                tickLeft.style.top = 30.5f;
                tickLeft.style.width = 12;
                tickLeft.style.height = 3;
                tickLeft.style.backgroundColor = crosshairColor;
                tickLeft.style.borderTopLeftRadius = 1.5f;
                tickLeft.style.borderBottomLeftRadius = 1.5f;
                rotatingReticleRing.Add(tickLeft);

                VisualElement tickRight = new VisualElement();
                tickRight.style.position = Position.Absolute;
                tickRight.style.right = 0;
                tickRight.style.top = 30.5f;
                tickRight.style.width = 12;
                tickRight.style.height = 3;
                tickRight.style.backgroundColor = crosshairColor;
                tickRight.style.borderTopRightRadius = 1.5f;
                tickRight.style.borderBottomRightRadius = 1.5f;
                rotatingReticleRing.Add(tickRight);

                zTargetReticle.Add(rotatingReticleRing);

                // Central Aiming Pip / Dot
                VisualElement centerPip = new VisualElement();
                centerPip.style.position = Position.Absolute;
                centerPip.style.width = 6;
                centerPip.style.height = 6;
                centerPip.style.left = 29;
                centerPip.style.top = 29;
                centerPip.style.backgroundColor = new Color(1f, 1f, 0.5f, 0.95f);
                centerPip.style.borderTopLeftRadius = 3;
                centerPip.style.borderTopRightRadius = 3;
                centerPip.style.borderBottomLeftRadius = 3;
                centerPip.style.borderBottomRightRadius = 3;
                zTargetReticle.Add(centerPip);

                root.Add(zTargetReticle);
            }

            // 4. Action Prompts Help Box (Bottom-Right)
            VisualElement promptBox = root.Q<VisualElement>("ActionPromptBox");
            if (promptBox == null)
            {
                promptBox = new VisualElement();
                promptBox.name = "ActionPromptBox";
                promptBox.style.position = Position.Absolute;
                promptBox.style.bottom = 20;
                promptBox.style.right = 24;
                promptBox.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
                promptBox.style.paddingTop = 10;
                promptBox.style.paddingBottom = 10;
                promptBox.style.paddingLeft = 14;
                promptBox.style.paddingRight = 14;
                promptBox.style.borderTopLeftRadius = 8;
                promptBox.style.borderTopRightRadius = 8;
                promptBox.style.borderBottomLeftRadius = 8;
                promptBox.style.borderBottomRightRadius = 8;

                actionPromptLabel = new Label("WASD: Move | Space: Jump / Dodge\nLMB: Sword Combo | RMB: Shield Block\nTab / MMB (Tap): Lock-On | Shift: Dive\nHold Q (0.25s): Weapon Wheel | ◀/▶: Cycle Accessory | ▼/E: Use Item");
                actionPromptLabel.style.fontSize = 13;
                actionPromptLabel.style.color = new Color(0.9f, 0.95f, 1f);
                actionPromptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                promptBox.Add(actionPromptLabel);
                root.Add(promptBox);
            }
        }

        private void Update()
        {
            if (mainCam == null) mainCam = Camera.main;

            UpdateZTargetReticle();

            if (swimController != null)
            {
                if (swimController.IsSwimming || swimController.IsDiving)
                {
                    UpdateOxygen(swimController.CurrentOxygen, swimController.MaxOxygen);
                }
                else if (oxygenContainer != null && oxygenContainer.style.display != DisplayStyle.None)
                {
                    oxygenContainer.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateZTargetReticle()
        {
            if (zTargetSystem != null && zTargetSystem.IsLockedOn && zTargetSystem.CurrentTarget != null && mainCam != null)
            {
                // Aim position tracks enemy chest/center
                Vector3 targetWorldPos = zTargetSystem.TargetAimPosition;
                Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPos);

                // Only render when target is in front of camera
                if (screenPos.z > 0f)
                {
                    zTargetReticle.style.display = DisplayStyle.Flex;

                    // Convert from screen coordinates (bottom-left origin) to panel space
                    Vector2 screen2D = new Vector2(screenPos.x, Screen.height - screenPos.y);
                    Vector2 panelPos = (root.panel != null) ? RuntimePanelUtils.ScreenToPanel(root.panel, screen2D) : screen2D;

                    float halfSize = 32f;
                    zTargetReticle.style.left = panelPos.x - halfSize;
                    zTargetReticle.style.top = panelPos.y - halfSize;

                    // Smooth continuous rotation & breathing pulse
                    float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                    if (rotatingReticleRing != null)
                    {
                        rotatingReticleRing.transform.rotation = Quaternion.Euler(0f, 0f, Time.time * 50f);
                        rotatingReticleRing.transform.scale = new Vector3(pulse, pulse, 1f);
                    }
                    return;
                }
            }

            zTargetReticle.style.display = DisplayStyle.None;
        }

        private void UpdateHearts(float currentHP, float maxHP)
        {
            heartsContainer.Clear();
            heartElements.Clear();

            int totalHearts = Mathf.CeilToInt(maxHP / 4f);
            float remainingHP = currentHP;

            for (int i = 0; i < totalHearts; i++)
            {
                VisualElement heart = new VisualElement();
                heart.style.width = 28;
                heart.style.height = 28;
                heart.style.marginRight = 6;
                heart.style.backgroundColor = new Color(0.2f, 0.05f, 0.05f, 0.8f);
                heart.style.borderTopWidth = 2;
                heart.style.borderBottomWidth = 2;
                heart.style.borderLeftWidth = 2;
                heart.style.borderRightWidth = 2;
                heart.style.borderTopColor = new Color(0.8f, 0.1f, 0.1f);
                heart.style.borderBottomColor = new Color(0.8f, 0.1f, 0.1f);
                heart.style.borderLeftColor = new Color(0.8f, 0.1f, 0.1f);
                heart.style.borderRightColor = new Color(0.8f, 0.1f, 0.1f);
                heart.style.borderTopLeftRadius = 14;
                heart.style.borderTopRightRadius = 14;
                heart.style.borderBottomLeftRadius = 14;
                heart.style.borderBottomRightRadius = 14;

                // Inner fill based on quarter hearts
                VisualElement innerFill = new VisualElement();
                float heartFillPercent = Mathf.Clamp01(remainingHP / 4f) * 100f;
                innerFill.style.width = Length.Percent(100);
                innerFill.style.height = Length.Percent(heartFillPercent);
                innerFill.style.backgroundColor = new Color(1f, 0.15f, 0.15f); // Zelda crimson
                innerFill.style.position = Position.Absolute;
                innerFill.style.bottom = 0;
                innerFill.style.borderBottomLeftRadius = 12;
                innerFill.style.borderBottomRightRadius = 12;
                if (heartFillPercent >= 90)
                {
                    innerFill.style.borderTopLeftRadius = 12;
                    innerFill.style.borderTopRightRadius = 12;
                }

                heart.Add(innerFill);
                heartsContainer.Add(heart);
                heartElements.Add(heart);

                remainingHP -= 4f;
            }
        }

        private void UpdateOxygen(float current, float max)
        {
            if (swimController != null && (swimController.IsSwimming || swimController.IsDiving))
            {
                oxygenContainer.style.display = DisplayStyle.Flex;
                float pct = Mathf.Clamp01(current / max) * 100f;
                oxygenBarFill.style.width = Length.Percent(pct);

                // Color warning when oxygen low
                oxygenBarFill.style.backgroundColor = pct < 30f ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 0.9f, 0.4f);
            }
            else
            {
                oxygenContainer.style.display = DisplayStyle.None;
            }
        }
    }
}
