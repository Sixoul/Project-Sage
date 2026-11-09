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
            psComp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.playOnAwake = false;
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
            psComp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = blockObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.playOnAwake = false;
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
            psComp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = waveObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            Color waveColor = level == 2 ? new Color(0.2f, 0.85f, 1f) : new Color(0.3f, 1f, 0.45f);

            var main = psComp.main;
            main.playOnAwake = false;
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

            // Procedural Fiery Explosion Particles Root
            GameObject boomObj = new GameObject("VFX_BombExplosion");
            boomObj.transform.position = position;

            // 1. Dynamic Point Light Flash
            GameObject lightObj = new GameObject("ExplosionLightFlash");
            lightObj.transform.position = position + Vector3.up * 0.6f;
            lightObj.transform.SetParent(boomObj.transform, true);
            Light expLight = lightObj.AddComponent<Light>();
            expLight.type = LightType.Point;
            expLight.range = 16f;
            expLight.intensity = 18f;
            expLight.color = new Color(1f, 0.72f, 0.3f);
            StartCoroutine(FadeExplosionLight(expLight, 0.28f));

            // 2. Child: Fire & Flash Core
            ParticleSystem psFlash = boomObj.AddComponent<ParticleSystem>();
            psFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var rendFlash = boomObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) rendFlash.material = particleAdditiveMat;

            var mainF = psFlash.main;
            mainF.playOnAwake = false;
            mainF.loop = false;
            mainF.duration = 0.4f;
            mainF.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.38f);
            mainF.startSpeed = new ParticleSystem.MinMaxCurve(6f, 15f);
            mainF.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            mainF.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f), new Color(1f, 0.32f, 0.04f));
            mainF.simulationSpace = ParticleSystemSimulationSpace.World;
            mainF.gravityModifier = -0.5f;

            var emF = psFlash.emission;
            emF.rateOverTime = 0;
            emF.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 36, 50) });

            var shapeF = psFlash.shape;
            shapeF.shapeType = ParticleSystemShapeType.Sphere;
            shapeF.radius = 0.6f;

            var colF = psFlash.colorOverLifetime;
            colF.enabled = true;
            Gradient gradF = new Gradient();
            gradF.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.85f), 0f), new GradientColorKey(new Color(0.95f, 0.25f, 0.02f), 0.65f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colF.color = gradF;

            // 3. Child: Expanding Ground Shockwave Ring
            GameObject shockObj = new GameObject("ExplosionShockwave");
            shockObj.transform.SetParent(boomObj.transform, false);
            shockObj.transform.localPosition = Vector3.up * 0.1f;
            ParticleSystem psShock = shockObj.AddComponent<ParticleSystem>();
            psShock.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var rendShock = shockObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) rendShock.material = particleAdditiveMat;

            var mainS = psShock.main;
            mainS.playOnAwake = false;
            mainS.loop = false;
            mainS.duration = 0.35f;
            mainS.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.32f);
            mainS.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
            mainS.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            mainS.startColor = new Color(1f, 0.75f, 0.35f, 0.9f);
            mainS.simulationSpace = ParticleSystemSimulationSpace.World;
            mainS.gravityModifier = 0f;

            var emS = psShock.emission;
            emS.rateOverTime = 0;
            emS.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 36, 48) });

            var shapeS = psShock.shape;
            shapeS.shapeType = ParticleSystemShapeType.Circle;
            shapeS.radius = 0.3f;
            shapeS.rotation = new Vector3(90f, 0f, 0f); // flat horizontal ring along the ground

            var colS = psShock.colorOverLifetime;
            colS.enabled = true;
            Gradient gradS = new Gradient();
            gradS.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(new Color(0.9f, 0.3f, 0.05f), 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colS.color = gradS;

            // 4. Child: Billowing Dark Smoke Clouds
            GameObject smokeObj = new GameObject("ExplosionSmoke");
            smokeObj.transform.SetParent(boomObj.transform, false);
            ParticleSystem psSmoke = smokeObj.AddComponent<ParticleSystem>();
            psSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var rendSmoke = smokeObj.GetComponent<ParticleSystemRenderer>();
            if (particleAlphaMat != null) rendSmoke.material = particleAlphaMat;

            var mainSmk = psSmoke.main;
            mainSmk.playOnAwake = false;
            mainSmk.loop = false;
            mainSmk.duration = 0.9f;
            mainSmk.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            mainSmk.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            mainSmk.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
            mainSmk.startColor = new Color(0.18f, 0.16f, 0.15f, 0.75f);
            mainSmk.simulationSpace = ParticleSystemSimulationSpace.World;
            mainSmk.gravityModifier = -0.4f; // Buoyant upward drift

            var emSmk = psSmoke.emission;
            emSmk.rateOverTime = 0;
            emSmk.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.04f, 22, 32) });

            var shapeSmk = psSmoke.shape;
            shapeSmk.shapeType = ParticleSystemShapeType.Sphere;
            shapeSmk.radius = 0.8f;

            var sizeSmk = psSmoke.sizeOverLifetime;
            sizeSmk.enabled = true;
            AnimationCurve smokeCurve = new AnimationCurve();
            smokeCurve.AddKey(0f, 0.5f);
            smokeCurve.AddKey(0.4f, 1.3f);
            smokeCurve.AddKey(1f, 2.0f);
            sizeSmk.size = new ParticleSystem.MinMaxCurve(1f, smokeCurve);

            var colSmk = psSmoke.colorOverLifetime;
            colSmk.enabled = true;
            Gradient gradSmk = new Gradient();
            gradSmk.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.25f, 0.2f, 0.18f), 0f), new GradientColorKey(new Color(0.12f, 0.11f, 0.11f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colSmk.color = gradSmk;

            // 5. Child: High-Velocity Fiery Shrapnel Sparks
            GameObject sparkObj = new GameObject("ExplosionSparks");
            sparkObj.transform.SetParent(boomObj.transform, false);
            ParticleSystem psSparks = sparkObj.AddComponent<ParticleSystem>();
            psSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var rendSparks = sparkObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) rendSparks.material = particleAdditiveMat;

            var mainSpk = psSparks.main;
            mainSpk.playOnAwake = false;
            mainSpk.loop = false;
            mainSpk.duration = 0.6f;
            mainSpk.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            mainSpk.startSpeed = new ParticleSystem.MinMaxCurve(12f, 22f);
            mainSpk.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            mainSpk.startColor = new Color(1f, 0.8f, 0.2f);
            mainSpk.simulationSpace = ParticleSystemSimulationSpace.World;
            mainSpk.gravityModifier = 2.4f;

            var emSpk = psSparks.emission;
            emSpk.rateOverTime = 0;
            emSpk.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30, 45) });

            var shapeSpk = psSparks.shape;
            shapeSpk.shapeType = ParticleSystemShapeType.Hemisphere;
            shapeSpk.radius = 0.25f;

            psFlash.Play();
            psShock.Play();
            psSmoke.Play();
            psSparks.Play();

            // 6. Ground Scorch Mark Decal
            SpawnGroundScorchMark(position);

            Destroy(boomObj, 1.6f);
        }

        private void SpawnGroundScorchMark(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 4f))
            {
                GameObject scorchObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                scorchObj.name = "BombScorchMark";
                scorchObj.transform.position = hit.point + hit.normal * 0.02f;
                scorchObj.transform.rotation = Quaternion.LookRotation(-hit.normal);
                scorchObj.transform.localScale = Vector3.one * 2.8f;

                var col = scorchObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mr = scorchObj.GetComponent<MeshRenderer>();
                Material sMat = particleAlphaMat != null ? new Material(particleAlphaMat) : new Material(Shader.Find("Sprites/Default"));
                sMat.color = new Color(0.06f, 0.05f, 0.04f, 0.78f);
                mr.material = sMat;

                StartCoroutine(FadeScorchMark(scorchObj, sMat, 4.5f));
            }
        }

        private IEnumerator FadeExplosionLight(Light light, float duration)
        {
            float elapsed = 0f;
            float initialIntensity = light != null ? light.intensity : 18f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (light != null)
                {
                    light.intensity = Mathf.Lerp(initialIntensity, 0f, t * t);
                }
                yield return null;
            }
            if (light != null)
            {
                Destroy(light.gameObject);
            }
        }

        private IEnumerator FadeScorchMark(GameObject scorchObj, Material scorchMat, float lifetime)
        {
            float elapsed = 0f;
            Color initialColor = scorchMat != null ? scorchMat.color : Color.black;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;
                if (t > 0.55f && scorchMat != null)
                {
                    float fadeT = (t - 0.55f) / 0.45f;
                    Color c = initialColor;
                    c.a = Mathf.Lerp(initialColor.a, 0f, fadeT);
                    scorchMat.color = c;
                }
                yield return null;
            }
            if (scorchObj != null)
            {
                Destroy(scorchObj);
            }
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
            psComp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = splashObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.playOnAwake = false;
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
            psComp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = bubbleObj.GetComponent<ParticleSystemRenderer>();
            if (particleAdditiveMat != null) renderer.material = particleAdditiveMat;

            var main = psComp.main;
            main.playOnAwake = false;
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
