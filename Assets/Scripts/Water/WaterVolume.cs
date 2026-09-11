using UnityEngine;

namespace ZeldaOoT.Water
{
    /// <summary>
    /// Defines a water volume where the player can swim and dive.
    /// Tracks water surface height and bottom depth.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WaterVolume : MonoBehaviour
    {
        [Header("Water Properties")]
        [SerializeField] private bool useExplicitSurface = false;
        [SerializeField] private float explicitSurfaceY = 0f;
        [SerializeField] private float surfaceOffset = 0f;
        [SerializeField] private float swimDamping = 0.5f;

        private Collider col;

        public float SurfaceY
        {
            get
            {
                if (useExplicitSurface) return explicitSurfaceY;
                if (col == null) col = GetComponent<Collider>();
                return col.bounds.max.y + surfaceOffset;
            }
        }

        public void SetExplicitSurface(float y)
        {
            useExplicitSurface = true;
            explicitSurfaceY = y;
        }

        public float BottomY
        {
            get
            {
                if (col == null) col = GetComponent<Collider>();
                return col.bounds.min.y;
            }
        }

        public Bounds Bounds
        {
            get
            {
                if (col == null) col = GetComponent<Collider>();
                return col.bounds;
            }
        }

        public float SwimDamping => swimDamping;

        private void Awake()
        {
            col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        public bool Contains(Vector3 position)
        {
            if (col == null) col = GetComponent<Collider>();
            return col.bounds.Contains(position);
        }

        private void OnTriggerEnter(Collider other)
        {
            var swimCtrl = other.GetComponent<WaterSwimController>();
            if (swimCtrl != null)
            {
                swimCtrl.EnterWater(this);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var swimCtrl = other.GetComponent<WaterSwimController>();
            if (swimCtrl != null && !swimCtrl.IsSwimming)
            {
                swimCtrl.EnterWater(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var swimCtrl = other.GetComponent<WaterSwimController>();
            if (swimCtrl != null)
            {
                swimCtrl.ExitWater(this);
            }
        }
    }
}
