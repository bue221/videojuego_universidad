using System.Reflection;
using UnityEngine;

namespace LightChasePrototype
{
    /// <summary>
    /// Attach this MonoBehaviour to each player avatar prefab.
    /// On Awake it wires up audio clips from the linked PlayerAvatarEntry
    /// directly into the StarterAssets.ThirdPersonController component,
    /// using reflection to avoid a hard assembly dependency.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAvatarBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Direct reference to the avatar entry. If null, the catalog is queried by _avatarId at runtime.")]
        private PlayerAvatarEntry _entry;

        [SerializeField]
        [Tooltip("Used as a fallback key when _entry is not assigned directly.")]
        private string _avatarId;

        private bool _bootstrapped;

        private void Awake()
        {
            if (_bootstrapped)
            {
                return;
            }

            // Resolve entry if not assigned directly.
            if (_entry == null && !string.IsNullOrEmpty(_avatarId))
            {
                _entry = PlayerAvatarCatalog.Load()?.FindById(_avatarId);
            }

            if (_entry == null)
            {
                return;
            }

            WireAudio();
            _bootstrapped = true;
        }

        private void WireAudio()
        {
            // Find ThirdPersonController via reflection to avoid hard assembly coupling.
            var tpc = FindThirdPersonController();
            if (tpc == null)
            {
                Debug.LogWarning($"[PlayerAvatarBootstrap] Could not find ThirdPersonController on '{gameObject.name}'. Audio not wired.", this);
                return;
            }

            var type = tpc.GetType();

            SetField(type, tpc, "FootstepAudioClips", _entry.footstepClips);
            SetField(type, tpc, "LandingAudioClip", _entry.landingClip);

            // Wire optional foley AudioSource.
            if (_entry.foleyClip != null)
            {
                var foleySource = transform.Find("Audio/Robot")?.GetComponent<AudioSource>();
                if (foleySource != null)
                {
                    foleySource.clip = _entry.foleyClip;
                }
            }
        }

        private Component FindThirdPersonController()
        {
            foreach (var comp in GetComponents<Component>())
            {
                if (comp == null)
                {
                    continue;
                }

                var typeName = comp.GetType().FullName;
                if (typeName != null && typeName.Contains("ThirdPersonController"))
                {
                    return comp;
                }
            }

            return null;
        }

        private static void SetField(System.Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            // Also try property.
            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
            }
        }
    }
}
