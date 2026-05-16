using System;
using UnityEngine;

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
                "Capsula",
                "Avatar minimalista para una lectura mas abstracta del movimiento.",
                "PlayerAvatars/PlayerCapsule")
        };

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
            foreach (var option in Options)
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
            return Resources.Load<GameObject>(GetAvatar(avatarId).ResourcePath);
        }

        public static bool IsValidAvatarId(string avatarId)
        {
            foreach (var option in Options)
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
    }
}
