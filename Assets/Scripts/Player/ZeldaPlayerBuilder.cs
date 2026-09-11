using UnityEngine;
using ZeldaOoT.Combat;
using ZeldaOoT.Water;
using ZeldaOoT.Inventory;
using ZeldaOoT.Items;

namespace ZeldaOoT.Player
{
    /// <summary>
    /// Constructs a high-detail stylized Link character model:
    /// anime face with blue eyes, pointed elf ears, flowing blonde hair,
    /// iconic hero cap with floppy tail, V-neck tunic, leather baldric,
    /// archer bracers, boots with cuffs, and back scabbard.
    /// </summary>
    public static class ZeldaPlayerBuilder
    {
        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        public static GameObject CreatePlayer(Vector3 spawnPosition)
        {
            GameObject player = new GameObject("Player_Link");
            player.tag = "Player";
            player.transform.position = spawnPosition;

            // Character Controller
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;

            // URP Shaders & Materials
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material skinMat = new Material(urpLit) { color = new Color(1.0f, 0.86f, 0.74f) };
            skinMat.SetFloat("_Smoothness", 0.4f);

            Material tunicMat = new Material(urpLit) { color = new Color(0.12f, 0.55f, 0.2f) }; // Kokiri Forest Green
            tunicMat.SetFloat("_Smoothness", 0.35f);

            Material whiteClothMat = new Material(urpLit) { color = new Color(0.94f, 0.94f, 0.92f) };
            whiteClothMat.SetFloat("_Smoothness", 0.3f);

            Material leatherMat = new Material(urpLit) { color = new Color(0.38f, 0.22f, 0.1f) }; // Brown leather
            leatherMat.SetFloat("_Smoothness", 0.5f);

            Material goldMat = new Material(urpLit) { color = new Color(1.0f, 0.82f, 0.15f) };
            goldMat.SetFloat("_Metallic", 0.85f);
            goldMat.SetFloat("_Smoothness", 0.85f);

            Material steelMat = new Material(urpLit) { color = new Color(0.88f, 0.9f, 0.92f) };
            steelMat.SetFloat("_Metallic", 0.92f);
            steelMat.SetFloat("_Smoothness", 0.9f);

            Material hairMat = new Material(urpLit) { color = new Color(0.96f, 0.84f, 0.28f) }; // Golden blonde
            hairMat.SetFloat("_Smoothness", 0.45f);

            Material blueEyeMat = new Material(urpLit) { color = new Color(0.12f, 0.45f, 0.85f) };
            Material royalBlueMat = new Material(urpLit) { color = new Color(0.1f, 0.25f, 0.7f) }; // Royal blue sheath

            // Model Root
            GameObject modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.SetParent(player.transform, false);

            // ==================== 1. TORSO & OUTFIT ====================
            // Main Tunic Torso
            GameObject tunic = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tunic.name = "Tunic_Torso";
            tunic.transform.SetParent(modelRoot.transform, false);
            tunic.transform.localPosition = new Vector3(0f, 0.96f, 0f);
            tunic.transform.localScale = new Vector3(0.52f, 0.44f, 0.38f);
            SafeDestroy(tunic.GetComponent<Collider>());
            tunic.GetComponent<MeshRenderer>().material = tunicMat;

            // White V-Neck Undershirt Collar
            GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collar.name = "Undershirt_Collar";
            collar.transform.SetParent(tunic.transform, false);
            collar.transform.localPosition = new Vector3(0f, 0.7f, 0.42f);
            collar.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            collar.transform.localScale = new Vector3(0.38f, 0.32f, 0.25f);
            SafeDestroy(collar.GetComponent<Collider>());
            collar.GetComponent<MeshRenderer>().material = whiteClothMat;

            // Leather Baldric / Bandolier (Diagonal chest strap)
            GameObject baldric = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baldric.name = "Leather_Baldric";
            baldric.transform.SetParent(tunic.transform, false);
            baldric.transform.localPosition = new Vector3(0.02f, 0.1f, 0.05f);
            baldric.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            baldric.transform.localScale = new Vector3(0.14f, 1.25f, 1.05f);
            SafeDestroy(baldric.GetComponent<Collider>());
            baldric.GetComponent<MeshRenderer>().material = leatherMat;

            // Wide Leather Belt & Gold Buckle
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            belt.name = "Belt";
            belt.transform.SetParent(modelRoot.transform, false);
            belt.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            belt.transform.localScale = new Vector3(0.54f, 0.09f, 0.4f);
            SafeDestroy(belt.GetComponent<Collider>());
            belt.GetComponent<MeshRenderer>().material = leatherMat;

            GameObject buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckle.name = "Triforce_Buckle";
            buckle.transform.SetParent(belt.transform, false);
            buckle.transform.localPosition = new Vector3(0f, 0f, 0.52f);
            buckle.transform.localScale = new Vector3(0.24f, 1.2f, 0.12f);
            SafeDestroy(buckle.GetComponent<Collider>());
            buckle.GetComponent<MeshRenderer>().material = goldMat;

            // Tunic Skirt (Lower drape over hips)
            GameObject skirt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            skirt.name = "Tunic_Skirt";
            skirt.transform.SetParent(modelRoot.transform, false);
            skirt.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            skirt.transform.localScale = new Vector3(0.56f, 0.16f, 0.42f);
            SafeDestroy(skirt.GetComponent<Collider>());
            skirt.GetComponent<MeshRenderer>().material = tunicMat;

            // ==================== 2. HEAD & HERO'S CAP ====================
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(modelRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.52f, 0f);
            head.transform.localScale = new Vector3(0.38f, 0.42f, 0.38f);
            SafeDestroy(head.GetComponent<Collider>());
            head.GetComponent<MeshRenderer>().material = skinMat;

            // Pointed Elf Ears
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ear.name = i < 0 ? "Ear_L" : "Ear_R";
                ear.transform.SetParent(head.transform, false);
                ear.transform.localPosition = new Vector3(i * 0.48f, 0.05f, -0.05f);
                ear.transform.localRotation = Quaternion.Euler(15f, i * 42f, i * -45f);
                ear.transform.localScale = new Vector3(0.14f, 0.45f, 0.08f);
                SafeDestroy(ear.GetComponent<Collider>());
                ear.GetComponent<MeshRenderer>().material = skinMat;
            }

