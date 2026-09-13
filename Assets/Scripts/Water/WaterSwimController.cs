using System;
using UnityEngine;
using ZeldaOoT.Player;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Water
{
    /// <summary>
    /// Authentic Zelda swimming & deep underwater diving physics:
    /// - Natural surface swimming with proper immersion (chest/shoulder level).
    /// - Plunge dive (Shift / C / Gamepad B) that submerges Link deep down to the lake bed (-5m).
    /// - 3D underwater free swimming following camera pitch.
    /// - Surface ascend stroke (Spacebar / Gamepad A).
    /// - Buoyancy, oxygen drain & suffocation, and surface breach splash rings.
    /// </summary>
    public class WaterSwimController : MonoBehaviour
    {
        [Header("Swim Settings")]
        [SerializeField] private float surfaceSwimSpeed = 5.5f;
        [SerializeField] private float underwaterSwimSpeed = 4.8f;
        [SerializeField] private float diveSpeed = 5.5f;
        [SerializeField] private float ascendSpeed = 5.5f;
        [SerializeField] private float surfaceFloatOffset = 1.15f; // Link's feet relative to water surface
        [SerializeField] private float buoyancyStrength = 6.0f;
        [SerializeField] private float maxOxygen = 12f; // seconds underwater

        public float CurrentOxygen { get; private set; }
        public float MaxOxygen => maxOxygen;
        public bool IsSwimming { get; private set; }
        public bool IsDiving { get; private set; }
        public WaterVolume CurrentWater { get; private set; }

        public event Action<float, float> OnOxygenChanged; // current, max

        private CharacterController characterController;
        private PlayerLocomotion playerLocomotion;
        private HealthSystem healthSystem;

        private float verticalVelocity = 0f;
        private bool wasDivingUnderwater = false;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerLocomotion = GetComponent<PlayerLocomotion>();
            healthSystem = GetComponent<HealthSystem>();
            CurrentOxygen = maxOxygen;
        }

        public void EnterWater(WaterVolume water)
        {
            CurrentWater = water;
            IsSwimming = true;
            IsDiving = false;
            wasDivingUnderwater = false;

            // Dampen violent downward falling momentum upon entering water
            if (verticalVelocity < -2f)
            {
                verticalVelocity = -2f;
            }

            if (playerLocomotion != null)
            {
                playerLocomotion.SetState(PlayerState.Swimming);
            }

            CreateWaterSplash(new Vector3(transform.position.x, water.SurfaceY, transform.position.z));
            OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
        }

        public void ExitWater(WaterVolume water)
        {
            if (CurrentWater == water)
            {
                CurrentWater = null;
                IsSwimming = false;
                IsDiving = false;
                wasDivingUnderwater = false;
                verticalVelocity = 0f;
                CurrentOxygen = maxOxygen;
                OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);

                if (playerLocomotion != null && (playerLocomotion.CurrentState == PlayerState.Swimming || playerLocomotion.CurrentState == PlayerState.Diving))
                {
                    playerLocomotion.SetState(PlayerState.Grounded);
                }
            }
        }

        public void ProcessWaterMovement(Vector2 moveInput, bool diveHeld, bool jumpHeld, Transform cameraTransform)
        {
            if (CurrentWater == null) return;

            float surfaceRestY = CurrentWater.SurfaceY - surfaceFloatOffset;
            float currentY = transform.position.y;
            bool isDeepUnderwater = currentY < surfaceRestY - 0.35f;

            // 1. VERTICAL MOVEMENT & DIVING PHYSICS
            if (diveHeld)
            {
                // Active plunge downwards
                IsDiving = true;
                if (!wasDivingUnderwater && currentY >= surfaceRestY - 0.2f)
                {
                    // Plunge burst impulse when initiating dive from surface
                    verticalVelocity = -4.5f;
                    CreateWaterSplash(new Vector3(transform.position.x, CurrentWater.SurfaceY, transform.position.z));
                }
                else
                {
                    verticalVelocity = Mathf.MoveTowards(verticalVelocity, -diveSpeed, 25f * Time.deltaTime);
                }

                if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Diving);
            }
            else if (jumpHeld)
            {
                // Active swim upwards towards surface
                verticalVelocity = Mathf.MoveTowards(verticalVelocity, ascendSpeed, 25f * Time.deltaTime);
                if (isDeepUnderwater)
                {
                    IsDiving = true;
                    if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Diving);
                }
                else
                {
                    IsDiving = false;
                    if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Swimming);
                }
            }
            else
            {
                // Neither dive nor jump is held
                if (isDeepUnderwater)
                {
                    IsDiving = true;
                    if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Diving);

                    // Buoyancy pull towards surface when submerged
                    float dist = surfaceRestY - currentY;
                    float targetUpward = Mathf.Clamp(dist * buoyancyStrength, 2.0f, 6.0f);
                    verticalVelocity = Mathf.MoveTowards(verticalVelocity, targetUpward, 20f * Time.deltaTime);
                }
                else
                {
                    // At or near surface
                    IsDiving = false;
                    if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Swimming);

                    // Surface buoyancy & steady rest floating clamp
                    if (currentY < surfaceRestY - 0.05f)
                    {
                        float dist = surfaceRestY - currentY;
                        float targetUpward = Mathf.Clamp(dist * buoyancyStrength, 2.0f, 6.0f);
                        verticalVelocity = Mathf.MoveTowards(verticalVelocity, targetUpward, 30f * Time.deltaTime);
                    }
                    else if (currentY > surfaceRestY + 0.08f)
                    {
                        // Sinking smoothly down to surface float level
                        verticalVelocity = Mathf.MoveTowards(verticalVelocity, -2.0f, 18f * Time.deltaTime);
                    }
                    else
                    {
                        verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0f, 25f * Time.deltaTime);
                    }
                }
            }

            // Surface breach detection (popping out of water from deep dive)
            if (wasDivingUnderwater && !isDeepUnderwater && verticalVelocity > 1.2f)
            {
                CreateWaterSplash(new Vector3(transform.position.x, CurrentWater.SurfaceY, transform.position.z));
            }
            wasDivingUnderwater = isDeepUnderwater;

            // 2. OXYGEN & SUFFOCATION
            if (isDeepUnderwater || IsDiving)
            {
                CurrentOxygen = Mathf.Max(0f, CurrentOxygen - Time.deltaTime);
                OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);

                // Suffocation damage if out of oxygen
                if (CurrentOxygen <= 0f && healthSystem != null)
                {
                    DamageInfo suffocationDmg = new DamageInfo(1f, transform.position, Vector3.up, 0f, null, false);
                    healthSystem.TakeDamage(suffocationDmg);
                }

                // Bubble trail while submerged
                if (Time.frameCount % 16 == 0)
                {
                    CreateBubble(transform.position + Vector3.up * 1.1f + UnityEngine.Random.insideUnitSphere * 0.25f);
                }
            }
            else
            {
                // Rapidly refill oxygen at surface
                if (CurrentOxygen < maxOxygen)
                {
                    CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + Time.deltaTime * 6f);
                }
                OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
            }

            // 3. HORIZONTAL & 3D STEERING
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;

            Vector3 planarForward = camForward;
            planarForward.y = 0f;
            planarForward.Normalize();

            Vector3 planarRight = camRight;
            planarRight.y = 0f;
            planarRight.Normalize();

            Vector3 horizontalMove = (planarForward * moveInput.y + planarRight * moveInput.x) * (isDeepUnderwater ? underwaterSwimSpeed : surfaceSwimSpeed);

            // In underwater 3D mode, if moving forward with WASD, tilt slightly into camera pitch
            if (isDeepUnderwater && moveInput.y > 0.1f && cameraTransform != null)
            {
                float pitchY = camForward.y * underwaterSwimSpeed * 0.7f;
                // Add vertical component from camera pitch if not pressing dive/jump overrides
                if (!diveHeld && !jumpHeld)
                {
                    verticalVelocity = Mathf.MoveTowards(verticalVelocity, pitchY, 14f * Time.deltaTime);
                }
            }

            // Rotation
            if (horizontalMove.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalMove.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }

            // 4. PREVENT DIVING BELOW LAKE FLOOR
            float lakeFloor = CurrentWater.BottomY + 0.1f;
            if (transform.position.y <= lakeFloor && verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }

            // 5. APPLY FINAL MOTION
            Vector3 finalMotion = horizontalMove;
            finalMotion.y = verticalVelocity;
            characterController.Move(finalMotion * Time.deltaTime);

            // 6. SHORE EXIT DETECTION
            // Only exit water when:
            // a) Link has walked up onto dry ground (grounded and his feet are near or above water surface)
            // b) Link jumped completely up and out of the water volume with positive upward velocity
            bool walkedOntoDryShore = characterController.isGrounded && transform.position.y >= CurrentWater.SurfaceY - 0.4f;
            bool jumpedOutOfWater = verticalVelocity > 0.5f && transform.position.y > CurrentWater.SurfaceY + 0.4f && !CurrentWater.Contains(transform.position);

            if (walkedOntoDryShore || jumpedOutOfWater)
            {
                ExitWater(CurrentWater);
            }
        }

        private void CreateWaterSplash(Vector3 pos)
        {
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayWaterSplash(pos);
            }
        }

        private void CreateBubble(Vector3 pos)
        {
            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayWaterBubble(pos);
            }
        }
    }
}
