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

public static class LightChaseWaterLevelBuilder
{
    private const string PlaygroundScenePath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Scenes/Playground.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string PrototypeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";
    private const string LakeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level04.unity";
    private const string PlayerPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
    private const string CameraPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private const string ScenarioEscena3ModelPath = "Assets/MeshyImports/Escena3/Meshy_AI_Moonlit_Keep_in_the_P_0518083210_texture.fbx";
    private const string ScenarioEscena3MaterialPath = "Assets/MeshyImports/Escena3/Material.001.mat";
    private const string ScenarioEscena3TexturePrefix = "Assets/MeshyImports/Escena3/Meshy_AI_Moonlit_Keep_in_the_P_0518083210_texture";
    private const float TargetKeepFootprint = 22f;
    private static readonly Vector3 KeepCenter = new(2f, 0f, 6f);
    private static readonly Vector3 WaterSpawnPosition = new(-32f, 1.15f, -14f);
    private static readonly Quaternion WaterSpawnRotation = Quaternion.Euler(0f, 48f, 0f);

    [MenuItem("Tools/Prototype/Build Light Chase Level 03")]
    public static void BuildLevel()
    {
        BuildLevelInternal(preserveExistingScenarioEnvironment: true);
    }

    public static void BuildLevelFullRebuild()
    {
        BuildLevelInternal(preserveExistingScenarioEnvironment: false);
    }

