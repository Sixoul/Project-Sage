using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZeldaOoT.Player;
using ZeldaOoT.CameraRig;
using ZeldaOoT.Input;
using ZeldaOoT.Combat;
using ZeldaOoT.DekuTree;
using ZeldaOoT.UI;
using ZeldaOoT.Inventory;

namespace ZeldaOoT.Kokiri
{
    /// <summary>
    /// Constructs the complete Kokiri Forest hub and connects to Inside the Great Deku Tree.
    /// Includes Link's treehouse, village green, grass patches, river with stepping stones,
    /// Kokiri shop (Deku Shield 40 Rupees), training grounds with rolling boulder and Kokiri Sword chest,
    /// Mido's gatekeeper barrier, and the animated Great Deku Tree clearing.
    /// </summary>
    public class KokiriForestBuilder : MonoBehaviour
    {
        [Header("Auto Build On Start")]
        [SerializeField] private bool buildOnStart = true;

        private void Start()
        {
            if (GameObject.Find("Kokiri_Forest_World") != null || GameObject.Find("Player_Link") != null)
            {
                return;
            }

            if (buildOnStart)
            {
                BuildKokiriForest();
            }
        }

        [ContextMenu("Build Kokiri Forest Now")]
        public void BuildKokiriForest()
        {
            // Clean up existing objects
            var oldForest = GameObject.Find("Kokiri_Forest_World");
            var oldPlayground = GameObject.Find("Zelda_Test_Playground");
            var oldPlayer = GameObject.Find("Player_Link");
            var oldCanvas = GameObject.Find("Zelda_UI_Toolkit_Canvas");

            if (Application.isPlaying)
            {
                if (oldForest != null) Destroy(oldForest);
                if (oldPlayground != null) Destroy(oldPlayground);
                if (oldPlayer != null) Destroy(oldPlayer);
                if (oldCanvas != null) Destroy(oldCanvas);
            }
            else
            {
                if (oldForest != null) DestroyImmediate(oldForest);
                if (oldPlayground != null) DestroyImmediate(oldPlayground);
                if (oldPlayer != null) DestroyImmediate(oldPlayer);
                if (oldCanvas != null) DestroyImmediate(oldCanvas);
            }

            GameObject worldRoot = new GameObject("Kokiri_Forest_World");

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material grassMat = new Material(s) { color = new Color(0.24f, 0.54f, 0.18f) };
            Material pathMat = new Material(s) { color = new Color(0.68f, 0.55f, 0.35f) }; // Dirt path
            Material woodMat = new Material(s) { color = new Color(0.48f, 0.32f, 0.16f) };
            Material barkMat = new Material(s) { color = new Color(0.35f, 0.22f, 0.11f) };
            Material leafMat = new Material(s) { color = new Color(0.18f, 0.42f, 0.14f) };
            Material stoneMat = new Material(s) { color = new Color(0.52f, 0.54f, 0.56f) };
            Material waterMat = new Material(s);
            waterMat.color = new Color(0.12f, 0.65f, 0.95f, 0.65f);
            waterMat.SetFloat("_Surface", 1f);
            waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // =========================================================================
            // 1. TERRAIN & VILLAGE GREEN
            // =========================================================================
            // Main village ground slab (80m x 80m)
            GameObject villageFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villageFloor.name = "Village_Ground_Floor";
            villageFloor.transform.SetParent(worldRoot.transform, false);
            villageFloor.transform.position = new Vector3(0f, -0.5f, 0f);
            villageFloor.transform.localScale = new Vector3(90f, 1f, 90f);
            villageFloor.GetComponent<MeshRenderer>().material = grassMat;

            // Dirt Pathways linking houses, training grounds, and Mido's gate
            CreatePath(worldRoot.transform, new Vector3(0f, 0.02f, 0f), new Vector3(8f, 0.05f, 45f), pathMat);
            CreatePath(worldRoot.transform, new Vector3(12f, 0.02f, -10f), new Vector3(32f, 0.05f, 6f), pathMat);
            CreatePath(worldRoot.transform, new Vector3(-14f, 0.02f, 12f), new Vector3(28f, 0.05f, 6f), pathMat);

            // Surrounding High Forest Cliffs / Tree Line (Sealing village boundary)
            BuildBoundaryForestCliffs(worldRoot.transform, 45f, barkMat, leafMat);

            // =========================================================================
            // 2. LINK'S ELEVATED TREEHOUSE & NAVI
            // =========================================================================
            Vector3 treehousePos = new Vector3(-18f, 0f, -22f);
            BuildLinksTreehouse(worldRoot.transform, treehousePos, woodMat, barkMat);

            // =========================================================================
            // 3. KOKIRI SHOP (DEKU SHIELD FOR 40 RUPEES)
            // =========================================================================
            Vector3 shopPos = new Vector3(22f, 0f, -8f);
            KokiriShop.CreateShopCounter(shopPos, Quaternion.Euler(0f, -65f, 0f)).transform.SetParent(worldRoot.transform, true);

            // =========================================================================
            // 4. SARIA'S HOUSE & MIDO'S HOUSE
            // =========================================================================
            BuildKokiriHouse(worldRoot.transform, new Vector3(24f, 0f, 18f), "Saria_House", woodMat, leafMat);
            BuildKokiriHouse(worldRoot.transform, new Vector3(-24f, 0f, 6f), "Mido_House", woodMat, leafMat);

            // =========================================================================
            // 5. KOKIRI RIVER & STEPPING STONES PUZZLE
            // =========================================================================
            BuildRiverAndSteppingStones(worldRoot.transform, new Vector3(8f, 0f, -14f), waterMat, stoneMat);

            // =========================================================================
            // 6. CUTTABLE GRASS PATCHES & RUPEE HARVESTING
            // =========================================================================
            SpawnFoliageAndGrass(worldRoot.transform, grassMat);

            // =========================================================================
            // 7. FOREST TRAINING GROUNDS: CRAWLSPACE & ROLLING BOULDER MAZE
            // =========================================================================
            Vector3 trainingPos = new Vector3(-26f, 0f, -6f);
            BuildTrainingGrounds(worldRoot.transform, trainingPos, pathMat, stoneMat, woodMat);

            // =========================================================================
            // 8. MIDO'S GATE & PROGRESSION BARRIER
            // =========================================================================
            Vector3 gatePos = new Vector3(0f, 0f, 28f);
            BuildMidosGate(worldRoot.transform, gatePos, woodMat);

            // =========================================================================
            // 9. DEKU BABA FOREST TUNNEL
            // =========================================================================
            BuildDekuBabaPath(worldRoot.transform, new Vector3(0f, 0f, 42f), barkMat, leafMat);

            // =========================================================================
            // 10. THE GREAT DEKU TREE CLEARING & DUNGEON ENTRANCE
            // =========================================================================
            Vector3 dekuTreePos = new Vector3(0f, 0f, 85f);
            BuildGreatDekuTreeExterior(worldRoot.transform, dekuTreePos, barkMat, leafMat);

            // =========================================================================
            // 11. DUNGEON INTERIOR: "INSIDE THE GREAT DEKU TREE"
            // =========================================================================
            Vector3 dungeonPos = new Vector3(0f, 0f, 115f);
            GreatDekuTreeDungeonBuilder.BuildDungeon(dungeonPos).transform.SetParent(worldRoot.transform, true);

            // =========================================================================
            // 12. LIGHTING & SUN
            // =========================================================================
            SetupForestLighting(worldRoot.transform);

            // =========================================================================
            // 13. SPAWN LINK & NAVI
            // =========================================================================
            Vector3 playerSpawn = treehousePos + new Vector3(0f, 4.8f, 0f); // Spawns on treehouse porch!
            GameObject player = ZeldaPlayerBuilder.CreatePlayer(playerSpawn);

            // Set authentic OoT starting inventory (starts without sword and shield!)
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.SetOoTStartingMode();
            }

