using System.Collections.Generic;
using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;

public class EnemySpawnerTests
{
    [SetUp]
    public void SetUp()
    {
        foreach (EnemyKind kind in System.Enum.GetValues(typeof(EnemyKind)))
        {
            EnemyBuilder.PrepareEnemyKindAssets(EnemyKindCatalog.GetAssets(kind));
        }
    }

    [TearDown]
    public void TearDown()
    {
        EnemySpawner.ClearExistingEnemies();
    }

    [Test]
    public void SpawnEnemies_CreatesOneSeekerPerSpawn()
    {
        var spawns = new List<EnemySpawner.EnemySpawn>
        {
            new(EnemyKind.Director, new Vector3(0f, 0f, 0f)),
            new(EnemyKind.Deshilachador, new Vector3(3f, 0f, 0f)),
            new(EnemyKind.BromaFinal, new Vector3(0f, 0f, 3f))
        };

        var results = EnemySpawner.SpawnEnemies(spawns);

        Assert.That(results.Count, Is.EqualTo(3));
        foreach (var seeker in results)
        {
            Assert.That(seeker, Is.Not.Null);
            Assert.That(seeker.GetComponent<UnityEngine.AI.NavMeshAgent>(), Is.Not.Null);
            Assert.That(seeker.GetComponentInChildren<Animator>(true), Is.Not.Null);
        }
    }

    [Test]
    public void SpawnEnemies_GroupsAllEnemiesUnderLightHuntersParent()
    {
        var spawns = new List<EnemySpawner.EnemySpawn>
        {
            new(EnemyKind.Director, Vector3.zero),
            new(EnemyKind.Deshilachador, new Vector3(2f, 0f, 0f))
        };

        EnemySpawner.SpawnEnemies(spawns);

        var group = GameObject.Find("LightHunters");
        Assert.That(group, Is.Not.Null);
        Assert.That(group.transform.childCount, Is.EqualTo(2));
    }

    [Test]
    public void SpawnEnemies_ClearsPreviousEnemiesBeforeSpawning()
    {
        EnemySpawner.SpawnEnemies(new List<EnemySpawner.EnemySpawn>
        {
            new(EnemyKind.Director, Vector3.zero),
            new(EnemyKind.Director, new Vector3(2f, 0f, 0f))
        });

        var results = EnemySpawner.SpawnEnemies(new List<EnemySpawner.EnemySpawn>
        {
            new(EnemyKind.Deshilachador, Vector3.zero)
        });

        var allSeekers = Object.FindObjectsByType<EnemyLightSeeker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(allSeekers.Length, Is.EqualTo(1));
        Assert.That(results.Count, Is.EqualTo(1));
    }
}
