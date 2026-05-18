using System.IO;
using LightChasePrototype;
using LightChasePrototype.EditorTools;
using StarterAssets;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LightChaseLakeLevelBuilder
{
    private const string PlaygroundScenePath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Scenes/Playground.unity";
    private const string LakeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level04.unity";
    private const string PrototypeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string PlayerPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
    private const string CameraPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private static readonly Vector3 LakeSpawnPosition = new(-34f, 1.1f, -8f);
    private static readonly Quaternion LakeSpawnRotation = Quaternion.Euler(0f, 62f, 0f);

    [MenuItem("Tools/Prototype/Build Light Chase Level 04")]
    public static void BuildLevel()
    {
        EnsureSceneFolderExists();
        EnsureLakeLevelSceneExists();
        var scene = EditorSceneManager.OpenScene(LakeLevelScenePath, OpenSceneMode.Single);

        ClearPreviousGeneratedContent();
        var player = FindOrCreatePlayer();
        var playerLightState = PlayerAvatarSetup.EnsureGameplayPresentation(player);
        PlayerAvatarSetup.BindCameraToPlayer(player);
        ConfigureAtmosphere();
        BuildHybridEnvironment();
        ConfigureLevelManager();
        ConfigureWaterHazards();
        ConfigureEnemy();
        ConfigureStars();
        ConfigureExit();
        ConfigureNavigation();
        EnsureGameplayScenesIncludedInBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = playerLightState.gameObject;
        Debug.Log($"Light Chase Level 04 listo en {LakeLevelScenePath}");
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

    private static void EnsureLakeLevelSceneExists()
    {
        if (File.Exists(LakeLevelScenePath))
        {
            return;
        }

        AssetDatabase.CopyAsset(PlaygroundScenePath, LakeLevelScenePath);
        AssetDatabase.Refresh();
    }

    private static void ClearPreviousGeneratedContent()
    {
        DestroyIfExists("Environment");
        DestroyIfExists("LakeLevelGeometry");
        DestroyIfExists("LakeSetDressing");
        DestroyIfExists("LakeHazards");
        DestroyIfExists("Collectibles");
        DestroyIfExists("Navigation");
        DestroyIfExists("GameplayHUD");
        DestroyIfExists("MainMenuOverlay");
        DestroyIfExists("GlobalUIRoot");
        DestroyIfExists("ExitPortal");
        DestroyIfExists("LightHunter");
        DestroyIfExists("LightHunters");
        DestroyIfExists("WaterHazards");
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
            playerController.transform.SetPositionAndRotation(LakeSpawnPosition, LakeSpawnRotation);
            return playerController.gameObject;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
        var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        PrefabUtility.InstantiatePrefab(cameraPrefab);
        player.transform.SetPositionAndRotation(LakeSpawnPosition, LakeSpawnRotation);
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

    private static void BuildHybridEnvironment()
    {
        var geometryRoot = new GameObject("LakeLevelGeometry");
        var dressingRoot = new GameObject("LakeSetDressing");
        var hazardsRoot = new GameObject("LakeHazards");

        // Level 04 intentionally avoids the "forest corridor" feel from level 02.
        // The route now wraps around a broad lake with optional risky shortcuts.
        var groundTiles = new (string PrefabName, Vector3 Position, Vector3 Scale, float RotationY)[]
        {
            ("Ground_03", new Vector3(-34f, -0.05f, -8f), new Vector3(2.8f, 1f, 2.6f), 2f),
            ("Ground_02", new Vector3(-28f, -0.05f, -16f), new Vector3(2.4f, 1f, 2.2f), 18f),
            ("Ground_01", new Vector3(-19f, -0.05f, -21f), new Vector3(2.2f, 1f, 2.1f), 4f),
            ("Ground_03", new Vector3(-8f, -0.05f, -23f), new Vector3(2.4f, 1f, 2.3f), -6f),
            ("Ground_02", new Vector3(4f, -0.05f, -21f), new Vector3(2.25f, 1f, 2.2f), 10f),
            ("Ground_03", new Vector3(16f, -0.05f, -16f), new Vector3(2.35f, 1f, 2.3f), -12f),
            ("Ground_02", new Vector3(25f, -0.05f, -7f), new Vector3(2.35f, 1f, 2.25f), 8f),
            ("Ground_01", new Vector3(29f, -0.05f, 5f), new Vector3(2.25f, 1f, 2.2f), -10f),
            ("Ground_03", new Vector3(25f, -0.05f, 18f), new Vector3(2.4f, 1f, 2.35f), 6f),
            ("Ground_02", new Vector3(15f, -0.05f, 26f), new Vector3(2.3f, 1f, 2.3f), -8f),
            ("Ground_01", new Vector3(2f, -0.05f, 28f), new Vector3(2.2f, 1f, 2.1f), 12f),
            ("Ground_03", new Vector3(-11f, -0.05f, 25f), new Vector3(2.3f, 1f, 2.25f), -14f),
            ("Ground_02", new Vector3(-22f, -0.05f, 18f), new Vector3(2.2f, 1f, 2.2f), 8f),
            ("Ground_01", new Vector3(-30f, -0.05f, 8f), new Vector3(2.2f, 1f, 2.1f), -10f),
            ("Ground_03", new Vector3(-31f, -0.05f, -1f), new Vector3(2.4f, 1f, 2.3f), 4f),
            ("Ground_02", new Vector3(-18f, -0.05f, -2f), new Vector3(2.1f, 1f, 2f), 18f),
            ("Ground_01", new Vector3(0f, -0.02f, 4f), new Vector3(1.3f, 1f, 1.3f), 0f),
            ("Ground_01", new Vector3(8f, -0.02f, 11f), new Vector3(1.2f, 1f, 1.2f), 20f)
        };

        foreach (var tile in groundTiles)
        {
            CreateGroundTile(geometryRoot.transform, tile.PrefabName, tile.Position, tile.Scale, tile.RotationY);
        }

        // A central lake creates a high-risk visibility funnel.
        var lakeBasin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lakeBasin.name = "LakeBasin";
        lakeBasin.transform.SetParent(hazardsRoot.transform, false);
        lakeBasin.transform.position = new Vector3(0f, -0.8f, 5f);
        lakeBasin.transform.localScale = new Vector3(8.6f, 0.7f, 7.2f);
        lakeBasin.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(
            "LakeBasinMaterial",
            new Color(0.09f, 0.17f, 0.16f),
            new Color(0f, 0f, 0f));

        // Lake centerpiece sits at ground level so it reads as a real lake, not a
        // raised disk. Slight negative Y keeps it from z-fighting with the basin.
        var lakeSurfaceContainer = new GameObject("LakeSurface");
        lakeSurfaceContainer.transform.SetParent(hazardsRoot.transform, false);
        lakeSurfaceContainer.transform.position = new Vector3(0f, 0.05f, 5f);
        LightChaseWaterSurface.CreateSurface(
            lakeSurfaceContainer.transform,
            "LakeSurface_Visual",
            Vector3.zero,
            new Vector3(16.2f, 0f, 13.4f));

        var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.name = "LakeBridge";
        bridge.transform.SetParent(hazardsRoot.transform, false);
        bridge.transform.position = new Vector3(-2f, 0.55f, 5f);
        bridge.transform.localScale = new Vector3(2.2f, 0.25f, 12.5f);
        bridge.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        bridge.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(
            "LakeBridgeMaterial",
            new Color(0.24f, 0.2f, 0.16f),
            new Color(0f, 0f, 0f));

        var dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dock.name = "LakeDock";
        dock.transform.SetParent(hazardsRoot.transform, false);
        dock.transform.position = new Vector3(16f, 0.48f, 12f);
        dock.transform.localScale = new Vector3(6.4f, 0.2f, 2.1f);
        dock.transform.rotation = Quaternion.Euler(0f, -24f, 0f);
        dock.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(
            "LakeDockMaterial",
            new Color(0.22f, 0.18f, 0.13f),
            new Color(0f, 0f, 0f));

        var props = new (string PrefabName, Vector3 Position, Vector3 Scale, float RotationY)[]
        {
            ("Rock_05", new Vector3(-36f, 0f, -13f), Vector3.one * 1.85f, -12f),
            ("Tree_03", new Vector3(-30f, 0f, -20f), Vector3.one * 1.18f, 9f),
            ("Rock_04", new Vector3(-21f, 0f, -16f), Vector3.one * 1.95f, 14f),
            ("Stump_01", new Vector3(-18f, 0f, -5f), Vector3.one * 1.35f, 0f),
            ("Rock_02", new Vector3(-8f, 0f, -18f), Vector3.one * 1.75f, -20f),
            ("Branch_01", new Vector3(-4f, 0.03f, -9f), Vector3.one * 1.55f, 18f),
            ("Rock_03", new Vector3(7f, 0f, -16f), Vector3.one * 1.7f, 10f),
            ("Tree_04", new Vector3(22f, 0f, -13f), Vector3.one * 1.24f, -12f),
            ("Rock_05", new Vector3(30f, 0f, -1f), Vector3.one * 1.82f, 6f),
            ("Tree_02", new Vector3(30f, 0f, 14f), Vector3.one * 1.34f, 20f),
            ("Rock_04", new Vector3(18f, 0f, 24f), Vector3.one * 1.9f, -18f),
            ("Bush_01", new Vector3(9f, 0f, 24f), Vector3.one * 1.42f, 0f),
            ("Rock_02", new Vector3(-6f, 0f, 27f), Vector3.one * 1.72f, 14f),
            ("Tree_03", new Vector3(-24f, 0f, 21f), Vector3.one * 1.2f, -6f),
            ("Rock_03", new Vector3(-33f, 0f, 11f), Vector3.one * 1.78f, 16f),
            ("Bush_02", new Vector3(-35f, 0f, -1f), Vector3.one * 1.48f, 10f),
            ("Flowers_02", new Vector3(-10f, 0.05f, 6f), Vector3.one * 1.72f, 0f),
            ("Grass_02", new Vector3(5f, 0.05f, 16f), Vector3.one * 2.08f, 0f),
            ("Flowers_01", new Vector3(14f, 0.05f, 10f), Vector3.one * 1.5f, 0f),
            ("Grass_01", new Vector3(20f, 0.05f, 5f), Vector3.one * 1.82f, 0f)
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
        serializedObject.FindProperty("starsRequiredToExit").intValue = 8;
        serializedObject.FindProperty("startingLives").intValue = 3;
        serializedObject.FindProperty("scorePerStar").intValue = 100;
        serializedObject.FindProperty("levelTimeSeconds").floatValue = 250f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }

    private static void ConfigureWaterHazards()
    {
        var waterRoot = new GameObject("WaterHazards");
        // LakeCore is already covered visually by the central LakeSurface, so we
        // skip its visual to avoid double-rendering transparency.
        CreateWaterZone(waterRoot.transform, "WaterZone_LakeCore", new Vector3(0f, 1.02f, 5f), new Vector3(14f, 2.5f, 11.4f), withVisual: false);
        CreateWaterZone(waterRoot.transform, "WaterZone_EastShelf", new Vector3(15f, 1.02f, 11f), new Vector3(7.4f, 2.2f, 5.4f), withVisual: true);
        CreateWaterZone(waterRoot.transform, "WaterZone_WestShelf", new Vector3(-13f, 1.02f, 4f), new Vector3(6.6f, 2.2f, 6f), withVisual: true);
        CreateWaterZone(waterRoot.transform, "WaterZone_NorthPocket", new Vector3(3f, 1.02f, 18f), new Vector3(8.2f, 2.2f, 4.6f), withVisual: true);
    }

    private static void CreateWaterZone(Transform parent, string objectName, Vector3 center, Vector3 size, bool withVisual)
    {
        var waterZone = new GameObject(objectName);
        waterZone.transform.SetParent(parent, false);
        waterZone.transform.position = center;

        var collider = waterZone.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;

        var waterVolume = waterZone.AddComponent<WaterVolume>();
        var serializedVolume = new SerializedObject(waterVolume);
        serializedVolume.FindProperty("moveSpeedMultiplier").floatValue = 0.52f;
        serializedVolume.FindProperty("sprintSpeedMultiplier").floatValue = 0.59f;
        serializedVolume.FindProperty("jumpHeightMultiplier").floatValue = 0.24f;
        serializedVolume.FindProperty("visualSinkDepth").floatValue = 0.66f;
        serializedVolume.ApplyModifiedPropertiesWithoutUndo();

        if (!withVisual)
        {
            return;
        }

        // Water surface stays at ground level instead of floating at the top of the
        // tall trigger collider. The collider remains thick so it still catches the
        // player from any direction.
        const float waterSurfaceWorldY = 0.05f;
        var surfaceLocalY = waterSurfaceWorldY - waterZone.transform.position.y;
        var surfaceCenter = new Vector3(0f, surfaceLocalY, 0f);
        var surfaceSize = new Vector3(size.x, 0f, size.z);
        LightChaseWaterSurface.CreateSurface(waterZone.transform, $"{objectName}_Surface", surfaceCenter, surfaceSize);
    }

    private static void ConfigureEnemy()
    {
        var anchors = new[]
        {
            new Vector3(4f, 0f, 11f),
            new Vector3(-18f, 0f, -8f),
            new Vector3(22f, 0f, -4f),
            new Vector3(-6f, 0f, 16f),
            new Vector3(14f, 0f, -14f),
            new Vector3(-22f, 0f, 8f)
        };

        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level04, anchors);
        EnemySpawner.SpawnEnemies(spawns);
    }

    private static void ConfigureStars()
    {
        var parent = new GameObject("Collectibles");
        var positions = new[]
        {
            new Vector3(-30f, 2.2f, -12f),
            new Vector3(-21f, 2.3f, -20f),
            new Vector3(-8f, 2.3f, -21f),
            new Vector3(5f, 2.25f, -17f),
            new Vector3(20f, 2.35f, -11f),
            new Vector3(28f, 2.35f, 3f),
            new Vector3(21f, 2.35f, 20f),
            new Vector3(6f, 2.35f, 28f),
            new Vector3(-18f, 2.35f, 19f),
            new Vector3(-2f, 2.35f, 5f)
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
            renderer.sharedMaterial = CreateLitMaterial(
                $"LakeLevelStar_{i + 1}",
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
        // Portal canon compartido (Meshy). Mantenemos el spot de salida sobre
        // el promontorio del lago pero reemplazamos el ancla decorativa y el
        // cilindro emisivo por el modelo unificado. Atravesable via trigger.
        ExitPortalBuilder.BuildPortal(new Vector3(16f, 0f, 26f));
    }

    private static void ConfigureNavigation()
    {
        var navigationObject = new GameObject("Navigation");
        var navMeshSurface = GetOrAddComponent<NavMeshSurface>(navigationObject);
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navMeshSurface.layerMask = ~0;
        navMeshSurface.BuildNavMesh();

        EnemySpawner.RebindEnemiesToNavMesh();
    }

    private static GameObject CreateGroundTile(Transform parent, string prefabName, Vector3 position, Vector3 scale, float rotationY)
    {
        var tile = CreatePrefabInstance(prefabName, position, scale, rotationY, parent);
        AlignInstanceTopToY(tile, 0f);
        ApplyNightPalette(tile, new Color(0.18f, 0.24f, 0.21f), 0.78f);
        return tile;
    }

    private static GameObject CreatePropCluster(Transform parent, string prefabName, Vector3 position, Vector3 scale, float rotationY)
    {
        var prop = CreatePrefabInstance(prefabName, position, scale, rotationY, parent);
        AlignInstanceBaseToY(prop, 0f);
        ApplyNightPalette(prop, new Color(0.22f, 0.28f, 0.24f), 0.74f);
        return prop;
    }

    private static GameObject CreatePrefabInstance(string prefabName, Vector3 position, Vector3 scale, float rotationY, Transform parent)
    {
        var prefabPath = $"Assets/ThirdParty/SimpleNaturePack/Prefabs/{prefabName}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"No se encontro el prefab {prefabPath}");
            var fallback = new GameObject(prefabName);
            if (parent != null)
            {
                fallback.transform.SetParent(parent, false);
            }

            fallback.transform.position = position;
            fallback.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            fallback.transform.localScale = scale;
            return fallback;
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

    private static void ApplyNightPalette(GameObject root, Color tint, float brightnessMultiplier)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial == null)
            {
                continue;
            }

            renderer.sharedMaterial = CreateNightMaterial(
                $"{renderer.sharedMaterial.name}_Night_{root.name}",
                renderer.sharedMaterial,
                tint,
                brightnessMultiplier);
        }
    }

    private static Material CreateNightMaterial(string materialName, Material sourceMaterial, Color tint, float brightnessMultiplier)
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
        var moonlitColor = Color.Lerp(baseColor * brightnessMultiplier, tint, 0.35f);
        material.color = moonlitColor;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", moonlitColor);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.18f);
        }

        var baseMap = sourceMaterial != null && sourceMaterial.HasProperty("_BaseMap")
            ? sourceMaterial.GetTexture("_BaseMap")
            : sourceMaterial != null && sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTexture("_MainTex")
                : null;
        if (baseMap != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseMap);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", baseMap);
            }
        }

        return material;
    }

    private static Material CreateLitMaterial(string materialName, Color baseColor, Color emissionColor)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader) { name = materialName };
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
            WaterLevelScenePath,
            LakeLevelScenePath
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
