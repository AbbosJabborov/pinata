using System.IO;
using Pinata.Candy;
using Pinata.Effects;
using Pinata.Gameplay;
using Pinata.Player;
using Pinata.Tools;
using Pinata.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Pinata.EditorTools
{
    public static class SceneSetupHelper
    {
        [MenuItem("Pinata/Setup Scene")]
        public static string SetupPinataScene()
        {
            EnsureFolders();

            // 1. Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material floorMat = GetOrCreateMaterial("Assets/Materials/Mat_Floor.mat", litShader, new Color(0.18f, 0.20f, 0.23f), 0.35f);
            Material wallMat = GetOrCreateMaterial("Assets/Materials/Mat_Wall.mat", litShader, new Color(0.85f, 0.87f, 0.88f), 0.15f);
            Material beamMat = GetOrCreateMaterial("Assets/Materials/Mat_Beam.mat", litShader, new Color(0.95f, 0.65f, 0.10f), 0.4f);

            Material pinkMat = GetOrCreateMaterial("Assets/Materials/Mat_PinataPink.mat", litShader, new Color(1.0f, 0.22f, 0.55f), 0.2f);
            Material cyanMat = GetOrCreateMaterial("Assets/Materials/Mat_PinataCyan.mat", litShader, new Color(0.10f, 0.80f, 0.95f), 0.2f);
            Material yellowMat = GetOrCreateMaterial("Assets/Materials/Mat_PinataYellow.mat", litShader, new Color(1.0f, 0.85f, 0.15f), 0.2f);

            Material mintMat = GetOrCreateMaterial("Assets/Materials/Mat_Mint.mat", litShader, new Color(0.15f, 0.95f, 0.80f), 0.75f);
            Material chocMat = GetOrCreateMaterial("Assets/Materials/Mat_Chocolate.mat", litShader, new Color(0.38f, 0.20f, 0.10f), 0.5f);
            Material truffleMat = GetOrCreateMaterial("Assets/Materials/Mat_Truffle.mat", litShader, new Color(1.0f, 0.82f, 0.20f), 0.9f, 0.95f);
            Material batMat = GetOrCreateMaterial("Assets/Materials/Mat_Bat.mat", litShader, new Color(0.72f, 0.45f, 0.25f), 0.4f);
            Material ropeMat = GetOrCreateMaterial("Assets/Materials/Mat_Rope.mat", litShader, new Color(0.82f, 0.72f, 0.52f), 0.2f);

            // Milestone 2 Materials
            Material tableMat = GetOrCreateMaterial("Assets/Materials/Mat_Table.mat", litShader, new Color(0.24f, 0.25f, 0.28f), 0.3f);
            Material crateMat = GetOrCreateMaterial("Assets/Materials/Mat_Crate.mat", litShader, new Color(0.18f, 0.19f, 0.21f), 0.2f);
            Material vacBodyMat = GetOrCreateMaterial("Assets/Materials/Mat_VacuumBody.mat", litShader, new Color(0.95f, 0.65f, 0.10f), 0.45f);
            Material vacNozzleMat = GetOrCreateMaterial("Assets/Materials/Mat_VacuumNozzle.mat", litShader, new Color(0.12f, 0.12f, 0.12f), 0.2f);
            Material kioskMat = GetOrCreateMaterial("Assets/Materials/Mat_Kiosk.mat", litShader, new Color(0.18f, 0.20f, 0.22f), 0.6f, 0.7f);
            Material kioskScreenMat = GetOrCreateMaterial("Assets/Materials/Mat_KioskScreen.mat", litShader, new Color(0.1f, 0.8f, 0.95f), 0.9f);

            var basketMaterials = new System.Collections.Generic.Dictionary<CandyType, Material>
            {
                { CandyType.Blue, GetOrCreateMaterial("Assets/Materials/Mat_Basket_Blue.mat", litShader, new Color(0.2f, 0.75f, 1.0f), 0.4f) },
                { CandyType.Green, GetOrCreateMaterial("Assets/Materials/Mat_Basket_Green.mat", litShader, new Color(0.25f, 0.9f, 0.35f), 0.4f) },
                { CandyType.Orange, GetOrCreateMaterial("Assets/Materials/Mat_Basket_Orange.mat", litShader, new Color(1.0f, 0.58f, 0.12f), 0.4f) },
                { CandyType.Pink, GetOrCreateMaterial("Assets/Materials/Mat_Basket_Pink.mat", litShader, new Color(1.0f, 0.35f, 0.72f), 0.4f) },
                { CandyType.Yellow, GetOrCreateMaterial("Assets/Materials/Mat_Basket_Yellow.mat", litShader, new Color(1.0f, 0.88f, 0.15f), 0.4f) },
                { CandyType.Purple, GetOrCreateMaterial("Assets/Materials/Mat_Basket_Purple.mat", litShader, new Color(0.75f, 0.32f, 0.98f), 0.4f) }
            };

            // Particle Material (URP Particles/Unlit)
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? litShader;
            Material particleMat = GetOrCreateMaterial("Assets/Materials/Mat_ParticleConfetti.mat", particleShader, Color.white, 0f);

            // 2. Physics Material - Low bounciness so candies settle realistically
            PhysicsMaterial candyPhysMat = GetOrCreatePhysicsMaterial("Assets/Materials/PhysMat_Candy.physicMaterial", 0.04f, 0.85f);

            // 3. Particle Prefabs
            ParticleSystem hitConfettiPrefab = CreateConfettiPrefab("Assets/Prefabs/FX_HitConfetti.prefab", particleMat, 15, 0.8f);
            ParticleSystem deathConfettiPrefab = CreateConfettiPrefab("Assets/Prefabs/FX_DeathExplosion.prefab", particleMat, 70, 1.8f);

            // 4. Candy Prefabs (6 uploaded models in Assets/Prefabs/Candies/)
            var candyConfigs = new[]
            {
                new { name = "Candy_Blue", type = CandyType.Blue, pool = 25 },
                new { name = "Candy_Green", type = CandyType.Green, pool = 25 },
                new { name = "Candy_Orange", type = CandyType.Orange, pool = 25 },
                new { name = "Candy_Pink", type = CandyType.Pink, pool = 25 },
                new { name = "Candy_Yellow", type = CandyType.Yellow, pool = 25 },
                new { name = "Candy_Purple", type = CandyType.Purple, pool = 25 }
            };

            var candyList = new System.Collections.Generic.List<CandyPool.CandyPrefabEntry>();
            foreach (var cfg in candyConfigs)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Candies/{cfg.name}.prefab");
                if (p != null)
                {
                    candyList.Add(new CandyPool.CandyPrefabEntry { type = cfg.type, prefab = p, initialPoolSize = cfg.pool });
                }
            }

            // 5. Pinata Prefabs (5 uploaded colored models)
            string[] variantNames = new[] { "Blue", "Green", "Purple", "Red", "Yellow" };
            var variantsList = new System.Collections.Generic.List<PinataHealth>();
            foreach (var vn in variantNames)
            {
                var pGo = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Pinatas/{vn}.prefab");
                if (pGo != null)
                {
                    var ph = pGo.GetComponent<PinataHealth>();
                    if (ph != null) variantsList.Add(ph);
                }
            }
            PinataHealth[] pinataVariants = variantsList.ToArray();
            PinataHealth pinataPrefab = pinataVariants.Length > 0 ? pinataVariants[0] : null;

            // 6. Build the Scene
            var scene = EditorSceneManager.GetActiveScene();

            // Clear old non-camera/light objects
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Directional Light" && root.name != "Global Volume")
                {
                    Object.DestroyImmediate(root);
                }
            }

            // Setup Warehouse Room
            GameObject room = new GameObject("Warehouse_Room");
            CreateBox("Floor", room.transform, new Vector3(0, -0.25f, 2.5f), new Vector3(16f, 0.5f, 18f), floorMat);
            CreateBox("Wall_North", room.transform, new Vector3(0, 3f, 11.5f), new Vector3(16f, 6f, 0.5f), wallMat);
            CreateBox("Wall_South", room.transform, new Vector3(0, 3f, -6.5f), new Vector3(16f, 6f, 0.5f), wallMat);
            CreateBox("Wall_East", room.transform, new Vector3(8f, 3f, 2.5f), new Vector3(0.5f, 6f, 18f), wallMat);
            CreateBox("Wall_West", room.transform, new Vector3(-8f, 3f, 2.5f), new Vector3(0.5f, 6f, 18f), wallMat);

            // Ceiling I-Beam for pinata suspension (placed high under ceiling at Y = 5.6m)
            GameObject beam = CreateBox("Ceiling_Beam", room.transform, new Vector3(0, 5.6f, 3.5f), new Vector3(0.4f, 0.4f, 8f), beamMat);
            var beamRb = beam.AddComponent<Rigidbody>();
            beamRb.isKinematic = true;

            // Managers: JuiceEffects, CandyPool & EconomyManager
            GameObject managers = new GameObject("Managers");
            var juice = managers.AddComponent<JuiceEffects>();
            var pool = managers.AddComponent<CandyPool>();
            var economy = managers.AddComponent<EconomyManager>();

            // Assign candy pool entries
            var field = typeof(CandyPool).GetField("candyEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(pool, candyList);
            }

            // Pinata Spawner and Rope
            GameObject spawnerObj = new GameObject("Pinata_Spawner");
            spawnerObj.transform.position = new Vector3(0, 5.6f, 3.5f);
            var spawner = spawnerObj.AddComponent<PinataSpawner>();
            spawner.PinataPrefab = pinataPrefab;
            spawner.PinataVariants = pinataVariants;

            var spawnerAnchorField = typeof(PinataSpawner).GetField("ceilingAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnerAnchorField != null) spawnerAnchorField.SetValue(spawner, beam.transform);

            // Suspension Rope LineRenderer
            GameObject ropeObj = new GameObject("Pinata_Rope");
            ropeObj.transform.SetParent(spawnerObj.transform);
            var line = ropeObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = ropeMat;
            line.startWidth = 0.04f;
            line.endWidth = 0.04f;
            line.widthMultiplier = 1.0f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            line.receiveShadows = true;

            var pinataRope = ropeObj.AddComponent<PinataRope>();
            spawner.Rope = pinataRope;

            // Pre-spawn Piñata in Edit Mode (Hangs 3.2m below ceiling beam at Y = 2.4m)
            GameObject pinataInstance = (GameObject)PrefabUtility.InstantiatePrefab(pinataPrefab.gameObject, spawnerObj.transform);
            pinataInstance.transform.position = new Vector3(0, 2.4f, 3.5f);
            pinataInstance.transform.rotation = Quaternion.identity;

            var pinataHealth = pinataInstance.GetComponent<PinataHealth>();

            // Setup 3D ConfigurableJoint - Anchor is at the beam 3.2m above pinata
            var joint = pinataInstance.GetComponent<ConfigurableJoint>();
            if (joint == null) joint = pinataInstance.AddComponent<ConfigurableJoint>();
            joint.connectedBody = beamRb;

            joint.anchor = pinataInstance.transform.InverseTransformPoint(beam.transform.position);
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            var highLimit = joint.highAngularXLimit; highLimit.limit = 80f; joint.highAngularXLimit = highLimit;
            var lowLimit = joint.lowAngularXLimit; lowLimit.limit = -80f; joint.lowAngularXLimit = lowLimit;
            var yLimit = joint.angularYLimit; yLimit.limit = 75f; joint.angularYLimit = yLimit;
            var zLimit = joint.angularZLimit; zLimit.limit = 80f; joint.angularZLimit = zLimit;

            joint.rotationDriveMode = RotationDriveMode.Slerp;
            var slerp = joint.slerpDrive;
            slerp.positionSpring = 85f;
            slerp.positionDamper = 6.5f;
            slerp.maximumForce = 1200f;
            joint.slerpDrive = slerp;

            pinataRope.SetAnchors(beam.transform, pinataHealth.AttachmentPoint);

            // Ceiling & Warehouse Lighting
            CreateBox("Ceiling", room.transform, new Vector3(0, 6.25f, 2.5f), new Vector3(16f, 0.5f, 18f), wallMat);

            GameObject spotLightObj = new GameObject("Pinata_SpotLight");
            spotLightObj.transform.position = new Vector3(0, 5.8f, 3.5f);
            spotLightObj.transform.rotation = Quaternion.Euler(90f, 0, 0);
            Light spot = spotLightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 8.5f;
            spot.spotAngle = 75f;
            spot.color = new Color(1f, 0.96f, 0.88f);
            spot.intensity = 28f;

            // First Person Player
            GameObject playerObj = new GameObject("Player_FPS");
            playerObj.transform.position = new Vector3(0, 1.0f, 0.5f);
            var cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            var fpsPlayer = playerObj.AddComponent<FirstPersonPlayer>();
            playerObj.tag = "Player";
            playerObj.AddComponent<PlayerInventory>();

            // Camera
            GameObject camObj = new GameObject("CameraHolder");
            camObj.transform.SetParent(playerObj.transform);
            camObj.transform.localPosition = new Vector3(0, 1.65f, 0);

            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            var uac = camObj.AddComponent<UniversalAdditionalCameraData>();
            uac.renderPostProcessing = true;

            var camHolderField = typeof(FirstPersonPlayer).GetField("cameraHolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camHolderField != null) camHolderField.SetValue(fpsPlayer, camObj.transform);

            juice.SetCameraTransform(camObj.transform);

            // 3-Slot Tool Socket & Tools
            GameObject toolSocket = new GameObject("ToolSocket");
            toolSocket.transform.SetParent(camObj.transform);
            toolSocket.transform.localPosition = Vector3.zero;
            toolSocket.transform.localRotation = Quaternion.identity;

            // Slot 0: Bat Tool
            GameObject batSlot = new GameObject("Slot_Bat");
            batSlot.transform.SetParent(toolSocket.transform);
            batSlot.transform.localPosition = Vector3.zero;
            batSlot.transform.localRotation = Quaternion.identity;
            GameObject batModel = CreateBatModel("Bat_Model", batSlot.transform, batMat);
            var batTool = batSlot.AddComponent<BatTool>();
            var batModelField = typeof(BatTool).GetField("batModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (batModelField != null) batModelField.SetValue(batTool, batModel.transform);

            // Slot 1: Broom Tool
            GameObject broomSlot = new GameObject("Slot_Broom");
            broomSlot.transform.SetParent(toolSocket.transform);
            broomSlot.transform.localPosition = Vector3.zero;
            broomSlot.transform.localRotation = Quaternion.identity;
            GameObject broomModel = CreateBroomModel("Broom_Model", broomSlot.transform, batMat, wallMat);
            var broomTool = broomSlot.AddComponent<BroomTool>();
            var broomModelField = typeof(BroomTool).GetField("broomModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (broomModelField != null) broomModelField.SetValue(broomTool, broomModel.transform);
            broomSlot.SetActive(false);

            // Slot 2: Hands Tool
            GameObject handsSlot = new GameObject("Slot_Hands");
            handsSlot.transform.SetParent(toolSocket.transform);
            handsSlot.transform.localPosition = Vector3.zero;
            handsSlot.transform.localRotation = Quaternion.identity;
            var handsTool = handsSlot.AddComponent<HandsTool>();
            handsSlot.SetActive(false);

            // Slot 3: Slicer Tool (Machete / Razor Blade)
            GameObject slicerSlot = new GameObject("Slot_Slicer");
            slicerSlot.transform.SetParent(toolSocket.transform);
            slicerSlot.transform.localPosition = Vector3.zero;
            slicerSlot.transform.localRotation = Quaternion.identity;
            GameObject bladeModel = CreateBladeModel("Blade_Model", slicerSlot.transform, batMat, beamMat);
            var slicerTool = slicerSlot.AddComponent<SlicerTool>();
            var bladeModelField = typeof(SlicerTool).GetField("bladeModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bladeModelField != null) bladeModelField.SetValue(slicerTool, bladeModel.transform);
            slicerSlot.SetActive(false);

            // Slot 4: Vacuum Tool (Shop-Vac Candy Collector)
            GameObject vacuumSlot = new GameObject("Slot_Vacuum");
            vacuumSlot.transform.SetParent(toolSocket.transform);
            vacuumSlot.transform.localPosition = Vector3.zero;
            vacuumSlot.transform.localRotation = Quaternion.identity;
            GameObject vacuumModel = CreateVacuumModel("Vacuum_Model", vacuumSlot.transform, vacBodyMat, vacNozzleMat);
            var vacuumTool = vacuumSlot.AddComponent<VacuumTool>();
            vacuumTool.VacuumModel = vacuumModel.transform;

            Transform nozzleTip = vacuumModel.transform.Find("NozzleTip") ?? vacuumModel.transform;
            vacuumTool.NozzleTip = nozzleTip;

            ParticleSystem vacParticles = CreateSuctionParticleSystem(nozzleTip, particleMat);
            vacuumTool.SuctionParticles = vacParticles;
            vacuumSlot.SetActive(false);

            // Tool Manager on Player (5 Tools)
            var toolManager = playerObj.AddComponent<ToolManager>();
            var socketField = typeof(ToolManager).GetField("toolSocket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (socketField != null) socketField.SetValue(toolManager, toolSocket.transform);

            var toolsListField = typeof(ToolManager).GetField("toolComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (toolsListField != null)
            {
                var list = new System.Collections.Generic.List<MonoBehaviour> { batTool, broomTool, handsTool, slicerTool, vacuumTool };
                toolsListField.SetValue(toolManager, list);
            }

            // Milestone 2: Sorting Station (6 Dedicated Color Baskets)
            CreateSortingStation(room.transform, basketMaterials, crateMat, tableMat, deathConfettiPrefab);

            // Milestone 2: Upgrade Kiosk
            CreateUpgradeKiosk(room.transform, kioskMat, kioskScreenMat);

            // Crosshair UI & Tool HUD Canvas
            CreateCrosshairCanvas(playerObj.transform);

            // Global Volume Post-Processing
            SetupPostProcessing();

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            return "Piñata Liquidation scene generated and saved successfully!";
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
        }

        private static Mesh GetOrCreateMesh(string path, System.Func<Mesh> createFunc)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = createFunc();
                AssetDatabase.CreateAsset(mesh, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            return mesh;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color color, float smoothness, float metallic = 0.0f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static PhysicsMaterial GetOrCreatePhysicsMaterial(string path, float bounciness, float friction)
        {
            PhysicsMaterial physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (physMat == null)
            {
                physMat = new PhysicsMaterial();
                physMat.bounciness = bounciness;
                physMat.bounceCombine = PhysicsMaterialCombine.Minimum;
                physMat.dynamicFriction = friction;
                physMat.staticFriction = friction;
                physMat.frictionCombine = PhysicsMaterialCombine.Maximum;
                AssetDatabase.CreateAsset(physMat, path);
            }
            else
            {
                physMat.bounciness = bounciness;
                physMat.bounceCombine = PhysicsMaterialCombine.Minimum;
                physMat.dynamicFriction = friction;
                physMat.staticFriction = friction;
                physMat.frictionCombine = PhysicsMaterialCombine.Maximum;
                EditorUtility.SetDirty(physMat);
            }
            return physMat;
        }

        private static ParticleSystem CreateConfettiPrefab(string path, Material particleMat, int count, float speed)
        {
            GameObject go = new GameObject("ConfettiFX");
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && particleMat != null)
            {
                psr.sharedMaterial = particleMat;
            }

            var main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.gravityModifier = 1.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = true;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.cyan, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab.GetComponent<ParticleSystem>();
        }

        private static GameObject CreateMintPrefab(string path, Material mat, PhysicsMaterial physMat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Candy_Mint";
            Vector3 scale = new Vector3(0.25f, 0.08f, 0.25f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            col.sharedMaterial = physMat;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var item = go.AddComponent<CandyItem>();
            item.Initialize(CandyType.Mint, 2, 1, scale);

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject CreateChocPrefab(string path, Material mat, PhysicsMaterial physMat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Candy_Chocolate";
            Vector3 scale = new Vector3(0.24f, 0.12f, 0.38f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            col.sharedMaterial = physMat;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.35f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var item = go.AddComponent<CandyItem>();
            item.Initialize(CandyType.Chocolate, 6, 3, scale);

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject CreateTrufflePrefab(string path, Material mat, PhysicsMaterial physMat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Candy_Truffle";
            Vector3 scale = new Vector3(0.28f, 0.28f, 0.28f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            col.sharedMaterial = physMat;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var item = go.AddComponent<CandyItem>();
            item.Initialize(CandyType.Truffle, 25, 12, scale);

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static PinataHealth CreatePinataPrefab(string path, Material pink, Material cyan, Material yellow, ParticleSystem hitConfetti, ParticleSystem deathConfetti)
        {
            GameObject root = new GameObject("Pinata_Donkey");

            // Root Collider & Rigidbody on Body
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0, 0);
            col.size = new Vector3(0.85f, 0.7f, 1.3f);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 7.0f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 1.2f;

            // Visual Body (Pink) - Subdivided for force-based deformation
            GameObject body = new GameObject("Body_Mesh");
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one;
            var mf = body.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrCreateMesh("Assets/Meshes/Mesh_PinataBody.asset", () => ProceduralMeshBuilder.CreateSubdividedBox(0.85f, 0.7f, 1.3f, 8));
            var mr = body.AddComponent<MeshRenderer>();
            mr.sharedMaterial = pink;
            body.AddComponent<MeshDeformer>();

            // Rope Attachment Point
            GameObject attach = new GameObject("AttachmentPoint");
            attach.transform.SetParent(root.transform);
            attach.transform.localPosition = new Vector3(0, 0.38f, 0);

            // Head (Cyan) - Subdivided Ragdoll Hinge with MeshDeformer
            GameObject head = new GameObject("Head");
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0, 0.65f, 0.6f);
            head.transform.localScale = Vector3.one;
            var hmf = head.AddComponent<MeshFilter>();
            hmf.sharedMesh = GetOrCreateMesh("Assets/Meshes/Mesh_PinataHead.asset", () => ProceduralMeshBuilder.CreateSubdividedBox(0.5f, 0.65f, 0.6f, 6));
            var hmr = head.AddComponent<MeshRenderer>();
            hmr.sharedMaterial = cyan;
            head.AddComponent<MeshDeformer>();

            var headCol = head.AddComponent<BoxCollider>();
            headCol.size = new Vector3(0.5f, 0.65f, 0.6f);

            var headRb = head.AddComponent<Rigidbody>();
            headRb.mass = 0.7f;
            headRb.linearDamping = 0.3f;
            headRb.angularDamping = 0.3f;

            var headJoint = head.AddComponent<HingeJoint>();
            headJoint.connectedBody = rb;
            headJoint.anchor = new Vector3(0, -0.3f, -0.3f);
            headJoint.axis = Vector3.right;
            headJoint.useSpring = true;
            headJoint.spring = new JointSpring { spring = 22f, damper = 1.8f };
            headJoint.useLimits = true;
            headJoint.limits = new JointLimits { min = -35f, max = 35f };

            // Ears (Yellow) attached to Head
            GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earL.name = "Ear_L";
            earL.transform.SetParent(head.transform);
            earL.transform.localPosition = new Vector3(-0.35f, 0.6f, 0);
            earL.transform.localScale = new Vector3(0.25f, 0.6f, 0.25f);
            earL.transform.localRotation = Quaternion.Euler(0, 0, 15f);
            earL.GetComponent<Renderer>().sharedMaterial = yellow;
            Object.DestroyImmediate(earL.GetComponent<Collider>());

            GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earR.name = "Ear_R";
            earR.transform.SetParent(head.transform);
            earR.transform.localPosition = new Vector3(0.35f, 0.6f, 0);
            earR.transform.localScale = new Vector3(0.25f, 0.6f, 0.25f);
            earR.transform.localRotation = Quaternion.Euler(0, 0, -15f);
            earR.GetComponent<Renderer>().sharedMaterial = yellow;
            Object.DestroyImmediate(earR.GetComponent<Collider>());

            // 4 Legs - Ragdoll Hinges
            Rigidbody[] limbs = new Rigidbody[5];
            limbs[0] = headRb;

            float legX = 0.3f;
            float legZ = 0.45f;
            float legY = -0.55f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -legX : legX;
                float z = (i < 2) ? legZ : -legZ;
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(root.transform);
                leg.transform.localPosition = new Vector3(x, legY, z);
                leg.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
                leg.GetComponent<Renderer>().sharedMaterial = yellow;

                var legRb = leg.AddComponent<Rigidbody>();
                legRb.mass = 0.35f;
                legRb.linearDamping = 0.3f;
                legRb.angularDamping = 0.3f;

                var legJoint = leg.AddComponent<HingeJoint>();
                legJoint.connectedBody = rb;
                legJoint.anchor = new Vector3(0, 0.25f, 0);
                legJoint.axis = Vector3.right;
                legJoint.useSpring = true;
                legJoint.spring = new JointSpring { spring = 14f, damper = 1.0f };
                legJoint.useLimits = true;
                legJoint.limits = new JointLimits { min = -45f, max = 45f };

                limbs[i + 1] = legRb;
            }

            var health = root.AddComponent<PinataHealth>();

            var hitFxField = typeof(PinataHealth).GetField("hitConfettiPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hitFxField != null) hitFxField.SetValue(health, hitConfetti);

            var deathFxField = typeof(PinataHealth).GetField("deathExplosionPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deathFxField != null) deathFxField.SetValue(health, deathConfetti);

            var attachField = typeof(PinataHealth).GetField("attachmentPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (attachField != null) attachField.SetValue(health, attach.transform);

            var limbsField = typeof(PinataHealth).GetField("ragdollLimbs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (limbsField != null) limbsField.SetValue(health, limbs);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab.GetComponent<PinataHealth>();
        }

        private static GameObject CreateBatModel(string name, Transform parent, Material batMat)
        {
            GameObject batPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bat.prefab");
            GameObject bat;
            if (batPrefab != null)
            {
                bat = (GameObject)PrefabUtility.InstantiatePrefab(batPrefab, parent);
                bat.name = name;
            }
            else
            {
                bat = new GameObject(name);
                bat.transform.SetParent(parent);
                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = "Barrel";
                barrel.transform.SetParent(bat.transform);
                barrel.transform.localPosition = new Vector3(0, 0.15f, 0);
                barrel.transform.localScale = new Vector3(0.065f, 0.42f, 0.065f);
                barrel.GetComponent<Renderer>().sharedMaterial = batMat;
                Object.DestroyImmediate(barrel.GetComponent<Collider>());
            }

            bat.transform.localPosition = new Vector3(0.35f, -0.32f, 0.58f);
            bat.transform.localRotation = Quaternion.Euler(60f, -25f, -30f);

            return bat;
        }

        private static GameObject CreateBroomModel(string name, Transform parent, Material handleMat, Material brushMat)
        {
            GameObject grip = new GameObject(name);
            grip.transform.SetParent(parent, false);

            GameObject broomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Broom.prefab");
            if (broomPrefab != null)
            {
                GameObject broomModel = (GameObject)PrefabUtility.InstantiatePrefab(broomPrefab, grip.transform);
                broomModel.name = "Broom_Mesh";
                broomModel.transform.localPosition = new Vector3(0, -0.70f, 0);
                broomModel.transform.localRotation = Quaternion.Euler(0, 105f, 180f);
            }
            else
            {
                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                handle.name = "Handle";
                handle.transform.SetParent(grip.transform);
                handle.transform.localPosition = new Vector3(0, 0.20f, 0);
                handle.transform.localScale = new Vector3(0.038f, 0.65f, 0.038f);
                handle.GetComponent<Renderer>().sharedMaterial = handleMat;
                Object.DestroyImmediate(handle.GetComponent<Collider>());

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                head.name = "BrushHead";
                head.transform.SetParent(grip.transform);
                head.transform.localPosition = new Vector3(0, -0.44f, 0);
                head.transform.localScale = new Vector3(0.36f, 0.12f, 0.10f);
                head.GetComponent<Renderer>().sharedMaterial = brushMat;
                Object.DestroyImmediate(head.GetComponent<Collider>());
            }

            grip.transform.localPosition = new Vector3(0.34f, -0.32f, 0.48f);
            grip.transform.localRotation = Quaternion.Euler(25f, 89f, 295f);

            return grip;
        }

        private static GameObject CreateBladeModel(string name, Transform parent, Material handleMat, Material steelMat)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0.35f, -0.35f, 0.60f);
            root.transform.localRotation = Quaternion.Euler(35f, -30f, 45f);

            // Grip Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Grip";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0, -0.16f, 0);
            handle.transform.localScale = new Vector3(0.04f, 0.14f, 0.04f);
            handle.GetComponent<Renderer>().sharedMaterial = handleMat;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // Crossguard
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Crossguard";
            guard.transform.SetParent(root.transform, false);
            guard.transform.localPosition = new Vector3(0, -0.01f, 0);
            guard.transform.localScale = new Vector3(0.12f, 0.025f, 0.06f);
            guard.GetComponent<Renderer>().sharedMaterial = steelMat;
            Object.DestroyImmediate(guard.GetComponent<Collider>());

            // Steel Blade
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(0, 0.32f, 0);
            blade.transform.localScale = new Vector3(0.018f, 0.60f, 0.07f);
            blade.GetComponent<Renderer>().sharedMaterial = steelMat;
            Object.DestroyImmediate(blade.GetComponent<Collider>());

            return root;
        }

        private static GameObject CreateVacuumModel(string name, Transform parent, Material bodyMat, Material nozzleMat)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0.34f, -0.36f, 0.62f);
            root.transform.localRotation = Quaternion.Euler(25f, -20f, 15f);

            // Canister Body (main motor housing)
            GameObject canister = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canister.name = "Canister";
            canister.transform.SetParent(root.transform, false);
            canister.transform.localPosition = new Vector3(0, 0.05f, -0.05f);
            canister.transform.localRotation = Quaternion.Euler(75f, 0, 0);
            canister.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
            canister.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(canister.GetComponent<Collider>());

            // Handle Grip on canister
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Handle";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = new Vector3(0, 0.16f, -0.05f);
            grip.transform.localScale = new Vector3(0.04f, 0.06f, 0.18f);
            grip.GetComponent<Renderer>().sharedMaterial = nozzleMat;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            // Vacuum Wand / Tube extending forward
            GameObject wand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wand.name = "Wand";
            wand.transform.SetParent(root.transform, false);
            wand.transform.localPosition = new Vector3(0, -0.06f, 0.22f);
            wand.transform.localRotation = Quaternion.Euler(70f, 0, 0);
            wand.transform.localScale = new Vector3(0.045f, 0.24f, 0.045f);
            wand.GetComponent<Renderer>().sharedMaterial = nozzleMat;
            Object.DestroyImmediate(wand.GetComponent<Collider>());

            // Suction Flared Nozzle Head
            GameObject nozzleHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nozzleHead.name = "NozzleHead";
            nozzleHead.transform.SetParent(root.transform, false);
            nozzleHead.transform.localPosition = new Vector3(0, -0.15f, 0.44f);
            nozzleHead.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            nozzleHead.transform.localScale = new Vector3(0.18f, 0.06f, 0.10f);
            nozzleHead.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(nozzleHead.GetComponent<Collider>());

            // Dark intake lip at front
            GameObject intakeLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            intakeLip.name = "IntakeLip";
            intakeLip.transform.SetParent(root.transform, false);
            intakeLip.transform.localPosition = new Vector3(0, -0.16f, 0.49f);
            intakeLip.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            intakeLip.transform.localScale = new Vector3(0.16f, 0.035f, 0.02f);
            intakeLip.GetComponent<Renderer>().sharedMaterial = nozzleMat;
            Object.DestroyImmediate(intakeLip.GetComponent<Collider>());

            // Nozzle tip marker for suction origin
            GameObject tipObj = new GameObject("NozzleTip");
            tipObj.transform.SetParent(root.transform, false);
            tipObj.transform.localPosition = new Vector3(0, -0.16f, 0.52f);
            tipObj.transform.localRotation = Quaternion.Euler(15f, 0, 0);

            return root;
        }

        private static ParticleSystem CreateSuctionParticleSystem(Transform parent, Material particleMat)
        {
            GameObject psObj = new GameObject("Suction_FX");
            psObj.transform.SetParent(parent, false);
            psObj.transform.localPosition = Vector3.zero;
            psObj.transform.localRotation = Quaternion.identity;

            var ps = psObj.AddComponent<ParticleSystem>();
            var psr = psObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null && particleMat != null)
            {
                psr.sharedMaterial = particleMat;
            }

            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = -2.5f; // Pulls toward nozzle tip
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 50;

            var emission = ps.emission;
            emission.rateOverTime = 30;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.25f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.9f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            return ps;
        }

        private static void CreateSortingStation(Transform room, System.Collections.Generic.Dictionary<CandyType, Material> basketMats, Material crateMat, Material tableMat, ParticleSystem confettiPrefab)
        {
            GameObject station = new GameObject("Sorting_Station");
            station.transform.SetParent(room);

            // Long Packing / Sorting Table placed along West wall
            // Top of table at Y = 0.85m, width 1.2m, length 6.4m
            GameObject table = CreateBox("Sorting_Table", station.transform, new Vector3(-6.4f, 0.425f, 2.5f), new Vector3(1.2f, 0.85f, 6.4f), tableMat);

            CandyType[] types = new CandyType[]
            {
                CandyType.Blue,
                CandyType.Green,
                CandyType.Orange,
                CandyType.Pink,
                CandyType.Yellow,
                CandyType.Purple
            };

            float startZ = 0.0f;
            float spacing = 1.0f;

            for (int i = 0; i < types.Length; i++)
            {
                CandyType cType = types[i];
                Material colorMat = basketMats[cType];
                Color color = colorMat.color;
                float zPos = startZ + i * spacing;

                GameObject basketObj = new GameObject($"Basket_{cType}");
                basketObj.transform.SetParent(station.transform);
                basketObj.transform.position = new Vector3(-6.3f, 0.85f, zPos);

                // Interaction Trigger Collider
                var col = basketObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0.6f, 0.4f, 0);
                col.size = new Vector3(1.8f, 1.6f, 0.95f);

                // Outer Crate Box
                GameObject crateBox = CreateBox("Crate_Outer", basketObj.transform, basketObj.transform.position + new Vector3(0, 0.16f, 0), new Vector3(0.75f, 0.32f, 0.75f), crateMat);
                Object.DestroyImmediate(crateBox.GetComponent<Collider>());

                // Color Rim on crate top
                GameObject rim = CreateBox("Crate_Rim", basketObj.transform, basketObj.transform.position + new Vector3(0, 0.33f, 0), new Vector3(0.78f, 0.04f, 0.78f), colorMat);
                Object.DestroyImmediate(rim.GetComponent<Collider>());

                // Inner Fill visual (scaled by CandyBasket)
                GameObject fill = CreateBox("Fill_Visual", basketObj.transform, basketObj.transform.position + new Vector3(0, 0.18f, 0), new Vector3(0.68f, 0.28f, 0.68f), colorMat);
                Object.DestroyImmediate(fill.GetComponent<Collider>());

                // Front Crate Color Tag
                GameObject frontTag = CreateBox("Front_Tag", basketObj.transform, new Vector3(-5.91f, 0.98f, zPos), new Vector3(0.03f, 0.14f, 0.28f), colorMat);
                Object.DestroyImmediate(frontTag.GetComponent<Collider>());

                // Wall-Mounted Shipping Signboard above each basket
                GameObject wallSign = CreateBox("Wall_Sign", basketObj.transform, new Vector3(-7.68f, 1.55f, zPos), new Vector3(0.06f, 0.32f, 0.55f), colorMat);
                Object.DestroyImmediate(wallSign.GetComponent<Collider>());

                // Confetti instance for when sold
                ParticleSystem basketConfetti = null;
                if (confettiPrefab != null)
                {
                    var psObj = (GameObject)PrefabUtility.InstantiatePrefab(confettiPrefab.gameObject, basketObj.transform);
                    psObj.transform.localPosition = new Vector3(0, 0.4f, 0);
                    basketConfetti = psObj.GetComponent<ParticleSystem>();
                }

                // CandyBasket Component
                var basket = basketObj.AddComponent<CandyBasket>();
                basket.SetupBasket(cType, color, fill.transform, rim.GetComponent<MeshRenderer>(), basketConfetti, capacity: 10, payout: 6);
            }
        }

        private static void CreateUpgradeKiosk(Transform room, Material kioskMat, Material screenMat)
        {
            GameObject kioskObj = new GameObject("Upgrade_Kiosk");
            kioskObj.transform.SetParent(room);
            kioskObj.transform.position = new Vector3(-6.4f, 0, -2.2f);
            kioskObj.transform.rotation = Quaternion.Euler(0, 90f, 0); // Faces East

            // Pedestal Base
            GameObject baseBox = CreateBox("Kiosk_Base", kioskObj.transform, kioskObj.transform.position + new Vector3(0, 0.55f, 0), new Vector3(0.8f, 1.1f, 0.8f), kioskMat);

            // Screen Housing angled toward player
            GameObject screenBox = CreateBox("Kiosk_Screen", kioskObj.transform, kioskObj.transform.position + new Vector3(0.05f, 1.32f, 0), new Vector3(0.65f, 0.45f, 0.12f), screenMat);
            screenBox.transform.rotation = Quaternion.Euler(-30f, 90f, 0);

            // Top Sign
            GameObject signBox = CreateBox("Kiosk_Header", kioskObj.transform, kioskObj.transform.position + new Vector3(0, 1.72f, 0), new Vector3(0.85f, 0.22f, 0.15f), kioskMat);

            // Trigger Collider
            var col = kioskObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0.6f, 1.0f, 0);
            col.size = new Vector3(2.2f, 2.2f, 2.2f);

            kioskObj.AddComponent<UpgradeKiosk>();
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = pos;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = mat;
            return box;
        }

        private static void CreateCrosshairCanvas(Transform parent)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Reticle Dot
            GameObject dotObj = new GameObject("Reticle_Dot");
            dotObj.transform.SetParent(canvasObj.transform);
            RectTransform dotRect = dotObj.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(6f, 6f);
            dotRect.anchoredPosition = Vector2.zero;

            Image dotImage = dotObj.AddComponent<Image>();
            dotImage.color = new Color(1f, 1f, 1f, 0.85f);

            // Crosshair script
            var crosshair = canvasObj.AddComponent<CrosshairUI>();
            var dotField = typeof(CrosshairUI).GetField("dot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dotField != null) dotField.SetValue(crosshair, dotRect);

            // Tool HUD Hotbar & Contextual Prompts
            canvasObj.AddComponent<Pinata.UI.ToolHUD>();

            // Warehouse HUD (Cash Balance, Backpack Gauge, Sorting Prompts & Upgrade Kiosk)
            canvasObj.AddComponent<Pinata.UI.WarehouseHUD>();
        }

        private static void SetupPostProcessing()
        {
            var volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null)
            {
                GameObject volObj = new GameObject("Global Volume");
                volume = volObj.AddComponent<Volume>();
                volume.isGlobal = true;
            }

            if (volume.profile == null)
            {
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(volume.profile, "Assets/Settings/PinataVolumeProfile.asset");
            }

            if (!volume.profile.Has<Bloom>())
            {
                var bloom = volume.profile.Add<Bloom>(true);
                bloom.threshold.value = 0.9f;
                bloom.intensity.value = 0.45f;
                bloom.scatter.value = 0.7f;
            }

            if (!volume.profile.Has<Tonemapping>())
            {
                var tonemapping = volume.profile.Add<Tonemapping>(true);
                tonemapping.mode.value = TonemappingMode.ACES;
            }

            if (!volume.profile.Has<Vignette>())
            {
                var vignette = volume.profile.Add<Vignette>(true);
                vignette.intensity.value = 0.22f;
                vignette.smoothness.value = 0.4f;
            }
        }
    }
}