            // Anime Eyes (Blue iris with white highlights)
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = i < 0 ? "Eye_L" : "Eye_R";
                eye.transform.SetParent(head.transform, false);
                eye.transform.localPosition = new Vector3(i * 0.14f, 0.08f, 0.42f);
                eye.transform.localScale = new Vector3(0.11f, 0.14f, 0.05f);
                SafeDestroy(eye.GetComponent<Collider>());
                eye.GetComponent<MeshRenderer>().material = blueEyeMat;

                GameObject eyeGlint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeGlint.name = "Glint";
                eyeGlint.transform.SetParent(eye.transform, false);
                eyeGlint.transform.localPosition = new Vector3(0.2f, 0.25f, 0.45f);
                eyeGlint.transform.localScale = Vector3.one * 0.4f;
                SafeDestroy(eyeGlint.GetComponent<Collider>());
                eyeGlint.GetComponent<MeshRenderer>().material = whiteClothMat;
            }

            // Golden Hair with Center Part Bangs & Side Locks
            GameObject bangs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bangs.name = "Hair_Bangs";
            bangs.transform.SetParent(head.transform, false);
            bangs.transform.localPosition = new Vector3(0f, 0.38f, 0.22f);
            bangs.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            bangs.transform.localScale = new Vector3(0.86f, 0.28f, 0.65f);
            SafeDestroy(bangs.GetComponent<Collider>());
            bangs.GetComponent<MeshRenderer>().material = hairMat;

