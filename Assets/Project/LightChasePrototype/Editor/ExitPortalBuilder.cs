using System.IO;
using LightChasePrototype;
using UnityEditor;
using UnityEngine;

// Builder compartido para el ExitPortal visual de todos los niveles.
//
// Decision de diseño:
// - Todos los niveles usan el mismo asset Meshy (Assets/MeshyImports/Portal) como
//   marco visual del portal, para que la lectura del objetivo sea identica entre
//   niveles aunque cambie el bioma.
// - El portal SIEMPRE debe ser atravesable: el jugador entra al trigger y dispara
//   ExitPortal.OnTriggerEnter. Por eso NO usamos MeshCollider (el FBX llega con
//   isReadable=0 y addColliders=0). En su lugar, envolvemos al modelo con un
//   BoxCollider isTrigger calculado a partir de los Renderers del FBX.
// - El componente ExitPortal sigue cambiando color locked/unlocked sobre el
//   renderer principal del modelo, asi que la lectura de "puedo salir" se
//   mantiene.
public static class ExitPortalBuilder
{
    private const string PortalFbxPath =
        "Assets/MeshyImports/Portal/Meshy_AI_Azure_Spiral_Portal_0518140858_texture.fbx";

    private const string PortalAlbedoPath =
        "Assets/MeshyImports/Portal/Meshy_AI_Azure_Spiral_Portal_0518140858_texture.png";

    private const string PortalEmissionPath =
        "Assets/MeshyImports/Portal/Meshy_AI_Azure_Spiral_Portal_0518140858_texture_emission.png";

    private const string PortalNormalPath =
        "Assets/MeshyImports/Portal/Meshy_AI_Azure_Spiral_Portal_0518140858_texture_normal.png";

    private const string PortalMaterialPath =
        "Assets/Project/LightChasePrototype/Art/Materials/MeshyExitPortal.mat";

    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    // Altura visual objetivo del marco del portal (en metros). Mantiene una
    // lectura silueta consistente entre niveles.
    private const float PortalTargetHeight = 3.2f;

    // El FBX Meshy de la espiral viene con eje Z como "arriba" (export tipo
    // Blender Z-up). Si lo dejamos con rotacion identidad, el portal queda
    // acostado sobre el piso. Esta correccion lo lleva a Y-up para que la
    // boca del portal mire al jugador y el marco se sostenga vertical.
    private static readonly Vector3 PortalModelOrientationFix = new(-90f, 0f, 0f);

    // Padding lateral y de profundidad del trigger que envuelve al portal,
    // para asegurar que el jugador siempre entre aunque el modelo Meshy sea
    // delgado en algun eje.
    private const float TriggerExpansion = 0.6f;

    // Ubicacion canonica del portal cuando no se pasa una explicita. Cada
    // builder de nivel sigue decidiendo donde poner el portal, pero esta
    // posicion es el fallback.
    public static readonly Vector3 DefaultPosition = new(18f, 0f, 20f);

    public static GameObject BuildPortal(Vector3 worldPosition)
    {
        return BuildPortal(worldPosition, 0f);
    }

    public static GameObject BuildPortal(Vector3 worldPosition, float yawDegrees)
    {
        var root = new GameObject("ExitPortal");
        root.transform.position = worldPosition;
        root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        var model = InstantiatePortalModel(root.transform);
        if (model == null)
        {
            Debug.LogError(
                $"[ExitPortalBuilder] No se pudo instanciar el portal Meshy desde {PortalFbxPath}. " +
                "Verifica que el asset existe en MeshyImports/Portal.");
            Object.DestroyImmediate(root);
            return null;
        }

        NormalizeModelHeight(model.transform, PortalTargetHeight);
        AlignModelBaseToParent(model.transform);

        var portalRenderer = ApplyPortalMaterial(model);
        ConfigureTraversableTrigger(root, model);

        var exit = root.AddComponent<ExitPortal>();
        exit.ConfigureRenderer(portalRenderer);

        return root;
    }

