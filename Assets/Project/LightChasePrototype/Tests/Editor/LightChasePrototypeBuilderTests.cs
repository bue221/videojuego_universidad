using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LightChasePrototypeBuilderTests
{
    private const string PrototypeScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype.unity";

    [Test]
    public void BuildLevel_CreatesPrototypeEnemyExitHudAndSevenStars()
    {
        LightChasePrototypeBuilder.BuildLevel();

        var scene = EditorSceneManager.GetActiveScene();
        Assert.That(scene.path, Is.EqualTo(PrototypeScenePath));
        Assert.That(GameObject.Find("LightHunter"), Is.Not.Null);
        Assert.That(GameObject.Find("ExitPortal"), Is.Not.Null);
        Assert.That(GameObject.Find("Navigation"), Is.Not.Null);
        Assert.That(GameObject.Find("GameplayHUD"), Is.Null);
        Assert.That(GameObject.Find("MainMenuOverlay"), Is.Null);
        Assert.That(GameObject.Find("GlobalUIRoot"), Is.Null);

        var collectibles = GameObject.Find("Collectibles");
        Assert.That(collectibles, Is.Not.Null);
        Assert.That(collectibles.transform.childCount, Is.EqualTo(7));
    }

    [Test]
    public void BuildLevel_ConfiguresPrototypePacingDefaults()
    {
        LightChasePrototypeBuilder.BuildLevel();

        var manager = Object.FindAnyObjectByType<PrototypeLevelManager>();
        Assert.That(manager, Is.Not.Null);

        var serializedObject = new SerializedObject(manager);
        Assert.That(serializedObject.FindProperty("starsRequiredToExit").intValue, Is.EqualTo(5));
        Assert.That(serializedObject.FindProperty("startingLives").intValue, Is.EqualTo(3));
        Assert.That(serializedObject.FindProperty("scorePerStar").intValue, Is.EqualTo(100));
        Assert.That(serializedObject.FindProperty("levelTimeSeconds").floatValue, Is.EqualTo(180f).Within(0.001f));
    }
}
