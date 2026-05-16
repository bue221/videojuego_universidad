using UnityEngine;
using UnityEngine.Rendering;

namespace LightChasePrototype
{
    public static class LightChaseAtmosphere
    {
        public static readonly Color NightFogColor = new(0.012f, 0.018f, 0.04f, 1f);
        public static readonly Color NightAmbientColor = new(0.008f, 0.012f, 0.022f, 1f);
        public static readonly Color NightHorizonColor = new(0.013f, 0.018f, 0.032f, 1f);
        public static readonly Color NightGroundColor = new(0.004f, 0.006f, 0.012f, 1f);
        public static readonly Color NightSkyColor = new(0.003f, 0.005f, 0.011f, 1f);
        public static readonly Color MoonlightColor = new(0.11f, 0.15f, 0.24f, 1f);

        public const float ReflectionIntensity = 0.02f;
        public const float FogDensity = 0.055f;
        public const float DirectionalLightIntensity = 0.015f;

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
