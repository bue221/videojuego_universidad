using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class LightChaseAtmosphereTests
{
    private Camera _camera;
    private Light _light;

    [SetUp]
    public void SetUp()
    {
        var cameraObject = new GameObject("AtmosphereCamera");
        _camera = cameraObject.AddComponent<Camera>();

        var lightObject = new GameObject("AtmosphereLight");
        _light = lightObject.AddComponent<Light>();
        _light.type = LightType.Directional;
    }

    [TearDown]
    public void TearDown()
    {
        if (_camera != null)
        {
            Object.DestroyImmediate(_camera.gameObject);
        }

        if (_light != null)
        {
            Object.DestroyImmediate(_light.gameObject);
        }
    }

    [Test]
    public void ApplyRenderSettings_UsesFlatDarkAmbientAndDenseFog()
    {
        LightChaseAtmosphere.ApplyRenderSettings();

        Assert.That(RenderSettings.skybox, Is.Null);
        Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
        Assert.That(RenderSettings.ambientLight, Is.EqualTo(LightChaseAtmosphere.NightAmbientColor));
        Assert.That(RenderSettings.ambientEquatorColor, Is.EqualTo(LightChaseAtmosphere.NightHorizonColor));
        Assert.That(RenderSettings.ambientGroundColor, Is.EqualTo(LightChaseAtmosphere.NightGroundColor));
        Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(LightChaseAtmosphere.ReflectionIntensity).Within(0.0001f));
        Assert.That(RenderSettings.fog, Is.True);
        Assert.That(RenderSettings.fogColor, Is.EqualTo(LightChaseAtmosphere.NightFogColor));
        Assert.That(RenderSettings.fogDensity, Is.EqualTo(LightChaseAtmosphere.FogDensity).Within(0.0001f));
    }

    [Test]
    public void ApplyToDirectionalLight_DimsLightIntoMoonlight()
    {
        LightChaseAtmosphere.ApplyToDirectionalLight(_light);

        Assert.That(_light.color, Is.EqualTo(LightChaseAtmosphere.MoonlightColor));
        Assert.That(_light.intensity, Is.EqualTo(LightChaseAtmosphere.DirectionalLightIntensity).Within(0.0001f));
        Assert.That(_light.shadowStrength, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void ApplyToCamera_UsesSolidNightSkyInsteadOfSkybox()
    {
        LightChaseAtmosphere.ApplyToCamera(_camera);

        Assert.That(_camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
        Assert.That(_camera.backgroundColor, Is.EqualTo(LightChaseAtmosphere.NightSkyColor));
    }

    [Test]
    public void ApplyToSceneCameras_UpdatesEveryCameraFound()
    {
        var extraCamera = new GameObject("ExtraAtmosphereCamera").AddComponent<Camera>();

        LightChaseAtmosphere.ApplyToSceneCameras();

        Assert.That(_camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
        Assert.That(extraCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
        Assert.That(extraCamera.backgroundColor, Is.EqualTo(LightChaseAtmosphere.NightSkyColor));

        Object.DestroyImmediate(extraCamera.gameObject);
    }
}
