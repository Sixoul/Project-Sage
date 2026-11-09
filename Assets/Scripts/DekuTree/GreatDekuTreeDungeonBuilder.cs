using System.Collections.Generic;
using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.Kokiri;
using ZeldaOoT.VFX;

namespace ZeldaOoT.DekuTree
{
    /// <summary>
    /// Procedurally constructs the complete multi-floor dungeon:
    /// "Inside the Great Deku Tree" (1F, 2F, 3F, B1, B2, and Boss Chamber).
    /// Faithfully implements all authentic Ocarina of Time puzzles, rooms, and hazards.
    /// </summary>
    public static class GreatDekuTreeDungeonBuilder
    {
        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        public static GameObject BuildDungeon(Vector3 origin)
        {
            GameObject dungeonRoot = new GameObject("Dungeon_Inside_Great_Deku_Tree");
            dungeonRoot.transform.position = origin;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material barkMat = new Material(s) { color = new Color(0.38f, 0.24f, 0.12f) };
            Material woodMat = new Material(s) { color = new Color(0.52f, 0.35f, 0.18f) };
            Material stoneMat = new Material(s) { color = new Color(0.45f, 0.45f, 0.48f) };
            Material metalMat = new Material(s) { color = new Color(0.28f, 0.28f, 0.32f) };
            Material vineMat = new Material(s) { color = new Color(0.18f, 0.52f, 0.16f) };
            Material sandMat = new Material(s) { color = new Color(0.85f, 0.78f, 0.55f) };

            // URP Translucent Water Material
            Material waterMat = new Material(s);
            waterMat.color = new Color(0.12f, 0.65f, 0.95f, 0.62f);
            waterMat.SetFloat("_Surface", 1f);
            waterMat.SetFloat("_Blend", 0f);
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // =========================================================================
            // 1. FLOOR 1F: MAIN TRUNK ROOM
            // =========================================================================
            float trunkRadius = 14f;
            float trunkHeight = 24f;

            // Hollow outer trunk cylinder walls (segmented)
            int wallSegments = 16;
            for (int i = 0; i < wallSegments; i++)
            {
                // Leave opening on South side for entrance tunnel
                if (i == 12 || i == 13) continue;

                float a = i * Mathf.PI * 2f / wallSegments;
                Vector3 wallP = new Vector3(Mathf.Cos(a) * trunkRadius, trunkHeight * 0.5f, Mathf.Sin(a) * trunkRadius);
                GameObject wallPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallPiece.name = $"Trunk_Wall_{i}";
                wallPiece.transform.SetParent(dungeonRoot.transform, false);
                wallPiece.transform.localPosition = wallP;
                wallPiece.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                wallPiece.transform.localScale = new Vector3(6f, trunkHeight, 1.8f);
                wallPiece.GetComponent<MeshRenderer>().material = barkMat;
            }

            // 1F Outer Rim Floor
            GameObject floor1F = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor1F.name = "Floor_1F_Rim";
            floor1F.transform.SetParent(dungeonRoot.transform, false);
            floor1F.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            floor1F.transform.localScale = new Vector3(trunkRadius * 2f, 0.5f, trunkRadius * 2f);
            floor1F.GetComponent<MeshRenderer>().material = woodMat;

            // Giant Spider Web covering the center hole of 1F leading to B1
            GameObject giantFloorWeb = SpiderWebObstacle.CreateWeb(
                dungeonRoot.transform.position + new Vector3(0f, 0.1f, 0f),
                new Vector3(10f, 10f, 1f),
                Quaternion.Euler(90f, 0f, 0f),
                true // Is Floor Web (breaks on terminal dive!)
            );
            giantFloorWeb.transform.SetParent(dungeonRoot.transform, true);

            // Climbable Vines on 1F walls with Skullwalltulas
            Vector3 vine1P = dungeonRoot.transform.position + new Vector3(-trunkRadius + 0.8f, 5.5f, 0f);
            GameObject vine1 = ClimbableVine.CreateVine(vine1P, new Vector3(3.5f, 9f, 1f), Quaternion.Euler(0f, 90f, 0f), origin.y + 9.5f);
            vine1.transform.SetParent(dungeonRoot.transform, true);

            // Skullwalltula guarding the vine
            GameObject skull1 = SkullwalltulaAI.CreateSkullwalltula(vine1P + new Vector3(0.3f, 1.5f, 0f), Quaternion.Euler(0f, 90f, 0f));
            skull1.transform.SetParent(dungeonRoot.transform, true);

            // Wooden ladder on East wall up to 2F
            BuildLadder(dungeonRoot.transform, new Vector3(trunkRadius - 1.2f, 0f, 0f), 8.5f, woodMat);

            // =========================================================================
            // 2. FLOOR 2F: SPIRAL BALCONIES & MAP ROOM (EAST)
            // =========================================================================
            float f2Height = 8.5f;

            // 2F Circular Balcony Ring
            BuildBalconyRing(dungeonRoot.transform, f2Height, trunkRadius, 3.2f, woodMat);

            // 2F East: Doorway & Map Room
            Vector3 mapRoomOrigin = new Vector3(trunkRadius + 14f, f2Height, 0f);
            BuildMapRoom(dungeonRoot.transform, mapRoomOrigin, woodMat, metalMat, stoneMat);

            // =========================================================================
            // 3. FLOOR 3F: SLINGSHOT ROOM & HIGH DIVE LEDGE
            // =========================================================================
            float f3Height = 16.5f;

            // 3F Circular Balcony Ring
            BuildBalconyRing(dungeonRoot.transform, f3Height, trunkRadius, 3.0f, woodMat);

            // 3F West: Slingshot Room
            Vector3 slingshotRoomOrigin = new Vector3(-trunkRadius - 14f, f3Height, 0f);
            BuildSlingshotRoom(dungeonRoot.transform, slingshotRoomOrigin, woodMat, metalMat, stoneMat);

            // The High Dive Diving Board Platform (extending right over center 1F web)
            GameObject diveBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diveBoard.name = "High_Dive_Ledge";
            diveBoard.transform.SetParent(dungeonRoot.transform, false);
            diveBoard.transform.localPosition = new Vector3(0f, f3Height, 5.5f);
            diveBoard.transform.localScale = new Vector3(3.5f, 0.5f, 6.5f);
            diveBoard.GetComponent<MeshRenderer>().material = woodMat;

            // =========================================================================
            // 4. BASEMENT 1 (B1): FLOODED WATER CAVERN & TORCH PUZZLE
            // =========================================================================
            float b1Depth = -9.5f;
            Vector3 b1Center = new Vector3(0f, b1Depth, 0f);
            BuildBasement1(dungeonRoot.transform, b1Center, trunkRadius, waterMat, stoneMat, sandMat, woodMat);

            // =========================================================================
            // 5. BASEMENT 2 (B2): THE 2-3-1 DEKU SCRUB PUZZLE & BOSS DOOR
            // =========================================================================
            Vector3 b2Center = new Vector3(0f, b1Depth - 4.5f, 32f);
            BuildBasement2(dungeonRoot.transform, b2Center, stoneMat, metalMat, woodMat);

            // =========================================================================
            // 6. BOSS CHAMBER: QUEEN GOHMA'S LAIR
            // =========================================================================
            Vector3 bossChamberCenter = new Vector3(0f, b1Depth - 4.5f, 72f);
            BuildBossChamber(dungeonRoot.transform, bossChamberCenter, stoneMat, barkMat);

            return dungeonRoot;
        }

