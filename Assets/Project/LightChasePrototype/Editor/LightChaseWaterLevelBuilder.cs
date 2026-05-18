using System.IO;
using LightChasePrototype;
using StarterAssets;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LightChaseWaterLevelBuilder
{
    private const string PlaygroundScenePath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Scenes/Playground.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string PlayerPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
    private const string CameraPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private const string ScenarioConvertedModelPath = "Assets/Project/LightChasePrototype/Resources/ModelosEscenarios/escenario_3_converted.fbx";
    private const string ScenarioModelPath = "Assets/Project/LightChasePrototype/Resources/ModelosEscenarios/escenario_3.glb";
    private const string ImportedScenarioModelPath = "Assets/MeshyImports/LightChaseLevel03/escenario_3_runtime.glb";
    private const string TextureFolder = "Assets/Project/LightChasePrototype/Resources/ModelosEscenarios/Textures";
    private const float TargetEnvironmentFootprint = 72f;
    private const string PrototypeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";

    [MenuItem("Tools/Prototype/Build Light Chase Level 03")]
    public static void BuildLevel()
    {
        BuildLevelInternal(preserveExistingScenarioEnvironment: true);
    }

    [MenuItem("Tools/Prototype/Build Light Chase Level 03 (Full Rebuild)")]
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
        ConfigureAtmosphere();

        var environment = BuildScenarioEnvironment(preserveExistingScenarioEnvironment);
        var environmentBounds = CalculateEnvironmentBounds(environment);
        ConfigureLevelManager();
        var player = FindOrCreatePlayer(environmentBounds);
        var playerLightState = PlayerAvatarSetup.EnsureGameplayPresentation(player);
        PlayerAvatarSetup.BindCameraToPlayer(player);

        ConfigureWater(environmentBounds);
        ConfigureEnemy(environmentBounds);
        ConfigureStars(environmentBounds);
        ConfigureExit(environmentBounds);
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

    private static GameObject BuildScenarioEnvironment(bool preserveExistingScenarioEnvironment)
    {
        if (preserveExistingScenarioEnvironment)
        {
            var existingEnvironment = GameObject.Find("Scenario3Environment");
            if (existingEnvironment != null)
            {
                EnsureRenderMeshesHaveColliders(existingEnvironment);
                ApplyScenarioMaterial(existingEnvironment);
                return existingEnvironment;
            }
        }

        var scenarioPrefab = LoadScenarioPrefab();
        if (scenarioPrefab == null)
        {
            Debug.LogWarning($"No se pudo cargar {ScenarioModelPath}. Se creara un fallback simple.");
            return BuildFallbackEnvironment();
        }

        var environment = PrefabUtility.InstantiatePrefab(scenarioPrefab) as GameObject;
        if (environment == null)
        {
            environment = Object.Instantiate(scenarioPrefab);
        }

        environment.name = "Scenario3Environment";

        var rendererCount = environment.GetComponentsInChildren<Renderer>().Length;
        var meshFilterCount = environment.GetComponentsInChildren<MeshFilter>().Length;
        Debug.Log($"[Level03] Instantiated '{scenarioPrefab.name}': renderers={rendererCount}, meshFilters={meshFilterCount}, localScale={environment.transform.localScale}");

        var initialBounds = CalculateEnvironmentBounds(environment);
        var meshBounds = CalculateMeshFilterBounds(environment);
        var currentFootprint = Mathf.Max(initialBounds.size.x, initialBounds.size.z);
        var meshFootprint = Mathf.Max(meshBounds.size.x, meshBounds.size.z);
        Debug.Log($"[Level03] Renderer bounds: size={initialBounds.size}, footprint={currentFootprint:F3}");
        Debug.Log($"[Level03] MeshFilter bounds: size={meshBounds.size}, footprint={meshFootprint:F3}");

        if (currentFootprint < 0.01f)
        {
            Debug.LogWarning("[Level03] Renderer footprint near zero — using MeshFilter fallback.");
            initialBounds = meshBounds;
            currentFootprint = meshFootprint;
        }

        if (currentFootprint > 0.01f)
        {
            var uniformScale = TargetEnvironmentFootprint / currentFootprint;
            environment.transform.localScale = Vector3.one * uniformScale;
            Debug.Log($"[Level03] Applied scale: {uniformScale:F3} (target={TargetEnvironmentFootprint})");
        }

        var scaledBounds = CalculateEnvironmentBounds(environment);
        if (scaledBounds.size.sqrMagnitude < 1f)
        {
            scaledBounds = CalculateMeshFilterBounds(environment);
        }

        environment.transform.position += new Vector3(-scaledBounds.center.x, -scaledBounds.min.y, -scaledBounds.center.z);
        Debug.Log($"[Level03] Final bounds: size={scaledBounds.size}, position={environment.transform.position}");

        EnsureRenderMeshesHaveColliders(environment);
        ApplyScenarioMaterial(environment);
        return environment;
    }

    private static GameObject LoadScenarioPrefab()
    {
        var convertedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioConvertedModelPath);
        if (convertedPrefab != null)
        {
            return convertedPrefab;
        }

        var directPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScenarioModelPath);
        if (directPrefab != null)
        {
            return directPrefab;
        }

        if (!File.Exists(ScenarioModelPath))
        {
            return null;
        }

        EnsureImportedScenarioCopyExists();
        return AssetDatabase.LoadAssetAtPath<GameObject>(ImportedScenarioModelPath);
    }

    private static void EnsureImportedScenarioCopyExists()
    {
        const string meshyRoot = "Assets/MeshyImports";
        const string levelFolder = "Assets/MeshyImports/LightChaseLevel03";

        if (!AssetDatabase.IsValidFolder(meshyRoot))
        {
            AssetDatabase.CreateFolder("Assets", "MeshyImports");
        }

        if (!AssetDatabase.IsValidFolder(levelFolder))
        {
            AssetDatabase.CreateFolder(meshyRoot, "LightChaseLevel03");
        }

        File.Copy(ScenarioModelPath, ImportedScenarioModelPath, true);
        AssetDatabase.ImportAsset(ImportedScenarioModelPath, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(ImportedScenarioModelPath) is ModelImporter importer)
        {
            importer.SaveAndReimport();
        }
    }

    private static GameObject BuildFallbackEnvironment()
    {
        var environment = GameObject.CreatePrimitive(PrimitiveType.Plane);
        environment.name = "Scenario3Environment";
        environment.transform.localScale = new Vector3(6f, 1f, 6f);
        return environment;
    }

    private static void EnsureRenderMeshesHaveColliders(GameObject root)
    {
        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            if (!meshFilter.TryGetComponent<MeshCollider>(out var meshCollider))
            {
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                | MeshColliderCookingOptions.EnableMeshCleaning
                | MeshColliderCookingOptions.WeldColocatedVertices;
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

    private static GameObject FindOrCreatePlayer(Bounds environmentBounds)
    {
        var spawnPosition = BuildGroundPoint(environmentBounds, 0.16f, 0.18f, 0.2f);
        var facingPoint = BuildGroundPoint(environmentBounds, 0.26f, 0.28f, 0.2f);
        var spawnRotation = Quaternion.LookRotation((facingPoint - spawnPosition).normalized);

        var playerController = Object.FindAnyObjectByType<ThirdPersonController>();
        if (playerController != null)
        {
            playerController.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            return playerController.gameObject;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);

        var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        PrefabUtility.InstantiatePrefab(cameraPrefab);
        player.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        return player;
    }

    private static void ConfigureWater(Bounds environmentBounds)
    {
        var root = new GameObject("WaterHazards");
        CreateWaterZone(root.transform, "WaterZone_CentralLagoon", environmentBounds, 0.48f, 0.42f, 0.34f, 0.26f);
        CreateWaterZone(root.transform, "WaterZone_MidChannel", environmentBounds, 0.62f, 0.54f, 0.24f, 0.18f);
        CreateWaterZone(root.transform, "WaterZone_FinalApproach", environmentBounds, 0.72f, 0.7f, 0.19f, 0.16f);
    }

    private static void CreateWaterZone(Transform parent, string objectName, Bounds environmentBounds, float centerXT, float centerZT, float widthT, float depthT)
    {
        var waterZone = new GameObject(objectName);
        waterZone.transform.SetParent(parent, false);

        var waterCenter = BuildFlatPoint(environmentBounds, centerXT, centerZT);
        waterCenter.y = environmentBounds.min.y + 1.15f;
        waterZone.transform.position = waterCenter;

        var collider = waterZone.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(
            Mathf.Max(6f, environmentBounds.size.x * widthT),
            2.4f,
            Mathf.Max(4f, environmentBounds.size.z * depthT));

        var waterVolume = waterZone.AddComponent<WaterVolume>();
        var serializedVolume = new SerializedObject(waterVolume);
        serializedVolume.FindProperty("moveSpeedMultiplier").floatValue = 0.58f;
        serializedVolume.FindProperty("sprintSpeedMultiplier").floatValue = 0.64f;
        serializedVolume.FindProperty("jumpHeightMultiplier").floatValue = 0.22f;
        serializedVolume.FindProperty("visualSinkDepth").floatValue = 0.62f;
        serializedVolume.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEnemy(Bounds environmentBounds)
    {
        var groundPoint = BuildGroundPoint(environmentBounds, 0.52f, 0.5f, 0f);
        var enemyObject = EnemyBuilder.BuildEnemyRoot("LightHunter", groundPoint);
        EnemyBuilder.AlignBaseToY(enemyObject, groundPoint.y);
        enemyObject.transform.position += Vector3.up * 0.02f;
        EnemyBuilder.ConfigureNavMeshAgent(enemyObject);
        EnemyBuilder.ConfigureEnemyLightSeeker(enemyObject);
    }

    private static void ConfigureStars(Bounds environmentBounds)
    {
        var parent = new GameObject("Collectibles");
        var starAnchors = new[]
        {
            new Vector2(0.24f, 0.22f),
            new Vector2(0.32f, 0.34f),
            new Vector2(0.44f, 0.3f),
            new Vector2(0.56f, 0.42f),
            new Vector2(0.63f, 0.56f),
            new Vector2(0.74f, 0.62f),
            new Vector2(0.79f, 0.74f),
            new Vector2(0.48f, 0.68f),
            new Vector2(0.29f, 0.58f)
        };

        for (var i = 0; i < starAnchors.Length; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = $"Star_{i + 1}";
            star.transform.SetParent(parent.transform);
            star.transform.position = BuildGroundPoint(environmentBounds, starAnchors[i].x, starAnchors[i].y, 2.4f);
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

    private static void ConfigureExit(Bounds environmentBounds)
    {
        var exitObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exitObject.name = "ExitPortal";
        exitObject.transform.position = BuildGroundPoint(environmentBounds, 0.84f, 0.83f, 1.7f);
        exitObject.transform.localScale = new Vector3(1.5f, 1.35f, 1.5f);

        var renderer = exitObject.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateLitMaterial(
            "WaterLevelExit",
            new Color(0.16f, 0.42f, 0.7f),
            new Color(0.08f, 0.48f, 0.95f));

        var collider = exitObject.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.height = 2.2f;
        collider.radius = 0.9f;

        var exitPortal = exitObject.AddComponent<ExitPortal>();
        exitPortal.ConfigureRenderer(renderer);
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

    private static Vector3 BuildGroundPoint(Bounds environmentBounds, float xT, float zT, float verticalOffset)
    {
        var flatPoint = BuildFlatPoint(environmentBounds, xT, zT);
        var rayOrigin = flatPoint + Vector3.up * (environmentBounds.size.y + 80f);
        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, environmentBounds.size.y + 160f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * verticalOffset;
        }

        return new Vector3(flatPoint.x, environmentBounds.center.y + verticalOffset, flatPoint.z);
    }

    private static Vector3 BuildFlatPoint(Bounds environmentBounds, float xT, float zT)
    {
        return new Vector3(
            Mathf.Lerp(environmentBounds.min.x, environmentBounds.max.x, xT),
            0f,
            Mathf.Lerp(environmentBounds.min.z, environmentBounds.max.z, zT));
    }

    private static void EnsureTextureImportSettings()
    {
        var normalPath = $"{TextureFolder}/escenario_3_normalMap.jpg";
        if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
        {
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
        }

        var metalPath = $"{TextureFolder}/escenario_3_metallicRoughness.jpg";
        if (AssetImporter.GetAtPath(metalPath) is TextureImporter metalImporter)
        {
            if (metalImporter.sRGBTexture)
            {
                metalImporter.sRGBTexture = false;
                metalImporter.SaveAndReimport();
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

        var material = new Material(shader) { name = "Scenario3_Lit" };

        var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/escenario_3_baseColor.jpg");
        var metalRough = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/escenario_3_metallicRoughness.jpg");
        var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/escenario_3_normalMap.jpg");
        var emission = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/escenario_3_emission.jpg");

        if (baseColor != null)
        {
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);
        }

        if (metalRough != null)
        {
            material.SetTexture("_MetallicGlossMap", metalRough);
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
            material.SetColor("_EmissionColor", Color.white);
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

        var textureCount = (baseColor != null ? 1 : 0) + (metalRough != null ? 1 : 0)
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
