using UnityEngine;
using UnityEngine.Rendering;

namespace LightChasePrototype
{
    public static class LightChaseAtmosphere
    {
        public static readonly Color NightFogColor = new(0.02f, 0.028f, 0.052f, 1f);
        public static readonly Color NightAmbientColor = new(0.018f, 0.024f, 0.04f, 1f);
        public static readonly Color NightHorizonColor = new(0.024f, 0.032f, 0.056f, 1f);
        public static readonly Color NightGroundColor = new(0.01f, 0.014f, 0.024f, 1f);
        public static readonly Color NightSkyColor = new(0.008f, 0.012f, 0.022f, 1f);
        public static readonly Color MoonlightColor = new(0.22f, 0.29f, 0.42f, 1f);

        public const float ReflectionIntensity = 0.05f;
        public const float FogDensity = 0.03f;
        public const float DirectionalLightIntensity = 0.12f;

        public static void ApplyRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = NightAmbientColor;
            RenderSettings.ambientSkyColor = NightAmbientColor;
            RenderSettings.ambientEquatorColor = NightHorizonColor;
            RenderSettings.ambientGroundColor = NightGroundColor;
            RenderSettings.subtractiveShadowColor = Color.black;
            RenderSettings.reflectionIntensity = ReflectionIntensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.fog = true;
            RenderSettings.fogColor = NightFogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = FogDensity;
        }

        public static void ApplyToDirectionalLight(Light mainLight)
        {
            if (mainLight == null)
            {
                return;
            }

            mainLight.color = MoonlightColor;
            mainLight.intensity = DirectionalLightIntensity;
            mainLight.shadowStrength = 1f;
        }

        public static void ApplyToSceneCameras()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>())
            {
                ApplyToCamera(camera);
            }
        }

        public static void ApplyToCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = NightSkyColor;
        }
    }
}
