using UnityEngine;
using UnityEngine.InputSystem;

namespace ZeldaOoT.Input
{
    /// <summary>
    /// Unified Input Reader supporting both the new Input System and direct device querying.
    /// Provides clean states and events for movement, camera, combat, diving, and BotW quick menus.
    /// </summary>
    public class ZeldaInputReader : MonoBehaviour
    {
        public static ZeldaInputReader Instance { get; private set; }

        [Header("Live Input Values")]
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackReleased { get; private set; }
        public bool BlockHeld { get; private set; }
        public bool LockOnPressed { get; private set; }
        public bool DiveHeld { get; private set; }
        public bool ItemActionPressed { get; private set; }
        public bool InventoryPressed { get; private set; }

        [Header("Weapon Wheel & D-Pad Accessory Menus")]
        public bool WeaponWheelHeld { get; private set; }
        public Vector2 WheelPointerInput { get; private set; } // stick or mouse delta
        public bool DPadLeftPressed { get; private set; }
        public bool DPadRightPressed { get; private set; }
        public bool DPadDownPressed { get; private set; }
        public bool DPadUpPressed { get; private set; }

        [Header("Legacy Quick Swap Menus")]
        public bool QuickWeaponHeld { get; private set; }
        public bool QuickShieldHeld { get; private set; }
        public bool QuickItemHeld { get; private set; }
        public float QuickCycleInput { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            ReadKeyboardAndMouse();
            ReadGamepad();
        }

        private void ReadKeyboardAndMouse()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null) return;

            // Move Vector
            float x = 0f;
            float y = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            MoveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);

            // Look Vector
            if (mouse != null)
            {
                LookInput = mouse.delta.ReadValue() * 0.1f;
            }

            // Actions
            JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            JumpHeld = keyboard.spaceKey.isPressed;
            AttackPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            AttackHeld = mouse != null && mouse.leftButton.isPressed;
            AttackReleased = mouse != null && mouse.leftButton.wasReleasedThisFrame;
            BlockHeld = mouse != null && mouse.rightButton.isPressed;
            LockOnPressed = mouse != null && mouse.middleButton.wasPressedThisFrame;
            DiveHeld = keyboard.leftShiftKey.isPressed || keyboard.cKey.isPressed;
            ItemActionPressed = keyboard.eKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame;
            InventoryPressed = keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame;

            // Horizon Weapon Wheel (Hold Q / Tab / Middle Mouse or Left Bumper)
            WeaponWheelHeld = keyboard.qKey.isPressed || (mouse != null && mouse.middleButton.isPressed);

            // Wheel pointer input (Mouse position offset or stick direction)
            if (mouse != null && WeaponWheelHeld)
            {
                // In wheel mode, mouse position relative to screen center gives direct radial vector
                Vector2 mouseScreenPos = mouse.position.ReadValue();
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 deltaFromCenter = mouseScreenPos - screenCenter;
                if (deltaFromCenter.sqrMagnitude > 400f) // deadzone of 20 pixels
                {
                    WheelPointerInput = deltaFromCenter.normalized;
                }
            }

            // D-Pad Accessory inputs (Arrow keys or Gamepad D-pad)
            DPadLeftPressed = keyboard.leftArrowKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame;
            DPadRightPressed = keyboard.rightArrowKey.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame;
            DPadDownPressed = keyboard.downArrowKey.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame;
            DPadUpPressed = keyboard.upArrowKey.wasPressedThisFrame;

            // Legacy BotW Quick Menus
            QuickWeaponHeld = keyboard.digit1Key.isPressed;
            QuickShieldHeld = keyboard.digit2Key.isPressed;
            QuickItemHeld = keyboard.digit3Key.isPressed;

            // Quick Cycle (Scroll wheel or [ ] keys)
            QuickCycleInput = 0f;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    QuickCycleInput = Mathf.Sign(scroll);
                }
            }
            if (keyboard.leftBracketKey.wasPressedThisFrame) QuickCycleInput = -1f;
            if (keyboard.rightBracketKey.wasPressedThisFrame) QuickCycleInput = 1f;
        }

        private void ReadGamepad()
        {
            var pad = Gamepad.current;
            if (pad == null) return;

            // Stick move & look override if used
            Vector2 stickMove = pad.leftStick.ReadValue();
            if (stickMove.sqrMagnitude > 0.05f)
            {
                MoveInput = stickMove;
            }

            Vector2 stickLook = pad.rightStick.ReadValue();
            if (stickLook.sqrMagnitude > 0.05f)
            {
                LookInput = stickLook * 2.5f;
            }

            // Gamepad buttons
            if (pad.buttonSouth.wasPressedThisFrame) JumpPressed = true;
            if (pad.buttonSouth.isPressed) JumpHeld = true;
            if (pad.buttonWest.wasPressedThisFrame) AttackPressed = true;
            if (pad.buttonWest.isPressed) AttackHeld = true;
            if (pad.buttonWest.wasReleasedThisFrame) AttackReleased = true;
            if (pad.leftTrigger.isPressed) BlockHeld = true;
            if (pad.rightStickButton.wasPressedThisFrame) LockOnPressed = true;
            if (pad.buttonEast.isPressed) DiveHeld = true;
            if (pad.buttonNorth.wasPressedThisFrame) ItemActionPressed = true;
            if (pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame) InventoryPressed = true;

            // Horizon Weapon Wheel (Hold Left Bumper / L1)
            if (pad.leftShoulder.isPressed)
            {
                WeaponWheelHeld = true;
                // Right stick points to weapon segment
                if (stickLook.sqrMagnitude > 0.25f)
                {
                    WheelPointerInput = stickLook.normalized;
                }
            }

            // D-Pad Quick Accessory Menu
            if (pad.dpad.left.wasPressedThisFrame) DPadLeftPressed = true;
            if (pad.dpad.right.wasPressedThisFrame) DPadRightPressed = true;
            if (pad.dpad.down.wasPressedThisFrame) DPadDownPressed = true;
            if (pad.dpad.up.wasPressedThisFrame) DPadUpPressed = true;

            if (pad.rightShoulder.wasPressedThisFrame) QuickCycleInput = 1f;
        }
    }
}
