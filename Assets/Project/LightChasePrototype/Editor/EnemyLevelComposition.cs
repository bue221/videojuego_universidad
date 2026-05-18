using System.Collections.Generic;
using UnityEngine;

public static class EnemyLevelComposition
{
    public enum LevelTier
    {
        Level01 = 1,
        Level02 = 2,
        Level03 = 3,
        Level04 = 4
    }

    public static List<EnemySpawner.EnemySpawn> BuildSpawns(LevelTier tier, IReadOnlyList<Vector3> anchors)
    {
        var kinds = ResolveKindPattern(tier);
        var count = ResolveEnemyCount(tier);
        var spawns = new List<EnemySpawner.EnemySpawn>(count);

        if (anchors == null || anchors.Count == 0)
        {
            return spawns;
        }

        for (var i = 0; i < count; i++)
        {
            var anchor = anchors[i % anchors.Count];
            var kind = kinds[i % kinds.Count];
            spawns.Add(new EnemySpawner.EnemySpawn(kind, anchor));
        }

        return spawns;
    }

    private static int ResolveEnemyCount(LevelTier tier)
    {
        switch (tier)
        {
            case LevelTier.Level01: return 1;
            case LevelTier.Level02: return 2;
            case LevelTier.Level03: return 4;
            case LevelTier.Level04: return 6;
            default: return 1;
        }
    }

    private static IReadOnlyList<EnemyKind> ResolveKindPattern(LevelTier tier)
    {
        switch (tier)
        {
            case LevelTier.Level01:
                return new[] { EnemyKind.Director };
            case LevelTier.Level02:
                return new[] { EnemyKind.Deshilachador, EnemyKind.Director };
            case LevelTier.Level03:
                return new[] { EnemyKind.BromaFinal, EnemyKind.Deshilachador, EnemyKind.BromaFinal, EnemyKind.Director };
            case LevelTier.Level04:
                return new[] { EnemyKind.Director, EnemyKind.Deshilachador, EnemyKind.BromaFinal, EnemyKind.Director, EnemyKind.Deshilachador, EnemyKind.BromaFinal };
            default:
                return new[] { EnemyKind.Deshilachador };
        }
    }
}
