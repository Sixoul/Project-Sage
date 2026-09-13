using System.Collections;
using UnityEngine;

namespace ZeldaOoT.VFX
{
    /// <summary>
    /// Centralized Visual Effects Manager using Unity ParticleSystem.
    /// Supports both custom ParticleSystem prefabs (assigned in Inspector)
    /// and high-performance procedural ParticleSystem generation at runtime.
    /// Replaces crude primitive mesh instantiations (Spheres/Cylinders) with authentic game VFX.
    /// </summary>
    public class ZeldaVFXManager : MonoBehaviour
    {
        private static ZeldaVFXManager instance;
        public static ZeldaVFXManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ZeldaVFXManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ZeldaVFXManager");
                        instance = go.AddComponent<ZeldaVFXManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Custom Particle Prefabs (Optional)")]
        [Tooltip("Assign your custom ParticleSystem prefab for sword hit sparks.")]
        [SerializeField] private ParticleSystem hitSparkPrefab;

        [Tooltip("Assign your custom ParticleSystem prefab for shield block deflection sparks.")]
        [SerializeField] private ParticleSystem blockSparkPrefab;

        [Tooltip("Assign your custom ParticleSystem prefab for Spin Attack energy waves.")]
        [SerializeField] private ParticleSystem spinWavePrefab;

        [Tooltip("Assign your custom ParticleSystem prefab for bomb explosions.")]
        [SerializeField] private ParticleSystem bombExplosionPrefab;

        [Tooltip("Assign your custom ParticleSystem prefab for water splashes.")]
        [SerializeField] private ParticleSystem waterSplashPrefab;

        [Tooltip("Assign your custom ParticleSystem prefab for underwater swimming bubbles.")]
        [SerializeField] private ParticleSystem waterBubblePrefab;

        private Material particleAdditiveMat;
        private Material particleAlphaMat;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeParticleMaterials();
        }

        private void InitializeParticleMaterials()
        {
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                          ?? Shader.Find("Particles/Standard Unlit")
                          ?? Shader.Find("Sprites/Default");

            if (pShader != null)
            {
                particleAdditiveMat = new Material(pShader);
                particleAdditiveMat.SetFloat("_Surface", 1f); // Transparent
                particleAdditiveMat.SetFloat("_Blend", 1f);   // Additive
                particleAdditiveMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                particleAdditiveMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                particleAdditiveMat.SetInt("_ZWrite", 0);

                particleAlphaMat = new Material(pShader);
                particleAlphaMat.SetFloat("_Surface", 1f); // Transparent
                particleAlphaMat.SetFloat("_Blend", 0f);   // Alpha Blend
                particleAlphaMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                particleAlphaMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                particleAlphaMat.SetInt("_ZWrite", 0);
            }
        }

