using UnityEditor;
using UnityEngine;

public static class LightChaseRebuildAll
{
    [MenuItem("Tools/Prototype/Rebuild All Levels")]
    public static void RebuildAllLevels()
    {
        PrepareEnemyAssetsOnce();

        try
        {
            AssetDatabase.StartAssetEditing();

            Debug.Log("[LightChaseRebuildAll] Rebuilding Level 01...");
            LightChasePrototypeBuilder.BuildLevel();

            Debug.Log("[LightChaseRebuildAll] Rebuilding Level 02...");
            LightChaseNatureLevelBuilder.BuildLevel();

            Debug.Log("[LightChaseRebuildAll] Rebuilding Level 03...");
            LightChaseWaterLevelBuilder.BuildLevel();

            Debug.Log("[LightChaseRebuildAll] Rebuilding Level 04...");
            LightChaseLakeLevelBuilder.BuildLevel();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[LightChaseRebuildAll] All levels rebuilt.");
    }

    private static void PrepareEnemyAssetsOnce()
    {
        Debug.Log("[LightChaseRebuildAll] Preparing enemy FBX importers (one-time)...");
        foreach (EnemyKind kind in System.Enum.GetValues(typeof(EnemyKind)))
        {
            var assets = EnemyKindCatalog.GetAssets(kind);
            EnemyBuilder.PrepareEnemyKindAssets(assets);
        }
    }
}
