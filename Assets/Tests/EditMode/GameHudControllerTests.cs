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
        Time.timeScale = 1f;
    }

    [Test]
    public void EnsureHudExists_CreatesVisualHudTexts()
    {
        GameHudController.EnsureHudExists(_levelManager);

        var livesText = GameObject.Find("LivesText").GetComponent<Text>();
        var scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
        var timerText = GameObject.Find("TimerText").GetComponent<Text>();
        var statusText = GameObject.Find("StatusText").GetComponent<Text>();
        var controlsText = GameObject.Find("ControlsText").GetComponent<Text>();

        Assert.That(livesText.text, Does.Contain("VIDAS"));
        Assert.That(scoreText.text, Does.Contain("SCORE"));
        Assert.That(timerText.text, Does.Contain("TIEMPO"));
        Assert.That(statusText.text, Does.Contain("RECOLECCION"));
        Assert.That(controlsText.text, Does.Contain("WASD"));
        Assert.That(controlsText.text, Does.Contain("Shift"));
    }

    [Test]
    public void StateChanges_UpdateStatusAndPanels()
    {
        GameHudController.EnsureHudExists(_levelManager);

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
        var timerPanel = GameObject.Find("TimerPanel").GetComponent<RectTransform>();
        var controlsPanel = GameObject.Find("ControlsPanel").GetComponent<RectTransform>();

        Assert.That(livesPanel.pivot, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(timerPanel.pivot, Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(livesPanel.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(timerPanel.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(controlsPanel.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(controlsPanel.anchoredPosition.y, Is.GreaterThan(0f));
    }
}
