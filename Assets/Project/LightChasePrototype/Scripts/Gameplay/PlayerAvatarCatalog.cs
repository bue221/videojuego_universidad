using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LightChasePrototype
{
    [CreateAssetMenu(fileName = "PlayerAvatarCatalog", menuName = "LightChase/Player Avatar Catalog")]
    public class PlayerAvatarCatalog : ScriptableObject
    {
        [SerializeField]
        private PlayerAvatarEntry[] _entries = System.Array.Empty<PlayerAvatarEntry>();

        /// <summary>
        /// Loads the catalog from Resources/PlayerAvatarCatalog. Returns null if not found.
        /// </summary>
        public static PlayerAvatarCatalog Load()
        {
            return Resources.Load<PlayerAvatarCatalog>("PlayerAvatarCatalog");
        }

        /// <summary>
        /// Read-only view of all registered entries.
        /// </summary>
        public IReadOnlyList<PlayerAvatarEntry> Entries => _entries;

        /// <summary>
        /// Finds an entry by avatarId. Returns null if not found.
        /// </summary>
        public PlayerAvatarEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var entry in _entries)
            {
                if (entry != null && string.Equals(entry.avatarId, id, System.StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Registers an entry if no entry with the same avatarId already exists.
        /// In the Editor, marks the asset dirty so changes are saved.
        /// </summary>
        public void Register(PlayerAvatarEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.avatarId))
            {
                return;
            }

            if (FindById(entry.avatarId) != null)
            {
                return;
            }

            var list = new List<PlayerAvatarEntry>(_entries) { entry };
            _entries = list.ToArray();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Converts a PlayerAvatarEntry to the lightweight AvatarOption struct used by PlayerAvatarSelection.
        /// </summary>
        public PlayerAvatarSelection.AvatarOption ToAvatarOption(PlayerAvatarEntry e)
        {
            return new PlayerAvatarSelection.AvatarOption(e.avatarId, e.displayName, e.description, e.resourcePath);
        }
    }
}
