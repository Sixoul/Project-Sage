using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Giant rolling boulder hazard in the Forest Training Center maze.
    /// Rolls continuously along waypoint tracks, knocking Link aside if hit.
    /// </summary>
    public class RollingBoulder : MonoBehaviour
    {
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float speed = 5.5f;
        [SerializeField] private float rollRadius = 1.0f;

        private int currentTargetIndex = 0;
        private Rigidbody rb;

        public static GameObject CreateBoulder(Vector3 startPos, Vector3[] circuitWaypoints)
        {
            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "Forest_Rolling_Boulder";
            boulder.transform.position = startPos;
            boulder.transform.localScale = Vector3.one * 2.0f;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material rockMat = new Material(s) { color = new Color(0.42f, 0.42f, 0.45f) };
            boulder.GetComponent<MeshRenderer>().material = rockMat;

            var comp = boulder.AddComponent<RollingBoulder>();
            comp.waypoints = circuitWaypoints;
            comp.rollRadius = 1.0f;

            return boulder;
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Vector3 target = waypoints[currentTargetIndex];
            Vector3 currentPos = transform.position;
            Vector3 moveDir = (target - currentPos);
            moveDir.y = 0f;

            float dist = moveDir.magnitude;
            if (dist < 0.3f)
            {
                currentTargetIndex = (currentTargetIndex + 1) % waypoints.Length;
            }
            else
            {
                Vector3 step = moveDir.normalized * (speed * Time.deltaTime);
                transform.position += step;

                // Physical rolling rotation around perpendicular axis
                Vector3 rollAxis = Vector3.Cross(Vector3.up, moveDir.normalized);
                float angle = (speed * Time.deltaTime / rollRadius) * Mathf.Rad2Deg;
                transform.Rotate(rollAxis, angle, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.gameObject);
        }

        private void HandleHit(GameObject hitObj)
        {
            var dmg = hitObj.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                Vector3 pushDir = (hitObj.transform.position - transform.position).normalized;
                pushDir.y = 0.3f;
                dmg.TakeDamage(new DamageInfo(2f, transform.position, pushDir, 8f, gameObject, false));

                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlayBlockSpark(transform.position, pushDir);
                }
            }
        }
    }
}
