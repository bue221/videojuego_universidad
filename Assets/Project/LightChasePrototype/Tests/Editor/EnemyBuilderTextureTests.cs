using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EnemyBuilderTextureTests
{
    private GameObject _enemyRoot;

    [SetUp]
    public void SetUp()
    {
        EnemyBuilder.PrepareEnemyKindAssets(EnemyKindCatalog.GetAssets(EnemyBuilder.DefaultEnemyKind));
    }

    [TearDown]
    public void TearDown()
    {
        if (_enemyRoot != null)
        {
            Object.DestroyImmediate(_enemyRoot);
        }
    }

    [Test]
    public void BuildEnemyRoot_AssignsBaseMapAndNormalAndMetallic()
    {
        var assets = EnemyKindCatalog.GetAssets(EnemyBuilder.DefaultEnemyKind);
        _enemyRoot = EnemyBuilder.BuildEnemyRoot("EnemyTextureSubject", Vector3.zero);

        Assert.That(_enemyRoot, Is.Not.Null);

        var renderer = _enemyRoot.GetComponentInChildren<Renderer>();
        Assert.That(renderer, Is.Not.Null);

        var material = renderer.sharedMaterial;
        Assert.That(material, Is.Not.Null, "Renderer must have a material assigned");

        var expectedAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.AlbedoPath);
        var expectedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.NormalPath);
        var expectedMetallic = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.MetallicPath);

        Assert.That(material.GetTexture("_BaseMap"), Is.EqualTo(expectedAlbedo));
        Assert.That(material.GetTexture("_BumpMap"), Is.EqualTo(expectedNormal));
        Assert.That(material.GetTexture("_MetallicGlossMap"), Is.EqualTo(expectedMetallic));
    }

    [Test]
    public void BuildEnemyRoot_AssignsRoughnessToSpecGlossMap()
    {
        var assets = EnemyKindCatalog.GetAssets(EnemyBuilder.DefaultEnemyKind);
        _enemyRoot = EnemyBuilder.BuildEnemyRoot("EnemyRoughnessSubject", Vector3.zero);

        var renderer = _enemyRoot.GetComponentInChildren<Renderer>();
        var material = renderer.sharedMaterial;

        var expectedRoughness = AssetDatabase.LoadAssetAtPath<Texture2D>(assets.RoughnessPath);
        if (expectedRoughness == null)
        {
            Assert.Pass($"Roughness asset not present at {assets.RoughnessPath}; skipping.");
            return;
        }

        Assert.That(material.GetTexture("_SpecGlossMap"), Is.EqualTo(expectedRoughness),
            "Roughness must be wired into the material so each enemy preserves its PBR signature");
    }

    [Test]
    public void BuildEnemyRoot_EnablesEmissionKeyword()
    {
        _enemyRoot = EnemyBuilder.BuildEnemyRoot("EnemyEmissionSubject", Vector3.zero);

        var renderer = _enemyRoot.GetComponentInChildren<Renderer>();
        var material = renderer.sharedMaterial;

        Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True,
            "Emission must be enabled so the enemy stays readable in dark areas of every level");

        var emission = material.GetColor("_EmissionColor");
        Assert.That(emission, Is.Not.EqualTo(Color.black),
            "Emission color must be tinted so the enemy never goes pitch black");
    }

    [Test]
    public void BuildEnemyRoot_AppliesNormalMapKeywordWhenNormalPresent()
    {
        _enemyRoot = EnemyBuilder.BuildEnemyRoot("EnemyNormalKeywordSubject", Vector3.zero);

        var renderer = _enemyRoot.GetComponentInChildren<Renderer>();
        var material = renderer.sharedMaterial;

        Assert.That(material.IsKeywordEnabled("_NORMALMAP"), Is.True,
            "Normal map keyword must be enabled when normal texture is wired");
    }
}
