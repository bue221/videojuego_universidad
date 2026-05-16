using UnityEngine;

namespace LightChasePrototype
{
    public static class PlayerAvatarSetup
    {
        public static readonly Vector3 DefaultSpawnPosition = new(0f, 0.15f, -6f);

        public static PlayerLightState EnsureGameplayPresentation(GameObject player)
        {
            var state = GetOrAddComponent<PlayerLightState>(player);

            var glowLightTransform = player.transform.Find("GlowLight");
            if (glowLightTransform == null)
            {
                var lightObject = new GameObject("GlowLight");
                lightObject.transform.SetParent(player.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                glowLightTransform = lightObject.transform;
            }

            var glowLight = GetOrAddComponent<Light>(glowLightTransform.gameObject);
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.6f, 0.8f, 1f);
            glowLight.intensity = 0.45f;
            glowLight.range = 3.25f;
            glowLight.shadows = LightShadows.Soft;

            var trailTransform = player.transform.Find("GlowTrail");
            if (trailTransform == null)
            {
                var trailObject = new GameObject("GlowTrail");
                trailObject.transform.SetParent(player.transform, false);
                trailObject.transform.localPosition = new Vector3(0f, 0.2f, -0.15f);
                trailTransform = trailObject.transform;
            }

            var trailRenderer = GetOrAddComponent<TrailRenderer>(trailTransform.gameObject);
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.time = 0.3f;
            trailRenderer.minVertexDistance = 0.05f;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.material = CreateUnlitMaterial("PlayerGlowTrail", new Color(0.65f, 0.85f, 1f));

            state.ConfigureVisuals(
                glowLight,
                trailRenderer,
                BuildGradient(new Color(0.55f, 0.8f, 1f), new Color(0.25f, 0.35f, 0.8f, 0f)),
                BuildGradient(new Color(1f, 0.95f, 0.55f), new Color(1f, 0.45f, 0.15f, 0f)));

            return state;
        }

        public static void BindCameraToPlayer(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            var cameraTarget = FindCameraTarget(player);
            if (cameraTarget == null)
            {
                return;
            }

            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour == null)
                {
                    continue;
                }

                var type = behaviour.GetType();
                if (type.FullName == null || !type.FullName.Contains("Cinemachine"))
                {
                    continue;
                }

                SetObjectMember(type, behaviour, "Follow", cameraTarget.transform);
                SetObjectMember(type, behaviour, "LookAt", cameraTarget.transform);
            }
        }

        public static GameObject EnsureSelectedAvatarInScene()
        {
            var selectedPrefab = PlayerAvatarSelection.LoadSelectedPrefab();
            var existingPlayer = FindExistingPlayer();
            if (selectedPrefab == null)
            {
                if (existingPlayer != null)
                {
                    EnsureGameplayPresentation(existingPlayer);
                    BindCameraToPlayer(existingPlayer);
                    return existingPlayer;
                }

                return null;
            }

            if (existingPlayer == null)
            {
                var createdPlayer = Object.Instantiate(selectedPrefab, DefaultSpawnPosition, Quaternion.identity);
                createdPlayer.name = selectedPrefab.name;
                EnsureGameplayPresentation(createdPlayer);
                BindCameraToPlayer(createdPlayer);
                return createdPlayer;
            }

            var currentPlayer = existingPlayer;
            if (currentPlayer.name.Contains(selectedPrefab.name))
            {
                EnsureGameplayPresentation(currentPlayer);
                BindCameraToPlayer(currentPlayer);
                return currentPlayer;
            }

            var replacementPlayer = Object.Instantiate(selectedPrefab, currentPlayer.transform.position, currentPlayer.transform.rotation);
            replacementPlayer.name = selectedPrefab.name;
            currentPlayer.SetActive(false);
            Object.Destroy(currentPlayer);
            EnsureGameplayPresentation(replacementPlayer);
            BindCameraToPlayer(replacementPlayer);
            return replacementPlayer;
        }

        private static Gradient BuildGradient(Color startColor, Color endColor)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(Color.Lerp(startColor, endColor, 0.5f), 0.55f),
                    new GradientColorKey(endColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(0.45f, 0.6f),
                    new GradientAlphaKey(endColor.a, 1f)
                });
            return gradient;
        }

        private static Material CreateUnlitMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var material = new Material(shader) { name = materialName, color = color };
            material.EnableKeyword("_EMISSION");
            return material;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGetComponent<T>(out var component))
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static GameObject FindExistingPlayer()
        {
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour == null || behaviour.GetType().FullName != "StarterAssets.ThirdPersonController")
                {
                    continue;
                }

                return behaviour.gameObject;
            }

            return Object.FindAnyObjectByType<PlayerLightState>()?.gameObject;
        }

        private static GameObject FindCameraTarget(GameObject player)
        {
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null)
                {
                    continue;
                }

                var behaviourType = behaviour.GetType();
                if (behaviourType.FullName != "StarterAssets.ThirdPersonController")
                {
                    continue;
                }

                var field = behaviourType.GetField("CinemachineCameraTarget");
                if (field != null && typeof(GameObject).IsAssignableFrom(field.FieldType))
                {
                    return field.GetValue(behaviour) as GameObject;
                }

                var property = behaviourType.GetProperty("CinemachineCameraTarget");
                if (property != null && typeof(GameObject).IsAssignableFrom(property.PropertyType))
                {
                    return property.GetValue(behaviour) as GameObject;
                }
            }

            return player.transform.Find("PlayerCameraRoot")?.gameObject;
        }

        private static void SetObjectMember(System.Type type, object target, string memberName, Transform value)
        {
            var property = type.GetProperty(memberName);
            if (property != null && property.CanWrite && property.PropertyType.IsAssignableFrom(typeof(Transform)))
            {
                property.SetValue(target, value);
                return;
            }

            var field = type.GetField(memberName);
            if (field != null && field.FieldType.IsAssignableFrom(typeof(Transform)))
            {
                field.SetValue(target, value);
            }
        }
    }
}
