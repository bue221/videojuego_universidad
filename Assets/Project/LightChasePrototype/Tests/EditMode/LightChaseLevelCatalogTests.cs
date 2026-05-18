using LightChasePrototype;
using NUnit.Framework;

public class LightChaseLevelCatalogTests
{
    [SetUp]
    public void SetUp()
    {
        LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.PrototypeLevelId);
    }

    [Test]
    public void SelectedLevel_DefaultsToPrototypeLevel()
    {
        Assert.That(LightChaseLevelCatalog.SelectedLevelId, Is.EqualTo(LightChaseLevelCatalog.PrototypeLevelId));
        Assert.That(LightChaseLevelCatalog.SelectedLevel.SceneName, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void SelectLevel_SwitchesToNatureLevel()
    {
        LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.NatureLevelId);

        Assert.That(LightChaseLevelCatalog.SelectedLevelId, Is.EqualTo(LightChaseLevelCatalog.NatureLevelId));
        Assert.That(LightChaseLevelCatalog.SelectedLevel.SceneName, Is.EqualTo("LightChasePrototype_Level02"));
    }

    [Test]
    public void GetLevelBySceneName_ResolvesKnownLevel()
    {
        var level = LightChaseLevelCatalog.GetLevelBySceneName("LightChasePrototype_Level02");

        Assert.That(level.Id, Is.EqualTo(LightChaseLevelCatalog.NatureLevelId));
        Assert.That(level.DisplayName, Is.EqualTo("Nivel 2"));
    }

    [Test]
    public void GetScenePath_ReturnsProjectScenePathForNatureLevel()
    {
        var scenePath = LightChaseLevelCatalog.GetScenePath("LightChasePrototype_Level02");

        Assert.That(scenePath, Is.EqualTo("Assets/Project/LightChasePrototype/Scenes/LightChasePrototype_Level02.unity"));
    }

    [Test]
    public void TryGetNextLevelSceneName_ReturnsNatureLevelAfterPrototype()
    {
        var foundNext = LightChaseLevelCatalog.TryGetNextLevelSceneName("LightChasePrototype", out var nextSceneName);

        Assert.That(foundNext, Is.True);
        Assert.That(nextSceneName, Is.EqualTo("LightChasePrototype_Level02"));
    }

    [Test]
    public void TryGetNextLevelSceneName_ReturnsFalseAfterLastLevel()
    {
        var foundNext = LightChaseLevelCatalog.TryGetNextLevelSceneName("LightChasePrototype_Level03", out var nextSceneName);

        Assert.That(foundNext, Is.False);
        Assert.That(nextSceneName, Is.Null);
    }

    [Test]
    public void SelectLevel_SwitchesToWaterLevel()
    {
        LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.WaterLevelId);

        Assert.That(LightChaseLevelCatalog.SelectedLevelId, Is.EqualTo(LightChaseLevelCatalog.WaterLevelId));
        Assert.That(LightChaseLevelCatalog.SelectedLevel.SceneName, Is.EqualTo("LightChasePrototype_Level03"));
    }

    [Test]
    public void GetLevelBySceneName_ResolvesWaterLevel()
    {
        var level = LightChaseLevelCatalog.GetLevelBySceneName("LightChasePrototype_Level03");

        Assert.That(level.Id, Is.EqualTo(LightChaseLevelCatalog.WaterLevelId));
        Assert.That(level.DisplayName, Is.EqualTo("Nivel 3"));
    }
}
