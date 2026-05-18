using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class EnemyBuilder
{
    public const string Enemy01ModelPath = "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_Animation_Walking_withSkin.fbx";
    public const string Enemy01MaterialPath = "Assets/MeshyImports/Enemigo_01/Material_1.mat";
    private const string AnimationFolder = "Assets/Project/LightChasePrototype/Animation";
    private const string WalkClipPath = "Assets/Project/LightChasePrototype/Animation/Enemigo01_Walk.anim";
    private const string ControllerPath = "Assets/Project/LightChasePrototype/Animation/Enemigo01.controller";

    public static GameObject BuildEnemyRoot(string objectName, Vector3 position)
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
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "FallbackEnemy";
            fallback.transform.SetParent(root.transform, false);
            fallback.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f);
        }

        ConfigureAnimator(root);
        ConfigureEnemyLight(root);
        return root;
    }

    public static void ConfigureAnimator(GameObject enemyRoot)
    {
        EnsureAnimationFolderExists();
        var walkClip = GetOrCreateWalkClip();
        var controller = GetOrCreateAnimatorController(walkClip);
        var animator = enemyRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = enemyRoot.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
    }

    private static void EnsureAnimationFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(AnimationFolder))
        {
            var parent = Path.GetDirectoryName(AnimationFolder);
            var name = Path.GetFileName(AnimationFolder);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static AnimationClip GetOrCreateWalkClip()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
        if (existing != null)
        {
            return existing;
        }

        var sourceClip = ExtractClipFromFbx();
        if (sourceClip != null)
        {
            var clone = Object.Instantiate(sourceClip);
            clone.name = "Enemigo01_Walk";
            AssetDatabase.CreateAsset(clone, WalkClipPath);
            AssetDatabase.SaveAssets();
            return clone;
        }

        return CreateFallbackClip();
    }

    private static AnimationClip ExtractClipFromFbx()
    {
        var importer = AssetImporter.GetAtPath(Enemy01ModelPath) as ModelImporter;
        if (importer == null)
        {
            return null;
        }

        if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
        {
            var so = new SerializedObject(importer);
            var clipsProp = so.FindProperty("m_ClipAnimations");
            clipsProp.arraySize = 1;
            var clipProp = clipsProp.GetArrayElementAtIndex(0);
            clipProp.FindPropertyRelative("name").stringValue = "Walk";
            clipProp.FindPropertyRelative("loopTime").boolValue = true;
            clipProp.FindPropertyRelative("loop").boolValue = true;
            clipProp.FindPropertyRelative("takeName").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(Enemy01ModelPath);
        foreach (var sub in subAssets)
        {
            if (sub is AnimationClip clip)
            {
                return clip;
            }
        }

        return null;
    }

    private static AnimationClip CreateFallbackClip()
    {
        var clip = new AnimationClip();
        clip.name = "Enemigo01_Walk";
        clip.frameRate = 30;
        clip.wrapMode = WrapMode.Loop;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.1f),
            new Keyframe(1f, 0f));
        clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
        AssetDatabase.CreateAsset(clip, WalkClipPath);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static RuntimeAnimatorController GetOrCreateAnimatorController(AnimationClip clip)
    {
        var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (existing != null)
        {
            return existing;
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var rootStateMachine = controller.layers[0].stateMachine;
        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = clip;
        walkState.speedParameter = "Speed";
        walkState.speedParameterActive = true;
        rootStateMachine.defaultState = walkState;

        AssetDatabase.SaveAssets();
        return controller;
    }

    public static void ConfigureEnemyLight(GameObject enemyRoot)
    {
        var lightTransform = enemyRoot.transform.Find("EnemyGlow");
        if (lightTransform != null)
        {
            return;
        }

        var lightObject = new GameObject("EnemyGlow");
        lightObject.transform.SetParent(enemyRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.4f, -0.6f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 5f;
        light.intensity = 0.7f;
        light.color = new Color(0.95f, 0.85f, 0.7f);
        light.shadows = LightShadows.None;
    }

    public static void ApplyEnemy01Material(GameObject modelInstance)
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
        var agent = enemyObject.AddComponent<NavMeshAgent>();
        agent.angularSpeed = 240f;
        agent.acceleration = 24f;
        agent.stoppingDistance = 1.35f;
    }

    public static EnemyLightSeeker ConfigureEnemyLightSeeker(GameObject enemyObject)
    {
        var enemy = enemyObject.AddComponent<EnemyLightSeeker>();
        var renderer = enemyObject.GetComponentInChildren<Renderer>();
        enemy.ConfigureRenderer(renderer);
        var glowLight = enemyObject.GetComponentInChildren<Light>();
        if (glowLight != null)
        {
            enemy.ConfigureGlow(glowLight);
        }

        return enemy;
    }
}
