using System;
using System.Collections;
using UnityEngine;
using ZeldaOoT.Player;

namespace ZeldaOoT.Combat
{
    /// <summary>
    /// Executes Zelda-style evasive maneuvers while locked on:
    /// Side-Hop (left/right leap) and Backflip (backward somersault) with I-frames.
    /// </summary>
    public class ZeldaDodgeController : MonoBehaviour
    {
        [Header("Dodge Parameters")]
        [SerializeField] private float sideHopDistance = 4.5f;
        [SerializeField] private float sideHopDuration = 0.38f;
        [SerializeField] private float sideHopHeight = 1.2f;

        [SerializeField] private float backflipDistance = 5.2f;
        [SerializeField] private float backflipDuration = 0.45f;
        [SerializeField] private float backflipHeight = 1.8f;

        public bool IsDodging { get; private set; }

        public event Action<bool> OnSideHop; // isRight
        public event Action OnBackflip;
        public event Action OnDodgeFinished;

        private CharacterController characterController;
        private HealthSystem healthSystem;
        private PlayerLocomotion playerLocomotion;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            healthSystem = GetComponent<HealthSystem>();
            playerLocomotion = GetComponent<PlayerLocomotion>();
        }

        public bool TryExecuteDodge(Vector2 moveInput, Transform cameraTransform, Transform lockTarget)
        {
            if (IsDodging) return false;

            // In Zelda, if moving sideways while locked on -> Side-Hop
            if (Mathf.Abs(moveInput.x) > 0.3f && moveInput.y > -0.5f)
            {
                bool isRight = moveInput.x > 0f;
                StartCoroutine(PerformSideHop(isRight, lockTarget));
                return true;
            }

            // If moving backward while locked on -> Backflip
            if (moveInput.y < -0.3f)
            {
                StartCoroutine(PerformBackflip(lockTarget));
                return true;
            }

            return false;
        }

        private IEnumerator PerformSideHop(bool isRight, Transform lockTarget)
        {
            IsDodging = true;
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.SideHopping);

            // Grant I-frames during hop
            if (healthSystem != null) healthSystem.SetInvincible(sideHopDuration * 0.9f);

            OnSideHop?.Invoke(isRight);

            Vector3 lateralDir = isRight ? transform.right : -transform.right;
            float elapsed = 0f;

            while (elapsed < sideHopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sideHopDuration);

                // Horizontal movement curve (fast start, smooth land)
                float horizSpeed = (sideHopDistance / sideHopDuration) * Mathf.Sin(t * Mathf.PI);

                // Parabolic vertical arc
                float heightOffset = 4f * sideHopHeight * (t - t * t);
                float prevHeight = 4f * sideHopHeight * ((t - Time.deltaTime / sideHopDuration) - (t - Time.deltaTime / sideHopDuration) * (t - Time.deltaTime / sideHopDuration));
                float vertDelta = (heightOffset - prevHeight) / Mathf.Max(0.001f, Time.deltaTime);

                Vector3 move = lateralDir * horizSpeed + Vector3.up * vertDelta;
                characterController.Move(move * Time.deltaTime);

                // Keep facing target during hop
                if (lockTarget != null)
                {
                    Vector3 lookDir = lockTarget.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }

                yield return null;
            }

            IsDodging = false;
            OnDodgeFinished?.Invoke();
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Grounded);
        }

        private IEnumerator PerformBackflip(Transform lockTarget)
        {
            IsDodging = true;
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Backflipping);

            // Grant I-frames during backflip
            if (healthSystem != null) healthSystem.SetInvincible(backflipDuration * 0.95f);

            OnBackflip?.Invoke();

            Vector3 backwardDir = -transform.forward;
            float elapsed = 0f;

            while (elapsed < backflipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / backflipDuration);

                // Backward translation
                float horizSpeed = (backflipDistance / backflipDuration) * Mathf.Sin(t * Mathf.PI);

                // Higher parabolic arc for somersault
                float heightOffset = 4f * backflipHeight * (t - t * t);
                float prevHeight = 4f * backflipHeight * ((t - Time.deltaTime / backflipDuration) - (t - Time.deltaTime / backflipDuration) * (t - Time.deltaTime / backflipDuration));
                float vertDelta = (heightOffset - prevHeight) / Mathf.Max(0.001f, Time.deltaTime);

                Vector3 move = backwardDir * horizSpeed + Vector3.up * vertDelta;
                characterController.Move(move * Time.deltaTime);

                // Keep facing target during backflip
                if (lockTarget != null)
                {
                    Vector3 lookDir = lockTarget.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }

                yield return null;
            }

            IsDodging = false;
            OnDodgeFinished?.Invoke();
            if (playerLocomotion != null) playerLocomotion.SetState(PlayerState.Grounded);
        }
    }
}
