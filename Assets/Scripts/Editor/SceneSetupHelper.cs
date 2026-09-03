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

            // Particle Material (URP Particles/Unlit)
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? litShader;
            Material particleMat = GetOrCreateMaterial("Assets/Materials/Mat_ParticleConfetti.mat", particleShader, Color.white, 0f);

            // 2. Physics Material
            PhysicsMaterial candyPhysMat = GetOrCreatePhysicsMaterial("Assets/Materials/PhysMat_Candy.physicMaterial", 0.75f, 0.2f);

            // 3. Particle Prefabs
            ParticleSystem hitConfettiPrefab = CreateConfettiPrefab("Assets/Prefabs/FX_HitConfetti.prefab", particleMat, 15, 0.8f);
            ParticleSystem deathConfettiPrefab = CreateConfettiPrefab("Assets/Prefabs/FX_DeathExplosion.prefab", particleMat, 70, 1.8f);

            // 4. Candy Prefabs
            GameObject mintPrefab = CreateMintPrefab("Assets/Prefabs/Candy_Mint.prefab", mintMat, candyPhysMat);
            GameObject chocPrefab = CreateChocPrefab("Assets/Prefabs/Candy_Chocolate.prefab", chocMat, candyPhysMat);
            GameObject trufflePrefab = CreateTrufflePrefab("Assets/Prefabs/Candy_Truffle.prefab", truffleMat, candyPhysMat);

            // 5. Pinata Prefab
            PinataHealth pinataPrefab = CreatePinataPrefab("Assets/Prefabs/Pinata_Donkey.prefab", pinkMat, cyanMat, yellowMat, hitConfettiPrefab, deathConfettiPrefab);

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

            // Managers: JuiceEffects & CandyPool
            GameObject managers = new GameObject("Managers");
            var juice = managers.AddComponent<JuiceEffects>();
            var pool = managers.AddComponent<CandyPool>();

            // Assign candy pool entries
            var field = typeof(CandyPool).GetField("candyEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var list = new System.Collections.Generic.List<CandyPool.CandyPrefabEntry>
                {
                    new CandyPool.CandyPrefabEntry { type = CandyType.Mint, prefab = mintPrefab, initialPoolSize = 30 },
                    new CandyPool.CandyPrefabEntry { type = CandyType.Chocolate, prefab = chocPrefab, initialPoolSize = 20 },
                    new CandyPool.CandyPrefabEntry { type = CandyType.Truffle, prefab = trufflePrefab, initialPoolSize = 10 }
                };
                field.SetValue(pool, list);
            }

            // Pinata Spawner and Rope
            GameObject spawnerObj = new GameObject("Pinata_Spawner");
            spawnerObj.transform.position = new Vector3(0, 5.6f, 3.5f);
            var spawner = spawnerObj.AddComponent<PinataSpawner>();

            var spawnerPrefabField = typeof(PinataSpawner).GetField("pinataPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnerPrefabField != null) spawnerPrefabField.SetValue(spawner, pinataPrefab);

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

            joint.anchor = new Vector3(0, 3.2f, 0);
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
            slerp.positionSpring = 45f;
            slerp.positionDamper = 3.2f;
            slerp.maximumForce = 500f;
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

            // Bat Tool Socket & Model
            GameObject toolSocket = new GameObject("ToolSocket");
            toolSocket.transform.SetParent(camObj.transform);
            toolSocket.transform.localPosition = Vector3.zero;
            toolSocket.transform.localRotation = Quaternion.identity;

            GameObject batObj = CreateBatModel("Bat_Model", toolSocket.transform, batMat);
            var batTool = toolSocket.AddComponent<BatTool>();

            var batModelField = typeof(BatTool).GetField("batModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (batModelField != null) batModelField.SetValue(batTool, batObj.transform);

            // Crosshair UI Canvas
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
                physMat.bounceCombine = PhysicsMaterialCombine.Maximum;
                physMat.dynamicFriction = friction;
                physMat.staticFriction = friction;
                physMat.frictionCombine = PhysicsMaterialCombine.Minimum;
                AssetDatabase.CreateAsset(physMat, path);
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
            rb.mass = 3.5f;
            rb.linearDamping = 0.3f;
            rb.angularDamping = 0.3f;

            // Visual Body (Pink)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body_Mesh";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.85f, 0.7f, 1.3f);
            body.GetComponent<Renderer>().sharedMaterial = pink;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Rope Attachment Point
            GameObject attach = new GameObject("AttachmentPoint");
            attach.transform.SetParent(root.transform);
            attach.transform.localPosition = new Vector3(0, 0.38f, 0);

            // Head (Cyan) - Ragdoll Hinge
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0, 0.65f, 0.6f);
            head.transform.localScale = new Vector3(0.5f, 0.65f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = cyan;

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
            GameObject bat = new GameObject(name);
            bat.transform.SetParent(parent);
            bat.transform.localPosition = new Vector3(0.35f, -0.35f, 0.60f);
            bat.transform.localRotation = Quaternion.Euler(60f, -25f, -30f);

            // Single unified bat cylinder
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(bat.transform);
            barrel.transform.localPosition = new Vector3(0, 0.15f, 0);
            barrel.transform.localScale = new Vector3(0.065f, 0.42f, 0.065f);
            barrel.GetComponent<Renderer>().sharedMaterial = batMat;
            Object.DestroyImmediate(barrel.GetComponent<Collider>());

            return bat;
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
        }

        private static void SetupPostProcessing()
        {
            var volume = Object.FindFirstObjectByType<Volume>();
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
