using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;

public class PrototypeLevelManagerTests
{
    private GameObject _root;
    private PrototypeLevelManager _levelManager;
    private GameObject _player;
    private PlayerLightState _playerLightState;

    [SetUp]
    public void SetUp()
    {
        DestroyAllPlayers();
        DestroyNamedObject("GameSessionManager");
        DestroyNamedObject("GlobalUIRoot");

        _player = new GameObject("Player");
        _player.transform.position = new Vector3(2f, 0.15f, -3f);
        _playerLightState = _player.AddComponent<PlayerLightState>();
        _player.AddComponent<CharacterController>();

        _root = new GameObject("PrototypeLevelManagerTests");
        _levelManager = _root.AddComponent<PrototypeLevelManager>();
        _ = _levelManager.LivesRemaining;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_player);
        Object.DestroyImmediate(_root);
        DestroyAllPlayers();
        DestroyNamedObject("GameSessionManager");
        DestroyNamedObject("GlobalUIRoot");
    }

    private static void DestroyNamedObject(string objectName)
    {
        var target = GameObject.Find(objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void DestroyAllPlayers()
    {
        foreach (var playerState in Object.FindObjectsByType<PlayerLightState>(FindObjectsSortMode.None))
        {
            if (playerState != null)
            {
                Object.DestroyImmediate(playerState.gameObject);
            }
        }
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
    public void ApplyPlayerHit_RespawnsPlayerAtStartingPositionWhileLivesRemain()
    {
        _player.transform.position = new Vector3(11f, 0.15f, 9f);

        var hitApplied = _levelManager.ApplyPlayerHit();

        Assert.That(hitApplied, Is.True);
        Assert.That(_levelManager.LivesRemaining, Is.EqualTo(2));
        Assert.That(_player.transform.position, Is.EqualTo(new Vector3(2f, 0.15f, -3f)));
    }

    [Test]
    public void ApplyPlayerHit_ClearsCollectedProgressAndRestoresPickupsWhileLivesRemain()
    {
        var pickupObject = new GameObject("StarPickup");
        pickupObject.transform.position = new Vector3(4f, 1f, 2f);
        pickupObject.AddComponent<SphereCollider>();
        pickupObject.AddComponent<MeshRenderer>();
        var pickup = pickupObject.AddComponent<StarPickup>();

        pickup.Collect(_playerLightState, _levelManager);
        _levelManager.Tick(12f);

        var hitApplied = _levelManager.ApplyPlayerHit();

        Assert.That(hitApplied, Is.True);
        Assert.That(_levelManager.LivesRemaining, Is.EqualTo(2));
        Assert.That(_levelManager.CollectedStars, Is.EqualTo(0));
        Assert.That(_levelManager.Score, Is.EqualTo(0));
        Assert.That(_levelManager.ExitUnlocked, Is.False);
        Assert.That(_levelManager.RemainingTime, Is.EqualTo(168f).Within(0.001f));
        Assert.That(_playerLightState.StarsCollected, Is.EqualTo(0));
        Assert.That(pickup.IsCollected, Is.False);

        Object.DestroyImmediate(pickupObject);
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
