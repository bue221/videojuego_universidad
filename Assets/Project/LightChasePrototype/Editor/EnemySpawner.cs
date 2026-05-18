using System.Collections.Generic;
using LightChasePrototype;
using UnityEngine;
using UnityEngine.AI;

public static class EnemySpawner
{
    private const string EnemyGroupName = "LightHunters";
    private const string PrimaryEnemyName = "LightHunter";
    private const float NavMeshSampleRadius = 4f;
    private const float GroundEpsilon = 0.02f;

    // Order used when the requested EnemyKind cannot be instantiated (e.g. its FBX is
    // broken or marked as crashed by the asset importer). The spawner walks this list
    // until it finds a model it can actually build, so a level never ends up empty.
    private static readonly EnemyKind[] FallbackOrder =
    {
        EnemyKind.Deshilachador,
        EnemyKind.BromaFinal,
        EnemyKind.Director
    };

    public readonly struct EnemySpawn
    {
        public EnemySpawn(EnemyKind kind, Vector3 position)
        {
            Kind = kind;
            Position = position;
        }

        public EnemyKind Kind { get; }
        public Vector3 Position { get; }
    }

    public static List<EnemyLightSeeker> SpawnEnemies(IReadOnlyList<EnemySpawn> spawns)
    {
        ClearExistingEnemies();

        var results = new List<EnemyLightSeeker>();
        if (spawns == null || spawns.Count == 0)
        {
            return results;
        }

        var group = new GameObject(EnemyGroupName);
        var successCount = 0;
        for (var i = 0; i < spawns.Count; i++)
        {
            var spawn = spawns[i];
            var name = successCount == 0 ? PrimaryEnemyName : $"{PrimaryEnemyName}_{successCount + 1}";
            var enemyObject = TryBuildWithFallback(spawn.Kind, name, spawn.Position);
            if (enemyObject == null)
            {
                continue;
            }

            EnemyBuilder.AlignBaseToY(enemyObject, 0f);
            enemyObject.transform.position += Vector3.up * GroundEpsilon;
            EnemyBuilder.ConfigureNavMeshAgent(enemyObject);
            var seeker = EnemyBuilder.ConfigureEnemyLightSeeker(enemyObject);

            enemyObject.transform.SetParent(group.transform, true);
            results.Add(seeker);
            successCount++;
        }

        if (successCount == 0)
        {
            UnityEngine.Object.DestroyImmediate(group);
        }

        return results;
    }

    // Reasienta cada enemigo en el NavMesh para que sus pies queden exactamente
    // sobre la superficie navegable. Debe llamarse despues de bakear el NavMesh,
    // pues los anchors literales en los builders viven en y=0 pero los suelos
    // de cada nivel pueden vivir en y!=0 (e.g. lake en y=-0.05 o mesetas).
    public static int RebindEnemiesToNavMesh()
    {
        var seekers = Object.FindObjectsByType<EnemyLightSeeker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var fixedCount = 0;

        foreach (var seeker in seekers)
        {
            if (seeker == null)
            {
                continue;
            }

            var origin = seeker.transform.position;
            if (NavMesh.SamplePosition(origin, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                // Sin epsilon vertical: el NavMeshAgent ya queda alineado al NavMesh,
                // y baseOffset negativo en EnemyBuilder compensa el desfase de voxelizacion
                // del bake para que los pies del enemigo apoyen sobre el piso visible.
                seeker.transform.position = hit.position;
                fixedCount++;
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] Enemigo '{seeker.name}' sin NavMesh dentro de {NavMeshSampleRadius}m de {origin}. Quedara en su anchor original y puede flotar.");
            }
        }

        return fixedCount;
    }

    private static GameObject TryBuildWithFallback(EnemyKind requestedKind, string name, Vector3 position)
    {
        var attempted = new HashSet<EnemyKind> { requestedKind };
        var primary = EnemyBuilder.BuildEnemyRoot(requestedKind, name, position);
        if (primary != null)
        {
            return primary;
        }

        foreach (var fallbackKind in FallbackOrder)
        {
            if (!attempted.Add(fallbackKind))
            {
                continue;
            }

            var fallback = EnemyBuilder.BuildEnemyRoot(fallbackKind, name, position);
            if (fallback != null)
            {
                Debug.LogWarning($"[EnemySpawner] Requested enemy kind '{requestedKind}' could not be built; using fallback '{fallbackKind}' for '{name}'.");
                return fallback;
            }
        }

        Debug.LogError($"[EnemySpawner] No enemy kind could be instantiated for '{name}'. Spawn skipped.");
        return null;
    }

    public static void ClearExistingEnemies()
    {
        var existingSeekers = Object.FindObjectsByType<EnemyLightSeeker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var seeker in existingSeekers)
        {
            if (seeker == null)
            {
                continue;
            }

            Object.DestroyImmediate(seeker.gameObject);
        }

        var legacyGroup = GameObject.Find(EnemyGroupName);
        if (legacyGroup != null)
        {
            Object.DestroyImmediate(legacyGroup);
        }

        var legacySingle = GameObject.Find(PrimaryEnemyName);
        if (legacySingle != null)
        {
            Object.DestroyImmediate(legacySingle);
        }
    }
}
