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
using UnityEngine.UI;

public static class LightChasePrototypeBuilder
{
    private const string PlaygroundScenePath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Scenes/Playground.unity";
    private const string PrototypeScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string PlayerPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
    private const string CameraPrefabPath = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private const string GridWhiteMaterialPath = "Assets/ThirdParty/StarterAssets/Environment/Art/Materials/GridWhite_01_Mat.mat";
    private const string GridOrangeMaterialPath = "Assets/ThirdParty/StarterAssets/Environment/Art/Materials/GridOrange_01_Mat.mat";
    private const string GridBlueMaterialPath = "Assets/ThirdParty/StarterAssets/Environment/Art/Materials/GridBlue_01_Mat.mat";
    private const string Enemy01ModelPath = "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_Animation_Walking_withSkin.fbx";
    private const string Enemy01MaterialPath = "Assets/MeshyImports/Enemigo_01/Material_1.mat";
    [MenuItem("Tools/Prototype/Build Light Chase Level")]
    public static void BuildLevel()
    {
        EnsurePrototypeSceneExists();
        var scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);

        ClearPreviousGeneratedContent();
        var player = FindOrCreatePlayer();
        var playerLightState = ConfigurePlayer(player);
        ConfigureAtmosphere();
        ConfigureLevelManager();
        ConfigureEnemy(player.transform);
        ConfigureStars();
        ConfigureExit();
        ConfigureNavigation();
        EnsureGameplayScenesIncludedInBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = playerLightState.gameObject;
        Debug.Log($"Light Chase prototype listo en {PrototypeScenePath}");
    }

    [MenuItem("Tools/Prototype/Apply Suspense Atmosphere")]
    public static void ApplySuspenseAtmosphere()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("No hay una escena cargada para aplicar la atmosfera.");
            return;
        }

        ConfigureAtmosphere();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Atmosfera de suspenso aplicada en {scene.path}");
    }

    private static void EnsurePrototypeSceneExists()
    {
        if (File.Exists(PrototypeScenePath))
        {
            return;
        }

        AssetDatabase.CopyAsset(PlaygroundScenePath, PrototypeScenePath);
        AssetDatabase.Refresh();
    }

    private static void ClearPreviousGeneratedContent()
    {
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
            return playerController.gameObject;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);

        var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        PrefabUtility.InstantiatePrefab(cameraPrefab);
        player.transform.position = PlayerAvatarSetup.DefaultSpawnPosition;
        return player;
    }

    private static PlayerLightState ConfigurePlayer(GameObject player)
    {
        return PlayerAvatarSetup.EnsureGameplayPresentation(player);
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

    private static PrototypeLevelManager ConfigureLevelManager()
    {
        var manager = Object.FindAnyObjectByType<PrototypeLevelManager>();
        if (manager == null)
        {
            var managerObject = new GameObject("PrototypeLevelManager");
            manager = managerObject.AddComponent<PrototypeLevelManager>();
        }

        var serializedObject = new SerializedObject(manager);
        serializedObject.FindProperty("starsRequiredToExit").intValue = 5;
        serializedObject.FindProperty("startingLives").intValue = 3;
        serializedObject.FindProperty("scorePerStar").intValue = 100;
        serializedObject.FindProperty("levelTimeSeconds").floatValue = 180f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }

    private static void ConfigureEnemy(Transform playerTransform)
    {
        var enemy = Object.FindAnyObjectByType<EnemyLightSeeker>();
        if (enemy == null)
        {
            var enemyObject = CreateEnemyRoot("LightHunter", new Vector3(12f, 0f, 11f));
            AlignBaseToY(enemyObject, 0f);
            enemyObject.transform.position += Vector3.up * 0.02f;

            var agent = enemyObject.AddComponent<NavMeshAgent>();
            agent.angularSpeed = 240f;
            agent.acceleration = 24f;
            agent.stoppingDistance = 1.35f;

            enemy = enemyObject.AddComponent<EnemyLightSeeker>();
            var renderer = enemyObject.GetComponentInChildren<Renderer>();
            enemy.ConfigureRenderer(renderer);
        }
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

    private static void AlignBaseToY(GameObject root, float targetY)
    {
        if (root == null)
        {
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        var delta = targetY - bounds.min.y;
        root.transform.position += Vector3.up * delta;
    }

    private static void ConfigureStars()
    {
        var parent = GameObject.Find("Collectibles");
        if (parent == null)
        {
            parent = new GameObject("Collectibles");
        }

        foreach (Transform child in parent.transform)
        {
            Object.DestroyImmediate(child.gameObject);
        }

        var positions = new[]
        {
            new Vector3(0f, 1.2f, 2f),
            new Vector3(6f, 1.4f, 6f),
            new Vector3(11f, 1.5f, 1f),
            new Vector3(-7f, 2f, 12f),
            new Vector3(14f, 2.5f, 14f),
            new Vector3(-12f, 1.5f, -4f),
            new Vector3(4f, 3f, 18f)
        };

        var material = AssetDatabase.LoadAssetAtPath<Material>(GridWhiteMaterialPath);

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
                $"StarGlow_{i + 1}",
                material,
                new Color(1f, 0.95f, 0.75f),
                new Color(1.6f, 1.3f, 0.55f));

            var starLight = GetOrAddComponent<Light>(star);
            starLight.type = LightType.Point;
            starLight.range = 2.75f;
            starLight.intensity = 0.8f;
            starLight.color = new Color(1f, 0.92f, 0.6f);
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
        var exit = Object.FindAnyObjectByType<ExitPortal>();
        if (exit == null)
        {
            var exitObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitObject.name = "ExitPortal";
            exitObject.transform.position = new Vector3(18f, 1.5f, 20f);
            exitObject.transform.localScale = new Vector3(2.5f, 3f, 0.5f);

            var renderer = exitObject.GetComponent<Renderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GridBlueMaterialPath);

            var collider = exitObject.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            exit = exitObject.AddComponent<ExitPortal>();
            exit.ConfigureRenderer(renderer);
        }
    }

    private static void ConfigureNavigation()
    {
        var navigationObject = GameObject.Find("Navigation");
        if (navigationObject == null)
        {
            navigationObject = new GameObject("Navigation");
        }

        var navMeshSurface = GetOrAddComponent<NavMeshSurface>(navigationObject);
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navMeshSurface.layerMask = ~0;
        navMeshSurface.BuildNavMesh();
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
            material.SetFloat("_Smoothness", 0.75f);
        }

        return material;
    }

    private static Text FindOrCreateHudText(Transform parent, string objectName, Font font, Vector2 anchoredPosition, TextAnchor alignment, int fontSize)
    {
        var existingTransform = parent.Find(objectName);
        GameObject textObject;
        if (existingTransform == null)
        {
            textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent);
        }
        else
        {
            textObject = existingTransform.gameObject;
        }

        var rectTransform = GetOrAddComponent<RectTransform>(textObject);
        var text = GetOrAddComponent<Text>(textObject);
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.95f, 0.95f, 1f, 0.98f);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        if (alignment == TextAnchor.UpperRight)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
        }
        else if (alignment == TextAnchor.LowerCenter)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(450f, 48f);
        return text;
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
            PrototypeScenePath,
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
