using System.Collections.Generic;
using System.Text.RegularExpressions;
using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

    [Test]
    public void SpawnEnemies_PlacesEachAgentAtItsAnchorBeforeNavMeshRebind()
    {
        var anchors = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(3f, 0f, 0f),
            new Vector3(0f, 0f, 3f)
        };

        var spawns = new List<EnemySpawner.EnemySpawn>
        {
            new(EnemyKind.Director, anchors[0]),
            new(EnemyKind.Deshilachador, anchors[1]),
            new(EnemyKind.BromaFinal, anchors[2])
        };

        var results = EnemySpawner.SpawnEnemies(spawns);

        Assert.That(results.Count, Is.EqualTo(3));
        for (var i = 0; i < results.Count; i++)
        {
            var pos = results[i].transform.position;
            Assert.That(pos.x, Is.EqualTo(anchors[i].x).Within(0.01f));
            Assert.That(pos.z, Is.EqualTo(anchors[i].z).Within(0.01f));
            // Pies sobre y=0 + epsilon antifight; no deben flotar arriba de 0.1m.
            Assert.That(pos.y, Is.LessThan(0.1f), $"Enemigo {i} flota por encima del suelo en y={pos.y}");
            Assert.That(pos.y, Is.GreaterThanOrEqualTo(0f), $"Enemigo {i} esta hundido en y={pos.y}");
        }
    }

    [Test]
    public void RebindEnemiesToNavMesh_DoesNotThrowWhenNoNavMeshIsPresent()
    {
        EnemySpawner.SpawnEnemies(new List<EnemySpawner.EnemySpawn>
        {
            new(EnemyKind.Deshilachador, Vector3.zero)
        });

        // Sin NavMesh bakeado el metodo loguea un warning por enemigo y devuelve 0,
        // pero no debe crashear.
        LogAssert.Expect(LogType.Warning, new Regex("EnemySpawner.*sin NavMesh"));
        Assert.DoesNotThrow(() => EnemySpawner.RebindEnemiesToNavMesh());
    }

    [Test]
    public void RebindEnemiesToNavMesh_ReturnsZeroWhenNoEnemiesExist()
    {
        EnemySpawner.ClearExistingEnemies();

        var fixedCount = EnemySpawner.RebindEnemiesToNavMesh();

        Assert.That(fixedCount, Is.EqualTo(0));
    }
}
