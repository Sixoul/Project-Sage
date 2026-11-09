using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.VFX;
using ZeldaOoT.Inventory;
using ZeldaOoT.Kokiri;

namespace ZeldaOoT.DekuTree
{
    public enum GohmaState
    {
        Intro,
        StalkingGround,
        ChargingAttack,
        StunnedOnGround,
        ClimbingToCeiling,
        LurkingOnCeiling,
        FallingFromCeiling,
        Dying
    }

    /// <summary>
    /// Queen Gohma - Parasitic Armored Arachnid.
    /// Authentic boss of the Great Deku Tree:
    /// Eye flash telegraphs, Slingshot eye stun, ceiling climb, Gohma Larvae egg drops,
    /// Heart Container reward, and Kokiri Emerald presentation.
    /// </summary>
    public class QueenGohmaBossAI : MonoBehaviour, IDamageable
    {
        [Header("Boss Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float groundSpeed = 4.2f;
        [SerializeField] private float ceilingHeight = 11.5f;

        public bool IsAlive => currentHealth > 0f;
        public Transform Transform => transform;
        public GohmaState State => currentState;

        private float currentHealth;
        private GohmaState currentState = GohmaState.StalkingGround;
        private Transform playerTarget;

        private GameObject eyeMesh;
        private Light eyeLight;
        private Material eyeMat;
        private bool isEyeRed = false;

        private float stateTimer = 0f;
        private Vector3 groundCenter;

        public static GameObject CreateGohma(Vector3 spawnPosition)
        {
            GameObject gohmaRoot = new GameObject("Boss_QueenGohma");
            gohmaRoot.transform.position = spawnPosition;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material shellMat = new Material(s) { color = new Color(0.28f, 0.22f, 0.16f) }; // Dark armored chitin
            Material bellyMat = new Material(s) { color = new Color(0.48f, 0.38f, 0.25f) };
            Material pincerMat = new Material(s) { color = new Color(0.6f, 0.18f, 0.12f) };

            Material eyeMat = new Material(s);
            eyeMat.color = new Color(0.9f, 0.85f, 0.2f);
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", new Color(0.9f, 0.85f, 0.2f) * 2f);

            // Main Carapace Body
            GameObject thorax = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            thorax.name = "Gohma_Thorax";
            thorax.transform.SetParent(gohmaRoot.transform, false);
            thorax.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            thorax.transform.localScale = new Vector3(3.2f, 2.2f, 3.8f);
            thorax.GetComponent<MeshRenderer>().material = shellMat;
            Destroy(thorax.GetComponent<Collider>());

            // Giant Center Eye
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Gohma_Giant_Eye";
            eye.transform.SetParent(thorax.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.1f, 0.48f);
            eye.transform.localScale = new Vector3(0.55f, 0.55f, 0.35f);
            eye.GetComponent<MeshRenderer>().material = eyeMat;
            Destroy(eye.GetComponent<Collider>());

            // Eye Light
            Light light = eye.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.yellow;
            light.range = 7f;
            light.intensity = 3f;

            // Pincers / Claws
            for (int p = -1; p <= 1; p += 2)
            {
                GameObject claw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                claw.name = p < 0 ? "Pincer_L" : "Pincer_R";
                claw.transform.SetParent(thorax.transform, false);
                claw.transform.localPosition = new Vector3(p * 0.65f, -0.3f, 0.52f);
                claw.transform.localRotation = Quaternion.Euler(75f, p * 35f, 0f);
                claw.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
                claw.GetComponent<MeshRenderer>().material = pincerMat;
                Destroy(claw.GetComponent<Collider>());
            }

            // 4 Giant Spider Legs
            for (int l = 0; l < 4; l++)
            {
                int side = (l % 2 == 0) ? -1 : 1;
                float zOff = (l < 2) ? 0.3f : -0.3f;

                GameObject legUpper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                legUpper.name = $"Leg_Upper_{l + 1}";
                legUpper.transform.SetParent(gohmaRoot.transform, false);
                legUpper.transform.localPosition = new Vector3(side * 1.6f, 2.2f, zOff);
                legUpper.transform.localRotation = Quaternion.Euler(0f, 0f, side * -45f);
                legUpper.transform.localScale = new Vector3(0.35f, 1.4f, 0.35f);
                legUpper.GetComponent<MeshRenderer>().material = shellMat;
                Destroy(legUpper.GetComponent<Collider>());

                GameObject legLower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                legLower.name = $"Leg_Lower_{l + 1}";
                legLower.transform.SetParent(legUpper.transform, false);
                legLower.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                legLower.transform.localRotation = Quaternion.Euler(0f, 0f, side * 75f);
                legLower.transform.localScale = new Vector3(0.9f, 1.3f, 0.9f);
                legLower.GetComponent<MeshRenderer>().material = shellMat;
                Destroy(legLower.GetComponent<Collider>());
            }

            // Main Hitbox Collider & LockOnTarget
            var col = gohmaRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.8f, 0f);
            col.radius = 1.8f;
            col.height = 3.2f;

            gohmaRoot.AddComponent<LockOnTarget>();

            var ai = gohmaRoot.AddComponent<QueenGohmaBossAI>();
            ai.eyeMesh = eye;
            ai.eyeLight = light;
            ai.eyeMat = eyeMat;
            ai.groundCenter = spawnPosition;

            return gohmaRoot;
        }

        private void Start()
        {
            currentHealth = maxHealth;
            var player = GameObject.FindWithTag("Player");
            if (player != null) playerTarget = player.transform;

            SetEyeRed(false);
        }

        private void Update()
        {
            if (currentState == GohmaState.Dying) return;

            if (playerTarget == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTarget = player.transform;
                else return;
            }

            stateTimer += Time.deltaTime;

            switch (currentState)
            {
                case GohmaState.StalkingGround:
                    UpdateGroundStalking();
                    break;
                case GohmaState.ChargingAttack:
                    UpdateChargingAttack();
                    break;
                case GohmaState.StunnedOnGround:
                    UpdateStunned();
                    break;
                case GohmaState.ClimbingToCeiling:
                    UpdateClimbing();
                    break;
                case GohmaState.LurkingOnCeiling:
                    UpdateCeilingLurking();
                    break;
                case GohmaState.FallingFromCeiling:
                    UpdateFalling();
                    break;
            }
        }

        private void UpdateGroundStalking()
        {
            Vector3 toPlayer = (playerTarget.position - transform.position);
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            // Turn toward player
            if (toPlayer.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 5f * Time.deltaTime);
            }

            // Walk toward player
            if (dist > 3.8f)
            {
                transform.position += transform.forward * (groundSpeed * Time.deltaTime);
            }

            // After stalking for 4 seconds, initiate red-eye charge attack
            if (stateTimer > 4.2f)
            {
                stateTimer = 0f;
                currentState = GohmaState.ChargingAttack;
                SetEyeRed(true);
            }
        }

