using UnityEngine;
using ZeldaOoT.Player;
using ZeldaOoT.Combat;
using ZeldaOoT.Water;

namespace ZeldaOoT.Environment
{
    /// <summary>
    /// Global Death Floor & Void Fall Hazard:
    /// Catches the player if they fall out of bounds or swim outside the level,
    /// deals 1 heart of void penalty damage, and safely respawns Link back on solid ground.
    /// </summary>
    public class DeathFloor : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField] private Vector3 respawnPoint = new Vector3(0f, 0.5f, -12f);
        [SerializeField] private float voidThresholdY = -14f;
        [SerializeField] private float voidDamageAmount = 4f; // 1 full heart (4 HP)

        private Transform playerTransform;
        private PlayerLocomotion playerLocomotion;
        private HealthSystem playerHealth;
        private WaterSwimController playerSwim;

        private float respawnCooldown = 0f;

        public void SetRespawnPoint(Vector3 point)
        {
            respawnPoint = point;
        }

        private void Start()
        {
            FindPlayerReferences();
        }

        private void FindPlayerReferences()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Player_Link");
            }

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerLocomotion = playerObj.GetComponent<PlayerLocomotion>();
                playerHealth = playerObj.GetComponent<HealthSystem>();
                playerSwim = playerObj.GetComponent<WaterSwimController>();
            }
        }

        private void Update()
        {
            if (respawnCooldown > 0f)
            {
                respawnCooldown -= Time.deltaTime;
                return;
            }

            if (playerTransform == null)
            {
                FindPlayerReferences();
                if (playerTransform == null) return;
            }

            // Continuous failsafe: if Link drops below the void floor threshold, immediately respawn
            if (playerTransform.position.y < voidThresholdY)
            {
                TriggerVoidRespawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (respawnCooldown > 0f) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerLocomotion>() != null)
            {
                TriggerVoidRespawn();
            }
        }

        public void TriggerVoidRespawn()
        {
            if (playerTransform == null) FindPlayerReferences();
            if (playerTransform == null) return;

            respawnCooldown = 1.2f;

            // 1. Exit water state if Link was swimming or diving when falling out of bounds
            if (playerSwim != null && playerSwim.IsSwimming)
            {
                playerSwim.ExitWater(playerSwim.CurrentWater);
            }

            // 2. Deal void penalty damage (1 heart)
            if (playerHealth != null && playerHealth.IsAlive)
            {
                DamageInfo voidDmg = new DamageInfo(voidDamageAmount, playerTransform.position, Vector3.up, 0f, null, false);
                playerHealth.TakeDamage(voidDmg);
            }

            // 3. Teleport back to safe ground with reset velocity
            if (playerLocomotion != null)
            {
                playerLocomotion.Teleport(respawnPoint, Quaternion.identity);
            }
            else
            {
                playerTransform.position = respawnPoint;
            }

            Debug.Log($"<b>[Death Floor]</b> Player fell out of bounds! Safely respawned at {respawnPoint}.");
        }
    }
}
