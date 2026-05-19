using UnityEngine;

namespace LightChasePrototype
{
    /// <summary>
    /// Injects a visual-only model (from Resources) under the player root, keeping the existing
    /// Starter Assets movement stack (CharacterController + ThirdPersonController + PlayerInput).
    /// </summary>
    public sealed class PlayerAvatarVisualInjector : MonoBehaviour
    {
        [Header("Resources Model")]
        [SerializeField] private string modelResourcePath = "PlayerAvatars/Avatar andres/avatar";
        [SerializeField] private string modelFallbackFolderPath = "PlayerAvatars/Avatar andres";

        [Header("Visual Instance")]
        [SerializeField] private string visualInstanceName = "AvatarVisual";
        [SerializeField] private bool disableChildNamedCapsule = true;
        [SerializeField] private Vector3 localPosition = Vector3.zero;
        [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 localScale = Vector3.one * 95f;

        private void Awake()
        {
            if (disableChildNamedCapsule)
            {
                var capsule = transform.Find("Capsule");
                if (capsule != null)
                {
                    capsule.gameObject.SetActive(false);
                }
            }

            // Avoid duplicating the visual when reloading / re-instantiating in editor.
            if (!string.IsNullOrWhiteSpace(visualInstanceName) && transform.Find(visualInstanceName) != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(modelResourcePath))
            {
                return;
            }

            var modelPrefab = Resources.Load<GameObject>(modelResourcePath);
            if (modelPrefab == null && !string.IsNullOrWhiteSpace(modelFallbackFolderPath))
            {
                // Fallback: search within Resources when paths contain spaces or unexpected casing.
                var candidates = Resources.LoadAll<GameObject>(modelFallbackFolderPath);
                foreach (var candidate in candidates)
                {
                    if (candidate != null)
                    {
                        modelPrefab = candidate;
                        break;
                    }
                }
            }

            if (modelPrefab == null)
            {
                // Last-resort diagnostic: try loading as Object to learn what Unity thinks this asset is.
                var any = Resources.Load(modelResourcePath);
                if (any != null)
                {
                    Debug.LogWarning(
                        $"PlayerAvatarVisualInjector: Resources.Load('{modelResourcePath}') returned type '{any.GetType().FullName}', not GameObject. " +
                        "Set modelResourcePath to a GameObject (FBX root) or a prefab.",
                        this);
                }

                Debug.LogWarning(
                    $"PlayerAvatarVisualInjector: Could not load model at Resources path '{modelResourcePath}'. " +
                    $"(fallback folder='{modelFallbackFolderPath}')",
                    this);
                return;
            }

            var instance = Instantiate(modelPrefab, transform);
            instance.name = string.IsNullOrWhiteSpace(visualInstanceName) ? modelPrefab.name : visualInstanceName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
            instance.transform.localScale = localScale;

            // The player root (this GameObject) owns the Animator used by StarterAssets.ThirdPersonController.
            // Remove any Animator that may come embedded in the imported model prefab to avoid conflicts.
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                Destroy(animator);
            }

            // Same for CharacterController/PlayerInput that could exist in some imported models.
            foreach (var controller in instance.GetComponentsInChildren<CharacterController>(true))
            {
                Destroy(controller);
            }

            // We intentionally avoid a compile-time dependency on Unity.InputSystem here, because this project
            // uses an asmdef without explicit references. If a model prefab brings its own PlayerInput, remove it.
            foreach (var component in instance.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().FullName == "UnityEngine.InputSystem.PlayerInput")
                {
                    Destroy(component);
                }
            }

            var rendererCount = instance.GetComponentsInChildren<Renderer>(true).Length;
            if (rendererCount == 0)
            {
                Debug.LogWarning(
                    $"PlayerAvatarVisualInjector: Loaded '{modelPrefab.name}' but it contains 0 renderers. " +
                    "Import settings or model contents might be missing meshes/materials.",
                    this);
            }
        }
    }
}