        private void UpdateChargingAttack()
        {
            // Charge forward! Eye is vulnerable RED
            transform.position += transform.forward * (groundSpeed * 1.6f * Time.deltaTime);

            if (stateTimer > 2.5f)
            {
                // Completed charge without being shot
                SetEyeRed(false);
                stateTimer = 0f;

                // If below 60% health, climb to ceiling!
                if (currentHealth < maxHealth * 0.6f && Random.value < 0.65f)
                {
                    currentState = GohmaState.ClimbingToCeiling;
                }
                else
                {
                    currentState = GohmaState.StalkingGround;
                }
            }
        }

        private void UpdateStunned()
        {
            // Collapsed on floor, eye turns dark/closed, taking heavy sword damage
            if (stateTimer > 4.5f)
            {
                // Recovers
                SetEyeRed(false);
                stateTimer = 0f;
                currentState = GohmaState.StalkingGround;
            }
        }

        private void UpdateClimbing()
        {
            // Ascend to ceiling
            transform.position += Vector3.up * (6f * Time.deltaTime);
            if (transform.position.y >= groundCenter.y + ceilingHeight)
            {
                transform.position = new Vector3(transform.position.x, groundCenter.y + ceilingHeight, transform.position.z);
                currentState = GohmaState.LurkingOnCeiling;
                stateTimer = 0f;
                SetEyeRed(true); // Vulnerable on ceiling!
                SpawnLarvaeEggs();
            }
        }

        private void UpdateCeilingLurking()
        {
            // Crawl around ceiling looking down at player
            Vector3 toPlayer = (playerTarget.position - transform.position);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 3f * Time.deltaTime);
            }

