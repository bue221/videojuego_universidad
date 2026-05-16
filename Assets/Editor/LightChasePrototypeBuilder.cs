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
    private const string PlaygroundScenePath = "Assets/StarterAssets/ThirdPersonController/Scenes/Playground.unity";
    private const string PrototypeScenePath = "Assets/Scenes/LightChasePrototype.unity";
    private const string PlayerPrefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
    private const string CameraPrefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private const string GridWhiteMaterialPath = "Assets/StarterAssets/Environment/Art/Materials/GridWhite_01_Mat.mat";
    private const string GridOrangeMaterialPath = "Assets/StarterAssets/Environment/Art/Materials/GridOrange_01_Mat.mat";
    private const string GridBlueMaterialPath = "Assets/StarterAssets/Environment/Art/Materials/GridBlue_01_Mat.mat";
    private static readonly Color NightFogColor = new(0.02f, 0.03f, 0.06f, 1f);
    private static readonly Color NightAmbientColor = new(0.015f, 0.02f, 0.035f, 1f);

    [MenuItem("Tools/Prototype/Build Light Chase Level")]
    public static void BuildLevel()
    {
        EnsurePrototypeSceneExists();
        var scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);

        var player = FindOrCreatePlayer();
        var playerLightState = ConfigurePlayer(player);
        ConfigureAtmosphere();
        var levelManager = ConfigureLevelManager();
        ConfigureEnemy(player.transform);
        ConfigureStars();
        ConfigureExit();
        ConfigureHud(levelManager);
        ConfigureNavigation();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = playerLightState.gameObject;
        Debug.Log($"Light Chase prototype listo en {PrototypeScenePath}");
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
        player.transform.position = new Vector3(0f, 0.15f, -6f);
        return player;
    }

    private static PlayerLightState ConfigurePlayer(GameObject player)
    {
        var state = GetOrAddComponent<PlayerLightState>(player);

        var glowLightTransform = player.transform.Find("GlowLight");
        if (glowLightTransform == null)
        {
            var lightObject = new GameObject("GlowLight");
            lightObject.transform.SetParent(player.transform);
            lightObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            glowLightTransform = lightObject.transform;
        }

        var glowLight = GetOrAddComponent<Light>(glowLightTransform.gameObject);
        glowLight.type = LightType.Point;
        glowLight.color = new Color(0.6f, 0.8f, 1f);
        glowLight.intensity = 0.45f;
        glowLight.range = 3.25f;
        glowLight.shadows = LightShadows.Soft;

        var trailTransform = player.transform.Find("GlowTrail");
        if (trailTransform == null)
        {
            var trailObject = new GameObject("GlowTrail");
            trailObject.transform.SetParent(player.transform);
            trailObject.transform.localPosition = new Vector3(0f, 0.2f, -0.15f);
            trailTransform = trailObject.transform;
        }

        var trailRenderer = GetOrAddComponent<TrailRenderer>(trailTransform.gameObject);
        trailRenderer.alignment = LineAlignment.View;
        trailRenderer.time = 0.3f;
        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.material = CreateUnlitMaterial("PlayerGlowTrail", new Color(0.65f, 0.85f, 1f));

        state.ConfigureVisuals(
            glowLight,
            trailRenderer,
            BuildGradient(new Color(0.55f, 0.8f, 1f), new Color(0.25f, 0.35f, 0.8f, 0f)),
            BuildGradient(new Color(1f, 0.95f, 0.55f), new Color(1f, 0.45f, 0.15f, 0f)));

        return state;
    }

    private static void ConfigureAtmosphere()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientLight = NightAmbientColor;
        RenderSettings.subtractiveShadowColor = Color.black;
        RenderSettings.reflectionIntensity = 0.1f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = NightFogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.035f;

        var directionalLight = GameObject.Find("Directional Light");
        if (directionalLight != null && directionalLight.TryGetComponent<Light>(out var mainLight))
        {
            mainLight.color = new Color(0.16f, 0.2f, 0.28f);
            mainLight.intensity = 0.035f;
            mainLight.shadowStrength = 1f;
        }

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.005f, 0.008f, 0.015f, 1f);
        }

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
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "LightHunter";
            enemyObject.transform.position = new Vector3(12f, 0.9f, 11f);
            enemyObject.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f);

            var agent = enemyObject.AddComponent<NavMeshAgent>();
            agent.angularSpeed = 240f;
            agent.acceleration = 24f;
            agent.stoppingDistance = 1.35f;

            enemy = enemyObject.AddComponent<EnemyLightSeeker>();
            var renderer = enemyObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GridOrangeMaterialPath);
                enemy.ConfigureRenderer(renderer);
            }
        }
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

    private static void ConfigureHud(PrototypeLevelManager levelManager)
    {
        var canvasObject = GameObject.Find("GameplayHUD");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("GameplayHUD");
        }

        var canvas = GetOrAddComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        GetOrAddComponent<CanvasScaler>(canvasObject).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        GetOrAddComponent<GraphicRaycaster>(canvasObject);

        var hudController = GetOrAddComponent<LightChasePrototype.UI.GameHudController>(canvasObject);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var livesText = FindOrCreateHudText(canvas.transform, "LivesText", font, new Vector2(20f, -20f), TextAnchor.UpperLeft, 30);
        var scoreText = FindOrCreateHudText(canvas.transform, "ScoreText", font, new Vector2(20f, -58f), TextAnchor.UpperLeft, 30);
        var timerText = FindOrCreateHudText(canvas.transform, "TimerText", font, new Vector2(-20f, -20f), TextAnchor.UpperRight, 30);
        var statusText = FindOrCreateHudText(canvas.transform, "StatusText", font, new Vector2(0f, 40f), TextAnchor.LowerCenter, 28);

        var statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.sizeDelta = new Vector2(900f, 70f);

        hudController.Configure(levelManager, livesText, scoreText, timerText, statusText);
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

    private static Gradient BuildGradient(Color startColor, Color endColor)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(Color.Lerp(startColor, endColor, 0.5f), 0.55f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(0.45f, 0.6f),
                new GradientAlphaKey(endColor.a, 1f)
            });
        return gradient;
    }

    private static Material CreateUnlitMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var material = new Material(shader) { name = materialName, color = color };
        material.EnableKeyword("_EMISSION");
        return material;
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

}
