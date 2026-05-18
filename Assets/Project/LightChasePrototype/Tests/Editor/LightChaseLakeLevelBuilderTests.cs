using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LightChaseLakeLevelBuilderTests
{
    private const string PrototypeScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";
    private const string LakeLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level04.unity";

    [Test]
    public void BuildLevel_CreatesLakeEnvironmentWaterHazardsAndTenStars()
    {
        LightChaseLakeLevelBuilder.BuildLevel();

        var scene = EditorSceneManager.GetActiveScene();
        Assert.That(scene.path, Is.EqualTo(LakeLevelScenePath));
        Assert.That(GameObject.Find("LakeLevelGeometry"), Is.Not.Null);
        Assert.That(GameObject.Find("LakeSetDressing"), Is.Not.Null);
        Assert.That(GameObject.Find("LakeHazards"), Is.Not.Null);
        Assert.That(GameObject.Find("WaterHazards"), Is.Not.Null);
        Assert.That(GameObject.Find("ExitPortal"), Is.Not.Null);
        Assert.That(GameObject.Find("LightHunter"), Is.Not.Null);

        var collectibles = GameObject.Find("Collectibles");
        Assert.That(collectibles, Is.Not.Null);
        Assert.That(collectibles.transform.childCount, Is.EqualTo(10));
    }

    [Test]
    public void BuildLevel_ConfiguresWaterRiskAndPacingForLakeRoute()
    {
        LightChaseLakeLevelBuilder.BuildLevel();

        var manager = Object.FindAnyObjectByType<PrototypeLevelManager>();
        Assert.That(manager, Is.Not.Null);

        var serializedObject = new SerializedObject(manager);
        Assert.That(serializedObject.FindProperty("starsRequiredToExit").intValue, Is.EqualTo(8));
        Assert.That(serializedObject.FindProperty("startingLives").intValue, Is.EqualTo(3));
        Assert.That(serializedObject.FindProperty("scorePerStar").intValue, Is.EqualTo(100));
        Assert.That(serializedObject.FindProperty("levelTimeSeconds").floatValue, Is.EqualTo(250f).Within(0.001f));

        var waterVolumes = Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None);
        Assert.That(waterVolumes.Length, Is.EqualTo(4));
    }

    [Test]
    public void BuildLevel_EnsuresLevelsOneToFourAreInBuildSettings()
    {
        LightChaseLakeLevelBuilder.BuildLevel();

        Assert.That(HasEnabledScene(PrototypeScenePath), Is.True);
        Assert.That(HasEnabledScene(NatureLevelScenePath), Is.True);
        Assert.That(HasEnabledScene(WaterLevelScenePath), Is.True);
        Assert.That(HasEnabledScene(LakeLevelScenePath), Is.True);
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
