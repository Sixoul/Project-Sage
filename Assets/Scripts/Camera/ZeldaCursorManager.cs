using UnityEngine;

namespace ZeldaOoT.CameraRig
{
    /// <summary>
    /// Manages mouse cursor lock state and visibility:
    /// - Locked & hidden during normal gameplay for seamless 360 camera orbit.
    /// - Automatically unlocked & visible whenever an inventory or pause menu is active.
    /// - Restores lock on mouse click in game view.
    /// </summary>
    public class ZeldaCursorManager : MonoBehaviour
    {
        public static ZeldaCursorManager Instance { get; private set; }

        private int menuOpenCounter = 0;

        public bool IsMenuOpen => menuOpenCounter > 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            LockCursor();
        }

        public void SetMenuOpen(bool open)
        {
            if (open)
            {
                menuOpenCounter++;
                UnlockCursor();
            }
            else
            {
                menuOpenCounter = Mathf.Max(0, menuOpenCounter - 1);
                if (menuOpenCounter == 0)
                {
                    LockCursor();
                }
            }
        }

        public void LockCursor()
        {
            if (IsMenuOpen) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            // If in gameplay and user clicks into the game window, ensure cursor is locked
            bool mouseClicked = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            if (!IsMenuOpen && mouseClicked)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    LockCursor();
                }
            }
        }
    }
}
