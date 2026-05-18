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
    public void BuildEnemyRoot_DoesNotApplyFlatEmissionTintOverTexture()
    {
        // La legibilidad del enemigo en zonas oscuras se resuelve con las luces
        // dedicadas (EnemyGlow y EnemyBodyGlow), no con un tinte plano de emision
        // que lavaba los colores reales de la textura. Aqui validamos justamente
        // que el material no inyecte un tinte naranja-marron permanente encima
        // del albedo.
        _enemyRoot = EnemyBuilder.BuildEnemyRoot("EnemyEmissionSubject", Vector3.zero);

        var renderer = _enemyRoot.GetComponentInChildren<Renderer>();
        var material = renderer.sharedMaterial;

        var emission = material.GetColor("_EmissionColor");
        Assert.That(emission, Is.EqualTo(Color.black),
            "Material must not tint the enemy with a flat emission color, the texture colors must read clean");
        Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.False,
            "Flat _EMISSION keyword must be disabled so the base texture is not lit on top of itself");
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
