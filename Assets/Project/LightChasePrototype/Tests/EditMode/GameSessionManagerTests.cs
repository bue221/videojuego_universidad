using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;

public class GameSessionManagerTests
{
    private GameObject _sessionObject;
    private GameSessionManager _sessionManager;

    [SetUp]
    public void SetUp()
    {
        _sessionObject = new GameObject("GameSessionManager");
        _sessionManager = _sessionObject.AddComponent<GameSessionManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_sessionObject != null)
        {
            Object.DestroyImmediate(_sessionObject);
        }
    }

    [Test]
    public void ConfigureLevel_ResetsRunWithConfiguredValues()
    {
        _sessionManager.ConfigureLevel(6, 4, 150, 210f);

        Assert.That(_sessionManager.StarsRequiredToExit, Is.EqualTo(6));
        Assert.That(_sessionManager.LivesRemaining, Is.EqualTo(4));
        Assert.That(_sessionManager.RemainingTime, Is.EqualTo(210f).Within(0.001f));
        Assert.That(_sessionManager.Score, Is.EqualTo(0));
    }

    [Test]
    public void RegisterStarCollected_UnlocksExitAtThreshold()
    {
        _sessionManager.ConfigureLevel(3, 3, 100, 180f);

        _sessionManager.RegisterStarCollected(3);

        Assert.That(_sessionManager.CollectedStars, Is.EqualTo(3));
        Assert.That(_sessionManager.ExitUnlocked, Is.True);
        Assert.That(_sessionManager.Score, Is.EqualTo(300));
    }

    [Test]
    public void ResetCollectedProgressAfterLifeLoss_KeepsPressureButClearsRunProgress()
    {
        _sessionManager.ConfigureLevel(3, 3, 100, 180f);
        _sessionManager.RegisterStarCollected(2);
        _sessionManager.Tick(25f);
        _sessionManager.ApplyPlayerHit();

        _sessionManager.ResetCollectedProgressAfterLifeLoss();

        Assert.That(_sessionManager.CollectedStars, Is.EqualTo(0));
        Assert.That(_sessionManager.Score, Is.EqualTo(0));
        Assert.That(_sessionManager.LivesRemaining, Is.EqualTo(2));
        Assert.That(_sessionManager.RemainingTime, Is.EqualTo(155f).Within(0.001f));
        Assert.That(_sessionManager.ExitUnlocked, Is.False);
    }
}