        // =========================================================================
        // 1. SWORD HIT SPARKS
        // =========================================================================
        public void PlayHitSpark(Vector3 position, Vector3 normal)
        {
            if (hitSparkPrefab != null)
            {
                var ps = Instantiate(hitSparkPrefab, position, Quaternion.LookRotation(normal));
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            // Procedural Hit Spark Particle System
            GameObject sparkObj = new GameObject("VFX_HitSpark");
            sparkObj.transform.position = position;
            sparkObj.transform.rotation = Quaternion.LookRotation(normal != Vector3.zero ? normal : Vector3.up);

            ParticleSystem psComp = sparkObj.AddComponent<ParticleSystem>();
            var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.4f), new Color(1f, 0.5f, 0.1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 2.5f;

            var emission = psComp.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16, 26) });

            var shape = psComp.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.05f;

            var colorOverLifetime = psComp.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            psComp.Play();
            Destroy(sparkObj, 0.5f);
        }

        // =========================================================================
        // 2. SHIELD DEFLECTION SPARKS
        // =========================================================================
        public void PlayBlockSpark(Vector3 position, Vector3 normal)
        {
            if (blockSparkPrefab != null)
            {
                var ps = Instantiate(blockSparkPrefab, position, Quaternion.LookRotation(normal));
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            // Procedural Shield Block Deflection Spark Particle System
            GameObject blockObj = new GameObject("VFX_BlockDeflectSpark");
            blockObj.transform.position = position;
            blockObj.transform.rotation = Quaternion.LookRotation(normal != Vector3.zero ? normal : Vector3.up);

            ParticleSystem psComp = blockObj.AddComponent<ParticleSystem>();
            var renderer = blockObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 13f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.9f, 1f), new Color(0.1f, 0.4f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;

            var emission = psComp.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 22, 34) });

            var shape = psComp.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f;

            var colorOverLifetime = psComp.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.8f, 0.98f, 1f), 0f), new GradientColorKey(new Color(0f, 0.5f, 1f), 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            psComp.Play();
            Destroy(blockObj, 0.6f);
        }

        // =========================================================================
        // 3. SPIN ATTACK ENERGY WAVE
        // =========================================================================
        public void PlaySpinWave(Vector3 position, float radius, int level)
        {
            if (spinWavePrefab != null)
            {
                var ps = Instantiate(spinWavePrefab, position, Quaternion.identity);
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            // Procedural Expanding Radial Spin Wave Particles
            GameObject waveObj = new GameObject("VFX_SpinWave");
            waveObj.transform.position = position + Vector3.up * 0.35f;

            ParticleSystem psComp = waveObj.AddComponent<ParticleSystem>();
            var renderer = waveObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            Color waveColor = level == 2 ? new Color(0.2f, 0.85f, 1f) : new Color(0.3f, 1f, 0.45f);

            var main = psComp.main;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 3.5f, radius * 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = waveColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = psComp.emission;
            emission.rateOverTime = 0;
            short particleCount = (short)(level == 2 ? 64 : 42);
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, particleCount) });

            var shape = psComp.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(90f, 0f, 0f); // flat horizontal circle

            var sizeOverLifetime = psComp.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.4f);
            sizeCurve.AddKey(0.4f, 1f);
            sizeCurve.AddKey(1f, 0.1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = psComp.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(waveColor, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            psComp.Play();
            Destroy(waveObj, 0.5f);
        }

        // =========================================================================
        // 4. BOMB EXPLOSION
        // =========================================================================
        public void PlayBombExplosion(Vector3 position)
        {
            if (bombExplosionPrefab != null)
            {
                var ps = Instantiate(bombExplosionPrefab, position, Quaternion.identity);
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            // Procedural Fiery Explosion Particles
            GameObject boomObj = new GameObject("VFX_BombExplosion");
            boomObj.transform.position = position;

            // Child 1: Fire & Flash Core
            ParticleSystem psFlash = boomObj.AddComponent<ParticleSystem>();
            var rendFlash = boomObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) rendFlash.material = particleAdditiveMat;

            var mainF = psFlash.main;
            mainF.loop = false;
            mainF.duration = 0.4f;
            mainF.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            mainF.startSpeed = new ParticleSystem.MinMaxCurve(5f, 14f);
            mainF.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            mainF.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.35f, 0.05f));
            mainF.simulationSpace = ParticleSystemSimulationSpace.World;
            mainF.gravityModifier = -0.5f;

            var emF = psFlash.emission;
            emF.rateOverTime = 0;
            emF.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30, 45) });

            var shapeF = psFlash.shape;
            shapeF.shapeType = ParticleSystemShapeType.Sphere;
            shapeF.radius = 0.5f;

            var colF = psFlash.colorOverLifetime;
            colF.enabled = true;
            Gradient gradF = new Gradient();
            gradF.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.8f), 0f), new GradientColorKey(new Color(0.9f, 0.2f, 0f), 0.6f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colF.color = gradF;

            // Child 2: Smoke & Shockwave Embers
            GameObject debrisObj = new GameObject("ExplosionDebris");
            debrisObj.transform.SetParent(boomObj.transform, false);
            ParticleSystem psDebris = debrisObj.AddComponent<ParticleSystem>();
            var rendDebris = debrisObj.GetComponent<ParticleSystemRenderer>();
            if (particleAlphaMat != null) rendDebris.material = particleAlphaMat;

            var mainD = psDebris.main;
            mainD.loop = false;
            mainD.duration = 0.6f;
            mainD.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            mainD.startSpeed = new ParticleSystem.MinMaxCurve(7f, 16f);
            mainD.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            mainD.startColor = new Color(0.2f, 0.18f, 0.16f, 0.8f);
            mainD.simulationSpace = ParticleSystemSimulationSpace.World;
            mainD.gravityModifier = 1.8f;

            var emD = psDebris.emission;
            emD.rateOverTime = 0;
            emD.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24, 36) });

            var shapeD = psDebris.shape;
            shapeD.shapeType = ParticleSystemShapeType.Hemisphere;
            shapeD.radius = 0.3f;

            psFlash.Play();
            psDebris.Play();
            Destroy(boomObj, 1.2f);
        }

        // =========================================================================
        // 5. WATER SPLASH & BREACH
        // =========================================================================
        public void PlayWaterSplash(Vector3 position, float scale = 1f)
        {
            if (waterSplashPrefab != null)
            {
                var ps = Instantiate(waterSplashPrefab, position, Quaternion.identity);
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            // Procedural Water Splash Droplets & Upward Fountain
            GameObject splashObj = new GameObject("VFX_WaterSplash");
            splashObj.transform.position = position;

            ParticleSystem psComp = splashObj.AddComponent<ParticleSystem>();
            var renderer = splashObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.loop = false;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f * scale, 9f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.22f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.96f, 1f, 0.9f), new Color(0.4f, 0.8f, 1f, 0.7f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 3.5f; // Up and quickly arcs back down into water

            var emission = psComp.emission;
            emission.rateOverTime = 0;
            short minSplash = (short)Mathf.Clamp(Mathf.RoundToInt(28 * scale), 1, short.MaxValue);
            short maxSplash = (short)Mathf.Clamp(Mathf.RoundToInt(42 * scale), 1, short.MaxValue);
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, minSplash, maxSplash) });

            var shape = psComp.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.3f * scale;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Upwards fountain

            var colorOverLifetime = psComp.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0.85f, 1f), 0.8f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            psComp.Play();
            Destroy(splashObj, 1.0f);
        }

        // =========================================================================
        // 6. UNDERWATER BUBBLES
        // =========================================================================
        public void PlayWaterBubble(Vector3 position, int count = 6)
        {
            if (waterBubblePrefab != null)
            {
                var ps = Instantiate(waterBubblePrefab, position, Quaternion.identity);
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            GameObject bubbleObj = new GameObject("VFX_WaterBubble");
            bubbleObj.transform.position = position;

            ParticleSystem psComp = bubbleObj.AddComponent<ParticleSystem>();
            var renderer = bubbleObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new Color(0.85f, 0.97f, 1f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.4f; // Buoyant upward float

            var emission = psComp.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = psComp.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = psComp.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.9f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            psComp.Play();
            Destroy(bubbleObj, 1.4f);
        }
    }
}
