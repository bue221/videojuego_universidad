using System.IO;
using UnityEditor;
using UnityEngine;

namespace LightChasePrototype.EditorTools
{
    public static class AndresPlayerPrefabBuilder
    {
        private const string PlayerAvatarsResourcesFolder = "Assets/Project/LightChasePrototype/Resources/PlayerAvatars";
        private const string OutputPrefabPath = PlayerAvatarsResourcesFolder + "/PlayerAndres.prefab";

        private const string BasePlayerPrefabPath = PlayerAvatarsResourcesFolder + "/PlayerCapsule.prefab";
        private const string AndresFbxPath = PlayerAvatarsResourcesFolder + "/Avatar andres/avatar.fbx";

        private const string StarterAssetsControllerGuid = "40db3173a05ae3242b1c182a09b0a183"; // StarterAssetsThirdPerson.controller
        private const float VisualScale = 95f;

        [MenuItem("Tools/Prototype/Avatar/Build PlayerAndres Prefab")]
        public static void Build()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePlayerPrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"No pude cargar el prefab base '{BasePlayerPrefabPath}'.");
                return;
            }

            var fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AndresFbxPath);
            if (fbxRoot == null)
            {
                Debug.LogError($"No pude cargar el FBX de Andres '{AndresFbxPath}'. Revisa que exista y que Unity lo importe.");
                return;
            }

            // Create temporary instance.
            var root = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (root == null)
            {
                Debug.LogError("No pude instanciar el prefab base.");
                return;
            }

            try
            {
                root.name = "PlayerAndres";

                // Make the prefab self-contained (no variant link to PlayerCapsule). This avoids
                // missing/partial overrides and keeps runtime behaviour predictable.
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);

                // Disable the Capsule visual if still present.
                var capsule = root.transform.Find("Capsule");
                if (capsule != null)
                {
                    capsule.gameObject.SetActive(false);
                }

                // Remove runtime visual injector if present (we want a static prefab).
                var injector = root.GetComponent<global::LightChasePrototype.PlayerAvatarVisualInjector>();
                if (injector != null)
                {
                    Object.DestroyImmediate(injector, true);
                }

                // Ensure Animator exists and is configured.
                var animator = root.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = root.AddComponent<Animator>();
                }

                // Assign Starter Assets controller if available in project.
                var controllerPath = AssetDatabase.GUIDToAssetPath(StarterAssetsControllerGuid);
                var controller = !string.IsNullOrWhiteSpace(controllerPath)
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath)
                    : null;
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }

                // Assign humanoid Avatar extracted from the FBX.
                animator.avatar = FindValidAvatarInModel(AndresFbxPath);

                // Attach FBX visual under the player root.
                var visual = PrefabUtility.InstantiatePrefab(fbxRoot) as GameObject;
                if (visual == null)
                {
                    visual = Object.Instantiate(fbxRoot);
                }

                visual.name = "AvatarVisual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * VisualScale;

                // The root owns the animator (Starter Assets reads it from the same GameObject).
                foreach (var childAnimator in visual.GetComponentsInChildren<Animator>(true))
                {
                    Object.DestroyImmediate(childAnimator, true);
                }

                // Save prefab.
                EnsureFolderExists(PlayerAvatarsResourcesFolder);
                PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);

                Debug.Log($"PlayerAndres prefab generado en '{OutputPrefabPath}'.");
            }
            finally
            {
                // If still not saved/connected, clean up.
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
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

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var name = Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
