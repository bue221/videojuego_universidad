using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LightChaseNatureLevelBuilderTests
{
    private const string NatureLevelScenePath = "Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity";

    [Test]
    public void BuildLevel_CreatesNatureRootsEnemyExitAndEightStars()
    {
        LightChaseNatureLevelBuilder.BuildLevel();

        var scene = EditorSceneManager.GetActiveScene();
        Assert.That(scene.path, Is.EqualTo(NatureLevelScenePath));
        Assert.That(GameObject.Find("NatureLevelGeometry"), Is.Not.Null);
        Assert.That(GameObject.Find("NatureSetDressing"), Is.Not.Null);
        var enemy = GameObject.Find("LightHunter");
        Assert.That(enemy, Is.Not.Null);
        Assert.That(enemy.transform.Find("Enemigo_01_Model"), Is.Not.Null);

        var enemyRenderer = enemy.GetComponentInChildren<Renderer>();
        Assert.That(enemyRenderer, Is.Not.Null);
        Assert.That(enemyRenderer.sharedMaterial, Is.Not.Null);
        Assert.That(enemyRenderer.sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
        Assert.That(GameObject.Find("ExitPortal"), Is.Not.Null);

        var collectibles = GameObject.Find("Collectibles");
        Assert.That(collectibles, Is.Not.Null);
        Assert.That(collectibles.transform.childCount, Is.EqualTo(8));
    }

    [Test]
    public void BuildLevel_ConfiguresLongerLevelPacingForNatureRoute()
    {
        LightChaseNatureLevelBuilder.BuildLevel();

        var manager = Object.FindAnyObjectByType<PrototypeLevelManager>();
        Assert.That(manager, Is.Not.Null);

        var serializedObject = new SerializedObject(manager);
        Assert.That(serializedObject.FindProperty("starsRequiredToExit").intValue, Is.EqualTo(6));
        Assert.That(serializedObject.FindProperty("startingLives").intValue, Is.EqualTo(3));
        Assert.That(serializedObject.FindProperty("scorePerStar").intValue, Is.EqualTo(100));
        Assert.That(serializedObject.FindProperty("levelTimeSeconds").floatValue, Is.EqualTo(210f).Within(0.001f));
    }

    [Test]
    public void BuildLevel_LiftsPlayerStarsAndEnemyAboveGroundPlane()
    {
        LightChaseNatureLevelBuilder.BuildLevel();

        var player = Object.FindAnyObjectByType<PlayerLightState>();
        var enemy = Object.FindAnyObjectByType<EnemyLightSeeker>();
        var stars = Object.FindObjectsByType<StarPickup>(FindObjectsSortMode.None);

        Assert.That(player, Is.Not.Null);
        Assert.That(player.transform.position.y, Is.GreaterThan(0.9f));
        Assert.That(enemy, Is.Not.Null);
        Assert.That(enemy.transform.position.y, Is.GreaterThanOrEqualTo(0f));
        Assert.That(stars.Length, Is.EqualTo(8));

        foreach (var star in stars)
        {
            Assert.That(star.transform.position.y, Is.GreaterThan(2f));
        }
    }
}