            // Spawn Navi the Fairy companion orbiting Link
            NaviCompanion.CreateNavi(player.transform).transform.SetParent(worldRoot.transform, true);

            // Setup Camera
            SetupCamera(player.transform);

            // Setup UI Toolkit HUD & Dialogue
            SetupUI();
        }

        private static void CreatePath(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "Dirt_Path";
            path.transform.SetParent(parent, false);
            path.transform.localPosition = pos;
            path.transform.localScale = scale;
            path.GetComponent<MeshRenderer>().material = mat;
        }

        private static void BuildBoundaryForestCliffs(Transform parent, float radius, Material barkMat, Material leafMat)
        {
            int count = 28;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Vector3 p = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);

                // Large ancient tree trunk
                GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = $"Boundary_Tree_{i}";
                tree.transform.SetParent(parent, false);
                tree.transform.localPosition = p + Vector3.up * 8f;
                tree.transform.localScale = new Vector3(4.5f, 8f, 4.5f);
                tree.GetComponent<MeshRenderer>().material = barkMat;

                // Foliage canopy
                GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = "Tree_Canopy";
                canopy.transform.SetParent(tree.transform, false);
                canopy.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                canopy.transform.localScale = new Vector3(2.2f, 1.4f, 2.2f);
                canopy.GetComponent<MeshRenderer>().material = leafMat;
            }
        }

        private static void BuildLinksTreehouse(Transform parent, Vector3 pos, Material woodMat, Material barkMat)
        {
            GameObject houseRoot = new GameObject("Links_Treehouse");
            houseRoot.transform.SetParent(parent, false);
            houseRoot.transform.localPosition = pos;

            // Elevated trunk base
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Treehouse_Trunk";
            trunk.transform.SetParent(houseRoot.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            trunk.transform.localScale = new Vector3(7.5f, 2.2f, 7.5f);
            trunk.GetComponent<MeshRenderer>().material = barkMat;

            // Porch platform (Y = 4.4f)
            GameObject porch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            porch.name = "Treehouse_Porch";
            porch.transform.SetParent(houseRoot.transform, false);
            porch.transform.localPosition = new Vector3(0f, 4.4f, 0f);
            porch.transform.localScale = new Vector3(8.5f, 0.4f, 8.5f);
            porch.GetComponent<MeshRenderer>().material = woodMat;

            // Wooden Access Ramp leading up from ground to porch
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Treehouse_Ramp";
            ramp.transform.SetParent(houseRoot.transform, false);
            ramp.transform.localPosition = new Vector3(0f, 2.2f, 7.5f);
            ramp.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            ramp.transform.localScale = new Vector3(2.8f, 0.35f, 10.5f);
            ramp.GetComponent<MeshRenderer>().material = woodMat;

            // House Cabin Walls
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wallZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallZ.transform.SetParent(houseRoot.transform, false);
                wallZ.transform.localPosition = new Vector3(0f, 6.2f, w * 3.2f);
                wallZ.transform.localScale = new Vector3(6.8f, 3.5f, 0.6f);
                wallZ.GetComponent<MeshRenderer>().material = woodMat;

                GameObject wallX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallX.transform.SetParent(houseRoot.transform, false);
                wallX.transform.localPosition = new Vector3(w * 3.4f, 6.2f, 0f);
                wallX.transform.localScale = new Vector3(0.6f, 3.5f, 6.4f);
                wallX.GetComponent<MeshRenderer>().material = woodMat;
            }

            // Link's Bed inside
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Links_Bed";
            bed.transform.SetParent(houseRoot.transform, false);
            bed.transform.localPosition = new Vector3(-1.8f, 4.8f, -1.8f);
            bed.transform.localScale = new Vector3(1.5f, 0.45f, 2.4f);
            bed.GetComponent<MeshRenderer>().material = woodMat;
        }

        private static void BuildKokiriHouse(Transform parent, Vector3 pos, string houseName, Material woodMat, Material leafMat)
        {
            GameObject house = new GameObject(houseName);
            house.transform.SetParent(parent, false);
            house.transform.localPosition = pos;

            // Round wood walls
            GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            walls.transform.SetParent(house.transform, false);
            walls.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            walls.transform.localScale = new Vector3(6.5f, 2.2f, 6.5f);
            walls.GetComponent<MeshRenderer>().material = woodMat;

            // Thatched leaf roof dome
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            roof.transform.SetParent(house.transform, false);
            roof.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            roof.transform.localScale = new Vector3(7.5f, 3.0f, 7.5f);
            roof.GetComponent<MeshRenderer>().material = leafMat;
        }

        private static void BuildRiverAndSteppingStones(Transform parent, Vector3 pos, Material waterMat, Material stoneMat)
        {
            GameObject riverRoot = new GameObject("Kokiri_River_SteppingStones");
            riverRoot.transform.SetParent(parent, false);
            riverRoot.transform.localPosition = pos;

            // River Water Surface
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "River_Water";
            water.transform.SetParent(riverRoot.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            water.transform.localScale = new Vector3(14f, 0.1f, 32f);
            water.GetComponent<MeshRenderer>().material = waterMat;
            Destroy(water.GetComponent<Collider>());

            // Stepping Stones across the river (Jumping puzzle yielding Blue Rupees!)
            for (int s = 0; s < 4; s++)
            {
                Vector3 stoneP = new Vector3(Mathf.Sin(s * 1.2f) * 2.5f, 0.35f, -9f + s * 5.5f);
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stone.name = $"Stepping_Stone_{s + 1}";
                stone.transform.SetParent(riverRoot.transform, false);
                stone.transform.localPosition = stoneP;
                stone.transform.localScale = new Vector3(1.8f, 0.35f, 1.8f);
                stone.GetComponent<MeshRenderer>().material = stoneMat;

                // Spawn Blue Rupee (+5) above each stepping stone
                RupeePickup.SpawnRupee(riverRoot.transform.position + stoneP + Vector3.up * 0.4f, RupeeType.Blue).transform.SetParent(riverRoot.transform, true);
            }
        }

        private static void SpawnFoliageAndGrass(Transform parent, Material grassMat)
        {
            // Clumps of cuttable grass patches across the village green
            Vector3[] grassLocations = new Vector3[]
            {
                new Vector3(-6f, 0f, -4f),
                new Vector3(-8f, 0f, -5f),
                new Vector3(-7f, 0f, -6f),
                new Vector3(4f, 0f, -8f),
                new Vector3(5f, 0f, -6f),
                new Vector3(14f, 0f, 6f),
                new Vector3(16f, 0f, 7f),
                new Vector3(-12f, 0f, 18f),
                new Vector3(-14f, 0f, 16f),
                new Vector3(6f, 0f, 22f),
                new Vector3(8f, 0f, 24f)
            };

            foreach (var loc in grassLocations)
            {
                GrassPatch.CreateGrassPatch(loc, grassMat).transform.SetParent(parent, true);
            }
        }

        private static void BuildTrainingGrounds(Transform parent, Vector3 pos, Material pathMat, Material stoneMat, Material woodMat)
        {
            GameObject trainRoot = new GameObject("Forest_Training_Meadow");
            trainRoot.transform.SetParent(parent, false);
            trainRoot.transform.localPosition = pos;

            // Dirt track maze (24m x 20m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Training_Dirt_Floor";
            floor.transform.SetParent(trainRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale = new Vector3(26f, 0.1f, 22f);
            floor.GetComponent<MeshRenderer>().material = pathMat;

            // Low stone channel walls for rolling boulder
            Vector3[] mazeWalls = new Vector3[]
            {
                new Vector3(-6f, 0.75f, 0f),
                new Vector3(6f, 0.75f, 0f)
            };

            foreach (var wPos in mazeWalls)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Boulder_Track_Wall";
                wall.transform.SetParent(trainRoot.transform, false);
                wall.transform.localPosition = wPos;
                wall.transform.localScale = new Vector3(1.2f, 1.5f, 16f);
                wall.GetComponent<MeshRenderer>().material = stoneMat;
            }

            // Rolling Boulder looping through track
            Vector3[] circuit = new Vector3[]
            {
                trainRoot.transform.position + new Vector3(-3.5f, 1.0f, -6f),
                trainRoot.transform.position + new Vector3(-3.5f, 1.0f, 6f),
                trainRoot.transform.position + new Vector3(3.5f, 1.0f, 6f),
                trainRoot.transform.position + new Vector3(3.5f, 1.0f, -6f)
            };
            RollingBoulder.CreateBoulder(circuit[0], circuit).transform.SetParent(trainRoot.transform, true);

            // Secluded treasure alcove past the boulder maze
            Vector3 chestPos = trainRoot.transform.position + new Vector3(0f, 0.3f, 8.5f);
            OoTChest.CreateChest(chestPos, Quaternion.Euler(0f, 180f, 0f), "kokiri_sword", "Kokiri Sword", "You found the Kokiri Sword! A treasured shortsword of the Kokiri. Equip it to slash enemies!", Color.green).transform.SetParent(trainRoot.transform, true);
        }

        private static void BuildMidosGate(Transform parent, Vector3 pos, Material woodMat)
        {
            GameObject gateRoot = new GameObject("Midos_Gatekeeper_Post");
            gateRoot.transform.SetParent(parent, false);
            gateRoot.transform.localPosition = pos;

            // Wooden fence barrier posts
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.transform.SetParent(gateRoot.transform, false);
            postL.transform.localPosition = new Vector3(-2.5f, 1.5f, 0f);
            postL.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            postL.GetComponent<MeshRenderer>().material = woodMat;

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.transform.SetParent(gateRoot.transform, false);
            postR.transform.localPosition = new Vector3(2.5f, 1.5f, 0f);
            postR.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            postR.GetComponent<MeshRenderer>().material = woodMat;

            // Gate Barrier beam (disabled once Mido permits passage)
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "Gate_Barrier_Beam";
            barrier.transform.SetParent(gateRoot.transform, false);
            barrier.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            barrier.transform.localScale = new Vector3(5.2f, 0.35f, 0.45f);
            barrier.GetComponent<MeshRenderer>().material = woodMat;

            // Spawn Mido right in the center!
            GameObject mido = MidoGatekeeper.CreateMido(gateRoot.transform.position + new Vector3(0f, 0f, -0.6f), Quaternion.identity);
            mido.transform.SetParent(gateRoot.transform, true);
            mido.GetComponent<MidoGatekeeper>().SetGateBarrier(barrier.transform);
        }

        private static void BuildDekuBabaPath(Transform parent, Vector3 pos, Material barkMat, Material leafMat)
        {
            GameObject pathRoot = new GameObject("DekuBaba_Forest_Tunnel");
            pathRoot.transform.SetParent(parent, false);
            pathRoot.transform.localPosition = pos;

            // Guarding Deku Babas along the path!
            Vector3[] babaPositions = new Vector3[]
            {
                pathRoot.transform.position + new Vector3(-3.2f, 0f, 4f),
                pathRoot.transform.position + new Vector3(3.5f, 0f, 12f),
                pathRoot.transform.position + new Vector3(-2.8f, 0f, 20f)
            };

            foreach (var bPos in babaPositions)
            {
                DekuBabaAI.CreateDekuBaba(bPos).transform.SetParent(pathRoot.transform, true);
            }
        }

        private static void BuildGreatDekuTreeExterior(Transform parent, Vector3 pos, Material barkMat, Material leafMat)
        {
            GameObject treeRoot = new GameObject("The_Great_Deku_Tree_Exterior");
            treeRoot.transform.SetParent(parent, false);
            treeRoot.transform.localPosition = pos;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Colossal Trunk (Radius 16m, Height 35m)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "DekuTree_Colossal_Trunk";
            trunk.transform.SetParent(treeRoot.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 17.5f, 0f);
            trunk.transform.localScale = new Vector3(28f, 17.5f, 28f);
            trunk.GetComponent<MeshRenderer>().material = barkMat;

            // Majestic Leaf Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "DekuTree_Majestic_Canopy";
            canopy.transform.SetParent(treeRoot.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 36f, 0f);
            canopy.transform.localScale = new Vector3(45f, 18f, 45f);
            canopy.GetComponent<MeshRenderer>().material = leafMat;

            // Carved Wooden Face on South side
            // Eyes
            Material eyeMat = new Material(s);
            eyeMat.color = new Color(0.95f, 0.85f, 0.2f);
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", new Color(0.95f, 0.85f, 0.2f) * 1.5f);

            for (int e = -1; e <= 1; e += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = e < 0 ? "Deku_Eye_L" : "Deku_Eye_R";
                eye.transform.SetParent(treeRoot.transform, false);
                eye.transform.localPosition = new Vector3(e * 5.5f, 16.5f, -14.2f);
                eye.transform.localScale = new Vector3(2.5f, 1.8f, 1.2f);
                eye.GetComponent<MeshRenderer>().material = eyeMat;
            }

            // Mustache & Nose
            GameObject mustache = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mustache.name = "Deku_Mustache";
            mustache.transform.SetParent(treeRoot.transform, false);
            mustache.transform.localPosition = new Vector3(0f, 11.5f, -14.5f);
            mustache.transform.localScale = new Vector3(12f, 1.8f, 2.2f);
            mustache.GetComponent<MeshRenderer>().material = barkMat;

            // Hollow Entrance Mouth leading inside
            GameObject mouthHole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouthHole.name = "Deku_Mouth_Entrance";
            mouthHole.transform.SetParent(treeRoot.transform, false);
            mouthHole.transform.localPosition = new Vector3(0f, 4.5f, -14.5f);
            mouthHole.transform.localScale = new Vector3(8f, 8f, 3.5f);
            Material darkHollow = new Material(s) { color = Color.black };
            mouthHole.GetComponent<MeshRenderer>().material = darkHollow;
            mouthHole.GetComponent<Collider>().isTrigger = true;

            // Greeting Speech Trigger
            var speechTrigger = treeRoot.AddComponent<BoxCollider>();
            speechTrigger.isTrigger = true;
            speechTrigger.center = new Vector3(0f, 3f, -22f);
            speechTrigger.size = new Vector3(22f, 6f, 14f);

            treeRoot.AddComponent<DekuTreeGreetingTrigger>();
        }

        private static void SetupForestLighting(Transform parent)
        {
            GameObject sunObj = new GameObject("Forest_Sun_DirectionalLight");
            sunObj.transform.SetParent(parent, false);
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.82f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void SetupCamera(Transform player)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            var rig = mainCam.GetComponent<ZeldaCameraRig>();
            if (rig == null)
            {
                rig = mainCam.gameObject.AddComponent<ZeldaCameraRig>();
            }
            rig.SetTarget(player);
        }

        private static void SetupUI()
        {
            var existingCanvas = FindFirstObjectByType<UIDocument>();
            if (existingCanvas == null)
            {
                GameObject canvasObj = new GameObject("Zelda_UI_Toolkit_Canvas");
                var uiDoc = canvasObj.AddComponent<UIDocument>();
                var panelSettings = Resources.Load<PanelSettings>("UI/ZeldaPanelSettings")
                                  ?? ScriptableObject.CreateInstance<PanelSettings>();
                uiDoc.panelSettings = panelSettings;
                canvasObj.AddComponent<ZeldaHUDUI>();
                canvasObj.AddComponent<ZeldaDialogueBox>();
            }
            else
            {
                if (existingCanvas.GetComponent<ZeldaDialogueBox>() == null)
                {
                    existingCanvas.gameObject.AddComponent<ZeldaDialogueBox>();
                }
            }
        }
    }
}