    private static void BuildLevelInternal(bool preserveExistingScenarioEnvironment)
    {
        EnsureSceneFolderExists();
        EnsureWaterLevelSceneExists();
        var scene = EditorSceneManager.OpenScene(WaterLevelScenePath, OpenSceneMode.Single);

        ClearPreviousGeneratedContent(preserveExistingScenarioEnvironment);

        var player = FindOrCreatePlayer();
        var playerLightState = PlayerAvatarSetup.EnsureGameplayPresentation(player);
        PlayerAvatarSetup.BindCameraToPlayer(player);

        ConfigureAtmosphere();
        BuildHybridEnvironment();
        BuildScenarioKeep(preserveExistingScenarioEnvironment);
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
        Debug.Log($"Light Chase Level 03 listo en {WaterLevelScenePath}");
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

    private static void EnsureWaterLevelSceneExists()
    {
        if (File.Exists(WaterLevelScenePath))
        {
            return;
        }

        AssetDatabase.CopyAsset(PlaygroundScenePath, WaterLevelScenePath);
        AssetDatabase.Refresh();
    }

    private static void ClearPreviousGeneratedContent(bool preserveExistingScenarioEnvironment)
    {
        DestroyIfExists("Environment");
        DestroyIfExists("WaterLevelGeometry");
        DestroyIfExists("WaterLevelDressing");
        DestroyIfExists("WaterLevelLighting");
        if (!preserveExistingScenarioEnvironment)
        {
            DestroyIfExists("Scenario3Environment");
        }

        DestroyIfExists("Scenario3FallbackGround");
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

    private static GameObject FindOrCreatePlayer()
    {
        var playerController = Object.FindAnyObjectByType<ThirdPersonController>();
        if (playerController != null)
        {
            playerController.transform.SetPositionAndRotation(WaterSpawnPosition, WaterSpawnRotation);
            return playerController.gameObject;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);

        var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        PrefabUtility.InstantiatePrefab(cameraPrefab);
        player.transform.SetPositionAndRotation(WaterSpawnPosition, WaterSpawnRotation);
        return player;
    }

    // Tras "perder" el loop por escenas vacias, este nivel ahora apoya su jugabilidad
    // en un terreno modular consistente con el resto del prototipo, y deja el Keep de
    // Meshy como pieza decorativa central con una zona inundada a sus pies.
    private static void BuildHybridEnvironment()
    {
        var geometryRoot = new GameObject("WaterLevelGeometry");
        var dressingRoot = new GameObject("WaterLevelDressing");

        var groundTiles = new (string PrefabName, Vector3 Position, Vector3 Scale, float RotationY)[]
        {
            ("Ground_03", new Vector3(-30f, -0.05f, -14f), new Vector3(2.7f, 1f, 2.6f), 4f),
            ("Ground_02", new Vector3(-22f, -0.05f, -19f), new Vector3(2.3f, 1f, 2.1f), 12f),
            ("Ground_01", new Vector3(-12f, -0.05f, -21f), new Vector3(2.2f, 1f, 2.1f), -8f),
            ("Ground_03", new Vector3(0f, -0.05f, -19f), new Vector3(2.3f, 1f, 2.2f), 6f),
            ("Ground_02", new Vector3(12f, -0.05f, -16f), new Vector3(2.3f, 1f, 2.2f), -10f),
            ("Ground_01", new Vector3(22f, -0.05f, -10f), new Vector3(2.2f, 1f, 2.15f), 8f),
            ("Ground_03", new Vector3(28f, -0.05f, 2f), new Vector3(2.4f, 1f, 2.3f), -6f),
            ("Ground_02", new Vector3(26f, -0.05f, 16f), new Vector3(2.3f, 1f, 2.3f), 10f),
            ("Ground_01", new Vector3(18f, -0.05f, 26f), new Vector3(2.25f, 1f, 2.2f), -12f),
            ("Ground_03", new Vector3(6f, -0.05f, 28f), new Vector3(2.3f, 1f, 2.25f), 6f),
            ("Ground_02", new Vector3(-8f, -0.05f, 26f), new Vector3(2.3f, 1f, 2.25f), -10f),
            ("Ground_01", new Vector3(-20f, -0.05f, 20f), new Vector3(2.2f, 1f, 2.15f), 8f),
            ("Ground_03", new Vector3(-28f, -0.05f, 10f), new Vector3(2.3f, 1f, 2.2f), -4f),
            ("Ground_02", new Vector3(-32f, -0.05f, -3f), new Vector3(2.3f, 1f, 2.2f), 10f)
        };

        foreach (var tile in groundTiles)
        {
            CreateGroundTile(geometryRoot.transform, tile.PrefabName, tile.Position, tile.Scale, tile.RotationY);
        }

        var props = new (string PrefabName, Vector3 Position, Vector3 Scale, float RotationY)[]
        {
            ("Rock_05", new Vector3(-36f, 0f, -17f), Vector3.one * 1.8f, -8f),
            ("Tree_03", new Vector3(-28f, 0f, -22f), Vector3.one * 1.2f, 10f),
            ("Rock_04", new Vector3(-18f, 0f, -24f), Vector3.one * 1.9f, 18f),
            ("Bush_03", new Vector3(-10f, 0f, -16f), Vector3.one * 1.5f, 0f),
            ("Rock_02", new Vector3(4f, 0f, -22f), Vector3.one * 1.7f, -14f),
            ("Tree_05", new Vector3(14f, 0f, -19f), Vector3.one * 1.32f, 12f),
            ("Bush_01", new Vector3(22f, 0f, -16f), Vector3.one * 1.45f, 0f),
            ("Rock_03", new Vector3(30f, 0f, -6f), Vector3.one * 1.8f, 22f),
            ("Tree_02", new Vector3(32f, 0f, 8f), Vector3.one * 1.32f, -10f),
            ("Rock_05", new Vector3(30f, 0f, 22f), Vector3.one * 1.85f, 6f),
            ("Tree_04", new Vector3(22f, 0f, 30f), Vector3.one * 1.24f, -16f),
            ("Bush_02", new Vector3(8f, 0f, 32f), Vector3.one * 1.5f, 14f),
            ("Rock_04", new Vector3(-6f, 0f, 30f), Vector3.one * 1.85f, -22f),
            ("Tree_03", new Vector3(-18f, 0f, 26f), Vector3.one * 1.22f, 4f),
            ("Rock_02", new Vector3(-28f, 0f, 18f), Vector3.one * 1.75f, 18f),
            ("Bush_03", new Vector3(-34f, 0f, 6f), Vector3.one * 1.5f, 0f),
            ("Tree_05", new Vector3(-36f, 0f, -8f), Vector3.one * 1.3f, 8f),
            ("Stump_01", new Vector3(-12f, 0f, -10f), Vector3.one * 1.3f, -8f),
            ("Branch_01", new Vector3(16f, 0.02f, -4f), Vector3.one * 1.5f, 18f),
            ("Flowers_02", new Vector3(-22f, 0.05f, 8f), Vector3.one * 1.6f, 0f),
            ("Grass_02", new Vector3(-16f, 0.05f, 16f), Vector3.one * 2.0f, 0f),
            ("Flowers_01", new Vector3(20f, 0.05f, 20f), Vector3.one * 1.55f, 0f),
            ("Grass_01", new Vector3(-4f, 0.05f, 18f), Vector3.one * 1.85f, 0f)
        };

        foreach (var prop in props)
        {
            CreatePropCluster(dressingRoot.transform, prop.PrefabName, prop.Position, prop.Scale, prop.RotationY);
        }
    }

    // El Keep importado se trata como una pieza dramatica central: se reescala a un
    // footprint manejable, se asienta sobre el suelo y se rodea de antorchas calidas
    // que rompen la oscuridad sin contradecir la fantasia nocturna.
    private static void BuildScenarioKeep(bool preserveExistingScenarioEnvironment)
    {
        GameObject environment;
        if (preserveExistingScenarioEnvironment)
        {
            environment = GameObject.Find("Scenario3Environment");
            if (environment != null)
            {
                EnsureRenderMeshesHaveColliders(environment);
                ApplyScenarioMaterial(environment);
                ConfigureKeepLighting(environment);
                return;
            }
        }

        var scenarioPrefab = LoadScenarioPrefab();
        if (scenarioPrefab == null)
        {
            Debug.LogWarning($"No se pudo cargar el escenario desde {ScenarioEscena3ModelPath}. Se creara un fallback simple.");
            environment = BuildFallbackEnvironment();
            ConfigureKeepLighting(environment);
            return;
        }

        environment = PrefabUtility.InstantiatePrefab(scenarioPrefab) as GameObject;
        if (environment == null)
        {
            environment = Object.Instantiate(scenarioPrefab);
        }

        environment.name = "Scenario3Environment";

        var meshBounds = CalculateMeshFilterBounds(environment);
        var rendererBounds = CalculateEnvironmentBounds(environment);
        var meshFootprint = Mathf.Max(meshBounds.size.x, meshBounds.size.z);
        var rendererFootprint = Mathf.Max(rendererBounds.size.x, rendererBounds.size.z);
        var footprint = Mathf.Max(meshFootprint, rendererFootprint);

        if (footprint > 0.01f)
        {
            var uniformScale = TargetKeepFootprint / footprint;
            environment.transform.localScale = Vector3.one * uniformScale;
        }

        var scaledBounds = CalculateEnvironmentBounds(environment);
        if (scaledBounds.size.sqrMagnitude < 1f)
        {
            scaledBounds = CalculateMeshFilterBounds(environment);
        }

        environment.transform.position = KeepCenter + new Vector3(-scaledBounds.center.x, -scaledBounds.min.y, -scaledBounds.center.z);
        Debug.Log($"[Level03] Keep colocado en {environment.transform.position}, escala {environment.transform.localScale.x:F3}, footprint {footprint:F2}");

        EnsureRenderMeshesHaveColliders(environment);
        ApplyScenarioMaterial(environment);
        ConfigureKeepLighting(environment);
    }

    private static void ConfigureKeepLighting(GameObject environment)
    {
        var lightingRoot = new GameObject("WaterLevelLighting");

        var torchOffsets = new[]
        {
            new Vector3(-6f, 2.8f, -5f),
            new Vector3(6f, 2.8f, -5f),
            new Vector3(-6f, 2.8f, 7f),
            new Vector3(6f, 2.8f, 7f),
            new Vector3(0f, 5.2f, 0f)
        };

        for (var i = 0; i < torchOffsets.Length; i++)
        {
            var torch = new GameObject($"KeepTorch_{i + 1}");
            torch.transform.SetParent(lightingRoot.transform, false);
            torch.transform.position = KeepCenter + torchOffsets[i];

            var torchLight = torch.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.range = 12f;
            torchLight.intensity = 1.4f;
            torchLight.color = new Color(1f, 0.78f, 0.46f);
            torchLight.shadows = LightShadows.None;
        }

        if (environment != null)
        {
            var fillLight = new GameObject("KeepRimFill");
            fillLight.transform.SetParent(lightingRoot.transform, false);
            fillLight.transform.position = KeepCenter + new Vector3(0f, 12f, -6f);
            fillLight.transform.rotation = Quaternion.Euler(35f, 20f, 0f);

            var directional = fillLight.AddComponent<Light>();
            directional.type = LightType.Spot;
            directional.range = 28f;
            directional.spotAngle = 90f;
            directional.intensity = 0.9f;
            directional.color = new Color(0.55f, 0.62f, 0.85f);
            directional.shadows = LightShadows.Soft;
        }
    }

    private static GameObject LoadScenarioPrefab()
    {
        var escena3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioEscena3ModelPath);
        if (escena3Prefab != null)
        {
            return escena3Prefab;
        }

        Debug.LogWarning($"No se encontro el prefab de Escena3 en {ScenarioEscena3ModelPath}");
        return null;
    }

    private static GameObject BuildFallbackEnvironment()
    {
        var environment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        environment.name = "Scenario3Environment";
        environment.transform.position = KeepCenter + new Vector3(0f, 2.5f, 0f);
        environment.transform.localScale = new Vector3(TargetKeepFootprint, 5f, TargetKeepFootprint);
        var renderer = environment.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateLitMaterial(
            "Scenario3Fallback",
            new Color(0.18f, 0.2f, 0.26f),
            Color.black);
        return environment;
    }

    // El Keep es un FBX denso (decenas de submeshes muy detallados). Generar un
    // MeshCollider por cada submesh dispara un cook de PhysX por mesh, lo que hace
    // que abrir la escena tarde varios segundos. Lo reemplazamos por un BoxCollider
    // simplificado que envuelve el footprint del Keep: el jugador no puede atravesarlo
    // y el coste de carga cae a casi cero.
    //
    // Adicionalmente, todo el sub-tree del Keep se mueve a la layer "Ignore Raycast"
    // para que el NavMeshSurface lo excluya por layerMask y no bake-e geometria densa.
    private static void EnsureRenderMeshesHaveColliders(GameObject root)
    {
        StripExistingMeshColliders(root);

        var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
        {
            SetLayerRecursively(root, ignoreRaycastLayer);
        }

        if (!TryGetCombinedRendererBounds(root, out var worldBounds))
        {
            return;
        }

        var colliderHost = new GameObject("KeepBlocker");
        colliderHost.transform.SetParent(root.transform, worldPositionStays: true);
        colliderHost.transform.position = worldBounds.center;
        colliderHost.transform.rotation = Quaternion.identity;
        colliderHost.transform.localScale = Vector3.one;
        colliderHost.layer = 0;

        var box = colliderHost.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        var lossy = root.transform.lossyScale;
        var safeLossy = new Vector3(
            Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x,
            Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y,
            Mathf.Approximately(lossy.z, 0f) ? 1f : lossy.z);
        var localSize = new Vector3(
            worldBounds.size.x / safeLossy.x,
            worldBounds.size.y / safeLossy.y,
            worldBounds.size.z / safeLossy.z);
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private static void StripExistingMeshColliders(GameObject root)
    {
        foreach (var existing in root.GetComponentsInChildren<MeshCollider>(true))
        {
            Object.DestroyImmediate(existing);
        }

        var legacyHost = root.transform.Find("KeepBlocker");
        if (legacyHost != null)
        {
            Object.DestroyImmediate(legacyHost.gameObject);
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static Bounds CalculateEnvironmentBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one * 10f);
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static Bounds CalculateMeshFilterBounds(GameObject root)
    {
        var meshFilters = root.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one * 10f);
        }

        var combined = new Bounds();
        var first = true;
        foreach (var filter in meshFilters)
        {
            if (filter.sharedMesh == null) continue;
            var meshBounds = filter.sharedMesh.bounds;
            var worldCenter = filter.transform.TransformPoint(meshBounds.center);
            var worldSize = Vector3.Scale(meshBounds.size, filter.transform.lossyScale);
            var worldBounds = new Bounds(worldCenter, worldSize);

            if (first)
            {
                combined = worldBounds;
                first = false;
            }
            else
            {
                combined.Encapsulate(worldBounds);
            }
        }

        return first ? new Bounds(root.transform.position, Vector3.one * 10f) : combined;
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
        serializedObject.FindProperty("starsRequiredToExit").intValue = 7;
        serializedObject.FindProperty("startingLives").intValue = 3;
        serializedObject.FindProperty("scorePerStar").intValue = 100;
        serializedObject.FindProperty("levelTimeSeconds").floatValue = 240f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }

    // Tres pasos inundados con tradeoff: el moat alrededor del Keep, un canal que
    // corta la ruta sur y un estanque cerca del portal que castiga apuros finales.
    private static void ConfigureWaterHazards()
    {
        var waterRoot = new GameObject("WaterHazards");
        CreateWaterZone(waterRoot.transform, "WaterZone_KeepMoat", new Vector3(2f, 0.9f, 6f), new Vector3(18f, 2.0f, 14f));
        CreateWaterZone(waterRoot.transform, "WaterZone_SouthChannel", new Vector3(-6f, 0.9f, -13f), new Vector3(14f, 1.8f, 6f));
        CreateWaterZone(waterRoot.transform, "WaterZone_NorthPond", new Vector3(20f, 0.9f, 22f), new Vector3(10f, 1.8f, 8f));
    }

    private static void CreateWaterZone(Transform parent, string objectName, Vector3 center, Vector3 size)
    {
        var waterZone = new GameObject(objectName);
        waterZone.transform.SetParent(parent, false);
        waterZone.transform.position = center;

        var collider = waterZone.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;

        var waterVolume = waterZone.AddComponent<WaterVolume>();
        var serializedVolume = new SerializedObject(waterVolume);
        serializedVolume.FindProperty("moveSpeedMultiplier").floatValue = 0.58f;
        serializedVolume.FindProperty("sprintSpeedMultiplier").floatValue = 0.64f;
        serializedVolume.FindProperty("jumpHeightMultiplier").floatValue = 0.22f;
        serializedVolume.FindProperty("visualSinkDepth").floatValue = 0.62f;
        serializedVolume.ApplyModifiedPropertiesWithoutUndo();

        // Water surface must sit at ground level (world y ~= 0), NOT at the top of
        // the trigger collider. The trigger is intentionally tall so it catches the
        // player entering from any direction, but the visible water should look like
        // a thin film on the ground, not a floating plane in mid-air.
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
            new Vector3(12f, 0f, -2f),
            new Vector3(-14f, 0f, 9f),
            new Vector3(6f, 0f, 12f),
            new Vector3(-10f, 0f, -10f)
        };

        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level03, anchors);
        EnemySpawner.SpawnEnemies(spawns);
    }

    // Nueve estrellas distribuidas con riesgo creciente: ribera segura, perimetro
    // del moat (exposicion media) y dos cerca del Keep (alta visibilidad).
    private static void ConfigureStars()
    {
        var parent = new GameObject("Collectibles");
        var positions = new[]
        {
            new Vector3(-28f, 2.3f, -16f),
            new Vector3(-16f, 2.3f, -20f),
            new Vector3(-4f, 2.3f, -22f),
            new Vector3(14f, 2.35f, -14f),
            new Vector3(26f, 2.35f, 0f),
            new Vector3(2f, 4.6f, 6f),
            new Vector3(-8f, 2.35f, 14f),
            new Vector3(10f, 2.35f, 18f),
            new Vector3(20f, 2.35f, 28f)
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
                $"WaterLevelStar_{i + 1}",
                new Color(1f, 0.94f, 0.72f),
                new Color(1.75f, 1.35f, 0.58f));

            var starLight = GetOrAddComponent<Light>(star);
            starLight.type = LightType.Point;
            starLight.range = 3.1f;
            starLight.intensity = 0.95f;
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
        // Portal canon compartido (Meshy). El bioma cambia pero el remate
        // visual del objetivo es el mismo en todos los niveles. Atravesable
        // via trigger isTrigger=true del propio root del portal.
        ExitPortalBuilder.BuildPortal(new Vector3(26f, 0f, 28f));
    }

    private static void ConfigureNavigation()
    {
        var navigationObject = new GameObject("Navigation");
        var navMeshSurface = GetOrAddComponent<NavMeshSurface>(navigationObject);
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

        // Excluimos "Ignore Raycast" porque ahi vive el Keep (FBX denso de Meshy).
        // Bakear sus submeshes hace que la carga de la escena tarde varios segundos
        // y no aporta nada: el Keep ya esta bloqueado por un BoxCollider simplificado.
        var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        navMeshSurface.layerMask = ignoreRaycastLayer >= 0 ? ~(1 << ignoreRaycastLayer) : ~0;
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
        ApplyNightPalette(prop, new Color(0.22f, 0.28f, 0.24f), 0.72f);
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

    private static void EnsureTextureImportSettings()
    {
        var normalPath = $"{ScenarioEscena3TexturePrefix}_normal.png";
        if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
        {
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
        }

        var metalPath = $"{ScenarioEscena3TexturePrefix}_metallic.png";
        if (AssetImporter.GetAtPath(metalPath) is TextureImporter metalImporter)
        {
            if (metalImporter.sRGBTexture)
            {
                metalImporter.sRGBTexture = false;
                metalImporter.SaveAndReimport();
            }
        }

        var roughnessPath = $"{ScenarioEscena3TexturePrefix}_roughness.png";
        if (AssetImporter.GetAtPath(roughnessPath) is TextureImporter roughnessImporter)
        {
            if (roughnessImporter.sRGBTexture)
            {
                roughnessImporter.sRGBTexture = false;
                roughnessImporter.SaveAndReimport();
            }
        }
    }

    private static void ApplyScenarioMaterial(GameObject environment)
    {
        AssetDatabase.Refresh();
        EnsureTextureImportSettings();

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("URP/Lit shader not found. Scenario materials will use defaults.");
            return;
        }

        var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(ScenarioEscena3MaterialPath);
        var material = sourceMaterial != null ? new Material(sourceMaterial) : new Material(shader);
        material.name = "Scenario3_Lit";
        material.shader = shader;

        var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ScenarioEscena3TexturePrefix}.png");
        var metal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ScenarioEscena3TexturePrefix}_metallic.png");
        var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ScenarioEscena3TexturePrefix}_normal.png");
        var emission = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ScenarioEscena3TexturePrefix}_emission.png");

        if (baseColor != null)
        {
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);
        }

        if (metal != null)
        {
            material.SetTexture("_MetallicGlossMap", metal);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 0.5f);
        }

        if (normalMap != null)
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", 1f);
        }

        if (emission != null)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetTexture("_EmissionMap", emission);
            material.SetColor("_EmissionColor", Color.white * 1.2f);
        }

        var renderers = environment.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }

        var textureCount = (baseColor != null ? 1 : 0) + (metal != null ? 1 : 0)
            + (normalMap != null ? 1 : 0) + (emission != null ? 1 : 0);
        Debug.Log($"Scenario3 material applied to {renderers.Length} renderers with {textureCount}/4 textures.");
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
