using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LightChaseWaterLevelBuilderTests
{
    private const string WaterLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level03.unity";

    [Test]
    public void BuildLevel_CreatesScenarioEnvironmentWaterHazardsAndNineStars()
    {
        LightChaseWaterLevelBuilder.BuildLevel();

        var scene = EditorSceneManager.GetActiveScene();
        Assert.That(scene.path, Is.EqualTo(WaterLevelScenePath));
        Assert.That(GameObject.Find("Scenario3Environment"), Is.Not.Null);
        Assert.That(GameObject.Find("WaterHazards"), Is.Not.Null);
        Assert.That(GameObject.Find("LightHunter"), Is.Not.Null);
        Assert.That(GameObject.Find("ExitPortal"), Is.Not.Null);

        var collectibles = GameObject.Find("Collectibles");
        Assert.That(collectibles, Is.Not.Null);
        Assert.That(collectibles.transform.childCount, Is.EqualTo(9));
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
}
