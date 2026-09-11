using UnityEngine;

namespace ZeldaOoT.Combat
{
    /// <summary>
    /// Component placed on enemies, bosses, or training dummies to make them lock-on targets.
    /// </summary>
    public class LockOnTarget : MonoBehaviour
    {
        [SerializeField] private Transform customTargetPoint;
        [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 1.2f, 0f);

        public Transform TargetPoint => customTargetPoint != null ? customTargetPoint : transform;
        public Vector3 AimPosition => customTargetPoint != null ? customTargetPoint.position : transform.position + defaultOffset;
    }
}
