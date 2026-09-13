using UnityEngine;
using ZeldaOoT.Input;
using ZeldaOoT.Combat;

namespace ZeldaOoT.CameraRig
{
    /// <summary>
    /// Zelda Third-Person Camera Rig:
    /// Orbit free-look exploration camera with collision smoothing and
    /// dedicated Z-Targeting framing mode that keeps Link and enemy in view.
    /// Compatible with Cinemachine or standalone cameras.
    /// </summary>
    public class ZeldaCameraRig : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private float defaultDistance = 4.8f;
        [SerializeField] private float minDistance = 1.2f;
        [SerializeField] private float maxDistance = 7.0f;

        [Header("Orbit & Sensitivity")]
        [SerializeField] private float sensitivityX = 2.2f;
        [SerializeField] private float sensitivityY = 1.8f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField] private float smoothFollowSpeed = 15f;

        [Header("Z-Targeting Framing")]
        [SerializeField] private float zTargetDistance = 5.2f;
        [SerializeField] private float zTargetHeightOffset = 1.8f;
        [SerializeField] private float zTargetSideOffset = 0.8f;

        [Header("Collision Avoidance")]
        [SerializeField] private LayerMask collisionLayers = 1; // Default
        [SerializeField] private float collisionRadius = 0.25f;

        private float currentYaw = 0f;
        private float currentPitch = 15f;
        private float currentDist;
        private float currentCollisionDist;
        private Vector3 smoothedFocusPoint;

        private ZeldaInputReader inputReader;
        private ZTargetSystem zTargetSystem;

        private void Start()
        {
            currentDist = defaultDistance;
            currentCollisionDist = Mathf.Max(minDistance, defaultDistance);
            EnsureTarget();

            currentYaw = transform.eulerAngles.y;
            inputReader = ZeldaInputReader.Instance;
        }

        private void EnsureTarget()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player == null) player = GameObject.Find("Player_Link");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                smoothedFocusPoint = target.position + targetOffset;
                zTargetSystem = target.GetComponent<ZTargetSystem>();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                smoothedFocusPoint = target.position + targetOffset;
                zTargetSystem = target.GetComponent<ZTargetSystem>();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                EnsureTarget();
                if (target == null) return;
            }
            if (inputReader == null) inputReader = ZeldaInputReader.Instance;

            Vector3 targetFocus = target.position + targetOffset;
            smoothedFocusPoint = Vector3.Lerp(smoothedFocusPoint, targetFocus, smoothFollowSpeed * Time.deltaTime);

            bool isLockedOn = zTargetSystem != null && zTargetSystem.IsLockedOn && zTargetSystem.CurrentTarget != null;

            if (isLockedOn)
            {
                UpdateZTargetFraming(zTargetSystem.CurrentTarget);
            }
            else
            {
                UpdateFreeOrbit();
            }

            ApplyCameraPositionAndRotation();
        }

        private void UpdateFreeOrbit()
        {
            Vector2 look = inputReader != null ? inputReader.LookInput : Vector2.zero;

            currentYaw += look.x * sensitivityX;
            currentPitch -= look.y * sensitivityY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            currentDist = Mathf.Lerp(currentDist, defaultDistance, 8f * Time.deltaTime);
        }

        private void UpdateZTargetFraming(Transform enemyTarget)
        {
            // Position camera behind player pointing towards enemy
            Vector3 toEnemy = enemyTarget.position - smoothedFocusPoint;
            toEnemy.y = 0f;

            if (toEnemy.sqrMagnitude > 0.01f)
            {
                float targetYaw = Quaternion.LookRotation(toEnemy).eulerAngles.y;
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, 10f * Time.deltaTime);
            }

            // Angle slightly downward so both entities are well framed
            currentPitch = Mathf.Lerp(currentPitch, 18f, 8f * Time.deltaTime);
            currentDist = Mathf.Lerp(currentDist, zTargetDistance, 8f * Time.deltaTime);
        }

        private void ApplyCameraPositionAndRotation()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // Calculate backward direction from focus point
            Vector3 backDir = rotation * Vector3.back;
            float desiredDist = Mathf.Clamp(currentDist, minDistance, maxDistance);

            // Start spherecast outside the player's clearance sphere (0.65m) to NEVER collide with player
            float playerClearance = 0.65f;
            Vector3 castOrigin = smoothedFocusPoint + backDir * playerClearance;
            float castDistance = Mathf.Max(0f, desiredDist - playerClearance);

            float targetDist = desiredDist;

            // Only spherecast if castDistance is valid
            if (castDistance > 0.01f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(castOrigin, collisionRadius, backDir, castDistance, collisionLayers, QueryTriggerInteraction.Ignore);
                float closestHitDist = float.MaxValue;

                foreach (var hit in hits)
                {
                    // Ignore the player, player children (weapons, shields), or triggers
                    if (target != null && (hit.transform == target || hit.transform.IsChildOf(target))) continue;
                    if (hit.collider.isTrigger) continue;

                    if (hit.distance < closestHitDist)
                    {
                        closestHitDist = hit.distance;
                    }
                }

                if (closestHitDist < float.MaxValue)
                {
                    targetDist = Mathf.Clamp(playerClearance + closestHitDist - 0.15f, minDistance, maxDistance);
                }
            }

            // Asymmetric damping: snap in quickly when obstacle appears, ease back out smoothly to prevent jitter
            if (targetDist < currentCollisionDist)
            {
                currentCollisionDist = Mathf.MoveTowards(currentCollisionDist, targetDist, 25f * Time.deltaTime);
            }
            else
            {
                currentCollisionDist = Mathf.Lerp(currentCollisionDist, targetDist, 6f * Time.deltaTime);
            }

            // Always enforce minimum distance to avoid zero-length vector or camera clipping inside player
            currentCollisionDist = Mathf.Clamp(currentCollisionDist, minDistance, maxDistance);

            Vector3 finalPos = smoothedFocusPoint + backDir * currentCollisionDist;
            transform.position = finalPos;

            // Guard against NaN look-rotation
            Vector3 lookDir = smoothedFocusPoint - finalPos;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
            else
            {
                transform.rotation = rotation;
            }
        }
    }
}
