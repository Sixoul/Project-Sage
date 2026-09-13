using System;
using UnityEngine;

namespace ZeldaOoT.Combat
{
    /// <summary>
    /// Implements authentic Zelda Z-Targeting / Lock-On mechanics.
    /// Finds enemies in view cone, manages lock state, orientates player,
    /// and provides lock-on targeting coordinates for camera and UI reticle.
    /// </summary>
    public class ZTargetSystem : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [SerializeField] private float lockOnRadius = 22f;
        [SerializeField] private float lockOnMaxAngle = 75f; // half angle from camera forward
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private LayerMask obstacleMask = 1; // Default layer

        public Transform CurrentTarget { get; private set; }
        public LockOnTarget CurrentLockOnTarget { get; private set; }
        public IDamageable CurrentDamageable { get; private set; }
        public bool IsLockedOn => CurrentTarget != null && (CurrentDamageable == null || CurrentDamageable.IsAlive);

        public Vector3 TargetAimPosition
        {
            get
            {
                if (CurrentLockOnTarget != null) return CurrentLockOnTarget.AimPosition;
                if (CurrentTarget != null)
                {
                    var lockable = CurrentTarget.GetComponentInParent<LockOnTarget>();
                    if (lockable != null) return lockable.AimPosition;
                    return CurrentTarget.position + Vector3.up * 1.25f;
                }
                return Vector3.zero;
            }
        }

        public event Action<Transform> OnTargetLocked;
        public event Action OnTargetLost;

        private Transform camTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (camTransform == null && Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }

            // Check if current target is still valid
            if (IsLockedOn)
            {
                float dist = Vector3.Distance(transform.position, CurrentTarget.position);
                if (dist > lockOnRadius * 1.35f || (CurrentDamageable != null && !CurrentDamageable.IsAlive))
                {
                    ClearTarget();
                }
            }
        }

        public void ToggleLockOn()
        {
            if (IsLockedOn)
            {
                ClearTarget();
            }
            else
            {
                FindBestTarget();
            }
        }

        public void FindBestTarget()
        {
            if (camTransform == null) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRadius, targetMask);
            Transform bestTarget = null;
            LockOnTarget bestLockable = null;
            float bestScore = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsAlive) continue;

                // Check if target has a LockOnTarget or is a valid combat entity
                var lockable = hit.GetComponentInParent<LockOnTarget>();
                if (damageable == null && lockable == null) continue;

                Transform targetT = lockable != null ? lockable.TargetPoint : hit.transform;
                Vector3 toTarget = targetT.position - camTransform.position;
                float angle = Vector3.Angle(camTransform.forward, toTarget);

                if (angle < lockOnMaxAngle)
                {
                    // Check line of sight (ignore the target itself)
                    Vector3 eyePos = transform.position + Vector3.up * 1.2f;
                    Vector3 rayDir = targetT.position - eyePos;
                    if (Physics.Raycast(eyePos, rayDir.normalized, out RaycastHit losHit, rayDir.magnitude - 0.2f, obstacleMask))
                    {
                        if (losHit.transform != targetT && !losHit.transform.IsChildOf(targetT) && !targetT.IsChildOf(losHit.transform))
                        {
                            continue; // Obstructed by wall or pillar
                        }
                    }

                    // Score combines angle and distance
                    float score = angle * 2f + toTarget.magnitude;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = targetT;
                        bestLockable = lockable;
                        CurrentDamageable = damageable;
                    }
                }
            }

            if (bestTarget != null)
            {
                CurrentTarget = bestTarget;
                CurrentLockOnTarget = bestLockable;
                OnTargetLocked?.Invoke(CurrentTarget);
            }
        }

        public void ClearTarget()
        {
            CurrentTarget = null;
            CurrentLockOnTarget = null;
            CurrentDamageable = null;
            OnTargetLost?.Invoke();
        }
    }
}
