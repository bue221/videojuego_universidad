using System;
using UnityEngine;

namespace LightChasePrototype
{
    public static class LightChaseLevelCatalog
    {
        private const string SelectedLevelKey = "LightChasePrototype.SelectedLevel";

        public const string PrototypeLevelId = "prototype-level-01";
        public const string NatureLevelId = "prototype-level-02";
        public const string WaterLevelId = "prototype-level-03";
        public const string LakeLevelId = "prototype-level-04";

        public static readonly LevelOption[] Options =
        {
            new(
                PrototypeLevelId,
                "Nivel 1",
                "LightChasePrototype",
                "Arena base del prototipo. Riesgo legible y rutas abiertas para aprender el loop."),
            new(
                NatureLevelId,
                "Nivel 2",
                "LightChasePrototype_Level02",
                "Bosque low poly oscuro con corredores naturales, cobertura visual y rutas mas tensas."),
            new(
                WaterLevelId,
                "Nivel 3",
                "LightChasePrototype_Level03",
                "Escenario costero importado desde MeshyImports/Escena3, con pasos inundados donde avanzar hunde parcialmente al avatar."),
            new(
                LakeLevelId,
                "Nivel 4",
                "LightChasePrototype_Level04",
                "Circuito alrededor de un lago amplio: rutas seguras largas o atajos por muelles y zonas inundadas con mayor exposicion.")
        };

        public static string SelectedLevelId
        {
            get
            {
                var storedValue = PlayerPrefs.GetString(SelectedLevelKey, PrototypeLevelId);
                return IsValidLevelId(storedValue) ? storedValue : PrototypeLevelId;
            }
        }

        public static LevelOption SelectedLevel => GetLevel(SelectedLevelId);

        public static string DefaultSceneName => Options[0].SceneName;

        public static bool TryGetNextLevelSceneName(string currentSceneName, out string nextSceneName)
        {
            nextSceneName = null;
            if (string.IsNullOrWhiteSpace(currentSceneName))
            {
                return false;
            }

            for (var i = 0; i < Options.Length; i++)
            {
                if (!string.Equals(Options[i].SceneName, currentSceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                var nextIndex = i + 1;
                if (nextIndex >= Options.Length)
                {
                    return false;
                }

                nextSceneName = Options[nextIndex].SceneName;
                return !string.IsNullOrWhiteSpace(nextSceneName);
            }

            return false;
        }

        public static void SelectLevel(string levelId)
        {
            var resolvedLevelId = IsValidLevelId(levelId) ? levelId : PrototypeLevelId;
            PlayerPrefs.SetString(SelectedLevelKey, resolvedLevelId);
            PlayerPrefs.Save();
        }

        public static LevelOption GetLevel(string levelId)
        {
            foreach (var option in Options)
            {
                if (string.Equals(option.Id, levelId, StringComparison.Ordinal))
                {
                    return option;
                }
            }

            return Options[0];
        }

        public static LevelOption GetLevelBySceneName(string sceneName)
        {
            foreach (var option in Options)
            {
                if (string.Equals(option.SceneName, sceneName, StringComparison.Ordinal))
                {
                    return option;
                }
            }

            return Options[0];
        }

        public static bool IsKnownSceneName(string sceneName)
        {
            foreach (var option in Options)
            {
                if (string.Equals(option.SceneName, sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsValidLevelId(string levelId)
        {
            foreach (var option in Options)
            {
                if (string.Equals(option.Id, levelId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetScenePath(string sceneName)
        {
            return $"Assets/Project/LightChasePrototype/Scenes/{sceneName}.unity";
        }

        public readonly struct LevelOption
        {
            public LevelOption(string id, string displayName, string sceneName, string description)
            {
                Id = id;
                DisplayName = displayName;
                SceneName = sceneName;
                Description = description;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string SceneName { get; }
            public string Description { get; }
        }
    }
}
