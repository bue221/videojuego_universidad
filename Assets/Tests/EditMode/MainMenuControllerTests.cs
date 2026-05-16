using LightChasePrototype;
using LightChasePrototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuControllerTests
{
    private GameObject _root;
    private GameObject _instructionsPanel;
    private GameObject _menuCanvas;
    private MainMenuController _controller;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("MainMenuRoot");
        _menuCanvas = new GameObject("MenuCanvas");
        _instructionsPanel = new GameObject("InstructionsPanel");
        _instructionsPanel.transform.SetParent(_root.transform);

        _controller = _root.AddComponent<MainMenuController>();
        _controller.Configure(_instructionsPanel, "LightChasePrototype");
        _controller.GetType().GetField("menuCanvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_controller, _menuCanvas.AddComponent<CanvasGroup>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        if (_menuCanvas != null)
        {
            Object.DestroyImmediate(_menuCanvas);
        }

        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        Time.timeScale = 1f;
    }

    [Test]
    public void Configure_HidesInstructionsByDefault()
    {
        Assert.That(_controller.InstructionsVisible, Is.False);
        Assert.That(_instructionsPanel.activeSelf, Is.False);
        Assert.That(_controller.GameplaySceneName, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void ShowInstructions_MakesPanelVisible()
    {
        _controller.ShowInstructions();

        Assert.That(_controller.InstructionsVisible, Is.True);
    }

    [Test]
    public void HideInstructions_HidesPanelAfterShowingIt()
    {
        _controller.ShowInstructions();

        _controller.HideInstructions();

        Assert.That(_controller.InstructionsVisible, Is.False);
    }

    [Test]
    public void PlayGame_UsesConfiguredSceneLoader()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");

        _controller.PlayGame();

        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void SelectAvatar_UpdatesPersistentSelection()
    {
        _controller.SelectAvatar(PlayerAvatarSelection.CapsuleAvatarId);

        Assert.That(_controller.SelectedAvatarId, Is.EqualTo(PlayerAvatarSelection.CapsuleAvatarId));
        Assert.That(PlayerAvatarSelection.SelectedAvatarId, Is.EqualTo(PlayerAvatarSelection.CapsuleAvatarId));
    }

    [Test]
    public void PlayGame_InGameplayScene_HidesMenuWithoutLoadingScene()
    {
        string loadedScene = null;
        _controller.ShowMenu();
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "LightChasePrototype");

        _controller.PlayGame();

        Assert.That(loadedScene, Is.Null);
        Assert.That(_controller.MenuVisible, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void QuitGame_UsesConfiguredQuitAction()
    {
        var quitCalled = false;
        _controller.ConfigureActionsForTests(null, () => quitCalled = true);

        _controller.QuitGame();

        Assert.That(quitCalled, Is.True);
    }

    [Test]
    public void EnsureMenuExists_CreatesVisibleOverlayAndPausesGame()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();

        Assert.That(createdMenu, Is.Not.Null);
        Assert.That(createdMenu.MenuVisible, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));
    }

    [Test]
    public void EnsureMenuExists_BuildsInstructionsThatExplainControlsAndEnemy()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();
        createdMenu.ShowInstructions();

        var instructionsBody = GameObject.Find("InstructionsBody").GetComponent<Text>();

        Assert.That(instructionsBody.text, Does.Contain("OBJETIVO"));
        Assert.That(instructionsBody.text, Does.Contain("WASD"));
        Assert.That(instructionsBody.text, Does.Contain("Shift izquierdo"));
        Assert.That(instructionsBody.text, Does.Contain("ENEMIGO"));
    }
}
