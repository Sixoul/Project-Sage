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
            // 9. THE FOREST GORGE & HOLLOW LOG TUNNELS (Z = 28 to 86)
            // =========================================================================
            BuildDekuBabaPath(worldRoot.transform, barkMat, leafMat, stoneMat, pathMat, grassMat, waterMat, woodMat);

            // =========================================================================
            // 10. THE GREAT DEKU TREE SACRED MEADOW & COLOSSAL EXTERIOR (Z = 86 to 145)
            // =========================================================================
            Vector3 dekuTreePos = new Vector3(0f, 0f, 115f);
            BuildGreatDekuTreeExterior(worldRoot.transform, dekuTreePos, barkMat, leafMat, stoneMat, waterMat);

            // =========================================================================
            // 11. DUNGEON INTERIOR: "INSIDE THE GREAT DEKU TREE" (Z = 115)
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
            // Spawns on the sunny village path in front of his treehouse, facing North towards the village green!
            Vector3 playerSpawn = treehousePos + new Vector3(0f, 0.5f, 12f);
            GameObject player = ZeldaPlayerBuilder.CreatePlayer(playerSpawn);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Ensure ZeldaInputReader exists immediately
            if (player.GetComponent<ZeldaInputReader>() == null)
            {
                player.AddComponent<ZeldaInputReader>();
            }

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
                float deg = a * Mathf.Rad2Deg;

                // Leave open gap on North side (towards Z = +45) for the Forest Path to the Great Deku Tree!
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 90f)) < 26f)
                {
                    continue;
                }

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
            porch.transform.localPosition = new Vector3(0f, 4.4f, 0.5f);
            porch.transform.localScale = new Vector3(8.5f, 0.4f, 8.5f);
            porch.GetComponent<MeshRenderer>().material = woodMat;

            // Authentic vertical wooden ladder down to the village path
            GameObject ladder = new GameObject("Treehouse_Ladder");
            ladder.transform.SetParent(houseRoot.transform, false);
            ladder.transform.localPosition = new Vector3(0f, 0f, 4.75f);

            for (int r = 0; r <= 8; r++)
            {
                float ry = r * 0.5f + 0.3f;
                GameObject rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rung.name = $"Rung_{r}";
                rung.transform.SetParent(ladder.transform, false);
                rung.transform.localPosition = new Vector3(0f, ry, 0f);
                rung.transform.localScale = new Vector3(1.2f, 0.08f, 0.12f);
                rung.GetComponent<MeshRenderer>().material = woodMat;
            }

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = side < 0 ? "Rail_L" : "Rail_R";
                rail.transform.SetParent(ladder.transform, false);
                rail.transform.localPosition = new Vector3(side * 0.65f, 2.2f, 0f);
                rail.transform.localScale = new Vector3(0.08f, 4.4f, 0.12f);
                rail.GetComponent<MeshRenderer>().material = woodMat;
            }

            // Cabin Walls with Open Front Doorway
            // South wall (back)
            GameObject wallSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallSouth.name = "Treehouse_Wall_South";
            wallSouth.transform.SetParent(houseRoot.transform, false);
            wallSouth.transform.localPosition = new Vector3(0f, 6.2f, -3.2f);
            wallSouth.transform.localScale = new Vector3(6.8f, 3.5f, 0.6f);
            wallSouth.GetComponent<MeshRenderer>().material = woodMat;

            // West wall (left)
            GameObject wallWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallWest.name = "Treehouse_Wall_West";
            wallWest.transform.SetParent(houseRoot.transform, false);
            wallWest.transform.localPosition = new Vector3(-3.4f, 6.2f, 0f);
            wallWest.transform.localScale = new Vector3(0.6f, 3.5f, 6.4f);
            wallWest.GetComponent<MeshRenderer>().material = woodMat;

            // East wall (right)
            GameObject wallEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallEast.name = "Treehouse_Wall_East";
            wallEast.transform.SetParent(houseRoot.transform, false);
            wallEast.transform.localPosition = new Vector3(3.4f, 6.2f, 0f);
            wallEast.transform.localScale = new Vector3(0.6f, 3.5f, 6.4f);
            wallEast.GetComponent<MeshRenderer>().material = woodMat;

            // North front wall with open doorway leading directly onto the porch
            GameObject wallNorthL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallNorthL.name = "Treehouse_Wall_North_L";
            wallNorthL.transform.SetParent(houseRoot.transform, false);
            wallNorthL.transform.localPosition = new Vector3(-2.2f, 6.2f, 3.2f);
            wallNorthL.transform.localScale = new Vector3(2.4f, 3.5f, 0.6f);
            wallNorthL.GetComponent<MeshRenderer>().material = woodMat;

            GameObject wallNorthR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallNorthR.name = "Treehouse_Wall_North_R";
            wallNorthR.transform.SetParent(houseRoot.transform, false);
            wallNorthR.transform.localPosition = new Vector3(2.2f, 6.2f, 3.2f);
            wallNorthR.transform.localScale = new Vector3(2.4f, 3.5f, 0.6f);
            wallNorthR.GetComponent<MeshRenderer>().material = woodMat;

            GameObject doorLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLintel.name = "Treehouse_Door_Lintel";
            doorLintel.transform.SetParent(houseRoot.transform, false);
            doorLintel.transform.localPosition = new Vector3(0f, 7.4f, 3.2f);
            doorLintel.transform.localScale = new Vector3(2.2f, 1.1f, 0.6f);
            doorLintel.GetComponent<MeshRenderer>().material = woodMat;

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

        private static GameObject BuildHollowLogTunnel(Transform parent, Vector3 center, float length, float radius, Material barkMat, Material woodMat)
        {
            GameObject tunnelRoot = new GameObject("Hollow_Log_Tunnel");
            tunnelRoot.transform.SetParent(parent, false);
            tunnelRoot.transform.localPosition = center;

            int segments = 12;
            float chord = 2f * radius * Mathf.Sin(Mathf.PI / segments) * 1.08f;
            float thickness = 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius + radius + 0.15f;

                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Log_Segment_{i}";
                seg.transform.SetParent(tunnelRoot.transform, false);
                seg.transform.localPosition = new Vector3(x, y, 0f);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg + 90f);
                seg.transform.localScale = new Vector3(chord, thickness, length);
                seg.GetComponent<MeshRenderer>().material = barkMat;
            }

            // Internal walking deck
            GameObject walkFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walkFloor.name = "Log_Walkway";
            walkFloor.transform.SetParent(tunnelRoot.transform, false);
            walkFloor.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            walkFloor.transform.localScale = new Vector3(radius * 1.4f, 0.25f, length);
            walkFloor.GetComponent<MeshRenderer>().material = woodMat;

            // Decorative root arches at entrance & exit
            for (int end = -1; end <= 1; end += 2)
            {
                GameObject rootArch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootArch.name = end < 0 ? "Log_Root_Arch_Front" : "Log_Root_Arch_Back";
                rootArch.transform.SetParent(tunnelRoot.transform, false);
                rootArch.transform.localPosition = new Vector3(0f, radius + 0.15f, end * (length * 0.5f));
                rootArch.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                rootArch.transform.localScale = new Vector3((radius + 0.6f) * 2f, 0.6f, (radius + 0.6f) * 2f);
                rootArch.GetComponent<MeshRenderer>().material = barkMat;
            }

            return tunnelRoot;
        }

        private static void BuildDekuBabaPath(Transform parent, Material barkMat, Material leafMat, Material stoneMat, Material pathMat, Material grassMat, Material waterMat, Material woodMat)
        {
            GameObject pathRoot = new GameObject("DekuBaba_Forest_Gorge");
            pathRoot.transform.SetParent(parent, false);

            // 1. Solid Ground Floor for entire gorge (Z = 28 to 86, length 58m, width 18m)
            GameObject pathFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pathFloor.name = "Gorge_Ground_Floor";
            pathFloor.transform.SetParent(pathRoot.transform, false);
            pathFloor.transform.localPosition = new Vector3(0f, -0.5f, 57f);
            pathFloor.transform.localScale = new Vector3(18f, 1f, 58f);
            pathFloor.GetComponent<MeshRenderer>().material = grassMat;

            // 2. High Mossy Cliff Canyon Walls on Left & Right
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject cliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cliff.name = side < 0 ? "Gorge_Cliff_West" : "Gorge_Cliff_East";
                cliff.transform.SetParent(pathRoot.transform, false);
                cliff.transform.localPosition = new Vector3(side * 8.5f, 4f, 57f);
                cliff.transform.localScale = new Vector3(2.5f, 8f, 58f);
                cliff.GetComponent<MeshRenderer>().material = stoneMat;

                // Towering trees planted along the cliff crest
                for (float z = 33f; z <= 81f; z += 12f)
                {
                    GameObject cliffTree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cliffTree.name = "Cliff_Canopy_Tree";
                    cliffTree.transform.SetParent(pathRoot.transform, false);
                    cliffTree.transform.localPosition = new Vector3(side * 9.5f, 10f, z);
                    cliffTree.transform.localScale = new Vector3(3.5f, 8f, 3.5f);
                    cliffTree.GetComponent<MeshRenderer>().material = barkMat;

                    GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    canopy.transform.SetParent(cliffTree.transform, false);
                    canopy.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                    canopy.transform.localScale = new Vector3(2.2f, 1.4f, 2.2f);
                    canopy.GetComponent<MeshRenderer>().material = leafMat;
                }
            }

            // 3. Hollow Log Tunnel 1 (Village exit -> Gorge entrance, Z = 30 to 42)
            BuildHollowLogTunnel(pathRoot.transform, new Vector3(0f, 0f, 36f), 12f, 3.2f, barkMat, woodMat);

            // 4. Winding Dirt Trail through the gorge (Z = 42 to 78)
            CreatePath(pathRoot.transform, new Vector3(0f, 0.02f, 45f), new Vector3(5f, 0.05f, 8f), pathMat);
            CreatePath(pathRoot.transform, new Vector3(-1.8f, 0.02f, 53f), new Vector3(5.5f, 0.05f, 9f), pathMat);
            CreatePath(pathRoot.transform, new Vector3(1.5f, 0.02f, 62f), new Vector3(5.5f, 0.05f, 10f), pathMat);
            CreatePath(pathRoot.transform, new Vector3(-1.0f, 0.02f, 71f), new Vector3(5f, 0.05f, 9f), pathMat);
            CreatePath(pathRoot.transform, new Vector3(0f, 0.02f, 77f), new Vector3(5f, 0.05f, 6f), pathMat);

            // 5. Trickling Forest Brook & Wooden Footbridge at Z = 59
            GameObject brook = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brook.name = "Gorge_Forest_Brook";
            brook.transform.SetParent(pathRoot.transform, false);
            brook.transform.localPosition = new Vector3(0f, 0.05f, 59f);
            brook.transform.localScale = new Vector3(14f, 0.15f, 3.5f);
            brook.GetComponent<MeshRenderer>().material = waterMat;

            // Wooden footbridge planks across brook
            GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "Gorge_Log_Bridge";
            bridge.transform.SetParent(pathRoot.transform, false);
            bridge.transform.localPosition = new Vector3(0.5f, 0.22f, 59f);
            bridge.transform.localScale = new Vector3(4.2f, 0.2f, 4.2f);
            bridge.GetComponent<MeshRenderer>().material = woodMat;

            // 6. Guarding Deku Babas (4 encounters along the winding path)
            Vector3[] babaPositions = new Vector3[]
            {
                new Vector3(-2.8f, 0f, 49f),
                new Vector3(3.2f, 0f, 54f),
                new Vector3(2.6f, 0f, 65f),
                new Vector3(-2.8f, 0f, 72f)
            };

            foreach (var bPos in babaPositions)
            {
                DekuBabaAI.CreateDekuBaba(bPos).transform.SetParent(pathRoot.transform, true);
            }

            // 7. Cuttable Grass Clumps with hidden Rupees along gorge trail
            Vector3[] grassLocs = new Vector3[]
            {
                new Vector3(-3.8f, 0f, 46f),
                new Vector3(3.6f, 0f, 51f),
                new Vector3(-3.5f, 0f, 63f),
                new Vector3(3.8f, 0f, 68f),
                new Vector3(-3.2f, 0f, 75f)
            };

            foreach (var gLoc in grassLocs)
            {
                GrassPatch.CreateGrassPatch(gLoc, grassMat).transform.SetParent(pathRoot.transform, true);
            }

            // 8. Hollow Log Tunnel 2 (Gorge exit -> Sacred Meadow, Z = 80 to 92)
            BuildHollowLogTunnel(pathRoot.transform, new Vector3(0f, 0f, 86f), 12f, 3.4f, barkMat, woodMat);
        }

        private static void BuildGreatDekuTreeExterior(Transform parent, Vector3 pos, Material barkMat, Material leafMat, Material stoneMat, Material waterMat)
        {
            GameObject clearingRoot = new GameObject("The_Great_Deku_Tree_Clearing");
            clearingRoot.transform.SetParent(parent, false);
            clearingRoot.transform.localPosition = pos; // pos = (0, 0, 115)

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Lush Green Meadow Floor (radius 30m, centered at pos)
            GameObject meadowFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meadowFloor.name = "DekuTree_Meadow_Floor";
            meadowFloor.transform.SetParent(clearingRoot.transform, false);
            meadowFloor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            meadowFloor.transform.localScale = new Vector3(64f, 1f, 64f);
            meadowFloor.GetComponent<MeshRenderer>().material = new Material(s) { color = new Color(0.18f, 0.44f, 0.16f) };

            // 2. Circular Ring of Colossal Ancient Trees enclosing the Sacred Meadow (Radius 28m)
            int clearingTreeCount = 20;
            for (int i = 0; i < clearingTreeCount; i++)
            {
                float a = i * Mathf.PI * 2f / clearingTreeCount;
                float deg = a * Mathf.Rad2Deg;

                // Leave open gap on South side (around -90 degrees, Z = -28) where Tunnel 2 enters!
                if (Mathf.Abs(Mathf.DeltaAngle(deg, -90f)) < 26f)
                {
                    continue;
                }

                Vector3 p = new Vector3(Mathf.Cos(a) * 28f, 0f, Mathf.Sin(a) * 28f);

                GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = $"Sacred_Grove_Tree_{i}";
                tree.transform.SetParent(clearingRoot.transform, false);
                tree.transform.localPosition = p + Vector3.up * 10f;
                tree.transform.localScale = new Vector3(5.5f, 10f, 5.5f);
                tree.GetComponent<MeshRenderer>().material = barkMat;

                GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = "Tree_Canopy";
                canopy.transform.SetParent(tree.transform, false);
                canopy.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                canopy.transform.localScale = new Vector3(2.4f, 1.5f, 2.4f);
                canopy.GetComponent<MeshRenderer>().material = leafMat;
            }

            // 3. Stone Flagstone Approach Walkway (From Tunnel 2 exit at Z = -23 to Deku Tree base at Z = -14)
            GameObject walkway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walkway.name = "Sacred_Flagstone_Walkway";
            walkway.transform.SetParent(clearingRoot.transform, false);
            walkway.transform.localPosition = new Vector3(0f, 0.05f, -18.5f);
            walkway.transform.localScale = new Vector3(6f, 0.1f, 11f);
            walkway.GetComponent<MeshRenderer>().material = stoneMat;

            // 4. Two Sacred Reflection Pools on Left & Right
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pool.name = side < 0 ? "Reflection_Pool_West" : "Reflection_Pool_East";
                pool.transform.SetParent(clearingRoot.transform, false);
                pool.transform.localPosition = new Vector3(side * 14f, 0.05f, -16f);
                pool.transform.localScale = new Vector3(11f, 0.15f, 11f);
                pool.GetComponent<MeshRenderer>().material = waterMat;

                // Pool ambient fairy light
                GameObject pLight = new GameObject("Pool_Fairy_Light");
                pLight.transform.SetParent(pool.transform, false);
                pLight.transform.localPosition = new Vector3(0f, 3f, 0f);
                Light pl = pLight.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(0.3f, 0.85f, 1f);
                pl.intensity = 2.5f;
                pl.range = 8f;
            }

            // 5. Floating Fae Fireflies in the Sacred Meadow
            Vector3[] fairyMotes = new Vector3[]
            {
                new Vector3(-6f, 1.8f, -12f),
                new Vector3(6f, 2.2f, -14f),
                new Vector3(-8f, 1.5f, -22f),
                new Vector3(7f, 2.0f, -20f)
            };

            for (int f = 0; f < fairyMotes.Length; f++)
            {
                GameObject mote = new GameObject($"Forest_Fae_Mote_{f}");
                mote.transform.SetParent(clearingRoot.transform, false);
                mote.transform.localPosition = fairyMotes[f];
                Light ml = mote.AddComponent<Light>();
                ml.type = LightType.Point;
                ml.color = new Color(1f, 0.9f, 0.4f);
                ml.intensity = 2f;
                ml.range = 5f;
            }

            // =========================================================================
            // 6. THE COLOSSAL GREAT DEKU TREE
            // =========================================================================
            // Colossal Trunk (Radius 14m, Height 35m)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "DekuTree_Colossal_Trunk";
            trunk.transform.SetParent(clearingRoot.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 17.5f, 0f);
            trunk.transform.localScale = new Vector3(28f, 17.5f, 28f);
            trunk.GetComponent<MeshRenderer>().material = barkMat;

            // Majestic Leaf Canopy
            GameObject canopyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopyObj.name = "DekuTree_Majestic_Canopy";
            canopyObj.transform.SetParent(clearingRoot.transform, false);
            canopyObj.transform.localPosition = new Vector3(0f, 37f, 0f);
            canopyObj.transform.localScale = new Vector3(48f, 18f, 48f);
            canopyObj.GetComponent<MeshRenderer>().material = leafMat;

            // Carved Wooden Face on South side (Z = -14.2f)
            // Eyes
            Material eyeMat = new Material(s);
            eyeMat.color = new Color(0.95f, 0.85f, 0.2f);
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", new Color(0.95f, 0.85f, 0.2f) * 2f);

            for (int e = -1; e <= 1; e += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = e < 0 ? "Deku_Eye_L" : "Deku_Eye_R";
                eye.transform.SetParent(clearingRoot.transform, false);
                eye.transform.localPosition = new Vector3(e * 5.5f, 16.5f, -14.2f);
                eye.transform.localScale = new Vector3(2.5f, 1.8f, 1.2f);
                eye.GetComponent<MeshRenderer>().material = eyeMat;
            }

            // Mustache & Nose
            GameObject mustache = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mustache.name = "Deku_Mustache";
            mustache.transform.SetParent(clearingRoot.transform, false);
            mustache.transform.localPosition = new Vector3(0f, 11.5f, -14.5f);
            mustache.transform.localScale = new Vector3(12f, 1.8f, 2.2f);
            mustache.GetComponent<MeshRenderer>().material = barkMat;

            // Colossal Hollow Mouth Entrance leading directly inside to 1F!
            GameObject mouthHole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouthHole.name = "Deku_Mouth_Entrance";
            mouthHole.transform.SetParent(clearingRoot.transform, false);
            mouthHole.transform.localPosition = new Vector3(0f, 4.5f, -14.2f);
            mouthHole.transform.localScale = new Vector3(7.5f, 7.5f, 2.5f);
            Material darkHollow = new Material(s) { color = new Color(0.08f, 0.05f, 0.03f) };
            mouthHole.GetComponent<MeshRenderer>().material = darkHollow;
            mouthHole.GetComponent<Collider>().isTrigger = true;

            // Greeting Speech Trigger (Z = -20f covers the approach path)
            var speechTrigger = clearingRoot.AddComponent<BoxCollider>();
            speechTrigger.isTrigger = true;
            speechTrigger.center = new Vector3(0f, 3f, -20f);
            speechTrigger.size = new Vector3(18f, 6f, 14f);

            clearingRoot.AddComponent<DekuTreeGreetingTrigger>();
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
            mainCam.transform.position = player.position + new Vector3(0f, 1.8f, -4.5f);
            mainCam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
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

