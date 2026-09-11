using System;
using UnityEngine;
using ZeldaOoT.Input;
using ZeldaOoT.Combat;
using ZeldaOoT.Water;
using ZeldaOoT.Inventory;
using ZeldaOoT.Items;

namespace ZeldaOoT.Player
{
    /// <summary>
    /// Core Player Controller for Modern Zelda:
    /// 360 free movement, free jumping, Z-targeting strafing, dodge integration,
    /// and swimming/diving coordination.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 6.5f;
        [SerializeField] private float strafeSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float acceleration = 12f;

        [Header("Jump & Gravity Settings")]
        [SerializeField] private float jumpHeight = 2.4f;
        [SerializeField] private float gravity = 22f;
        [SerializeField] private float fallMultiplier = 1.4f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.15f;

        public PlayerState CurrentState { get; private set; } = PlayerState.Grounded;
        public Vector3 Velocity => characterController.velocity;
        public bool IsGrounded => characterController.isGrounded;

        public event Action<PlayerState> OnStateChanged;

        private CharacterController characterController;
        private ZeldaInputReader inputReader;
        private ZTargetSystem zTargetSystem;
        private ZeldaDodgeController dodgeController;
        private PlayerCombat combatController;
        private WaterSwimController swimController;
        private ItemSystem itemSystem;
        private Transform cameraTransform;

        private Vector3 currentVelocity;
        private float verticalVelocity;
        private float coyoteTimer;
        private float jumpBufferTimer;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            zTargetSystem = GetComponent<ZTargetSystem>();
            dodgeController = GetComponent<ZeldaDodgeController>();
            combatController = GetComponent<PlayerCombat>();
            swimController = GetComponent<WaterSwimController>();
            itemSystem = GetComponent<ItemSystem>();
        }

        private void Start()
        {
            inputReader = ZeldaInputReader.Instance;
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        public void SetState(PlayerState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(CurrentState);
        }

        private void Update()
        {
            if (inputReader == null) inputReader = ZeldaInputReader.Instance;
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            HandleInputEvents();

            // If swimming or diving, let the water controller handle physics
            if (CurrentState == PlayerState.Swimming || CurrentState == PlayerState.Diving)
            {
                verticalVelocity = 0f;
                if (swimController != null)
                {
                    Vector2 move = inputReader != null ? inputReader.MoveInput : Vector2.zero;
                    bool dive = inputReader != null && inputReader.DiveHeld;
                    bool jump = inputReader != null && (inputReader.JumpHeld || inputReader.JumpPressed);
                    swimController.ProcessWaterMovement(move, dive, jump, cameraTransform);
                }
                return;
            }

            // If dodging (Side-hop or Backflip), dodge controller moves the character
            if (CurrentState == PlayerState.SideHopping || CurrentState == PlayerState.Backflipping)
            {
                return;
            }

            HandleGroundedAndJumpTimers();
            ProcessMovement();
        }

        private void HandleInputEvents()
        {
            if (inputReader == null) return;

            // Z-Target Toggle
            if (inputReader.LockOnPressed && zTargetSystem != null)
            {
                zTargetSystem.ToggleLockOn();
            }

            // Shield Block
            if (combatController != null)
            {
                if (CurrentState == PlayerState.Swimming || CurrentState == PlayerState.Diving)
                {
                    if (combatController.IsBlocking) combatController.SetBlock(false);
                }
                else
                {
                    combatController.SetBlock(inputReader.BlockHeld);
                }
            }

            // Sword Attack & Charge Spin Attack
            if (combatController != null && CurrentState != PlayerState.Swimming && CurrentState != PlayerState.Diving)
            {
                bool airborne = !characterController.isGrounded;
                combatController.ProcessAttackInput(inputReader.AttackPressed, inputReader.AttackHeld, inputReader.AttackReleased, airborne);
            }

            // Secondary Item Action (Bow / Bomb)
            if (inputReader.ItemActionPressed && itemSystem != null && !combatController.IsAttacking)
            {
                itemSystem.UseCurrentItem(cameraTransform);
            }

            // Jump / Dodge Buffer
            if (inputReader.JumpPressed)
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void HandleGroundedAndJumpTimers()
        {
            if (characterController.isGrounded)
            {
                coyoteTimer = coyoteTime;
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f; // Slight downward snap to maintain ground contact
                }

                if (CurrentState == PlayerState.Jumping || CurrentState == PlayerState.Falling)
                {
                    SetState(PlayerState.Grounded);
                }
            }
            else
            {
                coyoteTimer -= Time.deltaTime;

                if (verticalVelocity < 0f && CurrentState != PlayerState.Falling && CurrentState != PlayerState.Attacking)
                {
                    SetState(PlayerState.Falling);
                }
            }

            // Execute Jump or Zelda Dodge
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;

                // Check if locked on: evaluate for Side-hop or Backflip
                if (zTargetSystem != null && zTargetSystem.IsLockedOn && dodgeController != null)
                {
                    Vector2 move = inputReader != null ? inputReader.MoveInput : Vector2.zero;
                    if (dodgeController.TryExecuteDodge(move, cameraTransform, zTargetSystem.CurrentTarget))
                    {
                        return;
                    }
                }

                // Free Jump
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * gravity);
                SetState(PlayerState.Jumping);
            }

            // Gravity application
            float currentGrav = (verticalVelocity < 0f) ? gravity * fallMultiplier : gravity;
            verticalVelocity -= currentGrav * Time.deltaTime;
        }

        private void ProcessMovement()
        {
            Vector2 moveInput = inputReader != null ? inputReader.MoveInput : Vector2.zero;

            // If attacking, allow limited steering/lunging
            if (combatController != null && combatController.IsAttacking)
            {
                Vector3 attackMotion = Vector3.up * verticalVelocity;
                characterController.Move(attackMotion * Time.deltaTime);
                return;
            }

            // Direction calculation relative to camera
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 targetDir = (forward * moveInput.y + right * moveInput.x).normalized;

            bool isLockedOn = zTargetSystem != null && zTargetSystem.IsLockedOn && zTargetSystem.CurrentTarget != null;
            float currentMoveSpeed = isLockedOn ? strafeSpeed : walkSpeed;
            if (combatController != null && combatController.IsCharging)
            {
                currentMoveSpeed *= 0.5f;
            }

            Vector3 targetHorizVelocity = targetDir * (moveInput.magnitude * currentMoveSpeed);
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetHorizVelocity, acceleration * currentMoveSpeed * Time.deltaTime);

            // Rotation handling
            if (isLockedOn)
            {
                // In Zelda Z-Targeting, Link stays face-to-face with target (strafe circle)
                Vector3 toTarget = zTargetSystem.CurrentTarget.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
            else if (targetDir.sqrMagnitude > 0.01f)
            {
                // Free 360 directional turning
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Total movement vector
            Vector3 finalMove = currentVelocity;
            finalMove.y = verticalVelocity;

            characterController.Move(finalMove * Time.deltaTime);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (characterController != null) characterController.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            currentVelocity = Vector3.zero;
            verticalVelocity = 0f;
            if (characterController != null) characterController.enabled = true;
            SetState(PlayerState.Grounded);
        }
    }
}