            if (stateTimer > 7.0f)
            {
                // Drops down to floor
                currentState = GohmaState.FallingFromCeiling;
                SetEyeRed(false);
            }
        }

        private void UpdateFalling()
        {
            // Fall back to ground
            transform.position += Vector3.down * (18f * Time.deltaTime);
            if (transform.position.y <= groundCenter.y)
            {
                transform.position = new Vector3(transform.position.x, groundCenter.y, transform.position.z);
                currentState = GohmaState.StalkingGround;
                stateTimer = 0f;

                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlaySpinWave(transform.position, 6.0f, 1);
                }
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (currentState == GohmaState.Dying) return;

            // If hit by Slingshot or Arrow while eye is RED -> STUN!
            if (isEyeRed)
            {
                StunGohma();
                return;
            }

            // If already stunned on ground, take sword slash damage!
            if (currentState == GohmaState.StunnedOnGround)
            {
                currentHealth -= info.Amount;
                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlayHitSpark(info.HitPoint, Vector3.up);
                }

                if (currentHealth <= 0f)
                {
                    Die();
                }
            }
            else
            {
                // Armored shell deflects attacks!
                if (ZeldaVFXManager.Instance != null)
                {
                    ZeldaVFXManager.Instance.PlayBlockSpark(info.HitPoint, Vector3.up);
                }
            }
        }

        private void StunGohma()
        {
            SetEyeRed(false);
            stateTimer = 0f;

            if (currentState == GohmaState.LurkingOnCeiling || currentState == GohmaState.ClimbingToCeiling)
            {
                // Heavy crash from ceiling!
                transform.position = new Vector3(transform.position.x, groundCenter.y, transform.position.z);
            }

            currentState = GohmaState.StunnedOnGround;

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayHitSpark(eyeMesh.transform.position, Vector3.up);
            }
        }

        private void SetEyeRed(bool red)
        {
            isEyeRed = red;
            Color c = red ? Color.red : new Color(0.9f, 0.85f, 0.2f);
            if (eyeMat != null)
            {
                eyeMat.color = c;
                eyeMat.SetColor("_EmissionColor", c * 3.5f);
            }
            if (eyeLight != null)
            {
                eyeLight.color = c;
                eyeLight.intensity = red ? 4.5f : 2.5f;
            }
        }

        private void SpawnLarvaeEggs()
        {
            // Drop 2 egg sacs that hatch into mini spiders
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 eggP = groundCenter + new Vector3(i * 3.5f, 0.3f, Random.Range(-2f, 2f));
                GameObject egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                egg.name = "Gohma_Larva_Egg";
                egg.transform.position = eggP;
                egg.transform.localScale = Vector3.one * 0.7f;

                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(s) { color = new Color(0.2f, 0.85f, 0.35f) };
                egg.GetComponent<MeshRenderer>().material = mat;
                Destroy(egg, 6f);
            }
        }

        private void Die()
        {
            currentState = GohmaState.Dying;

            if (ZeldaVFXManager.Instance != null)
            {
                ZeldaVFXManager.Instance.PlayBombExplosion(transform.position + Vector3.up * 1.5f);
            }

            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < 2.0f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 2.0f;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }

            // 1. Spawn Heart Container in center of room
            SpawnHeartContainer(groundCenter + Vector3.up * 0.8f);

            // 2. Spawn Blue Warping Teleport Circle
            SpawnBlueWarp(groundCenter);

            // 3. Award Kokiri Emerald & Display Deku Tree Speech
            var inv = FindFirstObjectByType<PlayerInventory>();
            if (inv != null) inv.AwardSpecialItem("kokiri_emerald", "Kokiri's Emerald");

            string[] victoryDialogue = new string[]
            {
                "Queen Gohma has been defeated! The darkness within the Great Deku Tree has dissipated!",
                "You obtained the Spiritual Stone of the Forest: the Kokiri's Emerald!",
                "The Great Deku Tree: 'Well done, Link... You have shown great courage.'",
                "'Step into the blue light to return to the forest and continue your destiny...'"
            };

            if (ZeldaDialogueBox.Instance != null)
            {
                ZeldaDialogueBox.Instance.ShowDialogue("Great Deku Tree", victoryDialogue);
            }

            Destroy(gameObject);
        }

        private void SpawnHeartContainer(Vector3 pos)
        {
            GameObject heart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heart.name = "Boss_Heart_Container";
            heart.transform.position = pos;
            heart.transform.localScale = new Vector3(0.7f, 0.7f, 0.35f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.color = Color.red;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.red * 2.5f);
            heart.GetComponent<MeshRenderer>().material = mat;

            var col = heart.GetComponent<Collider>();
            col.isTrigger = true;

            var pickup = heart.AddComponent<HeartContainerPickup>();
        }

        private void SpawnBlueWarp(Vector3 pos)
        {
            GameObject warp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warp.name = "Blue_Warp_Portal";
            warp.transform.position = pos + Vector3.up * 0.05f;
            warp.transform.localScale = new Vector3(4.5f, 0.1f, 4.5f);

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            Color blue = new Color(0.2f, 0.7f, 1f, 0.8f);
            mat.color = blue;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", blue * 3f);
            warp.GetComponent<MeshRenderer>().material = mat;

            Light light = warp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = blue;
            light.range = 8f;
            light.intensity = 4f;
        }
    }
}

