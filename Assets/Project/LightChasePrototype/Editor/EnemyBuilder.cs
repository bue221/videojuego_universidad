using System.IO;
using LightChasePrototype;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class EnemyBuilder
{
    public const EnemyKind DefaultEnemyKind = EnemyKind.Deshilachador;

    // Legacy aliases kept so older callers and tests keep compiling. They resolve to the
    // default enemy kind assets, but new code should use EnemyKindCatalog.GetAssets(kind).
    public static string Enemy01ModelPath => EnemyKindCatalog.GetAssets(DefaultEnemyKind).ModelPath;
    public static string Enemy01MaterialPath => EnemyKindCatalog.GetAssets(DefaultEnemyKind).MaterialPath;

    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    private const float EnemyVisualHeight = 2.2f;
    private const float NavMeshAgentRadius = 0.45f;
    private const float NavMeshAgentHeight = 2.2f;

    // baseOffset por defecto al construir un enemigo. La compensacion fina contra la
    // voxelizacion del NavMesh y el desfase pivote-pies del FBX se hace en runtime
    // dentro de EnemyLightSeeker.CalibrateBaseOffsetFromGround, que mide el piso real
    // con un raycast y ajusta este valor. Lo dejamos en 0 al construir para que el
    // builder no peleé con esa calibracion.
    private const float NavMeshAgentBaseOffset = 0f;

    public static GameObject BuildEnemyRoot(string objectName, Vector3 position)
    {
        return BuildEnemyRoot(DefaultEnemyKind, objectName, position);
    }

    public static GameObject BuildEnemyRoot(EnemyKind kind, string objectName, Vector3 position)
    {
        var assets = EnemyKindCatalog.GetAssets(kind);
        var root = new GameObject(objectName);
        root.transform.position = position;

        GameObject modelInstance = TryInstantiateEnemyModel(root.transform, assets);
        if (modelInstance == null)
        {
            Debug.LogError($"[EnemyBuilder] Could not instantiate enemy model for kind '{kind}' from {assets.ModelPath}. Skipping enemy '{objectName}'.");
            Object.DestroyImmediate(root);
            return null;
        }

        ConfigureAnimator(modelInstance, assets);
        ConfigureEnemyLight(root);
        ConfigureBodyCollider(root);
        return root;
    }

    private static GameObject TryInstantiateEnemyModel(Transform parent, EnemyKindAssets assets)
    {
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assets.ModelPath);
        if (modelPrefab == null)
        {
            return null;
        }

        var modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
        if (modelInstance == null)
        {
            modelInstance = Object.Instantiate(modelPrefab);
        }
        else
        {
            PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        modelInstance.name = assets.ModelObjectName;
        modelInstance.transform.SetParent(parent, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;

        ApplyEnemyMaterial(modelInstance, assets);
        NormalizeModelHeight(modelInstance.transform, EnemyVisualHeight);
        AlignModelFeetToParent(modelInstance.transform);
        return modelInstance;
    }

    public static void ConfigureAnimator(GameObject animatorOwner)
    {
        ConfigureAnimator(animatorOwner, EnemyKindCatalog.GetAssets(DefaultEnemyKind));
    }

    public static void ConfigureAnimator(GameObject animatorOwner, EnemyKindAssets assets)
    {
        if (animatorOwner == null)
        {
            return;
        }

        EnsureAnimationFolderExists();
        var walkClip = GetOrCreateWalkClip(assets);
        var controller = GetOrCreateAnimatorController(assets, walkClip);
        var animator = animatorOwner.GetComponent<Animator>();
        if (animator == null)
        {
            animator = animatorOwner.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.avatar = LoadEnemyAvatar(assets);
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    // One-shot prepare step. Designed to be called once per editor session (e.g. from
    // Tools → Prototype → Rebuild All Levels) BEFORE any level builder runs, so that the
    // FBX is reimported at most once per kind even if multiple enemies are spawned later.
    //
    // We deliberately DO NOT touch importer.clipAnimations here. Writing a partially
    // initialised override array (e.g. just flipping loopTime on a default-constructed
    // ModelImporterClipAnimation) corrupts the importer state and crashes Unity natively
    // inside ModelImporter::SplitAnimationClips. The walk loop is guaranteed downstream
    // by cloning the clip into a project asset and setting loopTime on that copy.
    //
    // Por que se fuerza Generic (no Humanoid): los FBX de Meshy llegan marcados como
    // Humanoid pero sin mapping de huesos valido (humanDescription.human y skeleton
    // vacios). Eso hace que el clip clonado guarde paths como muscle hashes en vez de
    // strings de jerarquia, y al reproducirlo el modelo queda congelado en T-pose porque
    // el animator no puede mapear los hashes contra el armature. Generic preserva las
    // paths como rutas reales (Armature/Hips/...) y la animacion corre tal cual viene.
    public static void PrepareEnemyKindAssets(EnemyKindAssets assets)
    {
        var importer = AssetImporter.GetAtPath(assets.ModelPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        var needsReimport = false;
        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            needsReimport = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            needsReimport = true;
        }

        if (needsReimport)
        {
            // Si el FBX cambia de Humanoid a Generic, el clip clonado previo quedo
            // con paths como muscle hashes y ya no resuelve contra el armature.
            // Borramos el clon para que GetOrCreateWalkClip lo reconstruya con paths
            // de jerarquia reales en el siguiente build.
            InvalidateClonedWalkClip(assets);
            importer.SaveAndReimport();
        }
    }

    private static void InvalidateClonedWalkClip(EnemyKindAssets assets)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assets.WalkClipPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(assets.WalkClipPath);
        }
    }

    private static Avatar LoadEnemyAvatar(EnemyKindAssets assets)
    {
        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(assets.ModelPath))
        {
            if (sub is Avatar avatar && avatar.isValid)
            {
                return avatar;
            }
        }

        return null;
    }

    private static void EnsureAnimationFolderExists()
    {
        const string animationFolder = "Assets/Project/LightChasePrototype/Animation";
        if (!AssetDatabase.IsValidFolder(animationFolder))
        {
            var parent = Path.GetDirectoryName(animationFolder);
            var name = Path.GetFileName(animationFolder);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static AnimationClip GetOrCreateWalkClip(EnemyKindAssets assets)
    {
        // Use a project-owned clone of the FBX clip so we can safely flip loopTime
        // without modifying the model importer (which crashes the editor when fed
        // partially initialised override arrays).
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assets.WalkClipPath);
        if (existing != null)
        {
            EnsureClipLoops(existing);
            return existing;
        }

        var sourceClip = ExtractClipFromFbx(assets);
        if (sourceClip != null)
        {
            var clone = Object.Instantiate(sourceClip);
            clone.name = Path.GetFileNameWithoutExtension(assets.WalkClipPath);
            AssetDatabase.CreateAsset(clone, assets.WalkClipPath);
            EnsureClipLoops(clone);
            return clone;
        }

        return CreateFallbackClip(assets);
    }

    private static void EnsureClipLoops(AnimationClip clip)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        if (settings.loopTime)
        {
            return;
        }

        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static AnimationClip ExtractClipFromFbx(EnemyKindAssets assets)
    {
        AnimationClip best = null;
        foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(assets.ModelPath))
        {
            if (sub is AnimationClip clip && IsUsableClip(clip))
            {
                if (best == null || clip.length > best.length)
                {
                    best = clip;
                }
            }
        }

        if (best != null)
        {
            return best;
        }

        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(assets.ModelPath))
        {
            if (sub is AnimationClip clip && IsUsableClip(clip))
            {
                return clip;
            }
        }

        return null;
    }

    private static bool IsUsableClip(AnimationClip clip)
    {
        return clip != null
            && !clip.name.StartsWith("__preview__")
            && clip.length > 0.05f;
    }

    private static AnimationClip CreateFallbackClip(EnemyKindAssets assets)
    {
        var clip = new AnimationClip();
        clip.name = Path.GetFileNameWithoutExtension(assets.WalkClipPath);
        clip.frameRate = 30;
        clip.wrapMode = WrapMode.Loop;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.1f),
            new Keyframe(1f, 0f));
        clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
        AssetDatabase.CreateAsset(clip, assets.WalkClipPath);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static RuntimeAnimatorController GetOrCreateAnimatorController(EnemyKindAssets assets, AnimationClip clip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assets.ControllerPath);

        // If the on-disk controller is corrupt (empty state machine slot), nuke it and
        // recreate from scratch. This recovers from past failed runs without leaving the
        // Animator emitting "Statemachine for layer 'Base Layer' is missing" every frame.
        if (controller != null && IsControllerBaseLayerEmpty(controller))
        {
            AssetDatabase.DeleteAsset(assets.ControllerPath);
            controller = null;
        }

        var createdNow = false;
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(assets.ControllerPath);
            createdNow = true;
        }

        var paramChanged = EnsureSpeedParameter(controller);
        var stateChanged = EnsureWalkState(controller, clip);

        if (createdNow || paramChanged || stateChanged)
        {
            EditorUtility.SetDirty(controller);
        }

        return controller;
    }

    private static bool IsControllerBaseLayerEmpty(AnimatorController controller)
    {
        var layers = controller.layers;
        if (layers == null || layers.Length == 0)
        {
            return true;
        }

        return layers[0].stateMachine == null;
    }

    private static AnimatorStateMachine EnsureBaseLayerStateMachine(AnimatorController controller, out bool created)
    {
        created = false;
        var layers = controller.layers;
        if (layers == null || layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
            layers = controller.layers;
            created = true;
        }

        if (layers[0].stateMachine == null)
        {
            var stateMachine = new AnimatorStateMachine
            {
                name = string.IsNullOrEmpty(layers[0].name) ? "Base Layer" : layers[0].name,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);
            layers[0].stateMachine = stateMachine;
            controller.layers = layers;
            created = true;
        }

        return controller.layers[0].stateMachine;
    }

    private static bool EnsureSpeedParameter(AnimatorController controller)
    {
        foreach (var param in controller.parameters)
        {
            if (param.name == "Speed" && param.type == AnimatorControllerParameterType.Float)
            {
                return false;
            }
        }

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        return true;
    }

    private static bool EnsureWalkState(AnimatorController controller, AnimationClip clip)
    {
        var stateMachine = EnsureBaseLayerStateMachine(controller, out var layerCreated);
        var changedAtStart = layerCreated;

        AnimatorState walkState = null;
        foreach (var child in stateMachine.states)
        {
            if (child.state != null && child.state.name == "Walk")
            {
                walkState = child.state;
                break;
            }
        }

        var changed = changedAtStart;
        if (walkState == null)
        {
            walkState = stateMachine.AddState("Walk");
            changed = true;
        }

        if (walkState.motion != clip)
        {
            walkState.motion = clip;
            changed = true;
        }

        if (walkState.speedParameter != "Speed")
        {
            walkState.speedParameter = "Speed";
            changed = true;
        }

        if (!walkState.speedParameterActive)
        {
            walkState.speedParameterActive = true;
            changed = true;
        }

        if (stateMachine.defaultState != walkState)
        {
            stateMachine.defaultState = walkState;
            changed = true;
        }

        return changed;
    }

    public static void ConfigureEnemyLight(GameObject enemyRoot)
    {
        var lightTransform = enemyRoot.transform.Find("EnemyGlow");
        GameObject lightObject;
        if (lightTransform != null)
        {
            lightObject = lightTransform.gameObject;
        }
        else
        {
            lightObject = new GameObject("EnemyGlow");
            lightObject.transform.SetParent(enemyRoot.transform, false);
        }

        lightObject.transform.localPosition = new Vector3(0f, 1.7f, 0.35f);
        lightObject.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

        var light = lightObject.GetComponent<Light>();
        if (light == null)
        {
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Spot;
        light.range = 9f;
        light.spotAngle = 70f;
        light.innerSpotAngle = 35f;
        light.intensity = 1.4f;
        light.color = new Color(1f, 0.95f, 0.88f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.6f;
        light.renderMode = LightRenderMode.Auto;

        EnsureBodyGlow(enemyRoot);
    }

    private static void EnsureBodyGlow(GameObject enemyRoot)
    {
        var bodyGlowTransform = enemyRoot.transform.Find("EnemyBodyGlow");
        GameObject bodyGlow;
        if (bodyGlowTransform != null)
        {
            bodyGlow = bodyGlowTransform.gameObject;
        }
        else
        {
            bodyGlow = new GameObject("EnemyBodyGlow");
            bodyGlow.transform.SetParent(enemyRoot.transform, false);
        }

        bodyGlow.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        bodyGlow.transform.localRotation = Quaternion.identity;

        var pointLight = bodyGlow.GetComponent<Light>();
        if (pointLight == null)
        {
            pointLight = bodyGlow.AddComponent<Light>();
        }

        pointLight.type = LightType.Point;
        pointLight.range = 3.5f;
        pointLight.intensity = 0.45f;
        pointLight.color = new Color(1f, 0.92f, 0.82f);
        pointLight.shadows = LightShadows.None;
        pointLight.renderMode = LightRenderMode.Auto;
    }

    public static void ApplyEnemy01Material(GameObject modelInstance)
    {
        ApplyEnemyMaterial(modelInstance, EnemyKindCatalog.GetAssets(DefaultEnemyKind));
    }

    public static void ApplyEnemyMaterial(GameObject modelInstance, EnemyKindAssets assets)
    {
        if (modelInstance == null)
        {
            return;
        }

        var material = GetOrCreateEnemyMaterial(assets);
        if (material == null)
        {
            Debug.LogWarning($"[EnemyBuilder] No se pudo crear el material en {assets.MaterialPath}");
            return;
        }

        var renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[EnemyBuilder] Aplicando {material.name} a {renderers.Length} renderer(s) en {modelInstance.name}");

        foreach (var renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var slotCount = Mathf.Max(1, renderer.sharedMaterials?.Length ?? 1);
            var replacement = new Material[slotCount];
            for (var i = 0; i < slotCount; i++)
            {
                replacement[i] = material;
            }

            renderer.sharedMaterials = replacement;
        }
    }

    private static Material GetOrCreateEnemyMaterial(EnemyKindAssets assets)
    {
        EnsureAnimationFolderExists();
        var existing = AssetDatabase.LoadAssetAtPath<Material>(assets.MaterialPath);
        var createdNow = false;
        if (existing == null)
        {
            var shader = Shader.Find(UrpLitShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[EnemyBuilder] Shader '{UrpLitShaderName}' no encontrado en el proyecto.");
                return null;
            }

            existing = new Material(shader) { name = Path.GetFileNameWithoutExtension(assets.MaterialPath) };
            AssetDatabase.CreateAsset(existing, assets.MaterialPath);
            createdNow = true;
        }

        // Always verify texture bindings: a material may exist from a previous run that
        // failed before assigning maps. Reconfigure only if the expected albedo is missing
        // or has drifted from the asset on disk.
        if (createdNow || NeedsTextureReconfiguration(existing, assets))
        {
            ConfigureEnemyMaterialMaps(existing, assets);
            EditorUtility.SetDirty(existing);
        }

        return existing;
    }

    // Migracion: materiales viejos quedaron pintados con un tinte naranja plano via
    // _EmissionColor. Detectamos ese estado para forzar la reconfiguracion y limpiar
    // el tinte sin tener que borrar los .mat a mano.
    private static readonly Color LegacyOrangeEmissionColor = new(0.45f, 0.18f, 0.05f);

    private static bool NeedsTextureReconfiguration(Material material, EnemyKindAssets assets)
    {
        var expectedAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.AlbedoPath);
        if (expectedAlbedo == null)
        {
            return false;
        }

        if (material.GetTexture("_BaseMap") != expectedAlbedo)
        {
            return true;
        }

        var expectedRoughness = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.RoughnessPath);
        if (expectedRoughness != null && material.GetTexture("_SpecGlossMap") != expectedRoughness)
        {
            return true;
        }

        if (HasLegacyOrangeEmission(material))
        {
            return true;
        }

        return false;
    }

    private static bool HasLegacyOrangeEmission(Material material)
    {
        if (!material.HasProperty("_EmissionColor"))
        {
            return false;
        }

        var current = material.GetColor("_EmissionColor");
        const float tolerance = 0.02f;
        return Mathf.Abs(current.r - LegacyOrangeEmissionColor.r) < tolerance
            && Mathf.Abs(current.g - LegacyOrangeEmissionColor.g) < tolerance
            && Mathf.Abs(current.b - LegacyOrangeEmissionColor.b) < tolerance;
    }

    private static void ConfigureEnemyMaterialMaps(Material material, EnemyKindAssets assets)
    {
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.AlbedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.NormalPath);
        var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.MetallicPath);
        var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.RoughnessPath);

        EnsureNormalMapTextureType(assets.NormalPath);

        if (albedo != null)
        {
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_MainTex", albedo);
        }

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
        }
        else
        {
            material.DisableKeyword("_NORMALMAP");
        }

        if (metallic != null)
        {
            material.SetTexture("_MetallicGlossMap", metallic);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", 1f);
        }
        else
        {
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", 0f);
        }

        // Roughness se aplica al spec gloss slot. URP Lit lo lee cuando el
        // material esta en flujo Specular o cuando se usa como mascara extra.
        // Dejarlo asignado preserva la firma PBR generada por Meshy y permite
        // que cada enemigo se vea distinto en luz baja.
        if (roughness != null)
        {
            material.SetTexture("_SpecGlossMap", roughness);
        }

        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Smoothness", 0.35f);
        material.SetFloat("_SmoothnessTextureChannel", 0f);
        material.SetFloat("_WorkflowMode", 1f);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_ReceiveShadows", 1f);
        material.SetFloat("_EnvironmentReflections", 1f);

        // Sin tinte de emision plano: lavaba los colores reales de la textura.
        // La lectura en oscuridad se resuelve con las luces (EnemyGlow / EnemyBodyGlow)
        // que ya intensifican durante alerta y chase.
        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
    }

    private static void EnsureNormalMapTextureType(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        if (importer.textureType == TextureImporterType.NormalMap)
        {
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    public static void NormalizeModelHeight(Transform modelRoot, float targetHeight)
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

    private static void AlignModelFeetToParent(Transform modelRoot)
    {
        if (modelRoot == null || modelRoot.parent == null)
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

        var parentY = modelRoot.parent.position.y;
        var delta = parentY - bounds.min.y;
        modelRoot.localPosition += new Vector3(0f, delta, 0f);
    }

    public static void AlignBaseToY(GameObject root, float targetY)
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

    public static void ConfigureNavMeshAgent(GameObject enemyObject)
    {
        var agent = enemyObject.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = enemyObject.AddComponent<NavMeshAgent>();
        }

        agent.radius = NavMeshAgentRadius;
        agent.height = NavMeshAgentHeight;
        agent.baseOffset = NavMeshAgentBaseOffset;
        agent.angularSpeed = 240f;
        agent.acceleration = 24f;
        agent.stoppingDistance = 1.35f;
        agent.autoBraking = true;
        agent.autoTraverseOffMeshLink = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    }

    private static void ConfigureBodyCollider(GameObject enemyRoot)
    {
        var collider = enemyRoot.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = enemyRoot.AddComponent<CapsuleCollider>();
        }

        collider.isTrigger = false;
        collider.center = new Vector3(0f, NavMeshAgentHeight * 0.5f, 0f);
        collider.height = NavMeshAgentHeight;
        collider.radius = NavMeshAgentRadius;
        collider.direction = 1;
    }

    public static EnemyLightSeeker ConfigureEnemyLightSeeker(GameObject enemyObject)
    {
        var enemy = enemyObject.GetComponent<EnemyLightSeeker>();
        if (enemy == null)
        {
            enemy = enemyObject.AddComponent<EnemyLightSeeker>();
        }

        var renderer = enemyObject.GetComponentInChildren<Renderer>();
        enemy.ConfigureRenderer(renderer);

        var spotGlow = FindLightByChildName(enemyObject.transform, "EnemyGlow");
        if (spotGlow != null)
        {
            enemy.ConfigureGlow(spotGlow);
        }

        var bodyGlow = FindLightByChildName(enemyObject.transform, "EnemyBodyGlow");
        if (bodyGlow != null)
        {
            enemy.ConfigureBodyGlow(bodyGlow);
        }

        return enemy;
    }

    private static Light FindLightByChildName(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child != null ? child.GetComponent<Light>() : null;
    }
}