    private static GameObject InstantiatePortalModel(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalFbxPath);
        if (prefab == null)
        {
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
        }
        else
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        instance.name = "PortalModel";
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(PortalModelOrientationFix);
        return instance;
    }

    private static Renderer ApplyPortalMaterial(GameObject modelInstance)
    {
        var material = GetOrCreatePortalMaterial();
        var renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        Renderer mainRenderer = null;

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
            if (mainRenderer == null)
            {
                mainRenderer = renderer;
            }
        }

        return mainRenderer;
    }

    private static Material GetOrCreatePortalMaterial()
    {
        EnsureMaterialFolderExists();

        var existing = AssetDatabase.LoadAssetAtPath<Material>(PortalMaterialPath);
        var createdNow = false;
        if (existing == null)
        {
            var shader = Shader.Find(UrpLitShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[ExitPortalBuilder] Shader '{UrpLitShaderName}' no disponible.");
                return new Material(Shader.Find("Standard"));
            }

            existing = new Material(shader) { name = Path.GetFileNameWithoutExtension(PortalMaterialPath) };
            AssetDatabase.CreateAsset(existing, PortalMaterialPath);
            createdNow = true;
        }

        if (createdNow || NeedsTextureReconfiguration(existing))
        {
            ConfigurePortalMaterialMaps(existing);
            EditorUtility.SetDirty(existing);
        }

        return existing;
    }

    private static bool NeedsTextureReconfiguration(Material material)
    {
        var expectedAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(PortalAlbedoPath);
        if (expectedAlbedo != null && material.GetTexture("_BaseMap") != expectedAlbedo)
        {
            return true;
        }

        if (!material.IsKeywordEnabled("_EMISSION"))
        {
            return true;
        }

        return false;
    }

    private static void ConfigurePortalMaterialMaps(Material material)
    {
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(PortalAlbedoPath);
        var emission = AssetDatabase.LoadAssetAtPath<Texture2D>(PortalEmissionPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(PortalNormalPath);

        EnsureNormalMapTextureType(PortalNormalPath);

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

        if (emission != null)
        {
            material.SetTexture("_EmissionMap", emission);
        }

        // ExitPortal.RefreshVisual hace material.color = lockedColor/unlockedColor.
        // En URP Lit eso pisa _BaseColor, asi que arrancamos en blanco para que
        // el tinte de estado se lea correctamente sobre el albedo del portal.
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Smoothness", 0.85f);
        material.SetFloat("_Metallic", 0.2f);
        material.SetFloat("_WorkflowMode", 1f);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Cull", 2f);

        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.SetColor("_EmissionColor", new Color(0.4f, 0.85f, 1.5f));
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

    private static void EnsureMaterialFolderExists()
    {
        const string root = "Assets/Project/LightChasePrototype";
        const string artFolder = root + "/Art";
        const string materialsFolder = artFolder + "/Materials";

        if (!AssetDatabase.IsValidFolder(artFolder))
        {
            AssetDatabase.CreateFolder(root, "Art");
        }

        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            AssetDatabase.CreateFolder(artFolder, "Materials");
        }
    }

    private static void NormalizeModelHeight(Transform modelRoot, float targetHeight)
    {
        if (modelRoot == null)
        {
            return;
        }

        if (!TryEncapsulateRenderers(modelRoot, out var bounds))
        {
            return;
        }

        var height = bounds.size.y;
        if (height <= 0.01f)
        {
            return;
        }

        var factor = targetHeight / height;
        modelRoot.localScale *= factor;
    }

    private static void AlignModelBaseToParent(Transform modelRoot)
    {
        if (modelRoot == null || modelRoot.parent == null)
        {
            return;
        }

        if (!TryEncapsulateRenderers(modelRoot, out var bounds))
        {
            return;
        }

        var parentY = modelRoot.parent.position.y;
        var delta = parentY - bounds.min.y;
        modelRoot.localPosition += new Vector3(0f, delta, 0f);
    }

    private static void ConfigureTraversableTrigger(GameObject root, GameObject modelInstance)
    {
        if (!TryEncapsulateRenderers(modelInstance.transform, out var worldBounds))
        {
            // Fallback razonable cuando el modelo aun no tiene bounds calculados.
            var fallback = root.AddComponent<BoxCollider>();
            fallback.isTrigger = true;
            fallback.center = new Vector3(0f, 1.5f, 0f);
            fallback.size = new Vector3(2.5f, 3f, 2.5f);
            return;
        }

        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;

        var size = worldBounds.size + Vector3.one * TriggerExpansion;
        // Profundidad minima para que el jugador pueda cruzar el portal aunque
        // el FBX sea casi plano en algun eje.
        size.x = Mathf.Max(size.x, 2.2f);
        size.y = Mathf.Max(size.y, 2.6f);
        size.z = Mathf.Max(size.z, 1.6f);
        box.size = size;

        var centerWorld = worldBounds.center;
        box.center = root.transform.InverseTransformPoint(centerWorld);
    }

    private static bool TryEncapsulateRenderers(Transform modelRoot, out Bounds bounds)
    {
        bounds = default;
        var renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }
}
