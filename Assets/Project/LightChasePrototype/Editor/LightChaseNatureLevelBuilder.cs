using System.IO;
using LightChasePrototype;
using StarterAssets;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LightChaseNatureLevelBuilder
{
    private const string PlaygroundScenePath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Scenes/Playground.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";
    private const string PrototypeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string PlayerPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
    private const string CameraPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private const string Enemy01ModelPath = "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_Animation_Walking_withSkin.fbx";
    private const string Enemy01MaterialPath = "Assets/MeshyImports/Enemigo_01/Material_1.mat";
    private static readonly Vector3 NatureSpawnPosition = new(-24f, 1.15f, -20f);
    private static readonly Quaternion NatureSpawnRotation = Quaternion.Euler(0f, 32f, 0f);

    [MenuItem("Tools/Prototype/Build Light Chase Level 02")]
    public static void BuildLevel()
    {
        EnsureSceneFolderExists();
        EnsureNatureLevelSceneExists();
        var scene = EditorSceneManager.OpenScene(NatureLevelScenePath, OpenSceneMode.Single);

        ClearPreviousGeneratedContent();

        var player = FindOrCreatePlayer();
        var playerLightState = PlayerAvatarSetup.EnsureGameplayPresentation(player);
        PlayerAvatarSetup.BindCameraToPlayer(player);
        ConfigureAtmosphere();
        BuildNatureEnvironment();
        ConfigureLevelManager();
        ConfigureEnemy();
        ConfigureStars();
        ConfigureExit();
        ConfigureNavigation();
        EnsureGameplayScenesIncludedInBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = playerLightState.gameObject;
        Debug.Log($"Light Chase Level 02 listo en {NatureLevelScenePath}");
    }

    private static void EnsureSceneFolderExists()
    {
        const string folderPath = "Assets/Project/LightChasePrototype/Scenes";
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        AssetDatabase.CreateFolder("Assets/Project/LightChasePrototype", "Scenes");
    }

    private static void EnsureNatureLevelSceneExists()
    {
        if (File.Exists(NatureLevelScenePath))
        {
            return;
        }

        AssetDatabase.CopyAsset(PlaygroundScenePath, NatureLevelScenePath);
        AssetDatabase.Refresh();
    }

    private static void ClearPreviousGeneratedContent()
    {
        DestroyIfExists("Environment");
        DestroyIfExists("NatureLevelGeometry");
        DestroyIfExists("NatureSetDressing");
        DestroyIfExists("Collectibles");
        DestroyIfExists("Navigation");
        DestroyIfExists("GameplayHUD");
        DestroyIfExists("MainMenuOverlay");
        DestroyIfExists("GlobalUIRoot");
        DestroyIfExists("ExitPortal");
        DestroyIfExists("LightHunter");
        DestroyComponentOfType<EnemyLightSeeker>();
        DestroyComponentOfType<ExitPortal>();
        DestroyComponentOfType<PrototypeLevelManager>();
    }

    private static void DestroyIfExists(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void DestroyComponentOfType<T>() where T : Component
    {
        var component = Object.FindAnyObjectByType<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component.gameObject);
        }
    }

    private static GameObject FindOrCreatePlayer()
    {
        var playerController = Object.FindAnyObjectByType<ThirdPersonController>();
        if (playerController != null)
        {
            playerController.transform.SetPositionAndRotation(NatureSpawnPosition, NatureSpawnRotation);
            return playerController.gameObject;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);

        var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        PrefabUtility.InstantiatePrefab(cameraPrefab);
        player.transform.SetPositionAndRotation(NatureSpawnPosition, NatureSpawnRotation);
        return player;
    }

    private static void ConfigureAtmosphere()
    {
        LightChaseAtmosphere.ApplyRenderSettings();

        var directionalLight = GameObject.Find("Directional Light");
        if (directionalLight != null && directionalLight.TryGetComponent<Light>(out var mainLight))
        {
            LightChaseAtmosphere.ApplyToDirectionalLight(mainLight);
        }

        LightChaseAtmosphere.ApplyToSceneCameras();

        foreach (var volume in Object.FindObjectsByType<Volume>())
        {
            volume.weight = 0f;
        }
    }

    private static void BuildNatureEnvironment()
    {
        var geometryRoot = new GameObject("NatureLevelGeometry");
        var dressingRoot = new GameObject("NatureSetDressing");

        // The layout is intentionally shaped like a soft "S" so the player reads
        // three beats: safer spawn grove, tense middle shortcut, exposed final push.
        var groundTiles = new (string PrefabName, Vector3 Position, Vector3 Scale, float RotationY)[]
        {
            ("Ground_03", new Vector3(-24f, -0.05f, -20f), new Vector3(2.9f, 1f, 2.7f), 6f),
            ("Ground_02", new Vector3(-17f, -0.05f, -16f), new Vector3(2.3f, 1f, 2.2f), 14f),
            ("Ground_01", new Vector3(-11f, -0.05f, -10f), new Vector3(2.1f, 1f, 2.1f), -10f),
            ("Ground_03", new Vector3(-3f, -0.05f, -7f), new Vector3(2.35f, 1f, 2.2f), 7f),
            ("Ground_02", new Vector3(4f, -0.05f, -1f), new Vector3(2.2f, 1f, 2.2f), -6f),
            ("Ground_01", new Vector3(10f, -0.05f, 5f), new Vector3(2.15f, 1f, 2.15f), 10f),
            ("Ground_03", new Vector3(15f, -0.05f, 12f), new Vector3(2.3f, 1f, 2.4f), -8f),
            ("Ground_02", new Vector3(10f, -0.05f, 19f), new Vector3(2.5f, 1f, 2.25f), 16f),
            ("Ground_01", new Vector3(2f, -0.05f, 20f), new Vector3(2.25f, 1f, 2.1f), -14f),
            ("Ground_03", new Vector3(-7f, -0.05f, 17f), new Vector3(2.2f, 1f, 2.3f), 12f),
            ("Ground_02", new Vector3(-13f, -0.05f, 11f), new Vector3(2.25f, 1f, 2.2f), -10f),
            ("Ground_01", new Vector3(-19f, -0.05f, 5f), new Vector3(2.15f, 1f, 2.1f), 8f),
            ("Ground_03", new Vector3(-11f, -0.05f, 24f), new Vector3(2.1f, 1f, 2.15f), 0f),
            ("Ground_02", new Vector3(20f, -0.05f, 18f), new Vector3(2.2f, 1f, 2.25f), 12f),
            ("Ground_03", new Vector3(28f, -0.05f, 24f), new Vector3(2.45f, 1f, 2.55f), 4f)
        };

        foreach (var tile in groundTiles)
        {
            CreateGroundTile(geometryRoot.transform, tile.PrefabName, tile.Position, tile.Scale, tile.RotationY);
        }

        var props = new (string PrefabName, Vector3 Position, Vector3 Scale, float RotationY)[]
        {
            ("Tree_05", new Vector3(-31f, 0f, -26f), Vector3.one * 1.45f, 0f),
            ("Tree_04", new Vector3(-29f, 0f, -14f), Vector3.one * 1.22f, 12f),
            ("Tree_03", new Vector3(-21f, 0f, -26f), Vector3.one * 1.18f, 28f),
            ("Bush_03", new Vector3(-21f, 0f, -11f), Vector3.one * 1.55f, -10f),
            ("Rock_04", new Vector3(-13f, 0f, -17f), Vector3.one * 1.75f, 22f),
            ("Bush_02", new Vector3(-7f, 0f, -12f), Vector3.one * 1.5f, 0f),
            ("Tree_02", new Vector3(1f, 0f, -15f), Vector3.one * 1.28f, -16f),
            ("Rock_02", new Vector3(5f, 0f, -7f), Vector3.one * 1.6f, 34f),
            ("Tree_03", new Vector3(14f, 0f, -8f), Vector3.one * 1.22f, 14f),
            ("Bush_01", new Vector3(13f, 0f, -1f), Vector3.one * 1.45f, 0f),
            ("Rock_05", new Vector3(19f, 0f, 3f), Vector3.one * 1.65f, -18f),
            ("Tree_05", new Vector3(28f, 0f, 1f), Vector3.one * 1.35f, 10f),
            ("Rock_03", new Vector3(9f, 0f, 6f), Vector3.one * 1.8f, 18f),
            ("Tree_04", new Vector3(18f, 0f, 10f), Vector3.one * 1.18f, 0f),
            ("Bush_03", new Vector3(4f, 0f, 12f), Vector3.one * 1.6f, -12f),
            ("Tree_02", new Vector3(12f, 0f, 18f), Vector3.one * 1.3f, 18f),
            ("Rock_04", new Vector3(-2f, 0f, 18f), Vector3.one * 1.7f, -20f),
            ("Tree_04", new Vector3(-14f, 0f, 14f), Vector3.one * 1.24f, 0f),
            ("Bush_02", new Vector3(-20f, 0f, 8f), Vector3.one * 1.48f, 12f),
            ("Tree_04", new Vector3(-28f, 0f, 5f), Vector3.one * 1.18f, 0f),
            ("Rock_01", new Vector3(-10f, 0f, 26f), Vector3.one * 1.48f, -26f),
            ("Tree_05", new Vector3(26f, 0f, 27f), Vector3.one * 1.4f, 4f),
            ("Tree_03", new Vector3(35f, 0f, 25f), Vector3.one * 1.22f, -14f),
            ("Rock_02", new Vector3(18f, 0f, 28f), Vector3.one * 1.55f, 16f),
            ("Rock_05", new Vector3(24f, 0f, 20f), Vector3.one * 1.72f, -12f),
            ("Tree_05", new Vector3(33f, 0f, 15f), Vector3.one * 1.2f, 0f),
            ("Bush_01", new Vector3(21f, 0f, 15f), Vector3.one * 1.6f, 0f),
            ("Mushroom_02", new Vector3(-24f, 0.05f, -16f), Vector3.one * 1.3f, 14f),
            ("Flowers_02", new Vector3(-10f, 0.05f, -6f), Vector3.one * 1.7f, 0f),
            ("Grass_02", new Vector3(1f, 0.05f, 10f), Vector3.one * 2.1f, 0f),
            ("Flowers_01", new Vector3(10f, 0.05f, 23f), Vector3.one * 1.6f, 0f),
            ("Stump_01", new Vector3(23f, 0f, 18f), Vector3.one * 1.3f, -8f),
            ("Branch_01", new Vector3(7f, 0.02f, 7f), Vector3.one * 1.55f, 22f),
            ("Grass_01", new Vector3(29f, 0.05f, 19f), Vector3.one * 1.95f, 0f),
            ("Grass_02", new Vector3(-15f, 0.05f, 18f), Vector3.one * 1.8f, 0f),
            ("Flowers_02", new Vector3(16f, 0.05f, 7f), Vector3.one * 1.55f, 0f),
            ("Bush_03", new Vector3(-4f, 0f, 3f), Vector3.one * 1.5f, 18f),
            ("Tree_05", new Vector3(-24f, 0f, -6f), Vector3.one * 1.28f, 6f),
            ("Tree_03", new Vector3(-16f, 0f, 18f), Vector3.one * 1.2f, -8f),
            ("Tree_04", new Vector3(-6f, 0f, 10f), Vector3.one * 1.16f, 14f),
            ("Tree_02", new Vector3(6f, 0f, 14f), Vector3.one * 1.24f, -10f),
            ("Tree_05", new Vector3(16f, 0f, 20f), Vector3.one * 1.22f, 18f),
            ("Tree_04", new Vector3(24f, 0f, 8f), Vector3.one * 1.18f, -12f),
            ("Tree_03", new Vector3(30f, 0f, 20f), Vector3.one * 1.18f, 9f),
            ("Tree_02", new Vector3(-30f, 0f, 10f), Vector3.one * 1.28f, 0f)
        };

        foreach (var prop in props)
        {
            CreatePropCluster(dressingRoot.transform, prop.PrefabName, prop.Position, prop.Scale, prop.RotationY);
        }
    }

    private static PrototypeLevelManager ConfigureLevelManager()
    {
        var manager = Object.FindAnyObjectByType<PrototypeLevelManager>();
        if (manager == null)
        {
            var managerObject = new GameObject("PrototypeLevelManager");
            manager = managerObject.AddComponent<PrototypeLevelManager>();
        }

        var serializedObject = new SerializedObject(manager);
        serializedObject.FindProperty("starsRequiredToExit").intValue = 6;
        serializedObject.FindProperty("startingLives").intValue = 3;
        serializedObject.FindProperty("scorePerStar").intValue = 100;
        serializedObject.FindProperty("levelTimeSeconds").floatValue = 210f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }

    private static void ConfigureEnemy()
    {
        var enemyObject = CreateEnemyRoot("LightHunter", new Vector3(8f, 0f, 7f));
        AlignInstanceBaseToY(enemyObject, 0f);
        enemyObject.transform.position += Vector3.up * 0.02f;

        var renderer = enemyObject.GetComponentInChildren<Renderer>();

        var agent = enemyObject.AddComponent<NavMeshAgent>();
        agent.angularSpeed = 240f;
        agent.acceleration = 24f;
        agent.stoppingDistance = 1.35f;

        var enemy = enemyObject.AddComponent<EnemyLightSeeker>();
        enemy.ConfigureRenderer(renderer);
    }

    private static GameObject CreateEnemyRoot(string objectName, Vector3 position)
    {
        var root = new GameObject(objectName);
        root.transform.position = position;

        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Enemy01ModelPath);
        if (modelPrefab != null)
        {
            var modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = Object.Instantiate(modelPrefab);
            }

            modelInstance.name = "Enemigo_01_Model";
            modelInstance.transform.SetParent(root.transform, false);
            ApplyEnemy01Material(modelInstance);
            NormalizeModelHeight(modelInstance.transform, 2.2f);
            return root;
        }

        var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.name = "FallbackEnemy";
        fallback.transform.SetParent(root.transform, false);
        fallback.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f);
        return root;
    }

    private static void ApplyEnemy01Material(GameObject modelInstance)
    {
        if (modelInstance == null)
        {
            return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(Enemy01MaterialPath);
        if (material == null)
        {
            return;
        }

        foreach (var renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            var shared = renderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                renderer.sharedMaterial = material;
                continue;
            }

            for (var i = 0; i < shared.Length; i++)
            {
                shared[i] = material;
            }

            renderer.sharedMaterials = shared;
        }
    }

    private static void NormalizeModelHeight(Transform modelRoot, float targetHeight)
    {
        if (modelRoot == null)
        {
            return;
        }

        var renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        var height = bounds.size.y;
        if (height <= 0.01f)
        {
            return;
        }

        var factor = targetHeight / height;
        modelRoot.localScale *= factor;
    }

    private static void ConfigureStars()
    {
        var parent = new GameObject("Collectibles");

        var positions = new[]
        {
            new Vector3(-22f, 2.2f, -11f),
            new Vector3(-15f, 2.25f, -3f),
            new Vector3(-6f, 2.3f, -10f),
            new Vector3(2f, 2.25f, 0f),
            new Vector3(9f, 2.35f, 8f),
            new Vector3(-3f, 2.35f, 18f),
            new Vector3(18f, 2.25f, 16f),
            new Vector3(31f, 2.4f, 25f)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = $"Star_{i + 1}";
            star.transform.SetParent(parent.transform);
            star.transform.position = positions[i];
            star.transform.localScale = Vector3.one * 0.6f;

            var collider = star.GetComponent<SphereCollider>();
            collider.radius = 0.75f;
            collider.isTrigger = true;

            var renderer = star.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateEmissiveMaterial(
                $"NatureStarGlow_{i + 1}",
                renderer.sharedMaterial,
                new Color(1f, 0.94f, 0.72f),
                new Color(1.75f, 1.35f, 0.58f));

            var starLight = GetOrAddComponent<Light>(star);
            starLight.type = LightType.Point;
            starLight.range = 3f;
            starLight.intensity = 0.9f;
            starLight.color = new Color(1f, 0.92f, 0.62f);
            starLight.shadows = LightShadows.None;

            var pickup = star.AddComponent<StarPickup>();
            pickup.ConfigureLight(starLight);
            var serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("starValue").intValue = 1;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureExit()
    {
        var exitAnchor = CreatePrefabInstance("Tree_05", new Vector3(32f, 0f, 29f), new Vector3(1.35f, 1.75f, 1.35f), 0f, null);
        exitAnchor.name = "ExitPortal";
        AlignInstanceBaseToY(exitAnchor, 0f);

        var portalCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        portalCore.name = "PortalCore";
        portalCore.transform.SetParent(exitAnchor.transform, false);
        portalCore.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        portalCore.transform.localScale = new Vector3(1.35f, 1.3f, 1.35f);

        var renderer = portalCore.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateEmissiveMaterial(
            "NatureExitPortal",
            renderer.sharedMaterial,
            new Color(0.16f, 0.42f, 0.7f),
            new Color(0.08f, 0.48f, 0.95f));

        var collider = portalCore.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.height = 2.2f;
        collider.radius = 0.9f;

        var exit = portalCore.AddComponent<ExitPortal>();
        exit.ConfigureRenderer(renderer);
    }

    private static void ConfigureNavigation()
    {
        var navigationObject = new GameObject("Navigation");
        var navMeshSurface = GetOrAddComponent<NavMeshSurface>(navigationObject);
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navMeshSurface.layerMask = ~0;
        navMeshSurface.BuildNavMesh();
    }

    private static GameObject CreateGroundTile(Transform parent, string prefabName, Vector3 position, Vector3 scale, float rotationY)
    {
        var tile = CreatePrefabInstance(prefabName, position, scale, rotationY, parent);
        AlignInstanceTopToY(tile, 0f);
        ApplyMoonlitPalette(tile, new Color(0.18f, 0.24f, 0.21f), 0.78f);
        return tile;
    }

    private static GameObject CreatePropCluster(Transform parent, string prefabName, Vector3 position, Vector3 scale, float rotationY)
    {
        var prop = CreatePrefabInstance(prefabName, position, scale, rotationY, parent);
        AlignInstanceBaseToY(prop, 0f);
        ApplyMoonlitPalette(prop, new Color(0.22f, 0.28f, 0.24f), 0.72f);
        return prop;
    }

    private static GameObject CreatePrefabInstance(string prefabName, Vector3 position, Vector3 scale, float rotationY, Transform parent)
    {
        var prefabPath = $"Assets/ThirdParty/SimpleNaturePack/Prefabs/{prefabName}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"No se encontro el prefab {prefabPath}");
            return new GameObject(prefabName);
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.name = prefab.name;
        if (parent != null)
        {
            instance.transform.SetParent(parent, false);
        }

        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = scale;
        return instance;
    }

    private static void ApplyMoonlitPalette(GameObject root, Color tint, float brightnessMultiplier)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial == null)
            {
                continue;
            }

            renderer.sharedMaterial = CreateMoonlitMaterial(
                $"{renderer.sharedMaterial.name}_Moonlit_{root.name}",
                renderer.sharedMaterial,
                tint,
                brightnessMultiplier);
        }
    }

    private static void AlignInstanceBaseToY(GameObject instance, float targetY)
    {
        if (instance == null || !TryGetCombinedRendererBounds(instance, out var bounds))
        {
            return;
        }

        var position = instance.transform.position;
        position.y += targetY - bounds.min.y;
        instance.transform.position = position;
    }

    private static void AlignInstanceTopToY(GameObject instance, float targetY)
    {
        if (instance == null || !TryGetCombinedRendererBounds(instance, out var bounds))
        {
            return;
        }

        var position = instance.transform.position;
        position.y += targetY - bounds.max.y;
        instance.transform.position = position;
    }

    private static bool TryGetCombinedRendererBounds(GameObject instance, out Bounds bounds)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private static Material CreateMoonlitMaterial(string materialName, Material sourceMaterial, Color tint)
    {
        return CreateMoonlitMaterial(materialName, sourceMaterial, tint, 0.75f);
    }

    private static Material CreateMoonlitMaterial(string materialName, Material sourceMaterial, Color tint, float brightnessMultiplier)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = shader != null
            ? new Material(shader)
            : new Material(sourceMaterial != null ? sourceMaterial : new Material(Shader.Find("Standard")));
        material.name = materialName;

        var baseColor = sourceMaterial != null && sourceMaterial.HasProperty("_BaseColor")
            ? sourceMaterial.GetColor("_BaseColor")
            : sourceMaterial != null && sourceMaterial.HasProperty("_Color")
                ? sourceMaterial.color
                : Color.white;
        var baseMap = GetSourceTexture(sourceMaterial);
        var mainTextureScale = sourceMaterial != null ? sourceMaterial.mainTextureScale : Vector2.one;
        var mainTextureOffset = sourceMaterial != null ? sourceMaterial.mainTextureOffset : Vector2.zero;

        var moonlitColor = Color.Lerp(baseColor * brightnessMultiplier, tint, 0.35f);
        material.color = moonlitColor;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", moonlitColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        if (baseMap != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseMap);
                material.SetTextureScale("_BaseMap", mainTextureScale);
                material.SetTextureOffset("_BaseMap", mainTextureOffset);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", baseMap);
                material.SetTextureScale("_MainTex", mainTextureScale);
                material.SetTextureOffset("_MainTex", mainTextureOffset);
            }
        }

        // Trees, grass and flowers in this pack rely on alpha cutout textures.
        ConfigureAlphaClipFromSource(material, sourceMaterial, baseMap != null);

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.18f);
        }

        return material;
    }

    private static Texture GetSourceTexture(Material sourceMaterial)
    {
        if (sourceMaterial == null)
        {
            return null;
        }

        if (sourceMaterial.HasProperty("_BaseMap"))
        {
            var baseMap = sourceMaterial.GetTexture("_BaseMap");
            if (baseMap != null)
            {
                return baseMap;
            }
        }

        if (sourceMaterial.HasProperty("_MainTex"))
        {
            return sourceMaterial.GetTexture("_MainTex");
        }

        return null;
    }

    private static void ConfigureAlphaClipFromSource(Material material, Material sourceMaterial, bool hasBaseMap)
    {
        if (material == null)
        {
            return;
        }

        var shouldAlphaClip = hasBaseMap && LooksLikeFoliageMaterial(sourceMaterial);

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", shouldAlphaClip ? 1f : 0f);
        }

        if (material.HasProperty("_AlphaCutoff"))
        {
            material.SetFloat("_AlphaCutoff", shouldAlphaClip ? 0.5f : 0f);
        }

        if (shouldAlphaClip)
        {
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = -1;
        }
    }

    private static bool LooksLikeFoliageMaterial(Material sourceMaterial)
    {
        if (sourceMaterial == null)
        {
            return false;
        }

        var name = sourceMaterial.name.ToLowerInvariant();
        return name.Contains("nature")
            || name.Contains("leaf")
            || name.Contains("grass")
            || name.Contains("flower")
            || name.Contains("bush")
            || name.Contains("tree");
    }

    private static Material CreateEmissiveMaterial(string materialName, Material fallbackMaterial, Color baseColor, Color emissionColor)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = shader != null ? new Material(shader) : new Material(fallbackMaterial);
        material.name = materialName;
        material.color = baseColor;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", emissionColor);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.72f);
        }

        return material;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        if (!gameObject.TryGetComponent<T>(out var component))
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void EnsureGameplayScenesIncludedInBuildSettings()
    {
        var requiredScenes = new[]
        {
            PrototypeLevelScenePath,
            NatureLevelScenePath,
            WaterLevelScenePath
        };

        var existingScenes = EditorBuildSettings.scenes;
        var updatedScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(existingScenes);

        foreach (var scenePath in requiredScenes)
        {
            var exists = false;
            for (var i = 0; i < updatedScenes.Count; i++)
            {
                if (updatedScenes[i].path != scenePath)
                {
                    continue;
                }

                updatedScenes[i].enabled = true;
                exists = true;
                break;
            }

            if (!exists)
            {
                updatedScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = updatedScenes.ToArray();
    }
}