            // Side hair locks framing face
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject lockHair = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lockHair.name = i < 0 ? "SideLock_L" : "SideLock_R";
                lockHair.transform.SetParent(head.transform, false);
                lockHair.transform.localPosition = new Vector3(i * 0.38f, -0.05f, 0.2f);
                lockHair.transform.localRotation = Quaternion.Euler(20f, 0f, i * -15f);
                lockHair.transform.localScale = new Vector3(0.12f, 0.36f, 0.15f);
                SafeDestroy(lockHair.GetComponent<Collider>());
                lockHair.GetComponent<MeshRenderer>().material = hairMat;
            }

            // Hero's Cap Base & Crown
            GameObject capBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capBase.name = "Cap_Base";
            capBase.transform.SetParent(head.transform, false);
            capBase.transform.localPosition = new Vector3(0f, 0.46f, -0.05f);
            capBase.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            capBase.transform.localScale = new Vector3(0.96f, 0.25f, 0.94f);
            SafeDestroy(capBase.GetComponent<Collider>());
            capBase.GetComponent<MeshRenderer>().material = tunicMat;

            // Cap Cone tapering back
            GameObject capCone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capCone.name = "Cap_Cone";
            capCone.transform.SetParent(capBase.transform, false);
            capCone.transform.localPosition = new Vector3(0f, 0.9f, -0.2f);
            capCone.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            capCone.transform.localScale = new Vector3(0.72f, 0.9f, 0.68f);
            SafeDestroy(capCone.GetComponent<Collider>());
            capCone.GetComponent<MeshRenderer>().material = tunicMat;

            // Floppy Cap Tail hanging down Link's back
            GameObject capTail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capTail.name = "CapTail";
            capTail.transform.SetParent(capCone.transform, false);
            capTail.transform.localPosition = new Vector3(0f, 0.85f, -0.15f);
            capTail.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            capTail.transform.localScale = new Vector3(0.48f, 0.85f, 0.42f);
            SafeDestroy(capTail.GetComponent<Collider>());
            capTail.GetComponent<MeshRenderer>().material = tunicMat;

            // ==================== 3. ARMS & ARCHER BRACERS ====================
            // Right Arm (Sword Arm)
            GameObject rightArm = new GameObject("RightArm");
            rightArm.transform.SetParent(modelRoot.transform, false);
            rightArm.transform.localPosition = new Vector3(0.36f, 1.26f, 0f);

            // Green Shoulder Sleeve
            GameObject rShoulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rShoulder.name = "Shoulder_R";
            rShoulder.transform.SetParent(rightArm.transform, false);
            rShoulder.transform.localPosition = Vector3.zero;
            rShoulder.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            SafeDestroy(rShoulder.GetComponent<Collider>());
            rShoulder.GetComponent<MeshRenderer>().material = tunicMat;

            // White Sleeve Upper Arm
            GameObject rUpper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rUpper.name = "UpperArm_R";
            rUpper.transform.SetParent(rightArm.transform, false);
            rUpper.transform.localPosition = new Vector3(0.04f, -0.2f, 0f);
            rUpper.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            SafeDestroy(rUpper.GetComponent<Collider>());
            rUpper.GetComponent<MeshRenderer>().material = whiteClothMat;

            // Leather Archer Bracer Forearm
            GameObject rForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rForearm.name = "Bracer_R";
            rForearm.transform.SetParent(rightArm.transform, false);
            rForearm.transform.localPosition = new Vector3(0.06f, -0.44f, 0.08f);
            rForearm.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            rForearm.transform.localScale = new Vector3(0.19f, 0.22f, 0.19f);
            SafeDestroy(rForearm.GetComponent<Collider>());
            rForearm.GetComponent<MeshRenderer>().material = leatherMat;

            // Hand & Weapon Socket
            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rHand.name = "Hand_R";
            rHand.transform.SetParent(rightArm.transform, false);
            rHand.transform.localPosition = new Vector3(0.08f, -0.66f, 0.18f);
            rHand.transform.localScale = new Vector3(0.16f, 0.18f, 0.18f);
            SafeDestroy(rHand.GetComponent<Collider>());
            rHand.GetComponent<MeshRenderer>().material = skinMat;

            GameObject rightSocket = new GameObject("WeaponSocket");
            rightSocket.transform.SetParent(rHand.transform, false);
            rightSocket.transform.localPosition = Vector3.zero;

            // Left Arm (Shield Arm)
            GameObject leftArm = new GameObject("LeftArm");
            leftArm.transform.SetParent(modelRoot.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.36f, 1.26f, 0f);

            GameObject lShoulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lShoulder.name = "Shoulder_L";
            lShoulder.transform.SetParent(leftArm.transform, false);
            lShoulder.transform.localPosition = Vector3.zero;
            lShoulder.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            SafeDestroy(lShoulder.GetComponent<Collider>());
            lShoulder.GetComponent<MeshRenderer>().material = tunicMat;

            GameObject lUpper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lUpper.name = "UpperArm_L";
            lUpper.transform.SetParent(leftArm.transform, false);
            lUpper.transform.localPosition = new Vector3(-0.04f, -0.2f, 0f);
            lUpper.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            SafeDestroy(lUpper.GetComponent<Collider>());
            lUpper.GetComponent<MeshRenderer>().material = whiteClothMat;

            GameObject lForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lForearm.name = "Bracer_L";
            lForearm.transform.SetParent(leftArm.transform, false);
            lForearm.transform.localPosition = new Vector3(-0.06f, -0.44f, 0.08f);
            lForearm.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            lForearm.transform.localScale = new Vector3(0.19f, 0.22f, 0.19f);
            SafeDestroy(lForearm.GetComponent<Collider>());
            lForearm.GetComponent<MeshRenderer>().material = leatherMat;

            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lHand.name = "Hand_L";
            lHand.transform.SetParent(leftArm.transform, false);
            lHand.transform.localPosition = new Vector3(-0.08f, -0.66f, 0.18f);
            lHand.transform.localScale = new Vector3(0.16f, 0.18f, 0.18f);
            SafeDestroy(lHand.GetComponent<Collider>());
            lHand.GetComponent<MeshRenderer>().material = skinMat;

            GameObject leftSocket = new GameObject("ShieldSocket");
            leftSocket.transform.SetParent(lHand.transform, false);
            leftSocket.transform.localPosition = Vector3.zero;

            // ==================== 4. LEGS & ADVENTURER BOOTS ====================
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "L" : "R";

                // White Tights Thigh
                GameObject thigh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                thigh.name = $"Thigh_{side}";
                thigh.transform.SetParent(modelRoot.transform, false);
                thigh.transform.localPosition = new Vector3(i * 0.16f, 0.44f, 0f);
                thigh.transform.localScale = new Vector3(0.19f, 0.28f, 0.19f);
                SafeDestroy(thigh.GetComponent<Collider>());
                thigh.GetComponent<MeshRenderer>().material = whiteClothMat;

                // Boot Cuff (Turnover leather collar)
                GameObject bootCuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bootCuff.name = $"BootCuff_{side}";
                bootCuff.transform.SetParent(modelRoot.transform, false);
                bootCuff.transform.localPosition = new Vector3(i * 0.16f, 0.28f, 0.02f);
                bootCuff.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
                SafeDestroy(bootCuff.GetComponent<Collider>());
                bootCuff.GetComponent<MeshRenderer>().material = leatherMat;

                // Leather Boot Shaft & Foot
                GameObject boot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boot.name = $"Boot_{side}";
                boot.transform.SetParent(modelRoot.transform, false);
                boot.transform.localPosition = new Vector3(i * 0.16f, 0.12f, 0.06f);
                boot.transform.localScale = new Vector3(0.21f, 0.24f, 0.36f);
                SafeDestroy(boot.GetComponent<Collider>());
                boot.GetComponent<MeshRenderer>().material = leatherMat;
            }

            // ==================== 5. BACK SCABBARD SHEATH ====================
            GameObject backSocket = new GameObject("BackSheathSocket");
            backSocket.transform.SetParent(modelRoot.transform, false);
            backSocket.transform.localPosition = new Vector3(0.05f, 1.15f, -0.22f);
            backSocket.transform.localRotation = Quaternion.Euler(15f, 0f, -42f);

            // Royal Blue Scabbard with gold bands
            GameObject scabbard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scabbard.name = "MasterSword_Scabbard";
            scabbard.transform.SetParent(backSocket.transform, false);
            scabbard.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            scabbard.transform.localScale = new Vector3(0.12f, 0.95f, 0.06f);
            SafeDestroy(scabbard.GetComponent<Collider>());
            scabbard.GetComponent<MeshRenderer>().material = royalBlueMat;

            // Gold bands on scabbard
            for (int b = -1; b <= 1; b++)
            {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = "GoldBand";
                band.transform.SetParent(scabbard.transform, false);
                band.transform.localPosition = new Vector3(0f, b * 0.32f, 0f);
                band.transform.localScale = new Vector3(1.15f, 0.1f, 1.15f);
                SafeDestroy(band.GetComponent<Collider>());
                band.GetComponent<MeshRenderer>().material = goldMat;
            }

            // Item Spawn Socket (Bow origin)
            GameObject itemSpawn = new GameObject("ItemSpawnSocket");
            itemSpawn.transform.SetParent(player.transform, false);
            itemSpawn.transform.localPosition = new Vector3(0f, 1.3f, 0.6f);

            // Core Components
            player.AddComponent<HealthSystem>();
            player.AddComponent<ZTargetSystem>();
            player.AddComponent<ZeldaDodgeController>();
            player.AddComponent<PlayerCombat>();
            player.AddComponent<WaterSwimController>();
            player.AddComponent<ItemSystem>();

            var inventory = player.AddComponent<PlayerInventory>();
            inventory.SetSockets(rightSocket.transform, leftSocket.transform, backSocket.transform);

            player.AddComponent<PlayerLocomotion>();
            var animController = player.AddComponent<ZeldaAnimationController>();
            animController.SetupModelTransforms(modelRoot.transform, rightArm.transform, leftArm.transform);
            animController.SetupCapTail(capTail.transform);

            return player;
        }
    }
}
