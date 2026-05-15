using LightChasePrototype.UI;
using NUnit.Framework;
using UnityEngine;

public class MainMenuControllerTests
{
    private GameObject _root;
    private GameObject _instructionsPanel;
    private MainMenuController _controller;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("MainMenuRoot");
        _instructionsPanel = new GameObject("InstructionsPanel");
        _instructionsPanel.transform.SetParent(_root.transform);

        _controller = _root.AddComponent<MainMenuController>();
        _controller.Configure(_instructionsPanel, "LightChasePrototype");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
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
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null);

        _controller.PlayGame();

        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void QuitGame_UsesConfiguredQuitAction()
    {
        var quitCalled = false;
        _controller.ConfigureActionsForTests(null, () => quitCalled = true);

        _controller.QuitGame();

        Assert.That(quitCalled, Is.True);
    }
}
