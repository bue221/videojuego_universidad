using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;

public class PrototypeLevelManagerTests
{
    private GameObject _root;
    private PrototypeLevelManager _levelManager;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("PrototypeLevelManagerTests");
        _levelManager = _root.AddComponent<PrototypeLevelManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void Awake_InitializesRunWithThreeLivesAndThreeMinutes()
    {
        Assert.That(_levelManager.LivesRemaining, Is.EqualTo(3));
        Assert.That(_levelManager.RemainingTime, Is.EqualTo(180f).Within(0.001f));
        Assert.That(_levelManager.Score, Is.EqualTo(0));
    }

    [Test]
    public void RegisterStarCollected_AddsStarsAndScore()
    {
        _levelManager.RegisterStarCollected(2);

        Assert.That(_levelManager.CollectedStars, Is.EqualTo(2));
        Assert.That(_levelManager.Score, Is.EqualTo(200));
    }

    [Test]
    public void ApplyPlayerHit_RemovesLivesUntilGameOver()
    {
        _levelManager.ApplyPlayerHit();
        _levelManager.ApplyPlayerHit();
        var finalHitApplied = _levelManager.ApplyPlayerHit();

        Assert.That(finalHitApplied, Is.True);
        Assert.That(_levelManager.LivesRemaining, Is.EqualTo(0));
        Assert.That(_levelManager.GameOver, Is.True);
    }

    [Test]
    public void Tick_CountsDownTimerUntilExpired()
    {
        _levelManager.Tick(180f);

        Assert.That(_levelManager.RemainingTime, Is.EqualTo(0f).Within(0.001f));
        Assert.That(_levelManager.TimerExpired, Is.True);
        Assert.That(_levelManager.GameOver, Is.True);
    }
}