        private static void BuildBalconyRing(Transform parent, float height, float radius, float width, Material mat)
        {
            int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 p = new Vector3(Mathf.Cos(a) * (radius - width * 0.5f), height, Mathf.Sin(a) * (radius - width * 0.5f));
                GameObject balcony = GameObject.CreatePrimitive(PrimitiveType.Cube);
                balcony.name = $"Balcony_{height}_{i}";
                balcony.transform.SetParent(parent, false);
                balcony.transform.localPosition = p;
                balcony.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                balcony.transform.localScale = new Vector3(5.5f, 0.45f, width);
                balcony.GetComponent<MeshRenderer>().material = mat;
            }
        }

        private static void BuildLadder(Transform parent, Vector3 basePos, float height, Material mat)
        {
            GameObject ladderRoot = new GameObject("Wooden_Ladder");
            ladderRoot.transform.SetParent(parent, false);
            ladderRoot.transform.localPosition = basePos;

            int rungs = Mathf.RoundToInt(height / 0.6f);
            for (int r = 0; r < rungs; r++)
            {
                GameObject rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rung.name = $"Rung_{r}";
                rung.transform.SetParent(ladderRoot.transform, false);
                rung.transform.localPosition = new Vector3(0f, r * 0.6f + 0.3f, 0f);
                rung.transform.localScale = new Vector3(1.2f, 0.12f, 0.15f);
                rung.GetComponent<MeshRenderer>().material = mat;
            }

            // Climbable vine trigger along ladder height
            GameObject climbZone = GameObject.CreatePrimitive(PrimitiveType.Quad);
            climbZone.name = "Ladder_Climb_Trigger";
            climbZone.transform.SetParent(ladderRoot.transform, false);
            climbZone.transform.localPosition = new Vector3(0f, height * 0.5f, 0.1f);
            climbZone.transform.localScale = new Vector3(1.4f, height, 1f);
            climbZone.GetComponent<MeshRenderer>().enabled = false;
            var col = climbZone.GetComponent<Collider>();
            col.isTrigger = true;
            climbZone.AddComponent<ClimbableVine>();
        }

