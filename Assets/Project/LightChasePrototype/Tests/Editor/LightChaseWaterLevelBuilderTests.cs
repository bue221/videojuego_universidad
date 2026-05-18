using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LightChaseWaterLevelBuilderTests
{
    private const string PrototypeScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string LakeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level04.unity";

    [Test]
    public void BuildLevel_CreatesScenarioEnvironmentWaterHazardsAndNineStars()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        var scene = EditorSceneManager.GetActiveScene();
        Assert.That(scene.path, Is.EqualTo(WaterLevelScenePath));
        Assert.That(GameObject.Find("Scenario3Environment"), Is.Not.Null);
        Assert.That(GameObject.Find("WaterHazards"), Is.Not.Null);
        var enemy = GameObject.Find("LightHunter");
        Assert.That(enemy, Is.Not.Null);
        Assert.That(enemy.transform.Find("Enemigo_03_Model"), Is.Not.Null);

        var enemyRenderer = enemy.GetComponentInChildren<Renderer>();
        Assert.That(enemyRenderer, Is.Not.Null);
        Assert.That(enemyRenderer.sharedMaterial, Is.Not.Null);
        Assert.That(enemyRenderer.sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
        Assert.That(GameObject.Find("ExitPortal"), Is.Not.Null);

        var collectibles = GameObject.Find("Collectibles");
        Assert.That(collectibles, Is.Not.Null);
        Assert.That(collectibles.transform.childCount, Is.EqualTo(9));

        var environment = GameObject.Find("Scenario3Environment");
        var renderers = environment.GetComponentsInChildren<Renderer>();
        Assert.That(renderers.Length, Is.GreaterThan(0), "Scenario environment must have renderers");

        var firstMaterial = renderers[0].sharedMaterial;
        Assert.That(firstMaterial, Is.Not.Null, "Scenario renderer must have a material assigned");
        Assert.That(firstMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"),
            "Scenario material must use URP/Lit shader");
        Assert.That(firstMaterial.GetTexture("_BaseMap"), Is.Not.Null,
            "Scenario material must have a base color texture");
    }

    [Test]
    public void BuildLevel_ConfiguresWaterRoutePacingAndTraversal()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        var manager = Object.FindAnyObjectByType<PrototypeLevelManager>();
        Assert.That(manager, Is.Not.Null);

        var serializedObject = new SerializedObject(manager);
        Assert.That(serializedObject.FindProperty("starsRequiredToExit").intValue, Is.EqualTo(7));
        Assert.That(serializedObject.FindProperty("startingLives").intValue, Is.EqualTo(3));
        Assert.That(serializedObject.FindProperty("scorePerStar").intValue, Is.EqualTo(100));
        Assert.That(serializedObject.FindProperty("levelTimeSeconds").floatValue, Is.EqualTo(240f).Within(0.001f));

        var player = Object.FindAnyObjectByType<PlayerWaterTraversal>();
        var waterVolumes = Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None);

        Assert.That(player, Is.Not.Null);
        Assert.That(waterVolumes.Length, Is.EqualTo(3));
    }

    [Test]
    public void BuildLevel_PreservesExistingScenario3EnvironmentScale()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        var environment = GameObject.Find("Scenario3Environment");
        Assert.That(environment, Is.Not.Null);

        var expectedScale = new Vector3(1.37f, 1.37f, 1.37f);
        environment.transform.localScale = expectedScale;

        LightChaseWaterLevelBuilder.BuildLevel();

        var rebuiltEnvironment = GameObject.Find("Scenario3Environment");
        Assert.That(rebuiltEnvironment, Is.Not.Null);
        Assert.That(rebuiltEnvironment.transform.localScale.x, Is.EqualTo(expectedScale.x).Within(0.0001f));
        Assert.That(rebuiltEnvironment.transform.localScale.y, Is.EqualTo(expectedScale.y).Within(0.0001f));
        Assert.That(rebuiltEnvironment.transform.localScale.z, Is.EqualTo(expectedScale.z).Within(0.0001f));
    }

    [Test]
    public void BuildLevel_CreatesPlayableTerrainAndKeepLighting()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        var terrain = GameObject.Find("WaterLevelGeometry");
        Assert.That(terrain, Is.Not.Null, "Level 03 must include a playable terrain root, not just the Meshy keep.");
        Assert.That(terrain.transform.childCount, Is.GreaterThan(8),
            "Playable terrain must have multiple ground tiles so the level is not extra small.");

        var lighting = GameObject.Find("WaterLevelLighting");
        Assert.That(lighting, Is.Not.Null, "Level 03 must add local torch lighting around the keep so it is readable.");
        var torches = lighting.GetComponentsInChildren<Light>();
        Assert.That(torches.Length, Is.GreaterThanOrEqualTo(5),
            "Keep lighting must provide enough warm fill lights to fight the night atmosphere.");
    }

    [Test]
    public void BuildLevel_EnsuresLevelsOneToFourAreInBuildSettings()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        Assert.That(HasEnabledScene(PrototypeScenePath), Is.True);
        Assert.That(HasEnabledScene(NatureLevelScenePath), Is.True);
        Assert.That(HasEnabledScene(WaterLevelScenePath), Is.True);
        Assert.That(HasEnabledScene(LakeLevelScenePath), Is.True);
    }

    [Test]
    public void BuildLevelFullRebuild_RebuildsScenarioEnvironmentScale()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        var environment = GameObject.Find("Scenario3Environment");
        Assert.That(environment, Is.Not.Null);

        var forcedScale = new Vector3(9f, 9f, 9f);
        environment.transform.localScale = forcedScale;

        LightChaseWaterLevelBuilder.BuildLevelFullRebuild();

        var rebuiltEnvironment = GameObject.Find("Scenario3Environment");
        Assert.That(rebuiltEnvironment, Is.Not.Null);
        Assert.That(rebuiltEnvironment.transform.localScale.x, Is.Not.EqualTo(forcedScale.x).Within(0.0001f));
        Assert.That(rebuiltEnvironment.transform.localScale.y, Is.Not.EqualTo(forcedScale.y).Within(0.0001f));
        Assert.That(rebuiltEnvironment.transform.localScale.z, Is.Not.EqualTo(forcedScale.z).Within(0.0001f));
    }

    private static bool HasEnabledScene(string scenePath)
    {
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == scenePath)
            {
                return scene.enabled;
            }
        }

        return false;
    }
}
