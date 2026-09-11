using UnityEngine;
using UnityEngine.UIElements;
using ZeldaOoT.Player;
using ZeldaOoT.CameraRig;
using ZeldaOoT.Input;
using ZeldaOoT.Combat;
using ZeldaOoT.AI;
using ZeldaOoT.Water;
using ZeldaOoT.UI;

namespace ZeldaOoT.Environment
{
    /// <summary>
    /// Builds the complete Zelda showcase test environment:
    /// arena, water swimming/diving pool, jump obstacle courses,
    /// sparring enemies, target range, and player camera rig.
    /// </summary>
    public class ZeldaPlaygroundBuilder : MonoBehaviour
    {
        [Header("Auto Build On Start")]
        [SerializeField] private bool buildOnStart = true;

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        private void Start()
        {
            // If playground or player already exists in the scene, do NOT duplicate
            if (GameObject.Find("Zelda_Test_Playground") != null || GameObject.Find("Player_Link") != null)
            {
                return;
            }

            if (buildOnStart)
            {
                BuildPlayground();
            }
        }

        [ContextMenu("Build Playground Now")]
        public void BuildPlayground()
        {
            // 0. Clean up any existing duplicate objects before building
            var oldPlayground = GameObject.Find("Zelda_Test_Playground");
            var oldPlayer = GameObject.Find("Player_Link");
            var oldCanvas = GameObject.Find("Zelda_UI_Toolkit_Canvas");
            var oldDeathFloor = GameObject.Find("Death_Floor_Hazard");

            if (Application.isPlaying)
            {
                if (oldPlayground != null) Destroy(oldPlayground);
                if (oldPlayer != null) Destroy(oldPlayer);
                if (oldCanvas != null) Destroy(oldCanvas);
                if (oldDeathFloor != null) Destroy(oldDeathFloor);
            }
            else
            {
                if (oldPlayground != null) DestroyImmediate(oldPlayground);
                if (oldPlayer != null) DestroyImmediate(oldPlayer);
                if (oldCanvas != null) DestroyImmediate(oldCanvas);
                if (oldDeathFloor != null) DestroyImmediate(oldDeathFloor);
            }

            // Root container
            GameObject arenaRoot = new GameObject("Zelda_Test_Playground");

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Materials
            Material stoneMat = new Material(urpLit) { color = new Color(0.6f, 0.62f, 0.65f) };
            Material pillarMat = new Material(urpLit) { color = new Color(0.48f, 0.5f, 0.53f) };
            Material grassMat = new Material(urpLit) { color = new Color(0.28f, 0.55f, 0.22f) };
            Material woodMat = new Material(urpLit) { color = new Color(0.55f, 0.35f, 0.15f) };
            Material sandMat = new Material(urpLit) { color = new Color(0.85f, 0.78f, 0.55f) }; // Beach sand
            Material goldMat = new Material(urpLit) { color = new Color(0.95f, 0.78f, 0.1f) };

            // URP Translucent Water Material
            Material waterMat = new Material(urpLit);
            waterMat.color = new Color(0.12f, 0.65f, 0.95f, 0.62f);
            waterMat.SetFloat("_Surface", 1f); // Transparent
            waterMat.SetFloat("_Blend", 0f);   // Alpha blend
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            waterMat.SetFloat("_Smoothness", 0.95f);
            waterMat.SetFloat("_Metallic", 0.05f);

            // 1. Excavated Ground Floor (Lake is cut out in North-West quadrant from X: -35 to -6, Z: 4 to 35)
            // South Ground Slab (Covers starting area & combat ring: X: -35 to 35, Z: -35 to 4)
            GameObject floorSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorSouth.name = "Arena_Floor_South";
            floorSouth.transform.SetParent(arenaRoot.transform, false);
            floorSouth.transform.position = new Vector3(0f, -0.5f, -15.5f);
            floorSouth.transform.localScale = new Vector3(70f, 1f, 39f);
            floorSouth.GetComponent<MeshRenderer>().material = grassMat;

            // East Ground Slab (Covers obstacle platform course & archery: X: -6 to 35, Z: 4 to 35)
            GameObject floorEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorEast.name = "Arena_Floor_East";
            floorEast.transform.SetParent(arenaRoot.transform, false);
            floorEast.transform.position = new Vector3(14.5f, -0.5f, 19.5f);
            floorEast.transform.localScale = new Vector3(41f, 1f, 31f);
            floorEast.GetComponent<MeshRenderer>().material = grassMat;

            // 2. Colosseum Central Ring (Paved stone circle/square)
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ring.name = "Central_Combat_Ring";
            ring.transform.SetParent(arenaRoot.transform, false);
            ring.transform.position = new Vector3(0f, 0.05f, -8f);
            ring.transform.localScale = new Vector3(26f, 0.1f, 24f);
            ring.GetComponent<MeshRenderer>().material = stoneMat;

            // 3. Ancient Stone Pillars around Ring
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 15f, 3.5f, -8f + Mathf.Sin(angle) * 14f);

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i + 1}";
                pillar.transform.SetParent(arenaRoot.transform, false);
                pillar.transform.position = pos;
                pillar.transform.localScale = new Vector3(1.4f, 3.5f, 1.4f);
                pillar.GetComponent<MeshRenderer>().material = pillarMat;
            }

            // 4. Boundary Walls (Deep fortress walls extending from Y = -8f to Y = +8f to seal arena & lake)
            CreateWall(arenaRoot.transform, new Vector3(0f, 0f, 35.5f), new Vector3(74f, 16f, 3f), stoneMat);
            CreateWall(arenaRoot.transform, new Vector3(0f, 0f, -35.5f), new Vector3(74f, 16f, 3f), stoneMat);
            CreateWall(arenaRoot.transform, new Vector3(35.5f, 0f, 0f), new Vector3(3f, 16f, 74f), stoneMat);
            CreateWall(arenaRoot.transform, new Vector3(-35.5f, 0f, 0f), new Vector3(3f, 16f, 74f), stoneMat);

            // 5. Expansive Excavated Lake Hylia Basin & Deep Diving Pool (29m x 31m, 5.5m deep)
            BuildWaterPool(arenaRoot.transform, new Vector3(-20.5f, 0f, 19.5f), waterMat, stoneMat, sandMat);

            // 6. Platforming Obstacle Course (Free Jumping Tests)
            BuildPlatformingCourse(arenaRoot.transform, new Vector3(18f, 0f, 10f), stoneMat, woodMat);

            // 7. Target Range (Archery & Item Tests)
            BuildTargetRange(arenaRoot.transform, new Vector3(16f, 0f, 26f), woodMat, goldMat);

            // 8. Destructible Wooden Crates (Bomb & Slash Tests)
            BuildCrateStack(arenaRoot.transform, new Vector3(6f, 0f, -18f), woodMat);

            // 9. Sparring Dummies & Combat Enemies
            BuildCombatDummies(arenaRoot.transform, new Vector3(0f, 0f, -2f));

            // 10. Global Death Floor Hazard (Failsafe void catching & respawn)
            GameObject deathFloorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deathFloorObj.name = "Death_Floor_Hazard";
            deathFloorObj.transform.SetParent(arenaRoot.transform, false);
            deathFloorObj.transform.position = new Vector3(0f, -22f, 0f);
            deathFloorObj.transform.localScale = new Vector3(500f, 4f, 500f);
            var deathCol = deathFloorObj.GetComponent<BoxCollider>();
            deathCol.isTrigger = true;
            deathFloorObj.GetComponent<MeshRenderer>().enabled = false;
            var deathFloor = deathFloorObj.AddComponent<DeathFloor>();
            deathFloor.SetRespawnPoint(new Vector3(0f, 0.5f, -12f));

            // 11. Lighting & Sun
            SetupLighting(arenaRoot.transform);

            // 12. Input Reader
            if (FindFirstObjectByType<ZeldaInputReader>() == null)
            {
                GameObject inputObj = new GameObject("ZeldaInputReader");
                inputObj.AddComponent<ZeldaInputReader>();
            }

            // 13. Player Character Link
            GameObject player = ZeldaPlayerBuilder.CreatePlayer(new Vector3(0f, 0.2f, -12f));

            // 14. Camera Rig
            SetupCamera(player.transform);

            // 15. UI Toolkit HUD & BotW Quick-Swap UI
            SetupUI();
        }

        private void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().material = mat;
        }

        private void BuildWaterPool(Transform parent, Vector3 center, Material waterMat, Material stoneMat, Material sandMat)
        {
            GameObject poolRoot = new GameObject("Lake_Hylia_Water_Basin");
            poolRoot.transform.SetParent(parent, false);
            poolRoot.transform.position = center;

            float poolW = 28f;
            float poolL = 30f;
            float depth = 5.5f;

            // 1. Excavated Basin Solid Floor (Down at Y = -5.5f)
            GameObject basinFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basinFloor.name = "Basin_Bottom_Floor";
            basinFloor.transform.SetParent(poolRoot.transform, false);
            basinFloor.transform.localPosition = new Vector3(0f, -depth - 0.5f, 0f);
            basinFloor.transform.localScale = new Vector3(poolW + 4f, 1f, poolL + 4f);
            basinFloor.GetComponent<MeshRenderer>().material = sandMat;

            // 2. Solid Stone Retaining Walls (Completely seals basin from void and arena underfloor)
            // East Retaining Wall (Separates pool from East ground slab)
            GameObject wallEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallEast.name = "Basin_Retaining_Wall_East";
            wallEast.transform.SetParent(poolRoot.transform, false);
            wallEast.transform.localPosition = new Vector3(poolW * 0.5f + 0.6f, -depth * 0.5f, 0f);
            wallEast.transform.localScale = new Vector3(1.4f, depth + 1.2f, poolL);
            wallEast.GetComponent<MeshRenderer>().material = stoneMat;

            GameObject curbEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbEast.name = "Basin_Curb_East";
            curbEast.transform.SetParent(poolRoot.transform, false);
            curbEast.transform.localPosition = new Vector3(poolW * 0.5f + 0.4f, 0.25f, 0f);
            curbEast.transform.localScale = new Vector3(1.2f, 0.5f, poolL);
            curbEast.GetComponent<MeshRenderer>().material = stoneMat;

            // West Retaining Wall
            GameObject wallWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallWest.name = "Basin_Retaining_Wall_West";
            wallWest.transform.SetParent(poolRoot.transform, false);
            wallWest.transform.localPosition = new Vector3(-poolW * 0.5f - 0.6f, -depth * 0.5f, 0f);
            wallWest.transform.localScale = new Vector3(1.4f, depth + 1.2f, poolL + 2f);
            wallWest.GetComponent<MeshRenderer>().material = stoneMat;

            // North Retaining Wall
            GameObject wallNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallNorth.name = "Basin_Retaining_Wall_North";
            wallNorth.transform.SetParent(poolRoot.transform, false);
            wallNorth.transform.localPosition = new Vector3(0f, -depth * 0.5f, poolL * 0.5f + 0.6f);
            wallNorth.transform.localScale = new Vector3(poolW + 2f, depth + 1.2f, 1.4f);
            wallNorth.GetComponent<MeshRenderer>().material = stoneMat;

            // South Retaining Flanks (West & East of the 16m sandy beach ramp)
            float flankW = (poolW - 16f) * 0.5f; // 6m flank
            GameObject wallSouthWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallSouthWest.name = "Basin_Retaining_Wall_SouthWest";
            wallSouthWest.transform.SetParent(poolRoot.transform, false);
            wallSouthWest.transform.localPosition = new Vector3(-poolW * 0.5f + flankW * 0.5f, -depth * 0.5f, -poolL * 0.5f - 0.6f);
            wallSouthWest.transform.localScale = new Vector3(flankW + 0.5f, depth + 1.2f, 1.4f);
            wallSouthWest.GetComponent<MeshRenderer>().material = stoneMat;

            GameObject wallSouthEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallSouthEast.name = "Basin_Retaining_Wall_SouthEast";
            wallSouthEast.transform.SetParent(poolRoot.transform, false);
            wallSouthEast.transform.localPosition = new Vector3(poolW * 0.5f - flankW * 0.5f, -depth * 0.5f, -poolL * 0.5f - 0.6f);
            wallSouthEast.transform.localScale = new Vector3(flankW + 0.5f, depth + 1.2f, 1.4f);
            wallSouthEast.GetComponent<MeshRenderer>().material = stoneMat;

            // Sub-Ramp Solid Foundation (Blocks swimming underneath the beach ramp)
            GameObject rampFoundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampFoundation.name = "Beach_Ramp_Solid_Foundation";
            rampFoundation.transform.SetParent(poolRoot.transform, false);
            rampFoundation.transform.localPosition = new Vector3(0f, -depth * 0.5f - 0.5f, -poolL * 0.5f + 1f);
            rampFoundation.transform.localScale = new Vector3(16.5f, depth, 3.5f);
            rampFoundation.GetComponent<MeshRenderer>().material = stoneMat;

            // 3. Underwater Temple Pillars & Ruins to explore at the bottom
            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    GameObject sunkenPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    sunkenPillar.name = "Sunken_Ancient_Pillar";
                    sunkenPillar.transform.SetParent(poolRoot.transform, false);
                    sunkenPillar.transform.localPosition = new Vector3(i * 6f, -depth + 1.8f, j * 6f);
                    sunkenPillar.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
                    sunkenPillar.GetComponent<MeshRenderer>().material = stoneMat;
                }
            }

            // Sunken Treasure Chest at lake bottom
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "Sunken_Treasure_Chest";
            chest.transform.SetParent(poolRoot.transform, false);
            chest.transform.localPosition = new Vector3(0f, -depth + 0.5f, 0f);
            chest.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
            var chestMat = new Material(stoneMat) { color = new Color(0.85f, 0.65f, 0.1f) };
            chest.GetComponent<MeshRenderer>().material = chestMat;

            // 4. Shimmering Water Surface Plane (At Y = -0.05f)
            GameObject waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterSurface.name = "Water_Surface_Plane";
            waterSurface.transform.SetParent(poolRoot.transform, false);
            waterSurface.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            waterSurface.transform.localScale = new Vector3(poolW - 0.1f, 0.05f, poolL - 0.1f);
            waterSurface.GetComponent<MeshRenderer>().material = waterMat;
            SafeDestroy(waterSurface.GetComponent<Collider>()); // Visual only, collider is trigger below

            // 5. Full Water Volume Trigger (Contained snugly within retaining walls)
            GameObject waterTrigger = new GameObject("WaterVolumeTrigger");
            waterTrigger.transform.SetParent(poolRoot.transform, false);
            waterTrigger.transform.localPosition = new Vector3(0f, -depth * 0.5f, 0f);
            var boxCol = waterTrigger.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(poolW - 0.2f, depth + 0.6f, poolL - 0.2f);
            var waterVol = waterTrigger.AddComponent<WaterVolume>();
            waterVol.SetExplicitSurface(0f);

            // 5. Expansive Sandy Beach Entrance Ramp (South Shore)
            // Slopes gently from arena floor (Y = 0f) down into the water (Y = -4.5f)
            GameObject beachRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beachRamp.name = "Sandy_Beach_Entry_Ramp";
            beachRamp.transform.SetParent(poolRoot.transform, false);
            beachRamp.transform.localPosition = new Vector3(0f, -2.5f, -poolL * 0.5f + 4f);
            beachRamp.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            beachRamp.transform.localScale = new Vector3(16f, 0.5f, 13f);
            beachRamp.GetComponent<MeshRenderer>().material = sandMat;

            // 6. High Diving Platform & Pier (Juts out 9m over deep water from East Shore)
            GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "High_Diving_Pier";
            pier.transform.SetParent(poolRoot.transform, false);
            pier.transform.localPosition = new Vector3(poolW * 0.5f - 4.5f, 3.2f, 2f);
            pier.transform.localScale = new Vector3(9f, 0.4f, 3.5f);
            pier.GetComponent<MeshRenderer>().material = stoneMat;

            // Pier support pillars
            GameObject pierPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pierPost.name = "Pier_Support";
            pierPost.transform.SetParent(poolRoot.transform, false);
            pierPost.transform.localPosition = new Vector3(poolW * 0.5f - 7f, -0.5f, 2f);
            pierPost.transform.localScale = new Vector3(0.8f, 3.8f, 0.8f);
            pierPost.GetComponent<MeshRenderer>().material = stoneMat;

            // 7. Stepping Stone Island in Center of Lake
            GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            island.name = "Lake_Central_Island";
            island.transform.SetParent(poolRoot.transform, false);
            island.transform.localPosition = new Vector3(5f, 0.25f, 4f);
            island.transform.localScale = new Vector3(3.5f, 0.5f, 3.5f);
            island.GetComponent<MeshRenderer>().material = stoneMat;
        }

        private void BuildPlatformingCourse(Transform parent, Vector3 center, Material stoneMat, Material woodMat)
        {
            GameObject courseRoot = new GameObject("Platforming_Course");
            courseRoot.transform.SetParent(parent, false);
            courseRoot.transform.position = center;

            // Stepped tiered blocks
            float[] heights = { 0.8f, 1.6f, 2.4f, 3.2f, 4.0f };
            for (int i = 0; i < heights.Length; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_Platform_{i + 1}";
                step.transform.SetParent(courseRoot.transform, false);
                step.transform.localPosition = new Vector3((i - 2) * 2.8f, heights[i] * 0.5f, 0f);
                step.transform.localScale = new Vector3(2.2f, heights[i], 2.2f);
                step.GetComponent<MeshRenderer>().material = (i % 2 == 0) ? stoneMat : woodMat;
            }

            // Suspended jump balance beam
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Balance_Beam";
            beam.transform.SetParent(courseRoot.transform, false);
            beam.transform.localPosition = new Vector3(0f, 2.8f, -4.5f);
            beam.transform.localScale = new Vector3(10f, 0.4f, 0.8f);
            beam.GetComponent<MeshRenderer>().material = woodMat;
        }

        private void BuildTargetRange(Transform parent, Vector3 center, Material woodMat, Material goldMat)
        {
            GameObject rangeRoot = new GameObject("Target_Range");
            rangeRoot.transform.SetParent(parent, false);
            rangeRoot.transform.position = center;

            for (int i = -2; i <= 2; i++)
            {
                GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetObj.name = $"Bullseye_Target_{i + 3}";
                targetObj.transform.SetParent(rangeRoot.transform, false);
                targetObj.transform.localPosition = new Vector3(i * 4.5f, 2.0f, 0f);
                targetObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                targetObj.transform.localScale = new Vector3(1.6f, 0.15f, 1.6f);

                var mr = targetObj.GetComponent<MeshRenderer>();
                mr.material = goldMat;

                // Center red dot
                GameObject bullseye = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bullseye.name = "CenterDot";
                bullseye.transform.SetParent(targetObj.transform, false);
                bullseye.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                bullseye.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
                var dotMr = bullseye.GetComponent<MeshRenderer>();
                dotMr.material = new Material(mr.material) { color = Color.red };

                targetObj.AddComponent<TargetBullseye>();

                // Wooden post
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Post";
                post.transform.SetParent(rangeRoot.transform, false);
                post.transform.localPosition = new Vector3(i * 4.5f, 0.9f, 0.1f);
                post.transform.localScale = new Vector3(0.3f, 1.8f, 0.3f);
                post.GetComponent<MeshRenderer>().material = woodMat;
            }
        }

        private void BuildCrateStack(Transform parent, Vector3 center, Material woodMat)
        {
            GameObject crateRoot = new GameObject("Destructible_Crates");
            crateRoot.transform.SetParent(parent, false);
            crateRoot.transform.position = center;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crate.name = $"Crate_{x}_{y}";
                    crate.transform.SetParent(crateRoot.transform, false);
                    crate.transform.localPosition = new Vector3(x * 1.1f, 0.55f + y * 1.05f, 0f);
                    crate.transform.localScale = Vector3.one * 0.95f;
                    crate.GetComponent<MeshRenderer>().material = woodMat;
                    crate.AddComponent<DestructibleCrate>();
                }
            }
        }

        private void BuildCombatDummies(Transform parent, Vector3 center)
        {
            GameObject dummyRoot = new GameObject("Combat_Sparring_Zone");
            dummyRoot.transform.SetParent(parent, false);
            dummyRoot.transform.position = center;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Main Interactive Sparring Enemy (Stalfos / Bokoblin sparring partner)
            CreateDummy(dummyRoot.transform, new Vector3(0f, 0.1f, 0f), "Sparring_Partner_Stalfos", urpLit);
            CreateDummy(dummyRoot.transform, new Vector3(-5f, 0.1f, 2f), "Training_Dummy_Left", urpLit);
            CreateDummy(dummyRoot.transform, new Vector3(5f, 0.1f, 2f), "Training_Dummy_Right", urpLit);
        }

        private void CreateDummy(Transform parent, Vector3 localPos, string name, Shader shader)
        {
            GameObject dummy = new GameObject(name);
            dummy.transform.SetParent(parent, false);
            dummy.transform.localPosition = localPos;

            var cc = dummy.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0f, 1f, 0f);

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "DummyBody";
            body.transform.SetParent(dummy.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            SafeDestroy(body.GetComponent<Collider>());

            var mr = body.GetComponent<MeshRenderer>();
            var mat = new Material(shader) { color = new Color(0.75f, 0.22f, 0.22f) }; // Red enemy
            mr.material = mat;

            // Shield on dummy's left arm
            GameObject dShield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dShield.name = "DummyShield";
            dShield.transform.SetParent(dummy.transform, false);
            dShield.transform.localPosition = new Vector3(-0.55f, 1f, 0.3f);
            dShield.transform.localScale = new Vector3(0.1f, 0.7f, 0.6f);
            SafeDestroy(dShield.GetComponent<Collider>());
            dShield.GetComponent<MeshRenderer>().material = new Material(shader) { color = new Color(0.25f, 0.25f, 0.3f) };

            // Sword on dummy's right arm
            GameObject dSword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dSword.name = "DummySword";
            dSword.transform.SetParent(dummy.transform, false);
            dSword.transform.localPosition = new Vector3(0.55f, 1f, 0.3f);
            dSword.transform.localScale = new Vector3(0.08f, 1.1f, 0.12f);
            SafeDestroy(dSword.GetComponent<Collider>());
            dSword.GetComponent<MeshRenderer>().material = new Material(shader) { color = Color.gray };

            // Scripts
            dummy.AddComponent<HealthSystem>().SetHearts(5); // 20 HP
            dummy.AddComponent<LockOnTarget>();
            dummy.AddComponent<CombatDummyAI>();
        }

        private void SetupLighting(Transform parent)
        {
            // Directional Sunlight
            Light sun = FindFirstObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Zelda_Directional_Sun");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.transform.position = new Vector3(0f, 30f, 0f);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.color = new Color(1f, 0.96f, 0.85f); // Warm sun
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
        }

        private void SetupCamera(Transform playerTransform)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            var rig = cam.GetComponent<ZeldaCameraRig>();
            if (rig == null)
            {
                rig = cam.gameObject.AddComponent<ZeldaCameraRig>();
            }
            rig.SetTarget(playerTransform);

            if (FindFirstObjectByType<ZeldaCursorManager>() == null)
            {
                cam.gameObject.AddComponent<ZeldaCursorManager>();
            }
        }

        private void SetupUI()
        {
            GameObject uiCanvas = new GameObject("Zelda_UI_Toolkit_Canvas");
            var uidoc = uiCanvas.AddComponent<UIDocument>();

#if UNITY_EDITOR
            var existingSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/ZeldaPanelSettings.asset");
            if (existingSettings != null)
            {
                uidoc.panelSettings = existingSettings;
            }
            else
            {
                var settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                settings.match = 0.5f;
                if (!System.IO.Directory.Exists("Assets/UI")) System.IO.Directory.CreateDirectory("Assets/UI");
                UnityEditor.AssetDatabase.CreateAsset(settings, "Assets/UI/ZeldaPanelSettings.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                uidoc.panelSettings = settings;
            }

            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/ZeldaHUD.uxml");
            if (uxml != null)
            {
                uidoc.visualTreeAsset = uxml;
            }
#else
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            uidoc.panelSettings = settings;
#endif

            uiCanvas.AddComponent<ZeldaHUDUI>();
            uiCanvas.AddComponent<HorizonWheelMenuUI>();
            uiCanvas.AddComponent<BotWInventoryUI>();
        }
    }
}
