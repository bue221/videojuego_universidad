using LightChasePrototype;
using LightChasePrototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GameHudControllerTests
{
    private GameObject _root;
    private PrototypeLevelManager _levelManager;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("GameHudControllerTests");
        _levelManager = _root.AddComponent<PrototypeLevelManager>();
        _levelManager.ResetRun();
    }

    [TearDown]
    public void TearDown()
    {
        var hud = GameObject.Find("GameplayHUD");
        if (hud != null)
        {
            Object.DestroyImmediate(hud);
        }

        Object.DestroyImmediate(_root);
        DestroyNamedObject("GameSessionManager");
        DestroyNamedObject("GlobalUIRoot");
        Time.timeScale = 1f;
    }

    private static void DestroyNamedObject(string objectName)
    {
        var target = GameObject.Find(objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void EnsureHudExists_CreatesVisualHudTexts()
    {
        GameHudController.EnsureHudExists(_levelManager);

        var livesText = GameObject.Find("LivesText").GetComponent<Text>();
        var scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
        var timerText = GameObject.Find("TimerText").GetComponent<Text>();
        var statusText = GameObject.Find("StatusText").GetComponent<Text>();

        Assert.That(livesText.text, Does.Contain("VIDAS"));
        Assert.That(scoreText.text, Does.Contain("SCORE"));
        Assert.That(timerText.text, Does.Contain("TIEMPO"));
        Assert.That(statusText.text, Does.Contain("RECOLECCION"));
    }

    [Test]
    public void StateChanges_UpdateStatusAndPanels()
    {
        GameHudController.EnsureHudExists(_levelManager);

        _levelManager.ResetRun();
        _levelManager.RegisterStarCollected(5);

        var statusText = GameObject.Find("StatusText").GetComponent<Text>();
        var statusPanel = GameObject.Find("StatusPanel").GetComponent<Image>();

        Assert.That(statusText.text, Does.Contain("PORTAL ACTIVO"));
        Assert.That(statusPanel.color.a, Is.GreaterThan(0.8f));
    }

    [Test]
    public void EnsureHudExists_AnchorsTopPanelsFullyInsideScreen()
    {
        GameHudController.EnsureHudExists(_levelManager);

        var livesPanel = GameObject.Find("LivesPanel").GetComponent<RectTransform>();
        var scorePanel = GameObject.Find("ScorePanel").GetComponent<RectTransform>();
        var timerPanel = GameObject.Find("TimerPanel").GetComponent<RectTransform>();
        var statusPanel = GameObject.Find("StatusPanel").GetComponent<RectTransform>();

        Assert.That(livesPanel.pivot, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(scorePanel.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
        Assert.That(timerPanel.pivot, Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(statusPanel.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(livesPanel.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(scorePanel.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
        Assert.That(timerPanel.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(statusPanel.anchoredPosition.y, Is.GreaterThan(0f));
    }
}
