using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LightChasePrototype
{
    public static class PlayerAvatarSelection
    {
        private const string SelectedAvatarKey = "LightChasePrototype.SelectedAvatar";

        public const string ArmatureAvatarId = "armature";
        public const string CapsuleAvatarId = "capsule";

        public static readonly AvatarOption[] Options =
        {
            new(
                ArmatureAvatarId,
                "Humano",
                "Silueta completa para una lectura mas narrativa del riesgo.",
                "PlayerAvatars/PlayerArmature"),
            new(
                CapsuleAvatarId,
                "Andres",
                "Avatar humano (Andres) con lectura mas personal del riesgo.",
                "PlayerAvatars/PlayerAndres")
        };

        /// <summary>
        /// Returns the combined array of hardcoded Options plus any additional entries registered
        /// in the PlayerAvatarCatalog that are not already present (matched by Id/avatarId).
        /// </summary>
        public static AvatarOption[] GetAllOptions()
        {
            var catalog = PlayerAvatarCatalog.Load();
            if (catalog == null || catalog.Entries.Count == 0)
            {
                return Options;
            }

            var combined = new System.Collections.Generic.List<AvatarOption>(Options);
            foreach (var entry in catalog.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                var alreadyPresent = false;
                foreach (var existing in combined)
                {
                    if (string.Equals(existing.Id, entry.avatarId, StringComparison.Ordinal))
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    combined.Add(catalog.ToAvatarOption(entry));
                }
            }

            return combined.ToArray();
        }

        public static string SelectedAvatarId
        {
            get
            {
                var storedValue = PlayerPrefs.GetString(SelectedAvatarKey, ArmatureAvatarId);
                return IsValidAvatarId(storedValue) ? storedValue : ArmatureAvatarId;
            }
        }

        public static AvatarOption SelectedAvatar => GetAvatar(SelectedAvatarId);

        public static void SelectAvatar(string avatarId)
        {
            var resolvedAvatarId = IsValidAvatarId(avatarId) ? avatarId : ArmatureAvatarId;
            PlayerPrefs.SetString(SelectedAvatarKey, resolvedAvatarId);
            PlayerPrefs.Save();
        }

        public static AvatarOption GetAvatar(string avatarId)
        {
            foreach (var option in GetAllOptions())
            {
                if (string.Equals(option.Id, avatarId, StringComparison.Ordinal))
                {
                    return option;
                }
            }

            return Options[0];
        }

        public static GameObject LoadSelectedPrefab()
        {
            return LoadPrefab(SelectedAvatarId);
        }

        public static GameObject LoadPrefab(string avatarId)
        {
            var option = GetAvatar(avatarId);
            var prefab = Resources.Load<GameObject>(option.ResourcePath);
            if (prefab != null)
            {
                return prefab;
            }

            Debug.LogWarning($"PlayerAvatarSelection: No se pudo cargar prefab desde Resources '{option.ResourcePath}' para avatarId='{avatarId}'.");

            // Fail-safe: if Andres prefab wasn't built/imported yet, fall back to the capsule so gameplay still works.
            if (string.Equals(avatarId, CapsuleAvatarId, StringComparison.Ordinal))
            {
                var fallback = Resources.Load<GameObject>("PlayerAvatars/PlayerCapsule");
                if (fallback != null)
                {
                    Debug.LogWarning("PlayerAvatarSelection: usando fallback PlayerCapsule porque PlayerAndres no está disponible.");
                    return fallback;
                }
            }

            return null;
        }

        // Layer 31 is reserved for isolated avatar preview rendering to avoid
        // capturing scene objects when using Camera.Render() on a temporary camera.
        private const int PreviewLayer = 31;

        public static Sprite BuildAvatarPreviewSprite(string avatarId, int width = 512, int height = 512)
        {
            var prefab = LoadPrefab(avatarId);
            if (prefab == null)
            {
                return null;
            }

            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return BuildFallbackAvatarPreviewSprite(avatarId);
            }

            var previewRoot = new GameObject($"AvatarPreview_{avatarId}");
            previewRoot.hideFlags = HideFlags.HideAndDontSave;

            var instance = UnityEngine.Object.Instantiate(prefab, previewRoot.transform);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);
            instance.transform.localScale = Vector3.one;

            SetLayerRecursive(previewRoot, PreviewLayer);

            var bounds = CalculateBounds(instance);

            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.transform.SetParent(previewRoot.transform, false);
            var previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.02f, 0.04f, 0.09f, 0f);
            previewCamera.fieldOfView = 24f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 100f;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.cullingMask = 1 << PreviewLayer;

            var fillLightObject = new GameObject("PreviewFillLight");
            fillLightObject.hideFlags = HideFlags.HideAndDontSave;
            fillLightObject.transform.SetParent(previewRoot.transform, false);
            fillLightObject.transform.rotation = Quaternion.Euler(36f, -28f, 0f);
            var fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 1.1f;
            fillLight.color = new Color(0.85f, 0.92f, 1f);

            var rimLightObject = new GameObject("PreviewRimLight");
            rimLightObject.hideFlags = HideFlags.HideAndDontSave;
            rimLightObject.transform.SetParent(previewRoot.transform, false);
            rimLightObject.transform.rotation = Quaternion.Euler(18f, 135f, 0f);
            var rimLight = rimLightObject.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.intensity = 0.7f;
            rimLight.color = new Color(1f, 0.92f, 0.78f);

            PositionPreviewCamera(previewCamera.transform, bounds);

            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            if (!renderTexture.IsCreated() && !renderTexture.Create())
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(previewRoot);
                return BuildFallbackAvatarPreviewSprite(avatarId);
            }

            var previousActive = RenderTexture.active;
            previewCamera.targetTexture = renderTexture;
            previewCamera.Render();

            RenderTexture.active = renderTexture;
            var previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previewTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            previewTexture.Apply();

            previewCamera.targetTexture = null;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            UnityEngine.Object.DestroyImmediate(previewRoot);

            return Sprite.Create(previewTexture, new Rect(0f, 0f, previewTexture.width, previewTexture.height), new Vector2(0.5f, 0.5f));
        }

        public static bool IsValidAvatarId(string avatarId)
        {
            foreach (var option in GetAllOptions())
            {
                if (string.Equals(option.Id, avatarId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly struct AvatarOption
        {
            public AvatarOption(string id, string displayName, string description, string resourcePath)
            {
                Id = id;
                DisplayName = displayName;
                Description = description;
                ResourcePath = resourcePath;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public string ResourcePath { get; }
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void PositionPreviewCamera(Transform cameraTransform, Bounds bounds)
        {
            var center = bounds.center;
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            var distance = Mathf.Max(2.2f, size * 2f);
            cameraTransform.position = center + new Vector3(size * 0.35f, size * 0.1f, -distance);
            cameraTransform.LookAt(center + Vector3.up * (bounds.size.y * 0.08f));
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        private static Sprite BuildFallbackAvatarPreviewSprite(string avatarId)
        {
            var previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var fillColor = avatarId == CapsuleAvatarId
                ? new Color(0.4f, 0.72f, 1f, 1f)
                : new Color(1f, 0.84f, 0.45f, 1f);
            var pixels = new[] { fillColor, fillColor, fillColor, fillColor };
            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
            return Sprite.Create(previewTexture, new Rect(0f, 0f, previewTexture.width, previewTexture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
