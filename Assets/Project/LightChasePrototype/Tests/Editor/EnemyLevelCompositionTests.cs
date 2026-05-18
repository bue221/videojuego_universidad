using NUnit.Framework;
using UnityEngine;

public class EnemyLevelCompositionTests
{
    private static readonly Vector3[] FourAnchors =
    {
        new(0f, 0f, 0f),
        new(5f, 0f, 0f),
        new(0f, 0f, 5f),
        new(-5f, 0f, 0f)
    };

    [Test]
    public void Level01_HasOneDirectorEnemy()
    {
        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level01, FourAnchors);

        Assert.That(spawns.Count, Is.EqualTo(1));
        Assert.That(spawns[0].Kind, Is.EqualTo(EnemyKind.Director));
    }

    [Test]
    public void Level02_HasTwoEnemiesWithDeshilachadorDominant()
    {
        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level02, FourAnchors);

        Assert.That(spawns.Count, Is.EqualTo(2));
        Assert.That(spawns[0].Kind, Is.EqualTo(EnemyKind.Deshilachador));
    }

    [Test]
    public void Level03_HasFourEnemiesWithBromaFinalDominant()
    {
        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level03, FourAnchors);

        Assert.That(spawns.Count, Is.EqualTo(4));
        Assert.That(spawns[0].Kind, Is.EqualTo(EnemyKind.BromaFinal));
        Assert.That(spawns[2].Kind, Is.EqualTo(EnemyKind.BromaFinal));
    }

    [Test]
    public void Level04_HasSixEnemiesMixingAllKinds()
    {
        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level04, FourAnchors);

        Assert.That(spawns.Count, Is.EqualTo(6));

        var hasDirector = false;
        var hasDeshilachador = false;
        var hasBromaFinal = false;
        foreach (var spawn in spawns)
        {
            if (spawn.Kind == EnemyKind.Director) hasDirector = true;
            if (spawn.Kind == EnemyKind.Deshilachador) hasDeshilachador = true;
            if (spawn.Kind == EnemyKind.BromaFinal) hasBromaFinal = true;
        }

        Assert.That(hasDirector, Is.True);
        Assert.That(hasDeshilachador, Is.True);
        Assert.That(hasBromaFinal, Is.True);
    }

    [Test]
    public void BuildSpawns_CyclesAnchorsWhenCountExceedsAnchorList()
    {
        var anchors = new[] { new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f) };

        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level04, anchors);

        Assert.That(spawns.Count, Is.EqualTo(6));
        Assert.That(spawns[0].Position, Is.EqualTo(anchors[0]));
        Assert.That(spawns[1].Position, Is.EqualTo(anchors[1]));
        Assert.That(spawns[2].Position, Is.EqualTo(anchors[0]));
    }

    [Test]
    public void BuildSpawns_ReturnsEmptyWhenAnchorsAreNull()
    {
        var spawns = EnemyLevelComposition.BuildSpawns(EnemyLevelComposition.LevelTier.Level02, null);

        Assert.That(spawns, Is.Not.Null);
        Assert.That(spawns.Count, Is.EqualTo(0));
    }
}
