using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LightChasePrototype.EditorTools
{
    /// <summary>
    /// EditorWindow that automates creation of a new playable avatar from an FBX file:
    /// builds materials, prefab, PlayerAvatarEntry and registers it in the PlayerAvatarCatalog.
    /// </summary>
    public class AddAvatarFromFbxTool : EditorWindow
    {
        // ── Paths ──────────────────────────────────────────────────────────────
        private const string PlayerAvatarsResourcesFolder = "Assets/Project/LightChasePrototype/Resources/PlayerAvatars";
        private const string CatalogResourcesFolder = "Assets/Project/LightChasePrototype/Resources";
        private const string CatalogAssetPath = CatalogResourcesFolder + "/PlayerAvatarCatalog.asset";
        private const string StarterAssetsSfxFolder = "Assets/ThirdParty/StarterAssets/ThirdPersonController/Character/Sfx";
        private const string StarterAssetsControllerGuid = "40db3173a05ae3242b1c182a09b0a183";
        private const float VisualScale = 95f;

        // ── UI State ───────────────────────────────────────────────────────────
        private string _fbxPath = "";
        private string _avatarName = "";
        private string _avatarId = "";

        private bool _idManuallyEdited;
        private Vector2 _scrollPos;

        // ── Menu entry ─────────────────────────────────────────────────────────
        [MenuItem("Tools/LightChase/Add Avatar from FBX")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddAvatarFromFbxTool>("Add Avatar from FBX");
            window.minSize = new Vector2(440, 280);
        }

        // ── GUI ────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add Avatar from FBX", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // FBX path row
            EditorGUILayout.BeginHorizontal();
            _fbxPath = EditorGUILayout.TextField("FBX Path (Assets/...)", _fbxPath);
            if (GUILayout.Button("Browse", GUILayout.Width(64)))
            {
                var absolute = EditorUtility.OpenFilePanel("Select FBX", Application.dataPath, "fbx");
                if (!string.IsNullOrEmpty(absolute))
                {
                    // Convert absolute path to Assets-relative.
                    if (absolute.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _fbxPath = "Assets" + absolute.Substring(Application.dataPath.Length).Replace('\\', '/');
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Avatar Name
            EditorGUI.BeginChangeCheck();
            _avatarName = EditorGUILayout.TextField("Avatar Name", _avatarName);
            if (EditorGUI.EndChangeCheck() && !_idManuallyEdited)
            {
                _avatarId = _avatarName.ToLowerInvariant().Trim().Replace(' ', '_');
            }

            // Avatar ID (auto-generated but editable)
            EditorGUI.BeginChangeCheck();
            _avatarId = EditorGUILayout.TextField("Avatar ID", _avatarId);
            if (EditorGUI.EndChangeCheck())
            {
                _idManuallyEdited = true;
            }

            EditorGUILayout.Space(12);

            GUI.enabled = !string.IsNullOrWhiteSpace(_fbxPath)
                          && !string.IsNullOrWhiteSpace(_avatarName)
                          && !string.IsNullOrWhiteSpace(_avatarId);

            if (GUILayout.Button("Build Avatar", GUILayout.Height(32)))
            {
                TryBuildAvatar();
            }

            GUI.enabled = true;
            EditorGUILayout.EndScrollView();
        }

        // ── Pipeline ───────────────────────────────────────────────────────────
        private void TryBuildAvatar()
        {
            try
            {
                BuildAvatar(_fbxPath, _avatarName.Trim(), _avatarId.Trim().ToLowerInvariant());
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Add Avatar – Error", ex.Message, "OK");
                Debug.LogException(ex);
            }
        }

        public static void BuildAvatar(string fbxPath, string avatarName, string avatarId)
        {
            // ── 1. Validate FBX ───────────────────────────────────────────────
            if (!File.Exists(fbxPath) && !File.Exists(Path.GetFullPath(fbxPath)))
            {
                var fullCheck = Path.Combine(Directory.GetCurrentDirectory(), fbxPath);
                if (!File.Exists(fullCheck))
                {
                    throw new Exception($"FBX not found at '{fbxPath}'. Make sure the path is Assets-relative and the file is imported.");
                }
            }

            var fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxRoot == null)
            {
                throw new Exception($"Could not load FBX at '{fbxPath}'. Make sure Unity has imported it.");
            }

            // ── 2. Ensure Generated folder ────────────────────────────────────
            var fbxDir = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/') ?? "";
            var generatedFolder = fbxDir + "/Generated";
            var generatedMaterialsFolder = generatedFolder + "/Materials";

            // Accept ExtractedTextures/, Texturas/, or Textures/ — whichever exists.
            string[] texFolderCandidates = { fbxDir + "/ExtractedTextures", fbxDir + "/Texturas", fbxDir + "/Textures" };
            var extractedTexturesFolder = Array.Find(texFolderCandidates, AssetDatabase.IsValidFolder)
                                          ?? texFolderCandidates[0];

            EnsureFolder(generatedFolder);
            EnsureFolder(generatedMaterialsFolder);

            // ── 3. Create/update URP Lit materials from textures ──────────────
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new Exception("Could not find shader 'Universal Render Pipeline/Lit'. Is the project using URP?");
            }

            var meshNames = CollectMeshNames(fbxRoot);
            var texturePaths = CollectTexturePaths(extractedTexturesFolder);
            var materialRemap = BuildMaterialsFromTextures(meshNames, texturePaths, generatedMaterialsFolder, shader);

            // ── 4. Reimport FBX with materialImportMode = None + remap ────────
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                throw new Exception($"Could not get ModelImporter for '{fbxPath}'.");
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            foreach (var kvp in materialRemap)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kvp.Key), kvp.Value);
            }
            importer.SaveAndReimport();

            // Reload the FBX root after reimport.
            fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            // ── 5. Build prefab ───────────────────────────────────────────────
            var basePrefabPath = PlayerAvatarsResourcesFolder + "/PlayerCapsule.prefab";
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null)
            {
                throw new Exception($"Could not load base prefab '{basePrefabPath}'.");
            }

            EnsureFolder(PlayerAvatarsResourcesFolder);
            var outputPrefabPath = $"{PlayerAvatarsResourcesFolder}/Player{avatarName}.prefab";

            var root = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (root == null)
            {
                throw new Exception("Could not instantiate base prefab.");
            }

            try
            {
                root.name = $"Player{avatarName}";
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);

                // Disable Capsule visual if present.
                var capsule = root.transform.Find("Capsule");
                if (capsule != null)
                {
                    capsule.gameObject.SetActive(false);
                }

                // Remove runtime visual injector if present.
                var injector = root.GetComponent<PlayerAvatarVisualInjector>();
                if (injector != null)
                {
                    UnityEngine.Object.DestroyImmediate(injector, true);
                }

                // Ensure Animator and assign controller + avatar.
                var animator = root.GetComponent<Animator>() ?? root.AddComponent<Animator>();
                var controllerPath = AssetDatabase.GUIDToAssetPath(StarterAssetsControllerGuid);
                if (!string.IsNullOrWhiteSpace(controllerPath))
                {
                    var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                }

                animator.avatar = FindValidAvatarInModel(fbxPath);

                // Attach FBX visual.
                var visual = PrefabUtility.InstantiatePrefab(fbxRoot) as GameObject
                             ?? UnityEngine.Object.Instantiate(fbxRoot);
                visual.name = "AvatarVisual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * VisualScale;

                // Remove child animators so only the root animator drives the rig.
                foreach (var childAnimator in visual.GetComponentsInChildren<Animator>(true))
                {
                    UnityEngine.Object.DestroyImmediate(childAnimator, true);
                }

                PrefabUtility.SaveAsPrefabAsset(root, outputPrefabPath);
                Debug.Log($"[AddAvatarFromFbxTool] Prefab saved at '{outputPrefabPath}'.");
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            // ── 6. Collect footstep clips ─────────────────────────────────────
            var footstepClips = CollectFootstepClips();
            var landingClip = CollectLandingClip();

            // ── 7. Create PlayerAvatarEntry ───────────────────────────────────
            EnsureFolder(PlayerAvatarsResourcesFolder);
            var entryAssetPath = $"{PlayerAvatarsResourcesFolder}/{avatarId}_entry.asset";
            var entry = AssetDatabase.LoadAssetAtPath<PlayerAvatarEntry>(entryAssetPath);
            if (entry == null)
            {
                entry = ScriptableObject.CreateInstance<PlayerAvatarEntry>();
                AssetDatabase.CreateAsset(entry, entryAssetPath);
            }

            entry.avatarId = avatarId;
            entry.displayName = avatarName;
            entry.description = $"Avatar {avatarName}";
            entry.resourcePath = $"PlayerAvatars/Player{avatarName}";
            entry.footstepClips = footstepClips;
            entry.landingClip = landingClip;
            EditorUtility.SetDirty(entry);

            // ── 8 & 9. Load or create PlayerAvatarCatalog and register entry ──
            EnsureFolder(CatalogResourcesFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<PlayerAvatarCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlayerAvatarCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.Register(entry);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 10. Success log ───────────────────────────────────────────────
            Debug.Log($"[AddAvatarFromFbxTool] Avatar '{avatarName}' (id='{avatarId}') built successfully.\n" +
                      $"  Prefab: {outputPrefabPath}\n" +
                      $"  Entry:  {entryAssetPath}\n" +
                      $"  Catalog: {CatalogAssetPath}");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static HashSet<string> CollectMeshNames(GameObject fbxRoot)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var renderer in fbxRoot.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        names.Add(mat.name);
                    }
                }
            }
            return names;
        }

        private static List<string> CollectTexturePaths(string extractedTexturesFolder)
        {
            var paths = new List<string>();
            if (!AssetDatabase.IsValidFolder(extractedTexturesFolder))
            {
                return paths;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { extractedTexturesFolder });
            foreach (var guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            return paths;
        }

        /// <summary>
        /// Creates one URP/Lit material per mesh name found in the FBX.
        /// Assigns BaseMap and BumpMap using per-material prefix matching first,
        /// then falls back to the first keyword match across all textures.
        /// </summary>
        private static Dictionary<string, Material> BuildMaterialsFromTextures(
            HashSet<string> meshNames,
            List<string> texturePaths,
            string materialsFolder,
            Shader shader)
        {
            var result = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

            // filename (no ext, lowercase) → asset path
            var texByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tp in texturePaths)
            {
                var fn = Path.GetFileNameWithoutExtension(tp);
                if (!texByName.ContainsKey(fn))
                {
                    texByName[fn] = tp;
                }
            }

            foreach (var matName in meshNames)
            {
                var matPath = $"{materialsFolder}/{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(shader) { name = matName };
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    mat.shader = shader;
                }

                var matKey = ExtractMaterialKey(matName);
                AssignBaseMap(mat, texByName, matKey);
                AssignNormalMap(mat, texByName, matKey);

                EditorUtility.SetDirty(mat);
                result[matName] = mat;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        // "avaturn_hair_0_material.001" → "avaturn_hair_0"
        private static string ExtractMaterialKey(string matName)
        {
            var key = matName;
            var dotIdx = key.LastIndexOf('.');
            if (dotIdx >= 0 && int.TryParse(key.Substring(dotIdx + 1), out _))
            {
                key = key.Substring(0, dotIdx);
            }

            const string suffix = "_material";
            if (key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(0, key.Length - suffix.Length);
            }

            return key.ToLowerInvariant();
        }

        private static bool IsBaseColorName(string n) =>
            n.IndexOf("base", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("albedo", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("color", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("diffuse", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsNormalMapName(string n) =>
            n.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("nrm", StringComparison.OrdinalIgnoreCase) >= 0;

        private static void AssignBaseMap(Material mat, Dictionary<string, string> texByName, string matKey)
        {
            // 1st pass: texture name starts with matKey AND is a base-color texture
            foreach (var kvp in texByName)
            {
                if (kvp.Key.StartsWith(matKey, StringComparison.OrdinalIgnoreCase) && IsBaseColorName(kvp.Key))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
                    if (tex != null)
                    {
                        mat.SetTexture("_BaseMap", tex);
                        mat.SetColor("_BaseColor", Color.white);
                        return;
                    }
                }
            }

            // Fallback: first base-color texture regardless of material name
            foreach (var kvp in texByName)
            {
                if (IsBaseColorName(kvp.Key))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
                    if (tex != null)
                    {
                        mat.SetTexture("_BaseMap", tex);
                        mat.SetColor("_BaseColor", Color.white);
                        return;
                    }
                }
            }
        }

        private static void AssignNormalMap(Material mat, Dictionary<string, string> texByName, string matKey)
        {
            // 1st pass: texture name starts with matKey AND is a normal map
            foreach (var kvp in texByName)
            {
                if (kvp.Key.StartsWith(matKey, StringComparison.OrdinalIgnoreCase) && IsNormalMapName(kvp.Key))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
                    if (tex != null)
                    {
                        mat.SetTexture("_BumpMap", tex);
                        mat.EnableKeyword("_NORMALMAP");
                        return;
                    }
                }
            }

            // Fallback: first normal map regardless of material name
            foreach (var kvp in texByName)
            {
                if (IsNormalMapName(kvp.Key))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
                    if (tex != null)
                    {
                        mat.SetTexture("_BumpMap", tex);
                        mat.EnableKeyword("_NORMALMAP");
                        return;
                    }
                }
            }
        }

        private static AudioClip[] CollectFootstepClips()
        {
            var guids = AssetDatabase.FindAssets("Player_Footstep t:AudioClip", new[] { StarterAssetsSfxFolder });
            var clips = new List<AudioClip>();
            foreach (var guid in guids.Take(10))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            // Pad to 10 slots.
            while (clips.Count < 10)
            {
                clips.Add(null);
            }

            return clips.ToArray();
        }

        private static AudioClip CollectLandingClip()
        {
            var guids = AssetDatabase.FindAssets("Player_Land t:AudioClip", new[] { StarterAssetsSfxFolder });
            if (guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static Avatar FindValidAvatarInModel(string modelPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Avatar avatar && avatar.isValid)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