        private static void BuildMapRoom(Transform parent, Vector3 center, Material woodMat, Material metalMat, Material stoneMat)
        {
            GameObject room = new GameObject("Room_2F_Map");
            room.transform.SetParent(parent, false);
            room.transform.localPosition = center;

            // Floor
            GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fl.transform.SetParent(room.transform, false);
            fl.transform.localScale = new Vector3(16f, 0.6f, 16f);
            fl.GetComponent<MeshRenderer>().material = woodMat;

            // Walls
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wallZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallZ.transform.SetParent(room.transform, false);
                wallZ.transform.localPosition = new Vector3(0f, 3.5f, w * 8f);
                wallZ.transform.localScale = new Vector3(16f, 7f, 1.2f);
                wallZ.GetComponent<MeshRenderer>().material = woodMat;

                GameObject wallX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallX.transform.SetParent(room.transform, false);
                wallX.transform.localPosition = new Vector3(w * 8f, 3.5f, 0f);
                wallX.transform.localScale = new Vector3(1.2f, 7f, 16f);
                wallX.GetComponent<MeshRenderer>().material = woodMat;
            }

            // Treasure Chest with Dungeon Map!
            Vector3 chestP = room.transform.position + new Vector3(4.5f, 0.3f, 0f);
            OoTChest.CreateChest(chestP, Quaternion.Euler(0f, -90f, 0f), "dungeon_map", "Dungeon Map", "You got the Dungeon Map! Press M to view the layout of the Great Deku Tree!", Color.yellow).transform.SetParent(room.transform, true);
        }

        private static void BuildSlingshotRoom(Transform parent, Vector3 center, Material woodMat, Material metalMat, Material stoneMat)
        {
            GameObject room = new GameObject("Room_3F_Slingshot");
            room.transform.SetParent(parent, false);
            room.transform.localPosition = center;

            // Floor
            GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fl.transform.SetParent(room.transform, false);
            fl.transform.localScale = new Vector3(18f, 0.6f, 16f);
            fl.GetComponent<MeshRenderer>().material = woodMat;

            // Walls
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wallZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallZ.transform.SetParent(room.transform, false);
                wallZ.transform.localPosition = new Vector3(0f, 3.5f, w * 8f);
                wallZ.transform.localScale = new Vector3(18f, 7f, 1.2f);
                wallZ.GetComponent<MeshRenderer>().material = woodMat;
            }

            // Door Bars blocking exit until Eye Switch is shot
            GameObject bars = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bars.name = "SlingshotRoom_DoorBars";
            bars.transform.SetParent(room.transform, false);
            bars.transform.localPosition = new Vector3(8.5f, 2.5f, 0f);
            bars.transform.localScale = new Vector3(0.5f, 5f, 4f);
            bars.GetComponent<MeshRenderer>().material = metalMat;

            // Eye Switch mounted above opposite wall
            Vector3 eyeP = room.transform.position + new Vector3(-8.2f, 4.5f, 0f);
            EyeSwitch.CreateEyeSwitch(eyeP, Quaternion.Euler(0f, 90f, 0f), bars, null).transform.SetParent(room.transform, true);

            // Treasure Chest containing the Fairy Slingshot!
            Vector3 chestP = room.transform.position + new Vector3(-4.5f, 0.3f, 0f);
            OoTChest.CreateChest(chestP, Quaternion.Euler(0f, 90f, 0f), "fairy_slingshot", "Fairy Slingshot", "You found the Fairy Slingshot! Use Deku Seeds as ammunition to hit targets and switches from afar!", new Color(0.85f, 0.75f, 0.2f)).transform.SetParent(room.transform, true);
        }

        private static void BuildBasement1(Transform parent, Vector3 center, float radius, Material waterMat, Material stoneMat, Material sandMat, Material woodMat)
        {
            GameObject b1Root = new GameObject("Basement_B1_FloodedCavern");
            b1Root.transform.SetParent(parent, false);
            b1Root.transform.localPosition = center;

            // Deep stone cavern walls
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                // Leave opening on North side leading to B2 corridor
                if (i == 3 || i == 4) continue;

                float a = i * Mathf.PI * 2f / segments;
                Vector3 wallP = new Vector3(Mathf.Cos(a) * radius, 4.5f, Mathf.Sin(a) * radius);
                GameObject wallPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallPiece.name = $"B1_Cavern_Wall_{i}";
                wallPiece.transform.SetParent(b1Root.transform, false);
                wallPiece.transform.localPosition = wallP;
                wallPiece.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                wallPiece.transform.localScale = new Vector3(6f, 10f, 2f);
                wallPiece.GetComponent<MeshRenderer>().material = stoneMat;
            }

            // Water Pool Basin (Where Link lands after breaking 1F giant web!)
            GameObject waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterSurface.name = "B1_Water_Pool";
            waterSurface.transform.SetParent(b1Root.transform, false);
            waterSurface.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            waterSurface.transform.localScale = new Vector3(radius * 1.8f, 0.1f, radius * 1.8f);
            waterSurface.GetComponent<MeshRenderer>().material = waterMat;
            SafeDestroy(waterSurface.GetComponent<Collider>());

            // Sandy bottom
            GameObject sandFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandFloor.name = "B1_Sand_Floor";
            sandFloor.transform.SetParent(b1Root.transform, false);
            sandFloor.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            sandFloor.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            sandFloor.GetComponent<MeshRenderer>().material = sandMat;

            // Central landing platform island
            GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            island.name = "B1_Landing_Island";
            island.transform.SetParent(b1Root.transform, false);
            island.transform.localPosition = new Vector3(0f, 0.65f, -2.5f);
            island.transform.localScale = new Vector3(5.5f, 0.45f, 5.5f);
            island.GetComponent<MeshRenderer>().material = stoneMat;

            // Vines on South wall to climb back up to 1F
            Vector3 vineBackP = b1Root.transform.position + new Vector3(0f, 5.5f, -radius + 1.2f);
            ClimbableVine.CreateVine(vineBackP, new Vector3(3.5f, 9.5f, 1f), Quaternion.Euler(0f, 0f, 0f), 0f).transform.SetParent(b1Root.transform, true);

            // Lit Torch (Fire source for Deku Stick!)
            Vector3 litTorchP = b1Root.transform.position + new Vector3(-4.5f, 0.65f, 0f);
            TorchSwitchPuzzle.CreateTorch(litTorchP, true, null).transform.SetParent(b1Root.transform, true);

            // Unlit Torch across water on North platform
            Vector3 unlitTorchP = b1Root.transform.position + new Vector3(4.5f, 0.65f, 4.5f);
            GameObject doorBarsToB2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorBarsToB2.name = "DoorBars_B1_To_B2";
            doorBarsToB2.transform.SetParent(b1Root.transform, false);
            doorBarsToB2.transform.localPosition = new Vector3(0f, 1.8f, radius - 0.5f);
            doorBarsToB2.transform.localScale = new Vector3(4f, 4.5f, 0.5f);
            doorBarsToB2.GetComponent<MeshRenderer>().material = stoneMat;

            TorchSwitchPuzzle.CreateTorch(unlitTorchP, false, doorBarsToB2).transform.SetParent(b1Root.transform, true);

            // Spider Web blocking North passage
            Vector3 webDoorP = b1Root.transform.position + new Vector3(0f, 1.8f, radius + 2f);
            SpiderWebObstacle.CreateWeb(webDoorP, new Vector3(4f, 4f, 1f), Quaternion.Euler(0f, 0f, 0f), false).transform.SetParent(b1Root.transform, true);
        }

        private static void BuildBasement2(Transform parent, Vector3 center, Material stoneMat, Material metalMat, Material woodMat)
        {
            GameObject b2Root = new GameObject("Basement_B2_ScrubCavern");
            b2Root.transform.SetParent(parent, false);
            b2Root.transform.localPosition = center;

            // Room Floor
            GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fl.name = "B2_Floor";
            fl.transform.SetParent(b2Root.transform, false);
            fl.transform.localScale = new Vector3(22f, 0.8f, 26f);
            fl.GetComponent<MeshRenderer>().material = stoneMat;

            // Walls
            for (int w = -1; w <= 1; w += 2)
            {
                GameObject wallX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallX.transform.SetParent(b2Root.transform, false);
                wallX.transform.localPosition = new Vector3(w * 11f, 4f, 0f);
                wallX.transform.localScale = new Vector3(1.5f, 8f, 26f);
                wallX.GetComponent<MeshRenderer>().material = stoneMat;
            }

            // Massive Boss Door at North end (Z = 13f)
            GameObject bossDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bossDoor.name = "Boss_Door_CarvedWood";
            bossDoor.transform.SetParent(b2Root.transform, false);
            bossDoor.transform.localPosition = new Vector3(0f, 3.2f, 13f);
            bossDoor.transform.localScale = new Vector3(6f, 6.5f, 0.8f);
            Material doorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.32f, 0.18f, 0.08f)
            };
            bossDoor.GetComponent<MeshRenderer>().material = doorMat;

            // Iron Bars locking Boss Door
            GameObject bossDoorBars = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bossDoorBars.name = "Boss_Door_IronBars";
            bossDoorBars.transform.SetParent(bossDoor.transform, false);
            bossDoorBars.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            bossDoorBars.transform.localScale = new Vector3(1.1f, 0.95f, 0.4f);
            bossDoorBars.GetComponent<MeshRenderer>().material = metalMat;

            // 2-3-1 Deku Scrub Order Puzzle Manager
            var puzzleManager = b2Root.AddComponent<DekuScrubOrderPuzzle>();
            puzzleManager.SetBossDoorBars(bossDoorBars);

            // The 3 Deku Scrubs:
            // Scrub 1 (Left, Number 1)
            Vector3 p1 = b2Root.transform.position + new Vector3(-6f, 0.4f, 4f);
            GameObject s1 = DekuScrubAI.CreateScrub(p1, 1, puzzleManager);
            s1.transform.SetParent(b2Root.transform, true);
            puzzleManager.RegisterScrub(s1.GetComponent<DekuScrubAI>());

            // Scrub 2 (Middle, Number 2 - First in order!)
            Vector3 p2 = b2Root.transform.position + new Vector3(0f, 0.4f, 6f);
            GameObject s2 = DekuScrubAI.CreateScrub(p2, 2, puzzleManager);
            s2.transform.SetParent(b2Root.transform, true);
            puzzleManager.RegisterScrub(s2.GetComponent<DekuScrubAI>());

            // Scrub 3 (Right, Number 3 - Second in order!)
            Vector3 p3 = b2Root.transform.position + new Vector3(6f, 0.4f, 4f);
            GameObject s3 = DekuScrubAI.CreateScrub(p3, 3, puzzleManager);
            s3.transform.SetParent(b2Root.transform, true);
            puzzleManager.RegisterScrub(s3.GetComponent<DekuScrubAI>());
        }

        private static void BuildBossChamber(Transform parent, Vector3 center, Material stoneMat, Material barkMat)
        {
            GameObject bossRoot = new GameObject("Boss_Chamber_QueenGohma");
            bossRoot.transform.SetParent(parent, false);
            bossRoot.transform.localPosition = center;

            float chamberRadius = 18f;
            float chamberHeight = 16f;

            // Dark circular stone arena floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Boss_Arena_Floor";
            floor.transform.SetParent(bossRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            floor.transform.localScale = new Vector3(chamberRadius * 2f, 0.4f, chamberRadius * 2f);
            floor.GetComponent<MeshRenderer>().material = stoneMat;

            // Stone / Root Pillars
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 12f, chamberHeight * 0.5f, Mathf.Sin(a) * 12f);
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Boss_Pillar_{i + 1}";
                pillar.transform.SetParent(bossRoot.transform, false);
                pillar.transform.localPosition = p;
                pillar.transform.localScale = new Vector3(2.2f, chamberHeight * 0.5f, 2.2f);
                pillar.GetComponent<MeshRenderer>().material = barkMat;
            }

            // Circular walls
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                if (i == 12) continue; // Entrance from B2

                float a = i * Mathf.PI * 2f / segments;
                Vector3 wallP = new Vector3(Mathf.Cos(a) * chamberRadius, chamberHeight * 0.5f, Mathf.Sin(a) * chamberRadius);
                GameObject wallPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallPiece.name = $"Boss_Wall_{i}";
                wallPiece.transform.SetParent(bossRoot.transform, false);
                wallPiece.transform.localPosition = wallP;
                wallPiece.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                wallPiece.transform.localScale = new Vector3(7.5f, chamberHeight, 2.5f);
                wallPiece.GetComponent<MeshRenderer>().material = stoneMat;
            }

            // Ceiling (Webbed dome)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ceiling.name = "Boss_Ceiling_Webs";
            ceiling.transform.SetParent(bossRoot.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, chamberHeight, 0f);
            ceiling.transform.localScale = new Vector3(chamberRadius * 2f, 0.5f, chamberRadius * 2f);
            ceiling.GetComponent<MeshRenderer>().material = barkMat;

            // Spawn Queen Gohma in the center of the chamber!
            Vector3 gohmaP = bossRoot.transform.position + new Vector3(0f, 0.2f, 2.5f);
            GameObject gohma = QueenGohmaBossAI.CreateGohma(gohmaP);
            gohma.transform.SetParent(bossRoot.transform, true);
        }
    }
}
