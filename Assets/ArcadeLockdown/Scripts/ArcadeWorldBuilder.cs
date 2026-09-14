using System.Collections.Generic;
using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Builds the connected arcade, office, prize room, storage room, power room,
    /// imported CC0 models, PBR surfaces, animated props and puzzle objects.
    /// </summary>
    public static partial class ArcadeWorldBuilder
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static Transform world;

        public static void Build(ArcadeGameManager manager)
        {
            Materials.Clear();
            world = new GameObject("ARCADE LOCKDOWN - Generated 3D World").transform;

            ConfigureEnvironment();
            CreateMaterials();
            BuildArchitecture();
            BuildArchitecturalDetails();
            BuildDoors(manager);
            BuildMainArcade();
            BuildOffice();
            BuildPrizeRoom();
            BuildStorage();
            BuildPowerRoom();
            BuildLighting();
            BuildPlayer(manager);
            Physics.SyncTransforms();
        }

        private static void ConfigureEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0045f;
            RenderSettings.fogColor = new Color(0.018f, 0.019f, 0.032f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.26f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.15f, 0.17f);
            RenderSettings.ambientIntensity = 1f;
            UnityEngine.Rendering.SphericalHarmonicsL2 ambient = new UnityEngine.Rendering.SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(.38f,.40f,.44f));
            RenderSettings.ambientProbe = ambient;
        }

        private static void CreateMaterials()
        {
            Materials["Wall"] = MakePbrMaterial("Midnight concrete walls", new Color(0.28f, 0.24f, 0.36f),
                "PBR/Concrete034/Concrete034_1K-JPG_Color", "PBR/Concrete034/Concrete034_1K-JPG_NormalGL", null, new Vector2(3.2f, 1.2f), 0f, 0.2f);
            Materials["Wall2"] = MakePbrMaterial("Warm office concrete", new Color(0.46f, 0.38f, 0.36f),
                "PBR/Concrete034/Concrete034_1K-JPG_Color", "PBR/Concrete034/Concrete034_1K-JPG_NormalGL", null, new Vector2(2.4f, 1.1f), 0f, 0.24f);
            Materials["FloorA"] = MakePbrMaterial("Deep blue commercial carpet", new Color(0.24f, 0.27f, 0.52f),
                "PBR/Carpet012/Carpet012_1K-JPG_Color", "PBR/Carpet012/Carpet012_1K-JPG_NormalGL", "PBR/Carpet012/Carpet012_1K-JPG_AmbientOcclusion", new Vector2(5f, 4f), 0f, 0.08f);
            Materials["FloorB"] = MakePbrMaterial("Muted violet commercial carpet", new Color(0.38f, 0.24f, 0.48f),
                "PBR/Carpet012/Carpet012_1K-JPG_Color", "PBR/Carpet012/Carpet012_1K-JPG_NormalGL", "PBR/Carpet012/Carpet012_1K-JPG_AmbientOcclusion", new Vector2(4f, 4f), 0f, 0.08f);
            Materials["Ceiling"] = MakeMaterial("Acoustic black ceiling", new Color(0.035f, 0.038f, 0.05f), false, 0f, 0.12f);
            Materials["Black"] = MakeMaterial("Powder-coated cabinet black", new Color(0.018f, 0.02f, 0.028f), false, 0.15f, 0.46f);
            Materials["DarkMetal"] = MakeMaterial("Dark brushed metal", new Color(0.08f, 0.09f, 0.115f), false, 0.75f, 0.38f);
            Materials["Metal"] = MakeMaterial("Brushed steel", new Color(0.32f, 0.36f, 0.42f), false, 0.82f, 0.52f);
            Materials["Wood"] = MakeMaterial("Sealed walnut", new Color(0.28f, 0.125f, 0.06f), false, 0f, 0.42f);
            Materials["Cardboard"] = MakeMaterial("Corrugated cardboard", new Color(0.42f, 0.28f, 0.13f), false, 0f, 0.12f);
            Materials["White"] = MakeMaterial("Matte paper", new Color(0.86f, 0.87f, 0.82f), false, 0f, 0.18f);
            Materials["Red"] = MakeMaterial("Token red", new Color(0.95f, 0.06f, 0.12f), true);
            Materials["Blue"] = MakeMaterial("Token blue", new Color(0.02f, 0.35f, 1f), true);
            Materials["Yellow"] = MakeMaterial("Token yellow", new Color(1f, 0.68f, 0.02f), true);
            Materials["Cyan"] = MakeMaterial("Neon cyan", new Color(0.02f, 0.9f, 1f), true);
            Materials["Pink"] = MakeMaterial("Neon pink", new Color(1f, 0.03f, 0.58f), true);
            Materials["Green"] = MakeMaterial("Power green", new Color(0.08f, 1f, 0.32f), true);
            Materials["Orange"] = MakeMaterial("Arcade orange", new Color(1f, 0.24f, 0.035f), true);
            Materials["Glass"] = MakeMaterial("Reflective screen glass", new Color(0.008f, 0.018f, 0.026f), false, 0.18f, 0.92f);
            Materials["Skin"] = MakeMaterial("Young man skin", new Color(0.72f, 0.43f, 0.27f));
            Materials["Hair"] = MakeMaterial("Young man hair", new Color(0.055f, 0.025f, 0.018f));
            Materials["Shirt"] = MakeMaterial("Young man jacket", new Color(0.05f, 0.52f, 0.86f));
            Materials["Jeans"] = MakeMaterial("Young man jeans", new Color(0.045f, 0.09f, 0.22f));
            Materials["Shoes"] = MakeMaterial("Young man shoes", new Color(0.025f, 0.025f, 0.035f));
            CreateDetailedMaterials();
        }

        private static Material MakeMaterial(string name, Color color, bool emissive = false, float metallic = 0f, float smoothness = 0.26f)
        {
            bool scriptablePipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = scriptablePipeline ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader) { name = name };
            material.enableInstancing = true;
            SetMaterialColor(material, color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", emissive ? 0.68f : smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.7f);
            }
            return material;
        }

        private static Material MakePbrMaterial(string name, Color tint, string colorPath, string normalPath,
            string occlusionPath, Vector2 tiling, float metallic, float smoothness)
        {
            Material material = MakeMaterial(name, tint, false, metallic, smoothness);
            Texture2D color = Resources.Load<Texture2D>(colorPath);
            Texture2D normal = Resources.Load<Texture2D>(normalPath);
            Texture2D occlusion = string.IsNullOrEmpty(occlusionPath) ? null : Resources.Load<Texture2D>(occlusionPath);

            if (color != null)
            {
                SetMaterialTexture(material, "_BaseMap", "_MainTex", color);
                SetMaterialScale(material, "_BaseMap", "_MainTex", tiling);
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.72f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null && material.HasProperty("_OcclusionMap"))
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", 0.8f);
                material.EnableKeyword("_OCCLUSIONMAP");
            }
            return material;
        }

        private static void BuildArchitecture()
        {
            Transform architecture = new GameObject("ROOMS - Connected Level").transform;
            architecture.SetParent(world);

            BuildRoomBase(architecture, "MAIN ARCADE", Vector3.zero, new Vector2(22f, 16f), "FloorA");
            WallHorizontal(architecture, "Main north left", new Vector3(-6.15f, 1.65f, 8f), 9.7f, "Wall");
            WallHorizontal(architecture, "Main north right", new Vector3(6.15f, 1.65f, 8f), 9.7f, "Wall");
            LintelHorizontal(architecture, "Exit lintel", new Vector3(0f, 3.05f, 8f), 2.6f, "Wall");
            WallHorizontal(architecture, "Main south left", new Vector3(-6.15f, 1.65f, -8f), 9.7f, "Wall");
            WallHorizontal(architecture, "Main south right", new Vector3(6.15f, 1.65f, -8f), 9.7f, "Wall");
            LintelHorizontal(architecture, "Power lintel", new Vector3(0f, 3.05f, -8f), 2.6f, "Wall");
            WallVerticalWithDoor(architecture, "Main east", 11f, 0f, 16f, 3f, "Wall");
            WallVerticalWithDoor(architecture, "Main west", -11f, 0f, 16f, 3f, "Wall");

            BuildRoomBase(architecture, "MANAGER OFFICE", new Vector3(15f, 0f, 3f), new Vector2(8f, 10f), "FloorB");
            WallHorizontal(architecture, "Office north", new Vector3(15f, 1.65f, 8f), 8f, "Wall2");
            WallHorizontal(architecture, "Office south", new Vector3(15f, 1.65f, -2f), 8f, "Wall2");
            WallVertical(architecture, "Office east", new Vector3(19f, 1.65f, 3f), 10f, "Wall2");

            BuildRoomBase(architecture, "PRIZE ROOM", new Vector3(-15f, 0f, 3f), new Vector2(8f, 10f), "FloorB");
            WallHorizontal(architecture, "Prize north", new Vector3(-15f, 1.65f, 8f), 8f, "Wall");
            WallHorizontalWithDoor(architecture, "Prize south", -2f, -15f, 8f, -15f, "Wall");
            WallVertical(architecture, "Prize west", new Vector3(-19f, 1.65f, 3f), 10f, "Wall");

            BuildRoomBase(architecture, "STORAGE ROOM", new Vector3(-15f, 0f, -7f), new Vector2(8f, 10f), "FloorA");
            WallHorizontal(architecture, "Storage south", new Vector3(-15f, 1.65f, -12f), 8f, "Wall");
            WallVertical(architecture, "Storage west", new Vector3(-19f, 1.65f, -7f), 10f, "Wall");
            WallVertical(architecture, "Storage east", new Vector3(-11f, 1.65f, -7f), 10f, "Wall");

            BuildRoomBase(architecture, "POWER ROOM", new Vector3(0f, 0f, -11f), new Vector2(10f, 6f), "FloorB");
            WallHorizontal(architecture, "Power south", new Vector3(0f, 1.65f, -14f), 10f, "Wall");
            WallVertical(architecture, "Power west", new Vector3(-5f, 1.65f, -11f), 6f, "Wall");
            WallVertical(architecture, "Power east", new Vector3(5f, 1.65f, -11f), 6f, "Wall");

            BuildCheckerFloor(architecture);
            BuildNeonTrim(architecture);
        }

        private static void BuildRoomBase(Transform parent, string name, Vector3 center, Vector2 size, string floorMaterial)
        {
            Transform room = new GameObject(name).transform;
            room.SetParent(parent);
            Cube("Floor", room, new Vector3(center.x, -0.12f, center.z), new Vector3(size.x, 0.24f, size.y), Materials[floorMaterial]);
            Cube("Ceiling", room, new Vector3(center.x, 3.42f, center.z), new Vector3(size.x, 0.22f, size.y), Materials["Ceiling"]);
        }

        private static void WallHorizontal(Transform parent, string name, Vector3 position, float width, string material)
        {
            Cube(name, parent, position, new Vector3(width, 3.3f, 0.22f), Materials[material]);
        }

        private static void WallVertical(Transform parent, string name, Vector3 position, float length, string material)
        {
            Cube(name, parent, position, new Vector3(0.22f, 3.3f, length), Materials[material]);
        }

        private static void LintelHorizontal(Transform parent, string name, Vector3 position, float width, string material)
        {
            Cube(name, parent, position, new Vector3(width, 0.5f, 0.22f), Materials[material]);
        }

        private static void WallHorizontalWithDoor(Transform parent, string name, float z, float centerX, float totalWidth, float doorX, string material)
        {
            const float opening = 2.6f;
            float leftEdge = centerX - totalWidth * 0.5f;
            float rightEdge = centerX + totalWidth * 0.5f;
            float leftWidth = doorX - opening * 0.5f - leftEdge;
            float rightWidth = rightEdge - (doorX + opening * 0.5f);
            if (leftWidth > 0f) WallHorizontal(parent, name + " left", new Vector3(leftEdge + leftWidth * 0.5f, 1.65f, z), leftWidth, material);
            if (rightWidth > 0f) WallHorizontal(parent, name + " right", new Vector3(doorX + opening * 0.5f + rightWidth * 0.5f, 1.65f, z), rightWidth, material);
            LintelHorizontal(parent, name + " lintel", new Vector3(doorX, 3.05f, z), opening, material);
        }

        private static void WallVerticalWithDoor(Transform parent, string name, float x, float centerZ, float totalLength, float doorZ, string material)
        {
            const float opening = 2.6f;
            float lowEdge = centerZ - totalLength * 0.5f;
            float highEdge = centerZ + totalLength * 0.5f;
            float lowLength = doorZ - opening * 0.5f - lowEdge;
            float highLength = highEdge - (doorZ + opening * 0.5f);
            if (lowLength > 0f) WallVertical(parent, name + " low", new Vector3(x, 1.65f, lowEdge + lowLength * 0.5f), lowLength, material);
            if (highLength > 0f) WallVertical(parent, name + " high", new Vector3(x, 1.65f, doorZ + opening * 0.5f + highLength * 0.5f), highLength, material);
            Cube(name + " lintel", parent, new Vector3(x, 3.05f, doorZ), new Vector3(0.22f, 0.5f, opening), Materials[material]);
        }

        private static void BuildCheckerFloor(Transform parent)
        {
            Transform inlay = new GameObject("Commercial carpet inlay and floor trim").transform;
            inlay.SetParent(parent);
            Cube("Central carpet runner", inlay, new Vector3(0f, 0.012f, 0f), new Vector3(4.6f, 0.025f, 13.4f), Materials["FloorB"], false);
            Cube("Runner trim left", inlay, new Vector3(-2.34f, 0.025f, 0f), new Vector3(0.045f, 0.04f, 13.4f), Materials["Metal"], false);
            Cube("Runner trim right", inlay, new Vector3(2.34f, 0.025f, 0f), new Vector3(0.045f, 0.04f, 13.4f), Materials["Metal"], false);

            for (int i = -3; i <= 3; i++)
            {
                Material accent = (i & 1) == 0 ? Materials["Cyan"] : Materials["Pink"];
                Cube("Low-profile floor marker", inlay, new Vector3(0f, 0.035f, i * 1.85f),
                    new Vector3(0.8f, 0.018f, 0.025f), accent, false);
            }
        }

        private static void BuildNeonTrim(Transform parent)
        {
            Cube("North cyan neon", parent, new Vector3(0f, 2.75f, 7.82f), new Vector3(18f, 0.055f, 0.05f), Materials["Cyan"], false);
            Cube("South pink neon", parent, new Vector3(0f, 2.75f, -7.82f), new Vector3(18f, 0.055f, 0.05f), Materials["Pink"], false);
            Cube("Prize cyan neon", parent, new Vector3(-18.82f, 2.65f, 3f), new Vector3(0.05f, 0.055f, 7f), Materials["Cyan"], false);
            Cube("Office pink neon", parent, new Vector3(18.82f, 2.65f, 3f), new Vector3(0.05f, 0.055f, 7f), Materials["Pink"], false);
        }

        private static void BuildDoors(ArcadeGameManager manager)
        {
            DoorAnimator exit = CreateSlidingDoor("LOCKED EXIT", new Vector3(0f, 1.35f, 7.88f), 0f, Materials["DarkMetal"], InteractionKind.ExitKeypad, "Use security keypad", manager);
            manager.RegisterDoor("Exit", exit);
            BuildDoorLabel(exit.transform, "EXIT", Materials["Cyan"]);

            DoorAnimator office = CreateSlidingDoor("OFFICE DOOR", new Vector3(10.88f, 1.35f, 3f), 90f, Materials["DarkMetal"], InteractionKind.OfficeDoor, "Unlock manager's office", manager);
            manager.RegisterDoor("Office", office);
            BuildDoorLabel(office.transform, "OFFICE", Materials["Pink"]);

            DoorAnimator storage = CreateSlidingDoor("SCREWED STORAGE DOOR", new Vector3(-15f, 1.35f, -1.88f), 180f, Materials["DarkMetal"], InteractionKind.StorageDoor, "Inspect screwed lock plate", manager);
            manager.RegisterDoor("Storage", storage);
            BuildDoorLabel(storage.transform, "STORAGE", Materials["Yellow"]);
            AddDoorScrews(storage.transform);

            DoorAnimator prize = CreateSlidingDoor("PRIZE ROOM DOOR", new Vector3(-10.88f, 1.35f, 3f), -90f, Materials["DarkMetal"], InteractionKind.UnlockedDoor, "Open prize room", manager);
            prize.GetComponent<Interactable>().itemId = "Prize";
            prize.GetComponent<Interactable>().itemDisplayName = "PRIZE ROOM";
            manager.RegisterDoor("Prize", prize);
            BuildDoorLabel(prize.transform, "PRIZES", Materials["Pink"]);

            DoorAnimator power = CreateSlidingDoor("POWER ROOM DOOR", new Vector3(0f, 1.35f, -7.88f), 180f, Materials["DarkMetal"], InteractionKind.UnlockedDoor, "Open power room", manager);
            power.GetComponent<Interactable>().itemId = "Power";
            power.GetComponent<Interactable>().itemDisplayName = "POWER ROOM";
            manager.RegisterDoor("Power", power);
            BuildDoorLabel(power.transform, "POWER", Materials["Cyan"]);

            BuildKeypad("Exit security keypad", new Vector3(1.75f, 1.35f, 7.72f), 0f, InteractionKind.ExitKeypad, "Enter exit code", Materials["Cyan"]);
        }

        private static DoorAnimator CreateSlidingDoor(string name, Vector3 position, float yaw, Material material, InteractionKind interaction, string prompt, ArcadeGameManager manager)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(world);
            root.position = position;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            Cube("Door panel", root, Vector3.zero, new Vector3(2.42f, 2.7f, 0.16f), material);
            Cube("Door inset", root, new Vector3(0f, 0f, -0.09f), new Vector3(1.85f, 2.2f, 0.025f), Materials["Black"], false);
            DoorAnimator animator = root.gameObject.AddComponent<DoorAnimator>();
            Interactable interactable = root.gameObject.AddComponent<Interactable>();
            interactable.kind = interaction;
            interactable.prompt = prompt;
            return animator;
        }

        private static void BuildDoorLabel(Transform door, string label, Material accent)
        {
            DetailBox(label + " steel plaque", door, new Vector3(0f,.55f,-.13f),new Vector3(1.2f,.32f,.035f),Materials["Metal"],.018f);
            DetailBox(label + " printed face",door,new Vector3(0,.55f,-.153f),new Vector3(1.15f,.27f,.008f),Materials["SignFace"],.008f);
            Cube("Door label color key",door,new Vector3(-.51f,.55f,-.161f),new Vector3(.017f,.18f,.004f),accent,false);
            CreateText(label, door, new Vector3(.015f, .55f, -.167f), Quaternion.identity, .038f, Color.white, 64);
            DetailBox("Brushed door pull",door,new Vector3(.86f,-.12f,-.14f),new Vector3(.045f,.40f,.08f),Materials["Metal"],.014f);
        }

        private static void AddDoorScrews(Transform door)
        {
            Vector3[] positions =
            {
                new Vector3(-0.72f, -0.45f, -0.16f), new Vector3(0.72f, -0.45f, -0.16f),
                new Vector3(-0.72f, 0.45f, -0.16f), new Vector3(0.72f, 0.45f, -0.16f)
            };
            foreach (Vector3 position in positions)
            {
                GameObject screw = Primitive("Lock plate screw", PrimitiveType.Cylinder, door, position, new Vector3(0.08f, 0.025f, 0.08f), Materials["Metal"], false);
                screw.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private static void BuildMainArcade()
        {
            Transform props = new GameObject("MAIN ARCADE - Models and Puzzles").transform;
            props.SetParent(world);

            BuildSign(props, "Arcade title neon", new Vector3(0f, 2.45f, 7.68f), 0f, new Vector2(5.6f, 0.9f),
                "ARCADE LOCKDOWN", "The old neon sign flickers: ARCADE LOCKDOWN — EST. 1992.", Materials["Pink"], Color.white);

            GameObject highScore = BuildArcadeCabinet(props, "Champion machine", new Vector3(-7.6f, 0f, 6.4f), 0f, Materials["Orange"], "STAR FIGHTER", "HIGH SCORES");
            AddInteraction(highScore, InteractionKind.Information, "Read high scores",
                "HIGH SCORES\n\nAAA     1987\nMAX     1994\nJOE     1991\nSAM     1996\n\nSomeone circled MAX in pink marker.");

            BuildArcadeCabinet(props, "Galaxy cabinet", new Vector3(-4.9f, 0f, 6.4f), 0f, Materials["Cyan"], "GALAXY RUN", "INSERT COIN");
            BuildArcadeCabinet(props, "Turbo cabinet", new Vector3(4.9f, 0f, 6.4f), 0f, Materials["Pink"], "TURBO 90", "GAME OVER");
            BuildArcadeCabinet(props, "Ninja cabinet", new Vector3(7.6f, 0f, 6.4f), 0f, Materials["Yellow"], "NEON NINJA", "PLAYER 1");

            BuildSign(props, "First champion poster", new Vector3(-9.9f, 1.78f, 5.15f), 90f, new Vector2(1.55f, 2.25f),
                "OUR FIRST\nCHAMPION\n\nMAX", "OUR FIRST CHAMPION: MAX\n\nThe name MAX is underlined. The year of MAX's high score may be useful.", Materials["Pink"], Color.white);

            BuildLocker(props);
            BuildVendingMachine(props, new Vector3(9.55f, 0f, -3.4f), 90f);

            // Final powered machines. Their displays reveal the exit combination.
            BuildPoweredMachine(props, "PAC-RAT", new Vector3(-6f, 0f, -6.55f), 180f, Materials["Yellow"], "PAC-RAT\n8", new Color(1f, 0.75f, 0.05f));
            BuildPoweredMachine(props, "SPACE RAID", new Vector3(-2f, 0f, -6.55f), 180f, Materials["Cyan"], "SPACE RAID\n3", new Color(0.05f, 0.9f, 1f));
            BuildPoweredMachine(props, "RACE 199X", new Vector3(2f, 0f, -6.55f), 180f, Materials["Red"], "RACE 199X\n1", new Color(1f, 0.12f, 0.12f));
            BuildPoweredMachine(props, "BLOCK DROP", new Vector3(6f, 0f, -6.55f), 180f, Materials["Pink"], "BLOCK DROP\n7", new Color(1f, 0.08f, 0.7f));

            BuildSign(props, "Tournament order poster", new Vector3(9.9f, 1.82f, -0.5f), 90f, new Vector2(1.65f, 2.3f),
                "TOURNAMENT\nORDER\n\nSPACE\nRACE\nBLOCK\nPAC", "TOURNAMENT ORDER:\n\n1. SPACE\n2. RACE\n3. BLOCK\n4. PAC\n\nKeep this order in mind after the power returns.", Materials["Cyan"], Color.white);

            DetailBox("Token side table", props, new Vector3(2.3f, 0.83f, 4.8f), new Vector3(0.95f, 0.12f, 0.65f), Materials["Wood"], 0.035f, true);
            DetailBox("Token table pedestal", props, new Vector3(2.3f, 0.4f, 4.8f), new Vector3(0.36f, 0.8f, 0.34f), Materials["DarkMetal"], 0.02f, true);
            BuildToken(props, "Red token", "RedToken", "RED TOKEN", new Vector3(2.3f, 1.10f, 4.58f), Materials["Red"]);
            BuildBench(props, new Vector3(4.8f, 0f, 1.2f));
            BuildTrashCan(props, new Vector3(-9.4f, 0f, -4.5f));
            BuildDetailedAttractions(props);
        }

        private static void BuildLocker(Transform parent)
        {
            Transform locker = new GameObject("STAFF LOCKER - Puzzle 1").transform;
            locker.SetParent(parent);
            locker.position = new Vector3(9.45f, 1.15f, 1f);
            Cube("Locker body", locker, Vector3.zero, new Vector3(1.25f, 2.3f, 0.72f), Materials["Metal"]);
            for (int i = -2; i <= 2; i++)
                Cube("Vent", locker, new Vector3(0f, 0.6f + i * 0.08f, -0.37f), new Vector3(0.62f, 0.022f, 0.02f), Materials["Black"], false);
            BuildKeypad("Locker keypad", locker.TransformPoint(new Vector3(0.34f, -0.15f, -0.41f)), 0f, InteractionKind.LockerKeypad, "Use locker keypad", Materials["Pink"], locker);
            AddInteraction(locker.gameObject, InteractionKind.LockerKeypad, "Use locker keypad", string.Empty);
        }

        private static void BuildOffice()
        {
            Transform props = new GameObject("MANAGER OFFICE - Models and Puzzles").transform;
            props.SetParent(world);
            BuildSign(props, "Office sign", new Vector3(18.82f, 2.10f, 3f), 90f, new Vector2(1.15f, 0.33f),
                "MANAGER", "A brass manager sign. The office looks untouched since the 1990s.", Materials["Yellow"], Color.black);

            Transform desk = new GameObject("Manager desk").transform;
            desk.SetParent(props);
            desk.position = new Vector3(15.6f, 0f, 4.9f);
            BuildOfficeDeskDetails(desk);
            BuildComputer(desk);
            BuildOfficeAccessories(desk);
            BuildToken(props, "Blue token", "BlueToken", "BLUE TOKEN", new Vector3(14.5f, 1.12f, 4.27f), Materials["Blue"]);

            BuildSign(props, "Space Raiders poster", new Vector3(18.76f, 1.80f, 5.8f), 90f, new Vector2(.75f, 1.02f),
                "SPACE\nRAIDERS\n\nMACHINE\nNO. 07", "SPACE RAIDERS\nMachine No. 07\n\nThe password note says to use the favorite game plus the opening year.", Materials["Cyan"], Color.white);
            BuildSign(props, "Opening calendar", new Vector3(14.3f, 1.80f, 7.78f), 0f, new Vector2(.85f, .80f),
                "GRAND OPENING\n\nJUNE 1992", "ARCADE GRAND OPENING: 1992\n\nA date has been circled in red.", Materials["White"], Color.black);
            BuildSign(props, "Password note", new Vector3(16.55f, 1.6f, 7.78f), 0f, new Vector2(.76f, .45f),
                "PASSWORD:\nFavorite game +\nopening year", "PASSWORD:\nMy favorite game + opening year", Materials["White"], Color.black);
            BuildFilingCabinet(props, new Vector3(17.8f, 0f, 0f));
            BuildOfficeChair(props, new Vector3(15.5f, 0f, 2.8f));
        }

        private static void BuildLegacyComputer(Transform desk)
        {
            Transform computer = new GameObject("MANAGER COMPUTER - Puzzle 2").transform;
            computer.SetParent(desk);
            computer.localPosition = new Vector3(0f, 1.15f, -0.05f);
            Cube("CRT body", computer, Vector3.zero, new Vector3(1.25f, 0.92f, 0.82f), Materials["Metal"]);
            GameObject crtScreen = Cube("CRT screen", computer, new Vector3(0f, 0.06f, -0.43f), new Vector3(0.94f, 0.58f, 0.035f), Materials["Glass"], false);
            TextMesh crtText = CreateText("PASSWORD\n_ _ _ _ _ _", computer, new Vector3(0f, 0.06f, -0.46f), Quaternion.identity, 0.03f, new Color(0.1f, 1f, 0.4f), 48);
            Cube("Keyboard", computer, new Vector3(0f, -0.51f, -0.42f), new Vector3(1.18f, 0.09f, 0.46f), Materials["DarkMetal"]);
            MachineAnimator computerAnimation = computer.gameObject.AddComponent<MachineAnimator>();
            computerAnimation.screenRenderer = crtScreen.GetComponent<Renderer>();
            computerAnimation.screenText = crtText.transform;
            computerAnimation.glowColor = new Color(0.1f, 1f, 0.4f);
            computerAnimation.speed = 0.65f;
            AddInteraction(computer.gameObject, InteractionKind.OfficeComputer, "Use manager computer", string.Empty);
        }

        private static void BuildPrizeRoom()
        {
            Transform props = new GameObject("PRIZE ROOM - Models and Puzzles").transform;
            props.SetParent(world);
            BuildSign(props, "Prize room neon", new Vector3(-18.75f, 2.35f, 3f), -90f, new Vector2(3.2f, 0.72f),
                "PRIZE ZONE", "A neon sign buzzes over the prize counter.", Materials["Pink"], Color.white);

            Transform counter = new GameObject("PRIZE COUNTER - Puzzle 3").transform;
            counter.SetParent(props);
            counter.position = new Vector3(-15f, 0f, 5.6f);
            Cube("Counter base", counter, new Vector3(0f, 0.55f, 0f), new Vector3(5.4f, 1.1f, 1.1f), Materials["Wood"]);
            Cube("Glass display", counter, new Vector3(0f, 1.25f, 0.25f), new Vector3(5.1f, 1.05f, 0.55f), Materials["DarkMetal"]);
            Cube("Prize drawer", counter, new Vector3(0f, 0.62f, -0.58f), new Vector3(1.55f, 0.55f, 0.12f), Materials["Metal"]);
            BuildKeypad("Prize drawer keypad", counter.TransformPoint(new Vector3(1.25f, 0.72f, -0.7f)), 0f, InteractionKind.PrizeKeypad, "Use prize drawer keypad", Materials["Yellow"], counter);
            AddInteraction(counter.gameObject, InteractionKind.PrizeKeypad, "Use prize drawer keypad", string.Empty);

            BuildSign(props, "Token values sign", new Vector3(-18.75f, 1.6f, 5.65f), -90f, new Vector2(1.8f, 2.2f),
                "PRIZE VALUES\n\nRED = 5\nBLUE = 2\nYELLOW = 8", "PRIZE VALUES:\nRED = 5\nBLUE = 2\nYELLOW = 8", Materials["White"], Color.black);
            BuildSign(props, "Token order clue", new Vector3(-12.4f, 1.65f, 7.75f), 0f, new Vector2(2.0f, 2.0f),
                "TOKEN ORDER\n\nBLUE\nRED\nYELLOW\nRED", "TOKEN ORDER:\nBLUE — RED — YELLOW — RED", Materials["Cyan"], Color.white);

            BuildToken(props, "Yellow token", "YellowToken", "YELLOW TOKEN", new Vector3(-16.65f, 1.12f, -0.18f), Materials["Yellow"]);
            BuildPrizeShelf(props, new Vector3(-17.1f, 0f, 0.3f));
            BuildMascotRobot(props, "Pixel Pal mascot", new Vector3(-13f, 0f, 1.1f), new Color(1f, 0.1f, 0.65f));
            BuildPrizeWheel(props, new Vector3(-12f, 0f, 6.8f), 90f);
        }

        private static void BuildStorage()
        {
            Transform props = new GameObject("STORAGE ROOM - Models and Box 42").transform;
            props.SetParent(world);
            BuildBox(props, "Box 07", new Vector3(-17.1f, 0.45f, -4.2f), new Vector3(1.55f, 0.9f, 1.35f), "07", false);
            BuildBox(props, "Box 13", new Vector3(-13.1f, 0.55f, -9.8f), new Vector3(1.8f, 1.1f, 1.45f), "13", false);
            BuildBox(props, "Box 86", new Vector3(-17f, 0.48f, -9.9f), new Vector3(1.5f, 0.96f, 1.35f), "86", false);
            BuildBox(props, "BOX 42 - Fuse", new Vector3(-15.1f, 0.65f, -7.2f), new Vector3(2f, 1.3f, 1.6f), "42", true);
            BuildBrokenCabinet(props, new Vector3(-12.2f, 0f, -5.2f), 18f);
            BuildBrokenCabinet(props, new Vector3(-17.7f, 0f, -7.3f), -15f);
            BuildShelf(props, new Vector3(-18f, 0f, -5.5f));
            BuildFuseProp(props, new Vector3(-15.1f, 1.42f, -7.2f));
        }

        private static void BuildPowerRoom()
        {
            Transform props = new GameObject("POWER ROOM - Final Circuit").transform;
            props.SetParent(world);
            BuildSign(props, "Power warning", new Vector3(0f, 2.65f, -13.82f), 180f, new Vector2(3.7f, 0.72f),
                "DANGER — HIGH VOLTAGE", "The building's main electrical panel. It is missing one fuse.", Materials["Yellow"], Color.black);

            Transform console = new GameObject("MAIN POWER CONSOLE - Puzzle 5").transform;
            console.SetParent(props);
            console.position = new Vector3(0f, 1.25f, -13.35f);
            Cube("Console cabinet", console, Vector3.zero, new Vector3(3.7f, 2.5f, 0.75f), Materials["DarkMetal"]);
            GameObject circuitDisplay = Cube("Circuit display", console, new Vector3(0f, 0.35f, 0.39f), new Vector3(2.9f, 0.95f, 0.04f), Materials["Glass"], false);
            TextMesh circuitText = CreateText("POWER CIRCUIT\nFUSE MISSING\nA ─ ? ─ ? ─ C", console, new Vector3(0f, 0.35f, 0.43f), Quaternion.Euler(0f, 180f, 0f), 0.033f, new Color(1f, 0.2f, 0.12f), 48);
            MachineAnimator consoleAnimation = console.gameObject.AddComponent<MachineAnimator>();
            consoleAnimation.screenRenderer = circuitDisplay.GetComponent<Renderer>();
            consoleAnimation.screenText = circuitText.transform;
            consoleAnimation.glowColor = new Color(1f, 0.12f, 0.05f);
            consoleAnimation.speed = 0.75f;
            AddInteraction(console.gameObject, InteractionKind.PowerConsole, "Inspect main power console", string.Empty);

            BuildElectricalPanel(props, new Vector3(-3.8f, 1.55f, -11.5f), 90f);
            BuildElectricalPanel(props, new Vector3(3.8f, 1.55f, -11.5f), -90f);
            BuildCable(props, new Vector3(-2.5f, 0.12f, -10.2f), new Vector3(2.1f, 0.06f, 0.06f), Materials["Red"]);
            BuildCable(props, new Vector3(2.4f, 0.12f, -12.2f), new Vector3(2.4f, 0.06f, 0.06f), Materials["Blue"]);

            GameObject onlineLight = new GameObject("Power restored green lamp");
            onlineLight.transform.SetParent(props);
            onlineLight.transform.position = new Vector3(0f, 2.7f, -12.85f);
            Light lamp = onlineLight.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 8f;
            lamp.intensity = 3f;
            PowerReactive reactive = onlineLight.AddComponent<PowerReactive>();
            reactive.onColor = new Color(0.05f, 1f, 0.3f);
        }

        private static void BuildLighting()
        {
            Transform lights = new GameObject("LIGHTING - Practical fixtures, neon and reflections").transform;
            lights.SetParent(world);
            CreatePointLight(lights, "Main cool reflected light", new Vector3(-5.2f, 2.55f, 1f), new Color(0.28f, 0.62f, 1f), 10f, 2.6f);
            CreatePointLight(lights, "Main warm reflected light", new Vector3(5.2f, 2.55f, 1f), new Color(1f, 0.35f, 0.5f), 10f, 2.4f);
            CreateCeilingFixture(lights, "Main practical north", new Vector3(0f, 3.25f, 4.2f), new Color(0.72f, 0.83f, 1f), 8.5f, 2.8f);
            CreateCeilingFixture(lights, "Main practical south", new Vector3(0f, 3.25f, -4.2f), new Color(0.72f, 0.83f, 1f), 8.5f, 2.6f);
            CreateCeilingFixture(lights, "Office warm practical", new Vector3(15f, 3.22f, 3f), new Color(1f, 0.72f, 0.5f), 7f, 2.5f);
            CreateCeilingFixture(lights, "Prize neutral practical", new Vector3(-15f, 3.22f, 3f), new Color(0.56f, 0.72f, 1f), 7f, 2.6f);
            CreateCeilingFixture(lights, "Storage old practical", new Vector3(-15f, 3.2f, -7f), new Color(0.48f, 0.55f, 0.7f), 6f, 1.6f);
            CreateCeilingFixture(lights, "Power emergency practical", new Vector3(0f, 3.18f, -10.5f), new Color(1f, 0.18f, 0.08f), 6f, 1.7f);
            CreateReflectionProbe(lights, "Main room reflections", new Vector3(0f, 1.6f, 0f), new Vector3(21f, 3.2f, 15f));
            CreateReflectionProbe(lights, "Side rooms reflections", new Vector3(0f, 1.6f, 3f), new Vector3(38f, 3.2f, 10f));

            GameObject sun = new GameObject("Very soft fill light");
            sun.transform.SetParent(lights);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light directional = sun.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(0.42f, 0.48f, 0.62f);
            directional.intensity = 0.11f;
            directional.shadows = LightShadows.Soft;
            directional.shadowStrength = 0.55f;
        }

        private static void BuildPlayer(ArcadeGameManager manager)
        {
            GameObject playerObject = new GameObject("PLAYER - Animated Third Person Young Man");
            playerObject.transform.SetParent(world);
            playerObject.transform.position = new Vector3(0f, 0.04f, 1.2f);
            playerObject.transform.rotation = Quaternion.identity;
            playerObject.layer = 2; // Ignore Raycast: the interaction camera must see past the character.

            CharacterController controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.78f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.89f, 0f);
            controller.stepOffset = 0.22f;
            controller.slopeLimit = 50f;

            Transform visualRoot = new GameObject("Young Man Model and Rig - CC0 Quaternius").transform;
            visualRoot.SetParent(playerObject.transform);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            ImportedCharacterAnimator importedAnimator = BuildImportedYoungMan(visualRoot);
            YoungManAnimator youngManAnimator = importedAnimator == null ? BuildYoungMan(visualRoot) : null;
            SetLayerRecursively(visualRoot.gameObject, 2);

            GameObject cameraObject = new GameObject("Third Person Camera");
            cameraObject.transform.SetParent(world);
            cameraObject.transform.position = new Vector3(0f, 2.2f, -3.2f);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.06f;
            camera.farClipPlane = 70f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.006f, 0.018f);
            cameraObject.AddComponent<AudioListener>();

            ThirdPersonController thirdPerson = playerObject.AddComponent<ThirdPersonController>();
            thirdPerson.viewCamera = camera;
            thirdPerson.visualRoot = visualRoot;
            thirdPerson.characterAnimator = youngManAnimator;
            thirdPerson.importedCharacterAnimator = importedAnimator;
            manager.RegisterPlayer(thirdPerson, camera);
        }

        private static ImportedCharacterAnimator BuildImportedYoungMan(Transform modelRoot)
        {
            const string resourcePath = "Characters/Quaternius/Smooth_Male_Casual";
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return null;

            GameObject character = Object.Instantiate(prefab, modelRoot);
            character.name = "Smooth Male Casual - Animated CC0 Model";
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;
            character.transform.localScale = Vector3.one;
            RefineImportedRenderers(character, 0.04f, 0.38f);

            Animation animationComponent = character.GetComponent<Animation>();
            if (animationComponent == null) animationComponent = character.AddComponent<Animation>();
            ImportedCharacterAnimator driver = modelRoot.gameObject.AddComponent<ImportedCharacterAnimator>();
            driver.Configure(animationComponent, Resources.LoadAll<AnimationClip>(resourcePath));

            if (!driver.IsReady)
            {
                Object.Destroy(character);
                Object.Destroy(driver);
                return null;
            }
            modelRoot.gameObject.AddComponent<CharacterScaleNormalizer>();
            return driver;
        }

        private static YoungManAnimator BuildYoungMan(Transform modelRoot)
        {
            // Torso and clothing.
            Cube("Blue jacket torso", modelRoot, new Vector3(0f, 1.38f, 0f), new Vector3(0.72f, 0.78f, 0.38f), Materials["Shirt"], false);
            Cube("White shirt", modelRoot, new Vector3(0f, 1.43f, 0.205f), new Vector3(0.28f, 0.58f, 0.035f), Materials["White"], false);
            Cube("Jeans pelvis", modelRoot, new Vector3(0f, 0.92f, 0f), new Vector3(0.58f, 0.32f, 0.38f), Materials["Jeans"], false);
            Primitive("Neck", PrimitiveType.Cylinder, modelRoot, new Vector3(0f, 1.82f, 0f), new Vector3(0.12f, 0.13f, 0.12f), Materials["Skin"], false);

            // Head, face and styled hair.
            Transform head = CreateJoint("Head joint", modelRoot, new Vector3(0f, 1.94f, 0f));
            Primitive("Young man head", PrimitiveType.Sphere, head, new Vector3(0f, 0.13f, 0f), new Vector3(0.47f, 0.55f, 0.45f), Materials["Skin"], false);
            Primitive("Hair cap", PrimitiveType.Sphere, head, new Vector3(0f, 0.36f, -0.015f), new Vector3(0.5f, 0.25f, 0.47f), Materials["Hair"], false);
            Cube("Hair fringe left", head, new Vector3(-0.14f, 0.29f, 0.405f), new Vector3(0.18f, 0.18f, 0.08f), Materials["Hair"], false);
            Cube("Hair fringe right", head, new Vector3(0.12f, 0.31f, 0.405f), new Vector3(0.2f, 0.16f, 0.08f), Materials["Hair"], false);
            Primitive("Left eye", PrimitiveType.Sphere, head, new Vector3(-0.12f, 0.17f, 0.425f), new Vector3(0.045f, 0.055f, 0.025f), Materials["Black"], false);
            Primitive("Right eye", PrimitiveType.Sphere, head, new Vector3(0.12f, 0.17f, 0.425f), new Vector3(0.045f, 0.055f, 0.025f), Materials["Black"], false);
            Cube("Mouth", head, new Vector3(0f, 0.01f, 0.435f), new Vector3(0.14f, 0.025f, 0.018f), Materials["Red"], false);

            // Shoulder joints drive the complete arms for a clean low-poly gait.
            Transform leftArm = CreateJoint("Left shoulder joint", modelRoot, new Vector3(-0.47f, 1.66f, 0f));
            Transform rightArm = CreateJoint("Right shoulder joint", modelRoot, new Vector3(0.47f, 1.66f, 0f));
            BuildArm(leftArm, "Left");
            BuildArm(rightArm, "Right");

            // Hip joints drive the legs, including contrasting sneakers.
            Transform leftLeg = CreateJoint("Left hip joint", modelRoot, new Vector3(-0.19f, 0.88f, 0f));
            Transform rightLeg = CreateJoint("Right hip joint", modelRoot, new Vector3(0.19f, 0.88f, 0f));
            BuildLeg(leftLeg, "Left");
            BuildLeg(rightLeg, "Right");

            YoungManAnimator animator = modelRoot.gameObject.AddComponent<YoungManAnimator>();
            animator.torso = modelRoot;
            animator.head = head;
            animator.leftArm = leftArm;
            animator.rightArm = rightArm;
            animator.leftLeg = leftLeg;
            animator.rightLeg = rightLeg;
            return animator;
        }

        private static void BuildArm(Transform shoulder, string side)
        {
            Cube(side + " jacket sleeve", shoulder, new Vector3(0f, -0.24f, 0f), new Vector3(0.23f, 0.48f, 0.25f), Materials["Shirt"], false);
            Cube(side + " forearm", shoulder, new Vector3(0f, -0.58f, 0f), new Vector3(0.18f, 0.31f, 0.2f), Materials["Skin"], false);
            Primitive(side + " hand", PrimitiveType.Sphere, shoulder, new Vector3(0f, -0.79f, 0f), new Vector3(0.2f, 0.22f, 0.18f), Materials["Skin"], false);
        }

        private static void BuildLeg(Transform hip, string side)
        {
            Cube(side + " jeans leg", hip, new Vector3(0f, -0.38f, 0f), new Vector3(0.26f, 0.72f, 0.3f), Materials["Jeans"], false);
            Cube(side + " sneaker", hip, new Vector3(0f, -0.78f, 0.09f), new Vector3(0.3f, 0.18f, 0.48f), Materials["Shoes"], false);
            Cube(side + " sneaker sole", hip, new Vector3(0f, -0.88f, 0.1f), new Vector3(0.31f, 0.06f, 0.5f), Materials["White"], false);
        }

        private static GameObject BuildArcadeCabinet(Transform parent, string name, Vector3 position, float yaw, Material accent, string marquee, string screenText)
        {
            return BuildRealisticCabinet(parent, name, position, yaw, accent, marquee, screenText);
        }

        private static void BuildPoweredMachine(Transform parent, string name, Vector3 position, float yaw, Material accent, string onlineText, Color onlineColor)
        {
            GameObject machine = BuildArcadeCabinet(parent, name + " powered cabinet", position, yaw, accent, name, string.Empty);
            Transform screen = machine.transform.Find("Screen");
            PowerReactive screenPower = screen.gameObject.AddComponent<PowerReactive>();
            screenPower.offColor = new Color(0.008f, 0.01f, 0.014f);
            screenPower.onColor = onlineColor * 0.65f;
            MachineAnimator machineAnimation = machine.GetComponent<MachineAnimator>();
            machineAnimation.requiresPower = true;
            machineAnimation.powerState = screenPower;
            machineAnimation.glowColor = onlineColor;

            TextMesh screenLabel = machineAnimation.screenText.GetComponent<TextMesh>();
            screenLabel.gameObject.name = "Powered number display";
            PowerReactive textPower = screenLabel.gameObject.AddComponent<PowerReactive>();
            textPower.offText = "NO SIGNAL";
            textPower.onText = onlineText;
            textPower.offColor = new Color(0.08f, 0.09f, 0.11f);
            textPower.onColor = onlineColor;
            AddInteraction(machine, InteractionKind.Information, "Inspect tournament machine",
                "This tournament cabinet is offline. Restore the main power to read its number.");
        }

        private static void BuildSign(Transform parent, string name, Vector3 position, float yaw, Vector2 size, string visibleText, string inspectText, Material plaqueMaterial, Color textColor)
        {
            Transform sign = new GameObject(name).transform;
            sign.SetParent(parent);
            sign.position = position;
            sign.rotation = Quaternion.Euler(0f, yaw, 0f);
            DetailBox("Brushed sign frame", sign, Vector3.zero, new Vector3(size.x, size.y, 0.08f), Materials["DarkMetal"], .025f, true);
            DetailBox("Printed sign face", sign, new Vector3(0,0,-.043f), new Vector3(size.x-.045f, size.y-.045f, .009f), Materials["SignFace"], .012f);
            Cube("Sign color key", sign, new Vector3(-size.x*.43f,0,-.051f), new Vector3(.025f,size.y*.76f,.006f), plaqueMaterial, false);
            TextMesh label = CreateText(visibleText, sign, new Vector3(0.025f, 0f, -0.057f), Quaternion.identity,
                Mathf.Clamp(0.055f - visibleText.Length * 0.00035f, 0.025f, 0.055f), new Color(.92f,.94f,.92f), 64);
            FitWorldLabel fit=label.gameObject.AddComponent<FitWorldLabel>();fit.maximum=size*.82f;
            AddInteraction(sign.gameObject, InteractionKind.Information, "Read " + name, inspectText);
        }

        private static void BuildKeypad(string name, Vector3 worldPosition, float yaw, InteractionKind kind, string prompt, Material accent, Transform parent = null)
        {
            Transform keypad = new GameObject(name).transform;
            keypad.SetParent(parent == null ? world : parent);
            keypad.position = worldPosition;
            keypad.rotation = Quaternion.Euler(0f, yaw, 0f);
            Cube("Keypad body", keypad, Vector3.zero, new Vector3(0.38f, 0.55f, 0.13f), Materials["Black"]);
            Cube("Keypad screen", keypad, new Vector3(0f, 0.16f, -0.08f), new Vector3(0.27f, 0.12f, 0.025f), accent, false);
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    Cube("Key", keypad, new Vector3((col - 1) * 0.085f, 0.04f - row * 0.085f, -0.08f), new Vector3(0.045f, 0.045f, 0.02f), Materials["Metal"], false);
            AddInteraction(keypad.gameObject, kind, prompt, string.Empty);
        }

        private static void BuildToken(Transform parent, string name, string id, string displayName, Vector3 position, Material material)
        {
            GameObject token = new GameObject(name);
            token.transform.SetParent(parent, false);
            token.transform.localPosition = position;
            Transform coin = new GameObject("Spinning token detail").transform;
            coin.SetParent(token.transform, false);
            coin.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Primitive("Brass coin rim", PrimitiveType.Cylinder, coin, Vector3.zero, new Vector3(0.23f, 0.024f, 0.23f), Materials["Brass"], false);
            Primitive("Colored enamel face", PrimitiveType.Cylinder, coin, new Vector3(0f, 0.025f, 0f), new Vector3(0.18f, 0.004f, 0.18f), material, false);
            SphereCollider trigger = token.AddComponent<SphereCollider>();
            trigger.radius = 0.3f;
            trigger.isTrigger = true;
            token.AddComponent<TokenPickup>().coin = coin;
            Interactable interactable = token.GetComponent<Interactable>();
            interactable.kind = InteractionKind.CollectItem;
            interactable.prompt = "Take " + displayName + " (or walk closer)";
            interactable.itemId = id;
            interactable.itemDisplayName = displayName;
        }

        private static void BuildBox(Transform parent, string name, Vector3 position, Vector3 size, string number, bool interactive)
        {
            Transform box = new GameObject(name).transform;
            box.SetParent(parent);
            box.position = position;
            Cube("Cardboard box", box, Vector3.zero, size, Materials["Cardboard"]);
            Cube("Tape", box, new Vector3(0f, size.y * 0.5f + 0.012f, 0f), new Vector3(size.x * 0.22f, 0.025f, size.z), Materials["White"], false);
            CreateText(number, box, new Vector3(0f, 0f, -size.z * 0.505f), Quaternion.identity, 0.12f, Color.black, 48);
            if (interactive) AddInteraction(box.gameObject, InteractionKind.Box42, "Open Box 42", string.Empty);
            else AddInteraction(box.gameObject, InteractionKind.Information, "Inspect box " + number, "A dusty storage box marked " + number + ". Nothing useful is inside.");
        }

        private static void BuildFuseProp(Transform parent, Vector3 position)
        {
            Transform fuse = new GameObject("Visible replacement fuse prop").transform;
            fuse.SetParent(parent);
            fuse.position = position;
            fuse.rotation = Quaternion.Euler(0f, 0f, 90f);
            Primitive("Fuse glass", PrimitiveType.Cylinder, fuse, Vector3.zero, new Vector3(0.09f, 0.28f, 0.09f), Materials["Cyan"], false);
            Primitive("Fuse cap A", PrimitiveType.Cylinder, fuse, new Vector3(0f, 0.31f, 0f), new Vector3(0.12f, 0.08f, 0.12f), Materials["Metal"], false);
            Primitive("Fuse cap B", PrimitiveType.Cylinder, fuse, new Vector3(0f, -0.31f, 0f), new Vector3(0.12f, 0.08f, 0.12f), Materials["Metal"], false);
        }

        private static void BuildMascotRobot(Transform parent, string name, Vector3 position, Color color)
        {
            string badge = name.Contains("Byte") ? "BYTE BUDDY" : "PIXEL PAL";
            Material mascot = MakeMaterial(name + " material", color, true);
            Transform robot = new GameObject(name + " - Low-poly character").transform;
            robot.SetParent(parent);
            robot.position = position;
            Cube("Body", robot, new Vector3(0f, 0.95f, 0f), new Vector3(0.8f, 0.9f, 0.55f), mascot);
            Primitive("Head", PrimitiveType.Sphere, robot, new Vector3(0f, 1.72f, 0f), new Vector3(0.65f, 0.55f, 0.55f), mascot, false);
            Primitive("Left eye", PrimitiveType.Sphere, robot, new Vector3(-0.16f, 1.78f, -0.25f), new Vector3(0.09f, 0.09f, 0.06f), Materials["Cyan"], false);
            Primitive("Right eye", PrimitiveType.Sphere, robot, new Vector3(0.16f, 1.78f, -0.25f), new Vector3(0.09f, 0.09f, 0.06f), Materials["Cyan"], false);
            Cube("Left arm", robot, new Vector3(-0.58f, 1.02f, 0f), new Vector3(0.22f, 0.75f, 0.22f), mascot, false);
            Cube("Right arm", robot, new Vector3(0.58f, 1.02f, 0f), new Vector3(0.22f, 0.75f, 0.22f), mascot, false);
            Cube("Left leg", robot, new Vector3(-0.22f, 0.3f, 0f), new Vector3(0.25f, 0.55f, 0.3f), Materials["DarkMetal"], false);
            Cube("Right leg", robot, new Vector3(0.22f, 0.3f, 0f), new Vector3(0.25f, 0.55f, 0.3f), Materials["DarkMetal"], false);
            BuildSign(robot, "Mascot badge", robot.TransformPoint(new Vector3(0f, 1f, -0.31f)), 0f, new Vector2(0.55f, 0.24f),
                badge, badge + " — one of the arcade's cheerful low-poly mascot characters.", Materials["Yellow"], Color.black);
        }

        private static void BuildLegacyDetailedAttractions(Transform parent)
        {
            BuildShowcaseMachine(parent, "Working claw machine", "Models/KenneyArcade/claw-machine",
                new Vector3(9f, 0f, 4.8f), 90f, 2.45f,
                new Vector3(0f, 1.16f, 0f), new Vector3(1.65f, 2.34f, 1.72f),
                DisplayMachineAnimator.MotionStyle.Slide, new Vector3(0f, 1.72f, -0.78f), Vector3.right,
                0.34f, new Color(1f, 0.16f, 0.72f), "Watch the claw machine");

            BuildShowcaseMachine(parent, "Basketball challenge", "Models/KenneyArcade/basketball-game",
                new Vector3(-9f, 0f, -2.8f), -90f, 2.65f,
                new Vector3(0f, 1.06f, 0f), new Vector3(1.76f, 2.14f, 2.76f),
                DisplayMachineAnimator.MotionStyle.Bounce, new Vector3(0f, 0.72f, -0.72f), Vector3.up,
                0.58f, new Color(1f, 0.34f, 0.06f), "Watch the basketball challenge");

            BuildShowcaseMachine(parent, "Air hockey table", "Models/KenneyArcade/air-hockey",
                new Vector3(0f, 0f, -2.5f), 0f, 2.35f,
                new Vector3(0f, 0.6f, 0f), new Vector3(2.42f, 1.22f, 1.76f),
                DisplayMachineAnimator.MotionStyle.Orbit, new Vector3(0f, 1.22f, 0f), Vector3.up,
                0.34f, new Color(0.08f, 0.85f, 1f), "Watch the air-hockey puck");
        }

        private static void BuildShowcaseMachine(Transform parent, string name, string resourcePath,
            Vector3 position, float yaw, float scale, Vector3 colliderCenter, Vector3 colliderSize,
            DisplayMachineAnimator.MotionStyle motion, Vector3 movingPartPosition, Vector3 axis,
            float amplitude, Color accentColor, string prompt)
        {
            Transform root = new GameObject(name + " - Detailed animated model").transform;
            root.SetParent(parent);
            root.position = position;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (!BuildImportedProp(root, resourcePath, name + " CC0 model", Vector3.one * scale))
                Cube("Fallback attraction cabinet", root, colliderCenter, colliderSize, Materials["DarkMetal"]);
            AddBoxCollider(root.gameObject, colliderCenter, colliderSize);

            GameObject movingPart;
            if (motion == DisplayMachineAnimator.MotionStyle.Slide)
            {
                Transform claw = new GameObject("Animated mechanical claw").transform;
                claw.SetParent(root);
                claw.localPosition = movingPartPosition;
                Primitive("Claw motor", PrimitiveType.Cylinder, claw, Vector3.zero, new Vector3(0.08f, 0.1f, 0.08f), Materials["Metal"], false);
                Cube("Claw finger left", claw, new Vector3(-0.08f, -0.15f, 0f), new Vector3(0.035f, 0.24f, 0.035f), Materials["Metal"], false).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                Cube("Claw finger right", claw, new Vector3(0.08f, -0.15f, 0f), new Vector3(0.035f, 0.24f, 0.035f), Materials["Metal"], false).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                movingPart = claw.gameObject;
            }
            else if (motion == DisplayMachineAnimator.MotionStyle.Orbit)
            {
                movingPart = Primitive("Animated air-hockey puck", PrimitiveType.Cylinder, root, movingPartPosition,
                    new Vector3(0.11f, 0.035f, 0.11f), Materials["Pink"], false);
            }
            else
            {
                movingPart = Primitive("Animated basketball", PrimitiveType.Sphere, root, movingPartPosition,
                    new Vector3(0.2f, 0.2f, 0.2f), Materials["Orange"], false);
            }

            Light accent = CreateAccentLight(root, "Animated attraction glow", movingPartPosition + Vector3.up * 0.2f, accentColor, 3.2f, 1.2f);
            DisplayMachineAnimator animation = root.gameObject.AddComponent<DisplayMachineAnimator>();
            animation.movingPart = movingPart.transform;
            animation.accentLight = accent;
            animation.motionStyle = motion;
            animation.axis = axis;
            animation.amplitude = amplitude;
            animation.speed = motion == DisplayMachineAnimator.MotionStyle.Orbit ? 1.25f : 1f;
            AddInteraction(root.gameObject, InteractionKind.Information, prompt,
                name + " is running a polished attract-mode animation while the arcade waits for a player.");
        }

        private static void BuildLegacyPrizeWheel(Transform parent, Vector3 position, float yaw)
        {
            Transform root = new GameObject("Animated prize wheel and ticket machine").transform;
            root.SetParent(parent);
            root.position = position;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            BuildImportedProp(root, "Models/KenneyArcade/prize-wheel", "Detailed CC0 prize wheel", Vector3.one * 2.5f);
            AddBoxCollider(root.gameObject, new Vector3(0f, 0.82f, 0f), new Vector3(1.3f, 1.65f, 1.05f));

            GameObject rotor = Primitive("Spinning illuminated prize rotor", PrimitiveType.Cylinder, root,
                new Vector3(0f, 0.95f, -0.55f), new Vector3(0.42f, 0.018f, 0.42f), Materials["Pink"], false);
            rotor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            DisplayMachineAnimator animation = root.gameObject.AddComponent<DisplayMachineAnimator>();
            animation.movingPart = rotor.transform;
            animation.motionStyle = DisplayMachineAnimator.MotionStyle.Spin;
            animation.axis = Vector3.up;
            animation.speed = 0.7f;
            AddInteraction(root.gameObject, InteractionKind.Information, "Watch the prize wheel",
                "The motor turns smoothly, but its payout tray is empty.");
        }

        private static void BuildLegacyVendingMachine(Transform parent, Vector3 position, float yaw)
        {
            Transform machine = new GameObject("Broken vending machine").transform;
            machine.SetParent(parent);
            machine.position = position;
            machine.rotation = Quaternion.Euler(0f, yaw, 0f);
            bool imported = BuildImportedProp(machine, "Models/KenneyArcade/vending-machine", "Detailed CC0 vending machine", Vector3.one * 3f);
            if (!imported)
                Cube("Vending body", machine, new Vector3(0f, 1.15f, 0f), new Vector3(1.4f, 2.3f, 0.8f), Materials["Red"]);
            AddBoxCollider(machine.gameObject, new Vector3(0f, 1.13f, 0.04f), new Vector3(1.52f, 2.26f, 1.45f));
            GameObject vendingDisplay = Cube("Dark display", machine, new Vector3(0f, 1.5f, -0.69f), new Vector3(1.02f, 0.72f, 0.025f), Materials["Glass"], false);
            TextMesh vendingText = CreateText("OUT OF\nORDER", machine, new Vector3(0f, 1.5f, -0.712f), Quaternion.identity, 0.045f, Color.white, 52);
            MachineAnimator vendingAnimation = machine.gameObject.AddComponent<MachineAnimator>();
            vendingAnimation.screenRenderer = vendingDisplay.GetComponent<Renderer>();
            vendingAnimation.screenText = vendingText.transform;
            vendingAnimation.glowColor = new Color(1f, 0.08f, 0.03f);
            vendingAnimation.speed = 1.2f;
            AddInteraction(machine.gameObject, InteractionKind.Information, "Inspect vending machine", "OUT OF ORDER. The coin return contains only dust.");
        }

        private static void BuildBench(Transform parent, Vector3 position)
        {
            Transform bench = new GameObject("Arcade bench").transform;
            bench.SetParent(parent);
            bench.position = position;
            DetailBox("Upholstered bench seat", bench, new Vector3(0f, 0.48f, 0f), new Vector3(2.2f, 0.18f, 0.6f), Materials["Rubber"], .07f, true);
            DetailBox("Bench backrest", bench, new Vector3(0f, .8f, .24f), new Vector3(2.2f, .47f, .12f), Materials["Wood"], .035f, true);
            DetailBox("Leg L", bench, new Vector3(-.85f, 0.24f, 0f), new Vector3(0.08f, 0.48f, 0.5f), Materials["Metal"], .015f, true);
            DetailBox("Leg R", bench, new Vector3(.85f, 0.24f, 0f), new Vector3(0.08f, 0.48f, 0.5f), Materials["Metal"], .015f, true);
        }

        private static void BuildTrashCan(Transform parent, Vector3 position)
        {
            Primitive("Wire trash can", PrimitiveType.Cylinder, parent, position + Vector3.up * 0.42f, new Vector3(0.42f, 0.42f, 0.42f), Materials["Metal"], true);
        }

        private static void BuildTelephone(Transform desk)
        {
            Transform phone = new GameObject("Push-button telephone").transform;
            phone.SetParent(desk);
            phone.localPosition = new Vector3(-1.05f, 1.04f, 0.15f);
            Cube("Phone base", phone, Vector3.zero, new Vector3(0.65f, 0.18f, 0.48f), Materials["Black"]);
            Cube("Handset", phone, new Vector3(0f, 0.16f, 0f), new Vector3(0.72f, 0.12f, 0.18f), Materials["DarkMetal"]);
            AddInteraction(phone.gameObject, InteractionKind.Information, "Inspect telephone", "No dial tone. A faded sticker says: FOR EMERGENCIES CALL 199X.");
        }

        private static void BuildFilingCabinet(Transform parent, Vector3 position)
        {
            Transform cabinet = new GameObject("Filing cabinet").transform;
            cabinet.SetParent(parent);
            cabinet.position = position;
            Cube("Cabinet", cabinet, new Vector3(0f, 0.9f, 0f), new Vector3(1.1f, 1.8f, 0.75f), Materials["Metal"]);
            for (int i = 0; i < 3; i++)
            {
                Cube("Drawer seam", cabinet, new Vector3(0f, 0.5f + i * 0.52f, -0.39f), new Vector3(0.92f, 0.035f, 0.02f), Materials["Black"], false);
                Cube("Drawer handle", cabinet, new Vector3(0f, 0.62f + i * 0.52f, -0.43f), new Vector3(0.32f, 0.08f, 0.05f), Materials["DarkMetal"], false);
            }
        }

        private static void BuildLegacyOfficeChair(Transform parent, Vector3 position)
        {
            Transform chair = new GameObject("Manager chair").transform;
            chair.SetParent(parent);
            chair.position = position;
            Cube("Seat", chair, new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.16f, 0.75f), Materials["DarkMetal"]);
            Cube("Back", chair, new Vector3(0f, 1.05f, 0.32f), new Vector3(0.78f, 0.9f, 0.16f), Materials["DarkMetal"]);
            Primitive("Post", PrimitiveType.Cylinder, chair, new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.3f, 0.08f), Materials["Metal"], false);
        }

        private static void BuildPrizeShelf(Transform parent, Vector3 position)
        {
            Transform shelf = new GameObject("Prize shelf").transform;
            shelf.SetParent(parent);
            shelf.position = position;
            Cube("Back", shelf, new Vector3(0f, 1.25f, 0.35f), new Vector3(2.5f, 2.5f, 0.12f), Materials["Wood"]);
            for (int i = 0; i < 3; i++)
                Cube("Shelf", shelf, new Vector3(0f, 0.35f + i * 0.8f, 0f), new Vector3(2.5f, 0.12f, 0.7f), Materials["Wood"]);
            Primitive("Prize ball", PrimitiveType.Sphere, shelf, new Vector3(0.55f, 0.66f, -0.05f), new Vector3(0.28f, 0.28f, 0.28f), Materials["Pink"], false);
            Primitive("Prize ball", PrimitiveType.Sphere, shelf, new Vector3(-0.5f, 1.5f, -0.05f), new Vector3(0.3f, 0.3f, 0.3f), Materials["Cyan"], false);
        }

        private static void BuildBrokenCabinet(Transform parent, Vector3 position, float yaw)
        {
            GameObject cabinet = BuildArcadeCabinet(parent, "Broken arcade shell", position, yaw, Materials["Metal"], "BROKEN", "");
            cabinet.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
        }

        private static void BuildShelf(Transform parent, Vector3 position)
        {
            Transform shelf = new GameObject("Storage metal shelf").transform;
            shelf.SetParent(parent);
            shelf.position = position;
            Cube("Left post", shelf, new Vector3(-0.75f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 0.6f), Materials["Metal"]);
            Cube("Right post", shelf, new Vector3(0.75f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 0.6f), Materials["Metal"]);
            for (int i = 0; i < 3; i++)
                Cube("Shelf plate", shelf, new Vector3(0f, 0.25f + i * 0.9f, 0f), new Vector3(1.6f, 0.08f, 0.7f), Materials["Metal"]);
        }

        private static void BuildElectricalPanel(Transform parent, Vector3 position, float yaw)
        {
            Transform panel = new GameObject("Electrical breaker panel").transform;
            panel.SetParent(parent);
            panel.position = position;
            panel.rotation = Quaternion.Euler(0f, yaw, 0f);
            Cube("Panel box", panel, Vector3.zero, new Vector3(1.7f, 2.2f, 0.35f), Materials["Metal"]);
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 2; col++)
                    Cube("Breaker", panel, new Vector3((col - 0.5f) * 0.5f, 0.62f - row * 0.4f, -0.2f), new Vector3(0.28f, 0.18f, 0.08f), Materials[(row + col) % 2 == 0 ? "Red" : "Blue"], false);
        }

        private static void BuildCable(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            Primitive("Loose cable", PrimitiveType.Cylinder, parent, position, scale, material, false).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static bool BuildImportedProp(Transform parent, string resourcePath, string name, Vector3 scale)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return false;
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = scale;
            RefineImportedRenderers(instance, 0.18f, 0.48f);
            return true;
        }

        private static void RefineImportedRenderers(GameObject rootObject, float metallic, float smoothness)
        {
            Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                Material[] sourceMaterials = renderer.sharedMaterials;
                Material[] polishedMaterials = new Material[sourceMaterials.Length];
                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material source = sourceMaterials[i];
                    if (source == null) continue;
                    bool glass = source.name.ToLowerInvariant().Contains("glass");
                    if (glass)
                    {
                        polishedMaterials[i] = MakeGlass();
                        continue;
                    }

                    Color sourceColor = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
                        source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                    Texture sourceTexture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") :
                        source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                    Material polished = MakeMaterial(source.name + " - Pipeline polished", sourceColor, false, metallic, smoothness);
                    if (sourceTexture != null) SetMaterialTexture(polished, "_BaseMap", "_MainTex", sourceTexture);
                    polishedMaterials[i] = polished;
                }
                renderer.sharedMaterials = polishedMaterials;
            }
        }

        private static void AddBoxCollider(GameObject target, Vector3 center, Vector3 size)
        {
            if (target.GetComponent<Collider>() != null) return;
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static void CreateCeilingFixture(Transform parent, string name, Vector3 position, Color color, float range, float intensity)
        {
            Transform fixture = new GameObject(name).transform;
            fixture.SetParent(parent);
            fixture.position = position;
            Material diffuser = MakeMaterial(name + " diffuser", color, true, 0f, 0.55f);
            Cube("Metal fixture housing", fixture, Vector3.zero, new Vector3(2.2f, 0.11f, 0.62f), Materials["DarkMetal"], false);
            Cube("Luminous diffuser", fixture, new Vector3(0f, -0.07f, 0f), new Vector3(1.95f, 0.035f, 0.47f), diffuser, false);
            Light practical = CreateAccentLight(fixture, "Soft practical light", new Vector3(0f, -0.18f, 0f), color, range, intensity);
            practical.type = LightType.Spot;
            practical.transform.localRotation = Quaternion.Euler(90,0,0);
            practical.spotAngle = 125f;
            practical.innerSpotAngle = 95f;
            practical.intensity = intensity * 2.8f;
            practical.shadows = name.StartsWith("Main") ? LightShadows.Soft : LightShadows.None;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                practical.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            practical.shadowStrength = 0.52f;
        }

        private static Light CreateAccentLight(Transform parent, string name, Vector3 localPosition, Color color, float range, float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.localPosition = localPosition;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        private static void CreateReflectionProbe(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject probeObject = new GameObject(name);
            probeObject.transform.SetParent(parent);
            probeObject.transform.position = position;
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = size;
            probe.boxProjection = true;
            probe.intensity = 0.72f;
            probe.resolution = 128;
            probe.nearClipPlane = 0.12f;
            probe.farClipPlane = 45f;
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float range, float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.shadowStrength = 0.65f;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void SetMaterialTexture(Material material, string modernProperty, string legacyProperty, Texture texture)
        {
            if (material.HasProperty(modernProperty)) material.SetTexture(modernProperty, texture);
            if (material.HasProperty(legacyProperty)) material.SetTexture(legacyProperty, texture);
        }

        private static void SetMaterialScale(Material material, string modernProperty, string legacyProperty, Vector2 scale)
        {
            if (material.HasProperty(modernProperty)) material.SetTextureScale(modernProperty, scale);
            if (material.HasProperty(legacyProperty)) material.SetTextureScale(legacyProperty, scale);
        }

        private static void AddInteraction(GameObject target, InteractionKind kind, string prompt, string information)
        {
            Interactable interactable = target.GetComponent<Interactable>();
            if (interactable == null) interactable = target.AddComponent<Interactable>();
            interactable.kind = kind;
            interactable.prompt = prompt;
            interactable.information = information;
        }

        private static Transform CreateJoint(string name, Transform parent, Vector3 localPosition)
        {
            Transform joint = new GameObject(name).transform;
            joint.SetParent(parent);
            joint.localPosition = localPosition;
            joint.localRotation = Quaternion.identity;
            return joint;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider = true)
        {
            return Primitive(name, PrimitiveType.Cube, parent, localPosition, localScale, material, collider);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = localScale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            if (!collider)
            {
                Collider primitiveCollider = gameObject.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) Object.DestroyImmediate(primitiveCollider);
                    else
#endif
                        Object.Destroy(primitiveCollider);
                }
            }
            return gameObject;
        }

        private static TextMesh CreateText(string text, Transform parent, Vector3 localPosition, Quaternion localRotation, float characterSize, Color color, int fontSize)
        {
            GameObject textObject = new GameObject("Text - " + text.Replace("\n", " "));
            textObject.transform.SetParent(parent);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = localRotation;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.lineSpacing = 0.9f;
            textMesh.richText = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.font = font;
            Shader textShader = Shader.Find("ArcadeLockdown/WorldText");
            if (textShader != null)
            {
                Material fontMaterial = new Material(textShader) { name = "Depth-tested world lettering" };
                fontMaterial.mainTexture = font.material.mainTexture;
                textObject.GetComponent<Renderer>().sharedMaterial = fontMaterial;
            }
            textObject.AddComponent<ReadableWorldText>();
            return textMesh;
        }
    }
}
